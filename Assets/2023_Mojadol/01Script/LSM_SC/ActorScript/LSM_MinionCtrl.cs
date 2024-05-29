using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

using Photon.Pun;
using Unity.VisualScripting;

/* 2023_03_20_HSH_수정사항 : 미니언이 소속된 팀에 따라 Scene에서도 Color가 변경되도록 함(CHangeTeamColor).
 * ㄴ 미니언 피격 시 분홍색으로 하이라이트 + 넉백 추가(DamagedEffect)
 *
 */

// 미니언 스크립트.
// 후에 근접, 원거리 등의 미니언들은 해당 스크립트를 상속받고 할 생각임.
public class LSM_MinionCtrl : MonoBehaviourPunCallbacks, I_Actor, IPunObservable, I_Characters
{
	public MoonHeader.S_MinionStats stats;          // 미니언의 상태에 대한 구조체.
	public LSM_Spawner mySpawner;                   // 미니언의 마스터 스포너.
	private bool PlayerSelect, once_changeRound;    // PlayerSelect: 플레이어가 해당 미니언에 강령하였는지, once_changeRound: 라운드 변경 시 한번만 실행되도록.
	[SerializeField] private int way_index;         // 현재 미니언에 저장된 경로 중 몇번째를 목표로 삼는지.

	// 타겟 찾기, 공격 딜래이 등을 상수화
	const float MAXIMUMVELOCITY = 3f, SEARCHTARGET_DELAY = 1f, ATTACK_DELAY = 2f;

	// 아래 필요한 컴포넌트를 변수화
	private Rigidbody rigid;
	[SerializeField] private Animator anim;
	private NavMeshAgent nav;
	private NavMeshObstacle nav_ob;
	private IEnumerator navenable_IE;   // StopCorutine을 사용하기 위한 변수
	private bool is_attackFinish_Act;

	private Renderer[] bodies;  // 색상을 변경할 렌더러.

	public GameObject CameraPosition;       // 카메라를 초기화할 때 사용할 변수.
	private GameObject icon, playerIcon;    // 미니언에 존재하는 아이콘, 플레이어 아이콘
    public MeshRenderer icon_ren;
	List<Material> icon_materialL;
	bool selected_e;

	[SerializeField] protected GameObject target_attack;    // 미니언의 타겟
	protected I_Actor target_actor;
	[SerializeField]
	protected float searchRadius, minAtkRadius, maxAtkRadius;   // 미니언의 탐색 범위, 최소 공격 가능 거리, 최대 공격 가능 거리
	private float timer_Searching, timer_Attack;                // 탐색과 공격의 타이머 변수.

	public int minionBelong;    //Spawner.cs에서 자기가 몇 번 공격로 소속인지 받아옴
	public int minionType;  //0이면 원거리, 1이면 근거리 미니언

	public bool debugging_minion; // 디버깅 확인용...
	private Vector3 networkPosition, networkVelocity;

	public GameObject[] Sounds;//0: walk, 1: hit, 2: death, 3: attack
	private bool isMove;


    // 기즈모
    
    private void OnDrawGizmosSelected()
    {
		/*
		if (!PhotonNetwork.IsMasterClient)
		{
			Gizmos.color = Color.green;
			Gizmos.DrawRay(this.transform.position, networkPosition - this.transform.position);
			Gizmos.DrawCube(networkPosition, Vector3.one * 2);
		}
		*/
		//if (nav.enabled)
			//Gizmos.DrawCube(nav.destination + (Vector3.up * 12), Vector3.one * 1);
    }

