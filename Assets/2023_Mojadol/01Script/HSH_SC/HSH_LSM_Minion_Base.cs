using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/* LSM_MinionCtrl을 상속의 형태로 재활용하기 위해 만들어진 코드입니다.
 * 2023_03_20_HSH_수정사항 : 미니언이 소속된 팀에 따라 Scene에서도 Color가 변경되도록 함(CHangeTeamColor).
 * ㄴ 미니언 피격 시 분홍색으로 하이라이트 + 넉백 추가(DamagedEffect)
 * ㄴ 상속을 위해 private을 protected로 일괄 변경
 * ㄴ 
 */

public class HSH_LSM_Minion_Base : MonoBehaviour
{
	/*
	public MoonHeader.MinionStats stats;
	public LSM_Spawner mySpawner;
	protected bool PlayerSelect;
	[SerializeField]protected int way_index;

	protected const float MAXIMUMVELOCITY = 3f, SEARCHTARGET_DELAY = 1.5f, ATTACK_DELAY = 2f;

	protected Rigidbody rigid;
	protected NavMeshAgent nav;
	protected NavMeshObstacle nav_ob;

	public GameObject CameraPosition;
	public GameObject icon;

	[SerializeField]protected GameObject target_attack;
	[SerializeField]
	protected float searchRadius, minAtkRadius, maxAtkRadius;
	protected float timer_Searching, timer_Attack;

	public int minionBelong;	//Spawner.cs에서 자기가 몇 번 공격로 소속인지 받아옴


	protected void OnEnable()
	{
		
		nav_ob.enabled = false;
		nav.enabled = false;
	}

	protected virtual void Awake()
	{
		PlayerSelect = false;
		rigid = this.GetComponent<Rigidbody>();
		nav = this.GetComponent<NavMeshAgent>();
		nav_ob = this.GetComponent<NavMeshObstacle>();
		timer_Searching = 0;
		timer_Attack = 0;
		target_attack = null;
		nav.stoppingDistance = 3f;
		stats = new MoonHeader.MinionStats();

        icon = GameObject.Instantiate(PrefabManager.Instance.icons[0], transform);
        icon.transform.localPosition = new Vector3(0, 60, 0);
		searchRadius = 0f;
		minAtkRadius = 0f;
		maxAtkRadius = 0f;
    }
	protected void Start()
	{

	}
	protected void LateUpdate()
	{
		if (stats.state != MoonHeader.State.Dead && !ReferenceEquals(mySpawner, null))
		{
			// 현재 게임의 진행 상태가 어떻게 되는지 확인 후, 상태를 변경.
			if (GameManager.Instance.gameState != MoonHeader.GameState.Gaming)
			{
				if(nav.enabled)
					nav.isStopped = true;
				rigid.velocity = Vector3.zero;
				rigid.angularVelocity = Vector3.zero;
			}
			else
			{
				if (nav.enabled)
					nav.isStopped = false;
			}

			// 디버그용. 현재 상호작용 관련이 존재하지 않아 미니언들의 행동이 이상하여 최대 속도를 조정.
			if (MAXIMUMVELOCITY < Vector3.Magnitude(rigid.velocity))
			{
				//rigid.velocity = rigid.velocity.normalized * MAXIMUMVELOCITY;
				rigid.velocity = Vector3.zero;
			}

			SearchingTarget();
			Attack();
			MyDestination();
		}

		

	}
	
	// 미니언의 기본 스탯과 목적지를 정하는 함수. Spawner.cs에서 사용
	public void MonSetting(GameObject[] way, MoonHeader.Team t, LSM_Spawner spawn)
	{
		nav_ob.enabled = false;
		nav.enabled = true;
		PlayerSelect = false;
		timer_Searching = 0;
		timer_Attack = 0;
		target_attack = null;
        way_index = 0;

		transform.LookAt(stats.destination[way_index].transform);
		nav.destination = stats.destination[way_index].transform.position;
		mySpawner= spawn;
		CHangeTeamColor();
        // maxhealth, speed, atk, paths, team
        stats.Setting(10, 50f, 3, way, t);
        //stats = new MoonHeader.MinionStats(10, 50f, 10, way, t);
    }

    protected void SetRadius(float sr, float minr, float maxr) {searchRadius = sr; minAtkRadius = minr; maxAtkRadius = maxr; }

    protected void OnTriggerEnter(Collider other)
    {
		if (other.CompareTag("WayPoint") && stats.state != MoonHeader.State.Dead)
		{
			CheckingTurretTeam(other.transform.gameObject);
		}
    }


    // 미니언이 다음 길로 넘어가는 것을 구현한 함수
    public void MyDestination()
	{
		if (ReferenceEquals(target_attack, null) && nav.enabled)
		{
			nav.destination = stats.destination[way_index].transform.position;
		}
	}

	protected void CheckingTurretTeam(GameObject obj)
	{
        if (stats.destination[way_index].Equals(obj))
        {
            LSM_TurretSc dummySc = obj.transform.GetComponentInChildren<LSM_TurretSc>();
            if (dummySc.stats.Health <= 0 || dummySc.stats.team == this.stats.team)
            {
                way_index++;
            }
        }
    }

	// 미니언이 주변을 탐색하는 함수.
	protected virtual void SearchingTarget()
	{
		// 현재 미니언이 타겟을 확인 하였는지.
		if (ReferenceEquals(target_attack, null) && !PlayerSelect)
		{

			timer_Searching += Time.deltaTime;
			if (timer_Searching >= SEARCHTARGET_DELAY)
			{
				timer_Searching = 0;

				// 스피어캐스트를 사용하여 일정 반지름 내에 적이 있는지 확인.
				RaycastHit[] hits;
				hits = Physics.SphereCastAll(transform.position, searchRadius, Vector3.up, 0);
				float dummyDistance = float.MaxValue;
				foreach (RaycastHit hit in hits)
				{
					float hit_dummy_distance = Vector3.Distance(transform.position, hit.transform.position);
					if (dummyDistance > hit_dummy_distance)
					{
						bool different_Team = false;
						if (hit.transform.CompareTag("Minion"))	
						{different_Team = stats.team != hit.transform.GetComponent<HSH_LSM_Minion_Base>().stats.team; }
						else if (hit.transform.CompareTag("Turret"))
						{different_Team = stats.team != hit.transform.GetComponent<LSM_TurretSc>().stats.team; }

						if (different_Team)
						{
							dummyDistance = hit_dummy_distance;
							target_attack = hit.transform.gameObject;
							if (nav.enabled)
								nav.destination = target_attack.transform.position;
						}

					}
				}
			}
		}

		if (!ReferenceEquals(target_attack, null) && !PlayerSelect && nav.enabled)
		{
			
			nav.destination = target_attack.transform.position;
			// 타겟이 MaxDistance이상 떨어져있다면 null
			if (Vector3.Distance(target_attack.transform.position, this.transform.position) > maxAtkRadius)
			{ StartCoroutine(AttackFin()); }

			else if (Vector3.Distance(target_attack.transform.position, this.transform.position) <= minAtkRadius)
			{
				stats.state = MoonHeader.State.Attack;
			}

		}

	}

	protected virtual void Attack()
	{
		if (timer_Attack <= ATTACK_DELAY) { timer_Attack += Time.deltaTime; }

		if (!ReferenceEquals(target_attack, null))
		{
			if (!target_attack.activeSelf)
				StartCoroutine(AttackFin());

			else if (stats.state == MoonHeader.State.Attack)
			{
				// 만약 타겟의 위치가 공격 가능 범위보다 멀리 있다면, navmesh를 활성화, navObstacle을 비활성화
				bool dummy_cant_attack = Vector3.Distance(target_attack.transform.position, this.transform.position) > minAtkRadius * (nav.enabled ? 0.7f : 1f);

				if (dummy_cant_attack) { nav_ob.enabled = false; nav.enabled = true; }
				else { nav.enabled = false; nav_ob.enabled = true; }


				if (!dummy_cant_attack)
				{
					this.transform.LookAt(target_attack.transform.position);
					this.transform.rotation = Quaternion.Euler(0, this.transform.rotation.eulerAngles.y, 0);
					if (timer_Attack >= ATTACK_DELAY)
					{
						timer_Attack = 0;
						// 공격 애니메이션 실행. 지금은 즉발. 하지만 발사체를 사용할거면 이때 소환.
						switch (target_attack.tag)
						{
							case "Minion":
								HSH_LSM_Minion_Base dummy_ctrl = target_attack.GetComponent<HSH_LSM_Minion_Base>();
								dummy_ctrl.Damaged(this.stats.Atk);

								break;
							case "Turret":
								LSM_TurretSc dummy_Sc = target_attack.GetComponent<LSM_TurretSc>();
								if (dummy_Sc.stats.team != stats.team)
									dummy_Sc.Damaged(this.stats.Atk, this.stats.team);
								else
								{
									CheckingTurretTeam(target_attack.transform.parent.gameObject); StartCoroutine(AttackFin());
								}

								break;
						}

					}
				}
			}
		}

	}

	

	public int Damaged(int dam)
	{
		stats.health -= dam;
		StartCoroutine(DamagedEffect());

		//Debug.Log("Minion Damaged!! : " +stats.health);
		if (stats.health <= 0 && stats.state != MoonHeader.State.Dead)
		{
			StartCoroutine(DeadProcessing());
		}
		return stats.health;
	}

	protected IEnumerator DeadProcessing()
	{
		stats.state = MoonHeader.State.Dead;
		if(nav.enabled)
			nav.isStopped = true;
		yield return new WaitForSeconds(0.5f);
		this.gameObject.SetActive(false);
	}

	protected IEnumerator DamagedEffect()
	{
		Color damaged = new Color(255/255f, 150/255f, 150/255f);
		Color recovered = Color.white;
        Vector3 knockbackDirection = new Vector3(0f, 1000f, -1000f);

        transform.Find("Cylinder").gameObject.GetComponent<Renderer>().material.color = damaged;
        transform.Find("Head").gameObject.GetComponent<Renderer>().material.color = damaged;
        this.GetComponent<Rigidbody>().AddRelativeForce(knockbackDirection * 300 * Time.deltaTime);


        yield return new WaitForSeconds(0.25f);

        transform.Find("Cylinder").gameObject.GetComponent<Renderer>().material.color = recovered;
        transform.Find("Head").gameObject.GetComponent<Renderer>().material.color = recovered;
    }

    protected IEnumerator AttackFin()
    {
        target_attack = null;
        this.stats.state = MoonHeader.State.Normal;
        nav_ob.enabled = false;
        yield return new WaitForSeconds(0.5f);
        nav.enabled = true;
    }
    public void CHangeTeamColor()
	{
		Color dummy_color;
		switch (stats.team)
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
		this.gameObject.GetComponent<Renderer>().material.color = dummy_color;	//UI에서 뿐만 아니라 Scene에서도 색상이 변경
	}

	// 플레이어가 해당 미니언에게 강령
	public void PlayerConnect()
	{
		PlayerSelect = true;
		nav.enabled = false;
		nav_ob.enabled = true;
		
		//stats.team = MoonHeader.Team.Blue;
	}

	// 플레이어가 해당 미니언에게서 나옴.
	// 어디에서 빠져나왔을지 모르니, navmesh의 목적지를 재 설정해야함.
	public void PlayerDisConnect()
	{
		PlayerSelect = false;
		nav_ob.enabled = false;
		nav.enabled = true;
		
	}
	*/
}
