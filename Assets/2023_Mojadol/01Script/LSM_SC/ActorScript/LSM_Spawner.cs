using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.Rendering;

// �� �� ������ ������!!
public class LSM_Spawner : MonoBehaviour
{
	// ��ȯ�� ���� ��� ����.
	float BASEDELAY = 2f;
	float BASEWAVEDELAY = 35f;
	int BASEMINIONMULTIPLER = 3;
	int BASEMAXIMUMMELEE = 2;

	// �ִ� ��ȯ ������ ��
	public int MAX_NUM_MINION ;
	public MoonHeader.Team team;			// # �� ������ ���ָ� ��.

	public MoonHeader.SpawnerState state;			// �������� ���� ���¿� ���� enum
	//public GameObject[] way;
	public MoonHeader.S_SpawnerPaths[] spawnpoints;	// �����ʿ� ����� ��������Ʈ
	


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
		// ��������Ʈ�� �޾ƿ��� ����.
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

		// ���� �ʱ�ȭ
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

	
	// ���� �������� ���¸� Ȯ���ϸ� �������� ������ �ϴ� �Լ�
	private void CheckingSpawn()
	{
		// ���� �������� ���°� ���ݷ� �����̶�� ����
		if (state == MoonHeader.SpawnerState.Setting)
		{
			
		}
		// ������ ������ ���¶��
		else if (state == MoonHeader.SpawnerState.Spawn)
		{
			// ������ �������� ��� ��ȯ.
			if (GameManager.Instance.gameState == MoonHeader.GameState.Gaming)
			{
				delay += Time.deltaTime;

				// ���̺� �ִ� ��ȯ������ ���� ��ȯ�ߴٸ�, ��ȯ.
				if (delay > BASEDELAY && wave_Minions_Num < MAX_NUM_MINION)
				{
					delay = 0;

					for (int i = 0; i < spawnpoints.Length; i++)
					{
						// �� ���̺꿡 ��ȯ�� �̴Ͼ��� ������ Ȯ��.
						if (spawnpoints[i].num > spawnpoints[i].summon_)
						{
							GameObject dummy;
							MoonHeader.AttackType monT;
							// ���� �̴Ͼ��� ��ȯ ������ ���� ��ȯ�ƴٸ�, ����.
							if (spawnpoints[i].summon_ % BASEMINIONMULTIPLER < BASEMAXIMUMMELEE)
							{ dummy = PoolManager.Instance.Get_Minion(0); monT = MoonHeader.AttackType.Melee; }
							else 
							{ dummy = PoolManager.Instance.Get_Minion(1); monT = MoonHeader.AttackType.Range; }

							dummy.transform.position = spawnpoints[i].path.transform.position;
							//dummy.transform.parent = this.transform;
							// �̴Ͼ� ����
							LSM_MinionCtrl dummy_ctrl = dummy.GetComponent<LSM_MinionCtrl>();
							LSM_SpawnPointSc dummy_point = spawnpoints[i].path.GetComponent<LSM_SpawnPointSc>();
							dummy_ctrl.MonSetting(dummy_point, team, this.GetComponent<LSM_Spawner>(), monT);

							dummy_ctrl.minionBelong = dummy_point.number;
							//dummy_ctrl.minionType = spawnpoints[i].summon_ % 2;	//�̴Ͼ��� Ÿ���� ����

							spawnpoints[i].summon_++;
							wave_Minions_Num++;
						}
					}
				}
				// ���� �� ���̺꿡 ��ȯ�� ������ ���� �Ѿ��ٸ�, ���ο� ���̺� ��ȯ �ð����� ���
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

	//���� ����ɶ����� ���ӸŴ������� ȣ��
	public void ChangeTurn()
	{
		bool change_dummy = false;

		// ���� ���ݷ� ���� ���϶�
		if (GameManager.Instance.gameState == MoonHeader.GameState.SettingAttackPath)
		{change_dummy = true;}
		// ���ݷ� ���� ���� �ƴ� ���
		else
		{change_dummy = false;}

        if (change_dummy)
        { SettingPath_MinionSpawn(); delay = 0; }
        if (GameManager.Instance.mainPlayer.player.team != this.team) return;

        // ���ݷ� ������ ���� UI�� ���Ͽ�, �����̴��� ǥ�� ���θ� Ȯ��.
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

	// ���ݷ� ������ ���� ���� ���ݷο� ��ŭ ������ �Ͽ����� Ȯ��.
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

	// ���Ŵ������� ������ �������� ����Ǿ��ֱ⿡, ���Ŵ������� �޾ƿ;���.
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
