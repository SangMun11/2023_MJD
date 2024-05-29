using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

using Photon.Pun;
using Unity.VisualScripting;

/* 2023_03_20_HSH_�������� : �̴Ͼ��� �Ҽӵ� ���� ���� Scene������ Color�� ����ǵ��� ��(CHangeTeamColor).
 * �� �̴Ͼ� �ǰ� �� ��ȫ������ ���̶���Ʈ + �˹� �߰�(DamagedEffect)
 *
 */

// �̴Ͼ� ��ũ��Ʈ.
// �Ŀ� ����, ���Ÿ� ���� �̴Ͼ���� �ش� ��ũ��Ʈ�� ��ӹް� �� ������.
public class LSM_MinionCtrl : MonoBehaviourPunCallbacks, I_Actor, IPunObservable, I_Characters
{
	public MoonHeader.S_MinionStats stats;          // �̴Ͼ��� ���¿� ���� ����ü.
	public LSM_Spawner mySpawner;                   // �̴Ͼ��� ������ ������.
	private bool PlayerSelect, once_changeRound;    // PlayerSelect: �÷��̾ �ش� �̴Ͼ� �����Ͽ�����, once_changeRound: ���� ���� �� �ѹ��� ����ǵ���.
	[SerializeField] private int way_index;         // ���� �̴Ͼ� ����� ��� �� ���°�� ��ǥ�� �����.

	// Ÿ�� ã��, ���� ������ ���� ���ȭ
	const float MAXIMUMVELOCITY = 3f, SEARCHTARGET_DELAY = 1f, ATTACK_DELAY = 2f;

	// �Ʒ� �ʿ��� ������Ʈ�� ����ȭ
	private Rigidbody rigid;
	[SerializeField] private Animator anim;
	private NavMeshAgent nav;
	private NavMeshObstacle nav_ob;
	private IEnumerator navenable_IE;   // StopCorutine�� ����ϱ� ���� ����
	private bool is_attackFinish_Act;

	private Renderer[] bodies;  // ������ ������ ������.

	public GameObject CameraPosition;       // ī�޶� �ʱ�ȭ�� �� ����� ����.
	private GameObject icon, playerIcon;    // �̴Ͼ� �����ϴ� ������, �÷��̾� ������
    public MeshRenderer icon_ren;
	List<Material> icon_materialL;
	bool selected_e;

	[SerializeField] protected GameObject target_attack;    // �̴Ͼ��� Ÿ��
	protected I_Actor target_actor;
	[SerializeField]
	protected float searchRadius, minAtkRadius, maxAtkRadius;   // �̴Ͼ��� Ž�� ����, �ּ� ���� ���� �Ÿ�, �ִ� ���� ���� �Ÿ�
	private float timer_Searching, timer_Attack;                // Ž���� ������ Ÿ�̸� ����.

	public int minionBelong;    //Spawner.cs���� �ڱⰡ �� �� ���ݷ� �Ҽ����� �޾ƿ�
	public int minionType;  //0�̸� ���Ÿ�, 1�̸� �ٰŸ� �̴Ͼ�

	public bool debugging_minion; // ����� Ȯ�ο�...
	private Vector3 networkPosition, networkVelocity;

	public GameObject[] Sounds;//0: walk, 1: hit, 2: death, 3: attack
	private bool isMove;


    // �����
    
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

	// �ٽ� Ȱ��ȭ�� ��� Obstacle ��Ȱ��ȭ, Agent Ȱ��ȭ
	void OnEnable()
	{
		nav_ob.enabled = false;
		nav.enabled = false;
	}

	private void Awake()
	{
		PlayerSelect = false;
		// ������Ʈ �޾ƿ���.
		rigid = this.GetComponent<Rigidbody>();
		nav = this.GetComponent<NavMeshAgent>();
		nav_ob = this.GetComponent<NavMeshObstacle>();
		anim = this.GetComponent<Animator>();
		// �ʱ�ȭ
		timer_Searching = 0;
		timer_Attack = 0;
		target_attack = null;
		nav.stoppingDistance = 0;
		stats = new MoonHeader.S_MinionStats();
		bodies = this.transform.GetComponentsInChildren<Renderer>();
		navenable_IE = NavEnable(true);

		// ������ ���� �� ���� ����.
		icon = GameObject.Instantiate(PrefabManager.Instance.icons[0], transform);
		icon.transform.localPosition = new Vector3(0, 60, 0);
		playerIcon = GameObject.Instantiate(PrefabManager.Instance.icons[4], transform);
		playerIcon.transform.localPosition = new Vector3(0, 60, 0);
		playerIcon.SetActive(false);
		icon_materialL = new List<Material>();
        selected_e = false;

        // ����׿� �̸� ����. ���� Melee
        //searchRadius = 10f;
        //minAtkRadius = 9f;
        //maxAtkRadius = 13f;

    }

