using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.Rendering;

// 팀 별 마스터 스포너!!
public class LSM_Spawner : MonoBehaviour
{
	// 소환에 대한 상수 설정.
	float BASEDELAY = 2f;
	float BASEWAVEDELAY = 35f;
	int BASEMINIONMULTIPLER = 3;
	int BASEMAXIMUMMELEE = 2;

	// 최대 소환 가능한 수
	public int MAX_NUM_MINION ;
	public MoonHeader.Team team;			// # 팀 설정만 해주면 됨.

	public MoonHeader.SpawnerState state;			// 스포너의 현재 상태에 대한 enum
	//public GameObject[] way;
	public MoonHeader.S_SpawnerPaths[] spawnpoints;	// 스포너와 연결된 스폰포인트
	


	//public GameObject[] arrowDirect;

	public int wave_Minions_Num, selectedNum;
	//public byte num_attack;
	public float delay;

	private PhotonView photonView;

	public LSM_NexusSC thisNexus;

	private void Awake()
	{
		state = MoonHeader.SpawnerState.None;

		//GameObject[] ways = GameObject.FindGameObjectsWithTag("SpawnPoint");
		// 스폰포인트를 받아오는 구문.
		List<GameObject> ways = new List<GameObject>();
		foreach (Transform tr in gameObject.GetComponentsInChildren<Transform>())
		{
			if (tr.CompareTag("SpawnPoint"))
				ways.Add(tr.gameObject);
		}
		spawnpoints = new MoonHeader.S_SpawnerPaths[ways.Count];
		for (int i = 0; i < ways.Count; i++)
		{
			LSM_SpawnPointSc ways_script_dummy = ways[i].GetComponent<LSM_SpawnPointSc>();
			spawnpoints[ways_script_dummy.number] = new MoonHeader.S_SpawnerPaths(ways[i]);
		}

		// 변수 초기화
		delay = 0;
		wave_Minions_Num = 0;
		selectedNum = 0;
		MAX_NUM_MINION = 0;
		photonView = this.GetComponent<PhotonView>();
		thisNexus = this.GetComponentInChildren<LSM_NexusSC>();
	}

	private void Update()
	{
		if (!PhotonNetwork.IsMasterClient)
			return;
		//CheckingSpawn();
		if (GameManager.Instance.onceStart)
		{ CheckingSpawn(); }
	}

	
	// 현재 스포너의 상태를 확인하며 스포너의 동작을 하는 함수
	private void CheckingSpawn()
	{
		// 현재 스포너의 상태가 공격로 선택이라면 선택
		if (state == MoonHeader.SpawnerState.Setting)
		{
			
		}
		// 스폰이 가능한 상태라면
		else if (state == MoonHeader.SpawnerState.Spawn)
		{
			// 게임이 진행중일 경우 소환.
			if (GameManager.Instance.gameState == MoonHeader.GameState.Gaming)
			{
				delay += Time.deltaTime;

				// 웨이브 최대 소환수보다 적게 소환했다면, 소환.
				if (delay > BASEDELAY && wave_Minions_Num < MAX_NUM_MINION)
				{
					delay = 0;

					for (int i = 0; i < spawnpoints.Length; i++)
					{
						// 현 웨이브에 소환한 미니언의 마릿수 확인.
						if (spawnpoints[i].num > spawnpoints[i].summon_)
						{
							GameObject dummy;
							MoonHeader.AttackType monT;
							// 근접 미니언의 소환 수보다 적게 소환됐다면, 근접.
							if (spawnpoints[i].summon_ % BASEMINIONMULTIPLER < BASEMAXIMUMMELEE)
							{ dummy = PoolManager.Instance.Get_Minion(0); monT = MoonHeader.AttackType.Melee; }
							else 
							{ dummy = PoolManager.Instance.Get_Minion(1); monT = MoonHeader.AttackType.Range; }

							dummy.transform.position = spawnpoints[i].path.transform.position;
							//dummy.transform.parent = this.transform;
							// 미니언 세팅
							LSM_MinionCtrl dummy_ctrl = dummy.GetComponent<LSM_MinionCtrl>();
							LSM_SpawnPointSc dummy_point = spawnpoints[i].path.GetComponent<LSM_SpawnPointSc>();
							dummy_ctrl.MonSetting(dummy_point, team, this.GetComponent<LSM_Spawner>(), monT);

							dummy_ctrl.minionBelong = dummy_point.number;
							//dummy_ctrl.minionType = spawnpoints[i].summon_ % 2;	//미니언의 타입을 결정

							spawnpoints[i].summon_++;
							wave_Minions_Num++;
						}
					}
				}
				// 만약 한 웨이브에 소환이 가능한 수를 넘었다면, 새로운 웨이브 소환 시간까지 대기
				else if (wave_Minions_Num >= MAX_NUM_MINION)
				{
					if (delay > BASEWAVEDELAY)
					{
						wave_Minions_Num = 0;
						SettingPath_MinionSpawn();
					}
				}
			}
		}

	}

