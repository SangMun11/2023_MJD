using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

// ���� �ϳ��� ���Ŵ����� �߰��� ����.
// ���� ų, �۷ι� ���, �����ʰ��� ���� �ش� ��ũ��Ʈ���� ����.
public class TeamManager : MonoBehaviourPunCallbacks, IPunObservable
{
    public MoonHeader.Team team;		// �ش� ���Ŵ����� ��
										// # ���� ������ŭ ������Ʈ�� ��ȯ. blue, red�� ��ȯ.

    // ��� ��Ȳ�� �߰��� ����.
    public int kill, exp;

	// �ش� ���� �÷��̾�� ����Ʈ��, �ش����� ����(������ ������) ã�ƿ���.
    public List<LSM_PlayerCtrl> this_teamPlayers;
	public LSM_Spawner this_teamSpawner;

	public int MaximumSpawnNum;			// �� �� �ִ� ��ȯ ���� ���� ��.
	public int[] AttackPathNumber;      // �������� ��������Ʈ ������ŭ �迭�� ũ�⸦ ��������. ���� ����Ʈ���� ������ �̴Ͼ� ���� ��
	public int[] AttackPathNum_past;

	public int selectedNumber;          // ���� �÷��̾ ������ ���ݷ��� ���� ��. �̸� �̿��Ͽ� �����̴��� �ִ� ���� ����.
	private bool once;

	[SerializeField]private short addAtk, addHp;

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.IsWriting)
		{
			stream.SendNext(MaximumSpawnNum);
			stream.SendNext(AttackPathNumber);

			int dummy = (int)addAtk & (int)ushort.MaxValue;
			dummy += ((int)addHp & (int)ushort.MaxValue) << 16;
			stream.SendNext(dummy);
		}
		else
		{
			MaximumSpawnNum = (int)stream.ReceiveNext();
			AttackPathNumber = (int[])stream.ReceiveNext();

			int dummy_r = (int)stream.ReceiveNext();
			addAtk = (short)(dummy_r & (int)ushort.MaxValue);
			addHp = (short)((dummy_r >> 16) & (int)ushort.MaxValue);
		}
	}

	private void Awake()
	{
		// ��� �����ʸ� �޾ƿ� �� ���� �ش��ϴ� �����ʸ� �޾ƿ�. �Ѱ��ۿ� ���ٴ� �������� �ϳ��� �����ͽ����ʸ� �޾ƿ�.
		GameObject[] dummySpawners = GameObject.FindGameObjectsWithTag("Spawner");
		foreach (GameObject s in dummySpawners)
		{
			LSM_Spawner sSC = s.GetComponent<LSM_Spawner>();
			if (sSC.team == this.team) { this_teamSpawner = sSC; break; }
		}
	}

	private void Start()
	{
		// ������
		selectedNumber = 0;
		MaximumSpawnNum = 6;


		// �����ͽ����ʿ� �����ϴ� ��������Ʈ�� ������ŭ �迭�� ũ�⸦ ����.
		AttackPathNumber = new int[this_teamSpawner.spawnpoints.Length];
        AttackPathNum_past = new int[this_teamSpawner.spawnpoints.Length];
    }

    private void Update()
    {
		if (!once && GameManager.Instance.onceStart)
			Start_function();
    }
    private void Start_function()
	{
        // �ش� ���� �÷��̾���� �޾ƿ�. �ش� �÷��̾ �޾ƿ����� GameManager���� �÷��̾ �������� �����Ͽ� �޾ƿ��� ��.
        this_teamPlayers = new List<LSM_PlayerCtrl>();
        GameObject[] dummyplayer = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject p in dummyplayer)
        {
            LSM_PlayerCtrl pSC = p.GetComponent<LSM_PlayerCtrl>();
            if (pSC.player.team == this.team) { this_teamPlayers.Add(pSC); }
        }
    }

	// PathUI�� �����̴� �ִ� ���� �����ϴ� �Լ�.
	public void PathUI_ChangeMaxValue()	
	{
		// selectedNumber �� ���� ���ݷο� ������ ��� ������ ����.
		selectedNumber = 0;
		foreach (int n in AttackPathNumber)
		{ selectedNumber += n; }

		// �� �����̴��� �ִ� ��. �ش� ���ݷο� ������ �� + �ִ� ���� ������ �� - ������ ��
		for (int i = 0; i < AttackPathNumber.Length; i++)
		{
			this_teamSpawner.spawnpoints[i].path.GetComponent<LSM_SpawnPointSc>().pathUI.GetComponent<LSM_AttackPathUI>().sl.maxValue =
				AttackPathNumber[i] + MaximumSpawnNum - selectedNumber;
		}
	}

	// ���ݷ� ������ ���� ���� ���ݷο� ��ŭ ������ �Ͽ����� Ȯ��.
	public void CheckingSelectMon()
	{
		// ���ݷ� ���� ���� ���� ����.. ���� ����Ʈ�� �����Ѵٸ�... -> ���� ���� ������ ���ݷ�(But Top -> Mid -> Bottom������ Ȯ��.)�� �߰�.
		while (true)
		{
			if (selectedNumber >= MaximumSpawnNum)
				break;

			int minNum = int.MaxValue, index = -1;
			for (int i = 0; i < AttackPathNumber.Length; i++)
			{
				if (AttackPathNumber[i] < minNum)
				{ index = i; minNum = AttackPathNumber[i]; }
			}
			selectedNumber++; AttackPathNumber[index]++;
		}

		if (PhotonNetwork.IsMasterClient)
		{
			bool is_Change_tatic = false;
			for (int i = 0; i < AttackPathNumber.Length; i++)
			{
				if (AttackPathNumber[i] != AttackPathNum_past[i])
				{
					is_Change_tatic = true;
					break;
				}
			}
			if (is_Change_tatic) { GameManager.Instance.DisplayAdd(this.team.ToString() + " ���� ������ ����Ǿ����ϴ�."); }
			for (int i = 0; i < AttackPathNumber.Length; i++)
			{ AttackPathNum_past[i] = AttackPathNumber[i]; }
		}
    }

	public void PathingNumberSetting(int num, int settingV)
	{
		this.AttackPathNumber[num] = settingV;
		this.PathUI_ChangeMaxValue();
		photonView.RPC("PathNumSetting_RPC", RpcTarget.AllBuffered, num, settingV);
	}
	[PunRPC]private void PathNumSetting_RPC(int num, int settingV)
	{
        this.AttackPathNumber[num] = settingV;
        this.PathUI_ChangeMaxValue();
		this_teamSpawner.ChangePathNumber();
    }

	public void ExpAdding(short e)
	{
		foreach(LSM_PlayerCtrl item in this_teamPlayers)
		{
			item.SetExp(e);
		}
	}

	public void AddingAtkHp(short hp, short atk)
	{
		addAtk += atk; addHp += hp;
	}
	public short[] GetAtkHp() { return new short[] { addHp, addAtk }; }
}