	private void LateUpdate()
	{
		if (isMove) { PlaySFX(0); }
		else { StopSFX(0); }

		if (!PhotonNetwork.IsMasterClient) return;

		// ���� ������ ���� ���°� ��� �Ǵ��� Ȯ�� ��, ���¸� ����.
		if (once_changeRound && GameManager.Instance.gameState != MoonHeader.GameState.Gaming)
		{
			// �����ϰ����� ������, Agent�� Ȱ��ȭ�Ǿ����� ��
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

		// ���������, ������ �ʱ�ȭ�� �Ǿ��ְ�, ���� �������� ���¶�� �Ʒ� �Լ����� ����.
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

    // �̴Ͼ��� �⺻ ���Ȱ� �������� ���ϴ� �Լ�. Spawner.cs���� ���
    // ��� ���� �ʱ�ȭ
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
		// ���� �������̹Ƿ� �̸� �����ص�.
		object[] ob_d = LSM_SettingStatus.Instance.lvStatus[(int)MoonHeader.ActorType.Minion_Melee + (int)typeM-1]
			.getStatus_LV(Mathf.FloorToInt(GameManager.Instance.timer_inGameTurn / 3 * 60));
        // mh: �ִ� ü��, sp: ���ǵ�, atk: ���ݷ�, des: �����ʷκ��� �޾ƿ� �̵����, t: �̴Ͼ��� ��, type_d: �̴Ͼ��� Ÿ��,
        // e: �׾�����, Ȥ�� ������ ����Ǿ��� �� ��� ����ġ
        stats.Setting((short)ob_d[0], 4f, (short)ob_d[1], point.Ways, t, typeM,
			(short)(typeM == MoonHeader.AttackType.Melee ? 60:30), (short)(typeM == MoonHeader.AttackType.Melee ? 40: 30)) ;

		photonView.RPC("MS_RPC", RpcTarget.All, (short)stats.actorHealth.maxHealth, (short)stats.actorHealth.Atk, (short)t, (int)stats.speed, typeM);
		nav.speed = stats.speed;
		//stats = new MoonHeader.MinionStats(10, 50f, 10, way, t);

		// ��������Ʈ�� ����� ���ݷη� ������ ����.
		transform.LookAt(stats.destination[way_index].transform);
		MyDestination();
		//nav.avoidancePriority = 50;
		nav.isStopped = false;

		// ������ ������ ����. ������ Ȱ��ȭ.
		mySpawner = spawn;
		icon.SetActive(true);
		playerIcon.SetActive(false);
		nav.velocity = Vector3.zero;
		rigid.angularVelocity = Vector3.zero;
		rigid.velocity = Vector3.zero;

		// ������ �� ���� �� ���� ���� �°� ��ȭ
		ChangeTeamColor();
	}

	// ü��, ���ݷ�, ��
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


	// ��������Ʈ Ʈ���ſ� ��Ҵٸ� �ߵ��ϴ� �Լ�.
	// �ش� ��������Ʈ�� �̴Ͼ��� ���� �������� ������ Ȯ���ϴ� �Լ� ����.
	private void OnTriggerEnter(Collider other)
	{
		if (!PhotonNetwork.IsMasterClient) return;

		if (other.CompareTag("WayPoint") && stats.state != MoonHeader.State.Dead)
		{
			CheckingTurretTeam(other.transform.gameObject);
		}
	}


	// �̴Ͼ��� ���� ��� �Ѿ�� ���� ������ �Լ�.
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
        // �������� NavMesh�� �������� ���� ���, �������� ������ �� ����.
        Vector3 destination_direction = (this.transform.position - pos_dummy).normalized;
        float dist_dummy = 0;
        float destination_distance = Vector3.Distance(pos_dummy, this.transform.position);
		Vector3 result = pos_dummy;

		// �׷��Ƿ� �ش� �������� �̴Ͼ��� ���ϴ� ������ ����.
		// NavMesh�� SamplePosition�� ����Ͽ� ���ݾ� �̴Ͼ� �������� NavMesh�� �����ϴ��� Ȯ��.
		// ���� �ش� Hit Position�� ��ǥ�� ����.
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

	// �ͷ��� ���Ͽ�. �޾ƿ� �ͷ��� ���� �ڽ��� ��ǥ���� �ͷ��� �´��� Ȯ��.
	// ���� �����ϴٸ� way_index�� ��½��� ���� �������� �̵�.
	private void CheckingTurretTeam(GameObject obj)
	{
        LSM_TurretSc dummySc = obj.transform.GetComponentInChildren<LSM_TurretSc>();
		if (dummySc.stats.actorHealth.health <= 0 || dummySc.stats.actorHealth.team == this.stats.actorHealth.team)
		{
			if (stats.destination[way_index].Equals(dummySc.waypoint))
			{ way_index++; MyDestination(); }
		}
	}

	// �̴Ͼ��� �ֺ��� ���� ����� Ž���ϴ� �Լ�.
	private void SearchingTarget()
	{
		// ���� �̴Ͼ��� Ÿ���� �����ϴ���, �÷��̾ ���� ������, �̴Ͼ��� ����,���� ���� ���°� �ƴ��� Ȯ��.
		if (ReferenceEquals(target_attack, null) && !PlayerSelect && stats.state == MoonHeader.State.Normal)
		{
			timer_Searching += Time.deltaTime;
			if (timer_Searching >= SEARCHTARGET_DELAY)
			{
				timer_Searching = 0;

				// ���Ǿ�ĳ��Ʈ�� ����Ͽ� ���� ������ ���� ���� �ִ��� Ȯ��.
				RaycastHit[] hits;
				hits = Physics.SphereCastAll(transform.position, searchRadius, Vector3.up, 0);
				float dummyDistance = float.MaxValue;
				// ���� ���� �����ϴ� ��� ������Ʈ�� Ȯ�� �� ������ �������� ���� �Ǵ�.
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
						}   //�ڽŰ� ���� ���ݷ��� �ͷ��� ������� ����
						else if (hit.transform.CompareTag("PlayerMinion"))
						{
							I_Actor dummy_actor = hit.transform.GetComponent<I_Actor>();
							different_Team = (stats.actorHealth.team != dummy_actor.GetTeam() && hit.transform.GetComponent<I_Characters>().GetState() != (int)MoonHeader.State_P_Minion.Dead);
						}
						else if (hit.transform.CompareTag("Nexus"))
						{
							different_Team = (stats.actorHealth.team != hit.transform.GetComponent<LSM_NexusSC>().stats.actorHealth.team);
						}

						// ���� ���� ������, ����� ��� Ÿ������ ����.
						if (different_Team)
						{
							dummyDistance = hit_dummy_distance;
							target_attack = hit.transform.gameObject;
						}
					}
				}
				if (!ReferenceEquals(target_attack, null)) { target_actor = target_attack.GetComponent<I_Actor>(); }

				// Ÿ���� ã������, �̵����̶��.. �ڽ��� ��ǥ�� Ÿ������ ����. �ش� �������� �̵�
				if (!nav.isStopped && !ReferenceEquals(target_attack, null))
					nav.destination = CheckingPosition_NavMesh(target_attack.transform.position);
				// 
				//else if (ReferenceEquals(target_attack,null) && nav.enabled && nav.isStopped) { nav.isStopped = false; }
				// Ÿ���� ã�� ���Ͽ�����, NavMeshAgent�� ��Ȱ��ȭ�Ǿ��������, Obstacle ��Ȱ��ȭ, Agent Ȱ��ȭ
				else if (ReferenceEquals(target_attack, null) && !nav.enabled) { StopCoroutine(navenable_IE); navenable_IE = NavEnable(true); StartCoroutine(navenable_IE); }
			}
		}

		// Ÿ���� ã������, �÷��̾ �������� �ʾҰ�, ���� Agent�� Ȱ��ȭ �Ǿ����� ���
		else if (!ReferenceEquals(target_attack, null) && !PlayerSelect && !nav.isStopped)
		{
			// Agent�� �������� Ÿ���� ��ġ�� ����.
			nav.destination = CheckingPosition_NavMesh(target_attack.transform.position);


            // ����ĳ��Ʈ�� ���� ��ü�� ������ �ִ��� Ȯ��.
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

			// Ÿ���� MaxDistance�̻� �������ִٸ� Ÿ���� ������. null����.
			if (dist > maxAtkRadius && stats.state != MoonHeader.State.Thinking && stats.state != MoonHeader.State.Dead)
			{
				//Debug.Log("target setting null. : distance : " + Vector3.Distance(target_attack.transform.position, this.transform.position) + "target : " +target_attack.name);
				//Debug.Log("AttackFinish in Far away");
				StartCoroutine(AttackFin());
			}

			// ���� Ÿ�ٰ��� �Ÿ��� �ּ� ���� ���� �Ÿ����� ���ٸ� ���� �Լ� ȣ��.
			else if (dist <= minAtkRadius)
			{
				stats.state = MoonHeader.State.Attack;
			}

		}

	}

	// �̴Ͼ��� ���������, Ÿ���� ã�Ҵٸ� �ش� �Լ��� ����.
	// ���� �����Ҷ� NavAgent�� ��Ȱ��ȭ, NavObstacle�� Ȱ��ȭ �Ͽ�����, �̸� �����ϸ� NavMesh�� ���� ��ã�⸦ �ǽð����� �ٽ� �ݺ��ϴ� ����������.
	// �׷��Ƿ� NavAgent�� Priority�� �ϰ���Ű�� ������ �ش� �̴Ͼ��� ��ġ�� �ʰ� ����.
	// �� �ٽ� ����... NavObstacle�� ���. 

	private void Attack()
	{
		if (timer_Attack <= ATTACK_DELAY) { timer_Attack += Time.deltaTime; }

		// Ÿ���� �����Ѵٸ�.
		if (!ReferenceEquals(target_attack, null))
		{
			// Ÿ���� �ı��Ǿ��ٸ�. -> ���� ObjectPooling�� ����ϰ������Ƿ�, ActiveSelf�� ����Ͽ� ���� Ȱ��/��Ȱ�� ���¸� Ȯ��.
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
				// ���� �ٽ� NavMeshObstacle�� ��� ������ �𸣱⿡ �ش� �κ��� �ּ�ó���� ���ܵξ���.
				//bool dummy_cant_attack = Vector3.Distance(target_attack.transform.position, this.transform.position) > minAtkRadius * (nav.isStopped ? 1f : 0.7f);
				//if (!dummy_cant_attack) { nav.isStopped = true; nav.avoidancePriority = 10; }
				//else { nav.isStopped = false; nav.avoidancePriority = 50; }

				// Ÿ�ٰ��� �Ÿ��� minAtkRadius���� ũ�ٸ� ������ �Ұ���.
				//-> Agent�� �����ִ� ���(Ÿ���� �����ϱ� ���Ͽ� �����̰� �ִ� ���)���� ���� �� �ణ�� �����ӿ� ����Ͽ� �� �� ������ �ٰ����� ����.

				// ����ĳ��Ʈ�� ���� ��ü�� ������ �ִ��� Ȯ��.
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

				// ������ �Ұ����� ���, ������ ��쿡 ���� Obstacl, Agent�� Ȱ��/��Ȱ��.
				// Agent �� Obstacle�� ���ÿ� ����Ѵٸ� ���� �߻� -> �ڽ� ���� ��ֹ��̶� �����ϸ� �ڽ��� �ִ� ���� ���Ϸ��� ���
				// �׷��⿡ Obstacle�� Agent�� ���� Ű�� ���� �ϴ� ����. �㳪 ��Ȱ��ȭ�Ѵٰ� �ٷ� ��Ȱ��ȭ������ ��������.
				// �ణ�� ���� ���� �ʴ´ٸ� ���� �浹�Ͽ� �ðܳ����� ��찡 ������.

				if (dummy_cant_attack && nav.isStopped)
				{
					navenable_IE = NavEnable(true); StartCoroutine(navenable_IE);
				}
				else if (!dummy_cant_attack && !nav.isStopped)
				{
					navenable_IE = NavEnable(false); StartCoroutine(navenable_IE); rigid.velocity = Vector3.zero;
				} //���� ����. �Ƹ��� ������ ������ ��� �ҷ����µ�.

				// ���� ������ �����ϴٸ� �����ϴ� ����
				if (!dummy_cant_attack)
				{
					// y�� rotation���� ������ ����.
					Quaternion target_rotation = Quaternion.LookRotation((target_attack.transform.position - this.transform.position).normalized);

					Vector3 target_euler = Quaternion.RotateTowards(this.transform.rotation, target_rotation, 200 * Time.deltaTime).eulerAngles;
					this.transform.rotation = Quaternion.Euler(0, target_euler.y, 0);

					if (timer_Attack >= ATTACK_DELAY && Mathf.Abs(this.transform.rotation.eulerAngles.y - target_rotation.eulerAngles.y) < 10f)
					{
						timer_Attack = 0;
						// ���� �ִϸ��̼� ����. ������ ���. ������ �߻�ü�� ����ҰŸ� �̶� ��ȯ.
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

	// Generic ������ ����Ͽ� �ش� ������ �Լ�ȭ. IActor �������̽��� ���� player, turret, minion�� �����ϰ�����.
	// ���� Damaged�� ȣ���� ������.
	private void Attack_other<T>(GameObject other) where T : I_Actor {
		T Script = other.GetComponent<T>();
		Script.Damaged(this.stats.actorHealth.Atk, this.transform.position, this.stats.actorHealth.team, this.gameObject);
	}


	// �̴Ͼ��� �������� ���� �� ����ϴ� �Լ�.
	// dam = �̴Ͼ� Ȥ�� ��ž�� ���ݷ�. �̴Ͼ��� �޴� ������.
	// origin = ������ �ϴ� ��ü�� ��ġ. �̸� �̿��Ͽ� ���� �ڿ������� �˹��� ��������. // ���� �˹��� �����ϰ�����.

	public void Damaged(short dam, Vector3 origin, MoonHeader.Team t, GameObject other)
	{
		// ���� Ȥ�� ���� ������ ��� �������� ��������. �ٷ� return
		if (stats.state == MoonHeader.State.Invincibility || stats.state == MoonHeader.State.Dead || !PhotonNetwork.IsMasterClient)
			return;
		else if (t == this.stats.actorHealth.team)
			return;

		stats.actorHealth.health -= dam;

		if (stats.actorHealth.health > 0)
			photonView.RPC("DamMinion_RPC", RpcTarget.All);
		//StartCoroutine(DamagedEffect());

		//Debug.Log("Minion Damaged!! : " +stats.health);
		// ü���� 0 ���϶�� DeadProcessing
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

	// ü���� 0 ������ ��� ȣ��.
	// ������ Ÿ�Կ����� 0.5������ ��Ȱ��ȭ.
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
			other.GetComponent<I_Characters>().AddEXP((short)stats.exp);        // ���� �̴Ͼ��� �÷��̾� �̴Ͼ��̶�� ����ġ�� �ѹ� �� ��.
			other.GetComponent<I_Playable>().AddCS();
			//GameManager.Instance.DisplayAdd(string.Format("{0} killed {1}", other.name, this.name));
		}
		else if (other.transform.CompareTag("DamageArea"))
		{
			other.GetComponent<LSM_BasicProjectile>().orner.GetComponent<I_Characters>().AddEXP((short)stats.exp);
			other.GetComponent<LSM_BasicProjectile>().orner.GetComponent<I_Playable>().AddCS();
		}

		yield return new WaitForSeconds(2f);
		// ����ִ� ������Ʈ ����.
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

	// LSM ����. ��� ���� ������ �̴Ͼ��� �տ����� ���� ���� �� ����.
	// �׷��Ƿ� �ش� �̴Ͼ��� ��ġ�� �޾ƿ� ���� ���͸� ���, �� ���⺤�ͷ� ���� ���� ũ��� AddForce
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

		// ù��° ������ �������� ����.
		ChangeTeamColor();
	}

	// ������ ������ �� ȣ��.
	// ���� Ÿ���� null�� �ʱ�ȭ, Obstacle ��Ȱ��ȭ, Agent Ȱ��ȭ. �̶� �ణ�� ���� �����ؾ���.
	// ���� �������� ���� ���, �ڽ��� �ʺ�ŭ �����̵�.
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

	// NavMesh Agent�� Obstacle�� ���ÿ� Ű�� ����.
	// �׷��ٰ� ���� ���ְ� Ű�� �����̵� ����.
	// true��� Agent�� Ŵ.
	// false��� Obstacle�� Ŵ.
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

	// ������ �� ��ü �� ����.
	#region ChangeTeamColors
	// �����ε�. �Ű������� �������� ������� �̴Ͼ��� �������� ������ ����.
	public void ChangeTeamColor() { photonView.RPC("ChangeTC_RPC", RpcTarget.All); }

	// ���� Ȥ�� ������ �� �̴Ͼ��� ������ ���� ������ ����.
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

	// �÷��̾ �ش� �̴Ͼ𿡰� ����
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

	// �÷��̾ �ش� �̴Ͼ𿡰Լ� ����.
	

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

	// �÷��̾ �ش� �̴Ͼ��� ž���� �� �����Ͽ��� ���.
	public void PlayerSelected()
	{
		this.icon.GetComponent<Renderer>().material.color = Color.green;
	}

	// �ִϸ��̼� ���� ������ �������ִ� �Լ�
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


    // I_Actor ���� �Լ�
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