	public void SliderDisable()
	{
		foreach (MoonHeader.S_SpawnerPaths item in spawnpoints) { item.path.GetComponent<LSM_SpawnPointSc>().pathUI.GetComponent<LSM_AttackPathUI>().InvisibleSlider(false); }
	}

	//턴이 변경될때마다 게임매니저에서 호출
	public void ChangeTurn()
	{
		bool change_dummy = false;

		// 현재 공격로 지정 턴일때
		if (GameManager.Instance.gameState == MoonHeader.GameState.SettingAttackPath)
		{change_dummy = true;}
		// 공격로 지정 턴이 아닐 경우
		else
		{change_dummy = false;}

        if (change_dummy)
        { SettingPath_MinionSpawn(); delay = 0; }
        if (GameManager.Instance.mainPlayer.player.team != this.team) return;

        // 공격로 설정을 위한 UI에 대하여, 슬라이더의 표시 여부를 확인.
        foreach (MoonHeader.S_SpawnerPaths item in spawnpoints)
		{
			LSM_SpawnPointSc dummy = item.path.GetComponent<LSM_SpawnPointSc>();
            LSM_AttackPathUI dummy_path_ui = dummy.pathUI.GetComponent<LSM_AttackPathUI>();

			dummy_path_ui.InvisibleSlider(change_dummy);
			ChangePathNumber();
			GameManager.Instance.teamManagers[(int)this.team].PathUI_ChangeMaxValue();

			foreach (GameObject path in dummy.Paths)
				path.SetActive(change_dummy);
		}
		
	}

	// 공격로 선택이 끝난 이후 공격로에 얼만큼 지정을 하였는지 확인.
	public void CheckingSelectMon()
	{
		state = MoonHeader.SpawnerState.None;
		GameManager.Instance.teamManagers[(int)this.team].CheckingSelectMon();
		if (wave_Minions_Num <= 0)
		{
			wave_Minions_Num = 0;
			SettingPath_MinionSpawn();
		}
	}

	// 팀매니저에서 유저의 설정값이 저장되어있기에, 팀매니저에서 받아와야함.
	private void SettingPath_MinionSpawn()
	{
		MAX_NUM_MINION = GameManager.Instance.teamManagers[(int)this.team].MaximumSpawnNum * BASEMINIONMULTIPLER;
		for (int i = 0; i < spawnpoints.Length; i++)
		{
			spawnpoints[i].num = GameManager.Instance.teamManagers[(int)this.team].AttackPathNumber[i] * BASEMINIONMULTIPLER;
			spawnpoints[i].summon_ = 0;
		}
		wave_Minions_Num = 0;
	}

	public void ChangePathNumber() 
	{
		foreach(MoonHeader.S_SpawnerPaths item in spawnpoints)
		{
			item.path.GetComponent<LSM_SpawnPointSc>().pathUI_SC.CheckingServerValue();
		}
	}
}