    #region IPUnalsdfjaow
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.IsWriting)
		{
			stream.SendNext(this.gameObject.activeSelf);

			// maxHealth 2byte, health 2byte, team 8bit, atk 8bit, state 8bit
			ulong send_dummy = stats.SendDummyMaker();
            //stream.SendNext(send_dummy);

            int dummy_int1 = (int)(send_dummy & (ulong)uint.MaxValue);
			int dummy_int2 = (int)((send_dummy >> 32) & (ulong)uint.MaxValue);
            stream.SendNext(dummy_int1);
            stream.SendNext(dummy_int2);

            stream.SendNext(nav.enabled ? nav.velocity : Vector3.zero );

			isMove = nav.enabled ? !nav.isStopped : false;
            stream.SendNext(isMove);
		}
		else
		{
			bool isActive_ = (bool)stream.ReceiveNext();
			this.gameObject.SetActive(isActive_);

			ulong receive_dummy = ((ulong)(int)stream.ReceiveNext() & (ulong)uint.MaxValue);
			receive_dummy += (((ulong)(int)stream.ReceiveNext() & (ulong)uint.MaxValue) << 32);
            networkVelocity = (Vector3)stream.ReceiveNext();

            this.stats.ReceiveDummy(receive_dummy);

			rigid.velocity = networkVelocity;

			//float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.timestamp)) * 20;
			networkPosition = this.transform.position + networkVelocity * 5;
			isMove = (bool)stream.ReceiveNext();
		}
	}

	#endregion

	// 다시 활성화할 경우 Obstacle 비활성화, Agent 활성화
	void OnEnable()
	{
		nav_ob.enabled = false;
		nav.enabled = false;
	}

	private void Awake()
	{
		PlayerSelect = false;
		// 컴포넌트 받아오기.
		rigid = this.GetComponent<Rigidbody>();
		nav = this.GetComponent<NavMeshAgent>();
		nav_ob = this.GetComponent<NavMeshObstacle>();
		anim = this.GetComponent<Animator>();
		// 초기화
		timer_Searching = 0;
		timer_Attack = 0;
		target_attack = null;
		nav.stoppingDistance = 0;
		stats = new MoonHeader.S_MinionStats();
		bodies = this.transform.GetComponentsInChildren<Renderer>();
		navenable_IE = NavEnable(true);

		// 아이콘 생성 및 변수 저장.
		icon = GameObject.Instantiate(PrefabManager.Instance.icons[0], transform);
		icon.transform.localPosition = new Vector3(0, 60, 0);
		playerIcon = GameObject.Instantiate(PrefabManager.Instance.icons[4], transform);
		playerIcon.transform.localPosition = new Vector3(0, 60, 0);
		playerIcon.SetActive(false);
		icon_materialL = new List<Material>();
        selected_e = false;

        // 디버그용 미리 설정. 현재 Melee
        //searchRadius = 10f;
        //minAtkRadius = 9f;
        //maxAtkRadius = 13f;

    }

	private void LateUpdate()
	{
		if (isMove) { PlaySFX(0); }
		else { StopSFX(0); }

		if (!PhotonNetwork.IsMasterClient) return;

		// 현재 게임의 진행 상태가 어떻게 되는지 확인 후, 상태를 변경.
		if (once_changeRound && GameManager.Instance.gameState != MoonHeader.GameState.Gaming)
		{
			// 공격하고있지 않으며, Agent가 활성화되어있을 때
			if (!nav.isStopped && stats.state != MoonHeader.State.Attack)
			{ nav.velocity = Vector3.zero; nav.isStopped = true; }
			rigid.velocity = Vector3.zero;
			rigid.angularVelocity = Vector3.zero;
			once_changeRound = false;
		}
		else if (!once_changeRound && GameManager.Instance.gameState == MoonHeader.GameState.Gaming)
		{
			if (nav.enabled)
				nav.isStopped = false;
			once_changeRound = true;
		}

		// 살아있으며, 스포너 초기화가 되어있고, 현재 게임중인 상태라면 아래 함수들을 실행.
		if (stats.state != MoonHeader.State.Dead && !ReferenceEquals(mySpawner, null) && once_changeRound)
		{
			SearchingTarget();
			Attack();
			//MyDestination();
			AnimationSetting();
		}



	}

    private void Update()
    {
        if (!photonView.IsMine)
        {
			rigid.MovePosition(rigid.position + networkVelocity * Time.deltaTime);
        }
    }

    // 미니언의 기본 스탯과 목적지를 정하는 함수. Spawner.cs에서 사용
    // 모든 변수 초기화
    public void MonSetting(LSM_SpawnPointSc point, MoonHeader.Team t, LSM_Spawner spawn, MoonHeader.AttackType typeM)
	{
        //nav_ob.enabled = false;
        nav.enabled = true;
        nav.isStopped = false;
        anim.SetBool("Dead", false);

        PlayerSelect = false;
		timer_Searching = 0;
		timer_Attack = 0;
		target_attack = null;
		way_index = 0;
		is_attackFinish_Act = false;
		// maxhealth, speed, atk, paths, team
		// 현재 개발중이므로 미리 설정해둠.
		object[] ob_d = LSM_SettingStatus.Instance.lvStatus[(int)MoonHeader.ActorType.Minion_Melee + (int)typeM-1]
			.getStatus_LV(Mathf.FloorToInt(GameManager.Instance.timer_inGameTurn / 3 * 60));
        // mh: 최대 체력, sp: 스피드, atk: 공격력, des: 스포너로부터 받아올 이동경로, t: 미니언의 팀, type_d: 미니언의 타입,
        // e: 죽었을때, 혹은 게임이 종료되었을 때 얻는 경험치
        stats.Setting((short)ob_d[0], 4f, (short)ob_d[1], point.Ways, t, typeM,
			(short)(typeM == MoonHeader.AttackType.Melee ? 60:30), (short)(typeM == MoonHeader.AttackType.Melee ? 40: 30)) ;

		photonView.RPC("MS_RPC", RpcTarget.All, (short)stats.actorHealth.maxHealth, (short)stats.actorHealth.Atk, (short)t, (int)stats.speed, typeM);
		nav.speed = stats.speed;
		//stats = new MoonHeader.MinionStats(10, 50f, 10, way, t);

		// 스폰포인트에 저장된 공격로로 목적지 지정.
		transform.LookAt(stats.destination[way_index].transform);
		MyDestination();
		//nav.avoidancePriority = 50;
		nav.isStopped = false;

		// 마스터 스포너 지정. 아이콘 활성화.
		mySpawner = spawn;
		icon.SetActive(true);
		playerIcon.SetActive(false);
		nav.velocity = Vector3.zero;
		rigid.angularVelocity = Vector3.zero;
		rigid.velocity = Vector3.zero;

		// 아이콘 및 몸통 색 팀의 색상에 맞게 변화
		ChangeTeamColor();
	}

	// 체력, 공격력, 팀
	[PunRPC]private void MS_RPC(short mh, short atk, short t, int s, MoonHeader.AttackType typeM)
    {
		this.transform.gameObject.layer = 7;
		this.stats.actorHealth.Atk = atk;
		this.stats.actorHealth.maxHealth = mh;
		this.stats.actorHealth.health = mh;
		this.stats.speed = s;
		this.stats.actorHealth.team = (MoonHeader.Team)t;
		this.stats.actorHealth.type = typeM;
		this.transform.name = this.stats.actorHealth.team.ToString() + "Minion";
		PlayerDisConnect();
		ChangeTeamColor();
		//if (selected_e)
			//Unselected();
    }


	// 웨이포인트 트리거에 닿았다면 발동하는 함수.
	// 해당 웨이포인트와 미니언의 현재 목적지가 같은지 확인하는 함수 구현.
	private void OnTriggerEnter(Collider other)
	{
		if (!PhotonNetwork.IsMasterClient) return;

		if (other.CompareTag("WayPoint") && stats.state != MoonHeader.State.Dead)
		{
			CheckingTurretTeam(other.transform.gameObject);
		}
	}


	// 미니언이 다음 길로 넘어가는 것을 구현한 함수.
	public void MyDestination()
	{
		if (ReferenceEquals(target_attack, null) && nav.enabled)
		{
			nav.destination = CheckingPosition_NavMesh(stats.destination[way_index].transform.position);
		}
	}

	private Vector3 CheckingPosition_NavMesh(Vector3 pos)
	{
		Vector3 pos_dummy = new Vector3(pos.x, this.transform.position.y, pos.z);
        // 목적지에 NavMesh가 존재하지 않을 경우, 문제점이 존재할 수 있음.
        Vector3 destination_direction = (this.transform.position - pos_dummy).normalized;
        float dist_dummy = 0;
        float destination_distance = Vector3.Distance(pos_dummy, this.transform.position);
		Vector3 result = pos_dummy;

		// 그러므로 해당 지점에서 미니언을 향하는 방향을 저장.
		// NavMesh의 SamplePosition을 사용하여 조금씩 미니언 방향으로 NavMesh가 존재하는지 확인.
		// 이후 해당 Hit Position을 목표로 지정.
        while (true)
        {
            dist_dummy += 0.25f;
            NavMeshHit hit;
            Vector3 dummy_position = pos_dummy + (destination_direction * dist_dummy);
            if (NavMesh.SamplePosition(dummy_position, out hit, 1f, NavMesh.AllAreas))
            { result = hit.position; break; }
            if (dist_dummy >= destination_distance * 0.8f)
            {Debug.Log("Fail To NavMesh"); break; }
        }


        return result;
	}

	// 터렛에 대하여. 받아온 터렛이 현재 자신이 목표로한 터렛이 맞는지 확인.
	// 만약 동일하다면 way_index를 상승시켜 다음 지점으로 이동.
	private void CheckingTurretTeam(GameObject obj)
	{
        LSM_TurretSc dummySc = obj.transform.GetComponentInChildren<LSM_TurretSc>();
		if (dummySc.stats.actorHealth.health <= 0 || dummySc.stats.actorHealth.team == this.stats.actorHealth.team)
		{
			if (stats.destination[way_index].Equals(dummySc.waypoint))
			{ way_index++; MyDestination(); }
		}
	}

	// 미니언이 주변을 공격 대상을 탐색하는 함수.
	private void SearchingTarget()
	{
		// 현재 미니언의 타겟이 존재하는지, 플레이어가 강림 중인지, 미니언이 공격,죽음 등의 상태가 아닌지 확인.
		if (ReferenceEquals(target_attack, null) && !PlayerSelect && stats.state == MoonHeader.State.Normal)
		{
			timer_Searching += Time.deltaTime;
			if (timer_Searching >= SEARCHTARGET_DELAY)
			{
				timer_Searching = 0;

				// 스피어캐스트를 사용하여 일정 반지름 내에 적이 있는지 확인.
				RaycastHit[] hits;
				hits = Physics.SphereCastAll(transform.position, searchRadius, Vector3.up, 0);
				float dummyDistance = float.MaxValue;
				// 범위 내에 존재하는 모든 오브젝트를 확인 후 공격이 가능한지 여부 판단.
				foreach (RaycastHit hit in hits)
				{
					float hit_dummy_distance = Vector3.Distance(transform.position, hit.transform.position);
					if (dummyDistance > hit_dummy_distance)
					{
						bool different_Team = false;

						if (hit.transform.CompareTag("Minion"))
						{
							I_Actor dummy_actor = hit.transform.GetComponent<I_Actor>();
							LSM_MinionCtrl dummy_ctr = hit.transform.GetComponent<LSM_MinionCtrl>();
							different_Team = (stats.actorHealth.team != dummy_actor.GetTeam() && dummy_actor.GetHealth() > 0 && dummy_ctr.stats.state != MoonHeader.State.Dead && dummy_ctr.stats.state != MoonHeader.State.Invincibility);
						}  

						else if (hit.transform.CompareTag("Turret"))
						{
							CheckingTurretTeam(hit.transform.GetComponent<LSM_TurretSc>().waypoint);
							different_Team = (stats.actorHealth.team != hit.transform.GetComponent<LSM_TurretSc>().stats.actorHealth.team);
								//&& hit.transform.GetComponent<LSM_TurretSc>().TurretBelong == minionBelong;
						}   //자신과 같은 공격로의 터렛만 대상으로 지정
						else if (hit.transform.CompareTag("PlayerMinion"))
						{
							I_Actor dummy_actor = hit.transform.GetComponent<I_Actor>();
							different_Team = (stats.actorHealth.team != dummy_actor.GetTeam() && hit.transform.GetComponent<I_Characters>().GetState() != (int)MoonHeader.State_P_Minion.Dead);
						}
						else if (hit.transform.CompareTag("Nexus"))
						{
							different_Team = (stats.actorHealth.team != hit.transform.GetComponent<LSM_NexusSC>().stats.actorHealth.team);
						}

						// 팀이 같지 않으며, 가까운 경우 타겟으로 적용.
						if (different_Team)
						{
							dummyDistance = hit_dummy_distance;
							target_attack = hit.transform.gameObject;
						}
					}
				}
				if (!ReferenceEquals(target_attack, null)) { target_actor = target_attack.GetComponent<I_Actor>(); }

				// 타겟을 찾았으며, 이동중이라면.. 자신의 목표를 타겟으로 지정. 해당 지점으로 이동
				if (!nav.isStopped && !ReferenceEquals(target_attack, null))
					nav.destination = CheckingPosition_NavMesh(target_attack.transform.position);
				// 
				//else if (ReferenceEquals(target_attack,null) && nav.enabled && nav.isStopped) { nav.isStopped = false; }
				// 타겟을 찾지 못하였으며, NavMeshAgent가 비활성화되어있을경우, Obstacle 비활성화, Agent 활성화
				else if (ReferenceEquals(target_attack, null) && !nav.enabled) { StopCoroutine(navenable_IE); navenable_IE = NavEnable(true); StartCoroutine(navenable_IE); }
			}
		}

		// 타겟을 찾았으며, 플레이어가 강령하지 않았고, 현재 Agent가 활성화 되어있을 경우
		else if (!ReferenceEquals(target_attack, null) && !PlayerSelect && !nav.isStopped)
		{
			// Agent의 목적지를 타겟의 위치로 설정.
			nav.destination = CheckingPosition_NavMesh(target_attack.transform.position);


            // 레이캐스트를 쏴서 몸체에 가까이 있는지 확인.
            RaycastHit[] hits = Physics.RaycastAll(this.transform.position, (target_attack.transform.position - this.transform.position).normalized, maxAtkRadius);
			//Debug.DrawRay(this.transform.position, (target_attack.transform.position - this.transform.position).normalized * maxAtkRadius, Color.red);
			float dist = Vector3.Distance(target_attack.transform.position, this.transform.position);
			foreach (RaycastHit hit in hits)
			{
				if ((hit.transform.gameObject.Equals(target_attack)))
				{
					/*
					if (debugging_minion)
					{
						Debug.Log("my position = " + this.transform.position + " \n target point position  = " + hit.point + " \n hit.distance = " + hit.distance + 
						"\n distance < minatk = " +(dist <= minAtkRadius).ToString() +"\n hit.distance < minatk = " + (hit.distance < minAtkRadius)); }*/
					dist = hit.distance;
					break;
				}
			}

			// 타겟이 MaxDistance이상 떨어져있다면 타겟을 놓아줌. null지정.
			if (dist > maxAtkRadius && stats.state != MoonHeader.State.Thinking && stats.state != MoonHeader.State.Dead)
			{
				//Debug.Log("target setting null. : distance : " + Vector3.Distance(target_attack.transform.position, this.transform.position) + "target : " +target_attack.name);
				//Debug.Log("AttackFinish in Far away");
				StartCoroutine(AttackFin());
			}

			// 만약 타겟과의 거리가 최소 공격 가능 거리보다 적다면 공격 함수 호출.
			else if (dist <= minAtkRadius)
			{
				stats.state = MoonHeader.State.Attack;
			}

		}

	}

	// 미니언이 살아있으며, 타겟을 찾았다면 해당 함수를 실행.
	// 원래 공격할때 NavAgent를 비활성화, NavObstacle을 활성화 하였으나, 이를 실행하면 NavMesh를 통한 길찾기를 실시간으로 다시 반복하는 문제가있음.
	// 그러므로 NavAgent의 Priority를 하강시키는 것으로 해당 미니언을 밀치지 않게 설정.
	// 또 다시 변경... NavObstacle을 사용. 

	private void Attack()
	{
		if (timer_Attack <= ATTACK_DELAY) { timer_Attack += Time.deltaTime; }

		// 타겟이 존재한다면.
		if (!ReferenceEquals(target_attack, null))
		{
			// 타겟이 파괴되었다면. -> 현재 ObjectPooling을 사용하고있으므로, ActiveSelf를 사용하여 현재 활성/비활성 상태를 확인.
			if (!target_attack.activeSelf && this.stats.state != MoonHeader.State.Thinking
				|| this.stats.state == MoonHeader.State.Dead ||
				(!ReferenceEquals(target_attack.GetComponent<I_Characters>(), null) && target_attack.GetComponent<I_Characters>().GetState() == 1))
			{
				//Debug.Log("Attack Finish in Destroy"); 
				StartCoroutine(AttackFin());
			}

			else if (target_actor.GetTeam() == this.stats.actorHealth.team && target_attack.CompareTag("Turret"))
			{ CheckingTurretTeam(target_attack.GetComponent<LSM_TurretSc>().waypoint); StartCoroutine(AttackFin()); }
			else if (target_attack.CompareTag("Turret") && target_actor.GetHealth() <= 0)
			{ StartCoroutine(AttackFin()); }

			else if (stats.state == MoonHeader.State.Attack && !PlayerSelect)
			{
				// 언제 다시 NavMeshObstacle을 사용 안할지 모르기에 해당 부분을 주석처리로 남겨두었음.
				//bool dummy_cant_attack = Vector3.Distance(target_attack.transform.position, this.transform.position) > minAtkRadius * (nav.isStopped ? 1f : 0.7f);
				//if (!dummy_cant_attack) { nav.isStopped = true; nav.avoidancePriority = 10; }
				//else { nav.isStopped = false; nav.avoidancePriority = 50; }

				// 타겟과의 거리가 minAtkRadius보다 크다면 공격이 불가능.
				//-> Agent가 켜져있는 경우(타겟을 공격하기 위하여 움직이고 있는 경우)에는 오차 및 약간의 움직임에 대비하여 좀 더 가까이 다가가게 구현.

				// 레이캐스트를 쏴서 몸체에 가까이 있는지 확인.
				RaycastHit[] hits = Physics.RaycastAll(this.transform.position, (target_attack.transform.position - this.transform.position).normalized, maxAtkRadius);
				float dist = Vector3.Distance(target_attack.transform.position, this.transform.position);
				foreach (RaycastHit hit in hits)
				{
					if (hit.transform.gameObject.Equals(target_attack))
					{
						dist = Vector3.Distance(hit.point, this.transform.position);
						break;
					}
				}

				bool dummy_cant_attack = dist > minAtkRadius * (nav.enabled ? 0.7f : 1f);

				// 공격이 불가능한 경우, 가능한 경우에 따라 Obstacl, Agent를 활성/비활성.
				// Agent 및 Obstacle을 동시에 사용한다면 오류 발생 -> 자신 또한 장애물이라 생각하며 자신이 있는 길을 피하려는 모순
				// 그렇기에 Obstacle과 Agent를 서로 키고 끄고를 하는 것임. 허나 비활성화한다고 바로 비활성화되지는 않은듯함.
				// 약간의 텀을 주지 않는다면 서로 충돌하여 팅겨나가는 경우가 존재함.

				if (dummy_cant_attack && nav.isStopped)
				{
					navenable_IE = NavEnable(true); StartCoroutine(navenable_IE);
				}
				else if (!dummy_cant_attack && !nav.isStopped)
				{
					navenable_IE = NavEnable(false); StartCoroutine(navenable_IE); rigid.velocity = Vector3.zero;
				} //여기 오류. 아마도 공격이 끝나고 계속 불러오는듯.

				// 만약 공격이 가능하다면 공격하는 구문
				if (!dummy_cant_attack)
				{
					// y축 rotation만을 변경할 것임.
					Quaternion target_rotation = Quaternion.LookRotation((target_attack.transform.position - this.transform.position).normalized);

					Vector3 target_euler = Quaternion.RotateTowards(this.transform.rotation, target_rotation, 200 * Time.deltaTime).eulerAngles;
					this.transform.rotation = Quaternion.Euler(0, target_euler.y, 0);

					if (timer_Attack >= ATTACK_DELAY && Mathf.Abs(this.transform.rotation.eulerAngles.y - target_rotation.eulerAngles.y) < 10f)
					{
						timer_Attack = 0;
						// 공격 애니메이션 실행. 지금은 즉발. 하지만 발사체를 사용할거면 이때 소환.
						photonView.RPC("AAnim_RPC", RpcTarget.All);
						StartCoroutine(Attack_Anim());

					}

				}
			}
		}

	}

	[PunRPC]protected void AAnim_RPC() {PlaySFX(3); anim.SetTrigger("Attack"); }

	private IEnumerator Attack_Anim()
	{
		nav.speed = 0;
		yield return new WaitForSeconds(0.5f);
		if (!ReferenceEquals(target_attack, null))
		{
			switch (target_attack.tag)
			{
				case "Minion":
					Attack_other<LSM_MinionCtrl>(target_attack);

					break;
				case "Turret":
					Attack_other<LSM_TurretSc>(target_attack);
					LSM_TurretSc dummy_Sc = target_attack.GetComponent<LSM_TurretSc>();
					if (dummy_Sc.stats.actorHealth.team == stats.actorHealth.team && stats.state == MoonHeader.State.Attack && stats.state != MoonHeader.State.Dead)
					{ CheckingTurretTeam(dummy_Sc.waypoint); StartCoroutine(AttackFin()); //Debug.Log("Attack Finish in Turret destroy");
					}

					break;
				case "PlayerMinion":
					target_attack.GetComponent<I_Actor>().Damaged(this.stats.actorHealth.Atk, this.transform.position, this.stats.actorHealth.team, this.gameObject);
					//Attack_other<PSH_PlayerFPSCtrl>(target_attack);
					break;
			}
		}
		yield return new WaitForSeconds(1f);
		nav.speed = stats.speed;
	}

	// Generic 변수를 사용하여 해당 구문을 함수화. IActor 인터페이스는 현재 player, turret, minion가 구현하고있음.
	// 따라서 Damaged를 호출이 가능함.
	private void Attack_other<T>(GameObject other) where T : I_Actor {
		T Script = other.GetComponent<T>();
		Script.Damaged(this.stats.actorHealth.Atk, this.transform.position, this.stats.actorHealth.team, this.gameObject);
	}


	// 미니언이 데미지를 받을 때 사용하는 함수.
	// dam = 미니언 혹은 포탑의 공격력. 미니언이 받는 데미지.
	// origin = 공격을 하는 주체의 위치. 이를 이용하여 더욱 자연스러운 넉백이 가능해짐. // 현재 넉백을 제외하고있음.

	public void Damaged(short dam, Vector3 origin, MoonHeader.Team t, GameObject other)
	{
		// 죽음 혹은 무적 상태일 경우 데미지를 입지않음. 바로 return
		if (stats.state == MoonHeader.State.Invincibility || stats.state == MoonHeader.State.Dead || !PhotonNetwork.IsMasterClient)
			return;
		else if (t == this.stats.actorHealth.team)
			return;

		stats.actorHealth.health -= dam;

		if (stats.actorHealth.health > 0)
			photonView.RPC("DamMinion_RPC", RpcTarget.All);
		//StartCoroutine(DamagedEffect());

		//Debug.Log("Minion Damaged!! : " +stats.health);
		// 체력이 0 이하라면 DeadProcessing
		if (stats.actorHealth.health <= 0 && stats.state != MoonHeader.State.Dead)
		{
			StartCoroutine(DeadProcessing(other));
		}
		return;
	}

	[PunRPC] protected void DamMinion_RPC() {
		PlaySFX(1);
        StartCoroutine(DamagedEffect());
    }

	// 체력이 0 이하일 경우 호출.
	// 프로토 타입에서는 0.5초이후 비활성화.
	private IEnumerator DeadProcessing(GameObject other)
	{
		stats.state = MoonHeader.State.Dead;
		//nav_ob.enabled = false;
		nav.isStopped = true;

		anim.SetBool("Dead",true);
		photonView.RPC("DeadAnim",RpcTarget.All);

		if (nav.enabled)
		{ nav.velocity = Vector3.zero; nav.isStopped = true; }
		if (other.transform.CompareTag("PlayerMinion"))
		{
			other.GetComponent<I_Characters>().AddEXP((short)stats.exp);        // 잡은 미니언이 플레이어 미니언이라면 경험치를 한번 더 줌.
			other.GetComponent<I_Playable>().AddCS();
			//GameManager.Instance.DisplayAdd(string.Format("{0} killed {1}", other.name, this.name));
		}
		else if (other.transform.CompareTag("DamageArea"))
		{
			other.GetComponent<LSM_BasicProjectile>().orner.GetComponent<I_Characters>().AddEXP((short)stats.exp);
			other.GetComponent<LSM_BasicProjectile>().orner.GetComponent<I_Playable>().AddCS();
		}

		yield return new WaitForSeconds(2f);
		// 골드주는 오브젝트 생성.
		GameObject dummy_item = PoolManager.Instance.Get_Item(0);
		//dummy_item.transform.position = this.transform.position;
		dummy_item.GetComponent<LSM_ItemSC>().SpawnSetting(this.stats.gold, this.transform.position + Vector3.up * 1.1f);
		GiveExp();


		yield return new WaitForSeconds(1f);
		//this.gameObject.SetActive(false);
		photonView.RPC("DeadP",RpcTarget.All);
	}
	public void MinionDisable() { photonView.RPC("DeadP_M", RpcTarget.MasterClient); }

	public void GiveExp() 
	{
		RaycastHit[] hits;
		float expRadius = 10f;
		hits = Physics.SphereCastAll(transform.position, expRadius, Vector3.up, 0, 1 << LayerMask.NameToLayer("Minion"));
		foreach (RaycastHit hit in hits)
		{
			if (hit.transform.CompareTag("PlayerMinion"))
			{
				hit.transform.GetComponent<I_Characters>().AddEXP((short)stats.exp);
			}
		}
	}

	[PunRPC]protected void DeadAnim()
	{PlaySFX(2); anim.SetTrigger("DeadAnim"); this.transform.gameObject.layer = 12; }
	[PunRPC] protected void DeadP_M() {photonView.RPC("DeadP", RpcTarget.All); this.gameObject.SetActive(false); }
	[PunRPC]protected void DeadP()
	{ this.gameObject.SetActive(false); }

	// LSM 변경. 모든 적의 공격이 미니언의 앞에서만 오지 않을 수 있음.
	// 그러므로 해당 미니언의 위치를 받아와 방향 벡터를 얻고, 그 방향벡터로 일정 힘의 크기로 AddForce
	private IEnumerator DamagedEffect()
	{
		Color damagedColor = new Color32(255, 150, 150, 255);

		//Vector3 knockbackDirection = Vector3.Scale(this.transform.position - origin, Vector3.zero - Vector3.up).normalized * 500 + Vector3.up * 100;

		foreach (Renderer r in bodies)
		{ r.material.color = damagedColor; }
		//this.rigid.AddForce(knockbackDirection);

		yield return new WaitForSeconds(0.25f);
		foreach (Renderer r in bodies)
		{ r.material.color = Color.white; }

		// 첫번째 렌더러 팀색으로 변경.
		ChangeTeamColor();
	}

	// 공격이 끝났을 때 호출.
	// 공격 타겟을 null로 초기화, Obstacle 비활성화, Agent 활성화. 이때 약간의 텀이 존재해야함.
	// 텀이 존재하지 않을 경우, 자신의 너비만큼 순간이동.
	protected IEnumerator AttackFin()
	{
		if (!PlayerSelect && !is_attackFinish_Act)
		{
			is_attackFinish_Act = true;
			//Debug.Log("Attack Finish");
			this.stats.state = MoonHeader.State.Thinking;

			StopCoroutine(navenable_IE);
			navenable_IE = NavEnable(true);
			yield return StartCoroutine(navenable_IE);
			yield return new WaitForSeconds(0.5f);
			target_attack = null;
			this.stats.state = MoonHeader.State.Normal;
            MyDestination();

            //nav.isStopped = false;
            timer_Searching = SEARCHTARGET_DELAY;
			is_attackFinish_Act = false;
		}
	}

	// NavMesh Agent와 Obstacle을 동시에 키면 오류.
	// 그렇다고 텀을 안주고 키면 순간이동 버그.
	// true라면 Agent를 킴.
	// false라면 Obstacle을 킴.
	protected IEnumerator NavEnable(bool on)
	{
		rigid.velocity = Vector3.zero;
		if (!on)
		{
			//nav.enabled = false;
			yield return new WaitForSeconds(0.1f);
			//nav_ob.enabled = true;
			if (PhotonNetwork.IsMasterClient)
			{
				nav.enabled = true;
				nav.isStopped = true;
				nav.velocity = Vector3.zero;
			}
		}
		else
		{
			nav_ob.enabled = false;
			yield return new WaitForSeconds(0.1f);
			nav.enabled = true;
			nav.isStopped = false;
		}
	}

	// 아이콘 및 몸체 색 변경.
	#region ChangeTeamColors
	// 오버로드. 매개변수가 존재하지 않을경우 미니언의 아이콘의 색상을 변경.
	public void ChangeTeamColor() { photonView.RPC("ChangeTC_RPC", RpcTarget.All); }

	// 시작 혹은 생성할 때 미니언의 아이콘 등의 색상을 변경.
    [PunRPC]public void ChangeTC_RPC()
	{
		Color dummy_color;
		switch (stats.actorHealth.team)
		{
			case MoonHeader.Team.Red:
				dummy_color = Color.red;
				break;
			case MoonHeader.Team.Blue:
				dummy_color = Color.blue;
				break;
			case MoonHeader.Team.Yellow:
				dummy_color = Color.yellow;
				break;
			default: dummy_color = Color.gray; break;
		}
		icon.GetComponent<Renderer>().material.color = dummy_color;
		playerIcon.GetComponent<Renderer>().material.color = dummy_color;
		bodies[0].GetComponent<Renderer>().material.color = dummy_color;
	}
	#endregion

	// 플레이어가 해당 미니언에게 강령
	public void PlayerConnect() { photonView.RPC("PlayerC_RPC", RpcTarget.All); }
    [PunRPC]public void PlayerC_RPC()
	{
		PlayerSelect = true;

		navenable_IE = NavEnable(false);
		StartCoroutine(navenable_IE);

		icon.SetActive(false);
		playerIcon.SetActive(true);
		stats.state = MoonHeader.State.Invincibility;
		//stats.team = MoonHeader.Team.Blue;
	}

	// 플레이어가 해당 미니언에게서 나옴.
	

	public void PlayerDisConnect()
	{
		PlayerSelect = false;
		//nav_ob.enabled = false;
		if (PhotonNetwork.IsMasterClient)
		{
			nav.enabled = true;
			nav.isStopped = false;
		}

		icon.SetActive(true);
		playerIcon.SetActive(false);
	}

	// 플레이어가 해당 미니언을 탑뷰일 때 선택하였을 경우.
	public void PlayerSelected()
	{
		this.icon.GetComponent<Renderer>().material.color = Color.green;
	}

	// 애니메이션 관련 변수를 변경해주는 함수
	private void AnimationSetting()
	{
		if (nav.enabled)
			anim.SetFloat("Velocity", Vector3.Magnitude(nav.velocity));
		else
			anim.SetFloat("Velocity", 0f);
	}

	public void ParentSetting_Pool(int index) { photonView.RPC("ParentSPool_RPC", RpcTarget.AllBuffered, index); }
	[PunRPC] private void ParentSPool_RPC(int index) {
		this.transform.parent = PoolManager.Instance.gameObject.transform;
		PoolManager.Instance.poolList_Minion[index].Add(this.gameObject);
    }

    protected void PlaySFX(int num)
    {
        AudioSource dummy_s = Sounds[num].GetComponent<AudioSource>();
        if (dummy_s.isPlaying) { return; }
        else dummy_s.Play();
    }
    protected void StopSFX(int num)
    {
        AudioSource dummy_s = Sounds[num].GetComponent<AudioSource>();
        if (dummy_s.isPlaying) { dummy_s.Stop(); }
        else { return; }
    }


    // I_Actor 구현 함수
    #region I_Actor
    public short GetHealth(){return this.stats.actorHealth.health;}
	public short GetMaxHealth() { return this.stats.actorHealth.maxHealth; }
	public MoonHeader.Team GetTeam() { return this.stats.actorHealth.team; }
	public void AddEXP(short exp) { }
	public MoonHeader.S_ActorState GetActor() { return this.stats.actorHealth; }
	public GameObject GetCameraPos() { return CameraPosition; }
	public int GetState() { return (int)stats.state; }
	public void Selected() 
	{
		/*
		icon_ren = icon.GetComponent<MeshRenderer>();

		icon_materialL.Clear();
		icon_materialL.AddRange(icon_ren.materials);
		icon_materialL.Add(PrefabManager.Instance.outline);

		icon_ren.materials = icon_materialL.ToArray();
		selected_e = true;
		*/
        this.icon.GetComponent<Renderer>().material.color = MoonHeader.SelectedColors[(int)this.stats.actorHealth.team];
    }

	public void Unselected()
	{
        MeshRenderer renderer_d = icon.GetComponent<MeshRenderer>();

		icon_materialL.Clear();
		icon_materialL.AddRange(renderer_d.materials);
		//icon_materialL.Remove(PrefabManager.Instance.outline);

		icon_ren.materials = icon_materialL.ToArray();
		selected_e = false;

    }
	#endregion
}
