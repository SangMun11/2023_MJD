using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

// 팀당 하나의 팀매니저를 추가할 예정.
// 팀별 킬, 글로벌 골드, 스포너관리 등을 해당 스크립트에서 조정.
public class TeamManager : MonoBehaviourPunCallbacks, IPunObservable
{
    public MoonHeader.Team team;		// 해당 팀매니저의 팀
										// # 팀의 개수만큼 오브젝트를 소환. blue, red로 전환.

    // 모든 전황을 추가할 것임.
    public int kill, exp;

	// 해당 팀의 플레이어는 리스트로, 해당팀의 기지(마스터 스포너) 찾아오기.
    public List<LSM_PlayerCtrl> this_teamPlayers;
	public LSM_Spawner this_teamSpawner;

	public int MaximumSpawnNum;			// 팀 별 최대 소환 가능 조직 수.
	public int[] AttackPathNumber;      // 스포너의 스폰포인트 개수만큼 배열의 크기를 갖고있음. 스폰 포인트마다 설정된 미니언 생성 수
	public int[] AttackPathNum_past;

	public int selectedNumber;          // 현재 플레이어가 설정한 공격로의 조직 수. 이를 이용하여 슬라이더의 최대 값을 조정.
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
		// 모든 스포너를 받아온 후 팀에 해당하는 스포너를 받아옴. 한개밖에 없다는 가정으로 하나의 마스터스포너를 받아옴.
		GameObject[] dummySpawners = GameObject.FindGameObjectsWithTag("Spawner");
		foreach (GameObject s in dummySpawners)
		{
			LSM_Spawner sSC = s.GetComponent<LSM_Spawner>();
			if (sSC.team == this.team) { this_teamSpawner = sSC; break; }
		}
	}

	private void Start()
	{
		// 디버깅용
		selectedNumber = 0;
		MaximumSpawnNum = 6;


		// 마스터스포너에 존재하는 스폰포인트의 개수만큼 배열의 크기를 지정.
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
        // 해당 팀의 플레이어들을 받아옴. 해당 플레이어를 받아오기전 GameManager에서 플레이어를 생성함을 가정하여 받아오는 중.
        this_teamPlayers = new List<LSM_PlayerCtrl>();
        GameObject[] dummyplayer = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject p in dummyplayer)
        {
            LSM_PlayerCtrl pSC = p.GetComponent<LSM_PlayerCtrl>();
            if (pSC.player.team == this.team) { this_teamPlayers.Add(pSC); }
        }
    }

	// PathUI의 슬라이더 최대 값을 조정하는 함수.
	public void PathUI_ChangeMaxValue()	
	{
		// selectedNumber 즉 현재 공격로에 설정된 모든 개수를 저장.
		selectedNumber = 0;
		foreach (int n in AttackPathNumber)
		{ selectedNumber += n; }

		// 각 슬라이더의 최대 값. 해당 공격로에 설정된 값 + 최대 설정 가능한 값 - 설정된 값
		for (int i = 0; i < AttackPathNumber.Length; i++)
		{
			this_teamSpawner.spawnpoints[i].path.GetComponent<LSM_SpawnPointSc>().pathUI.GetComponent<LSM_AttackPathUI>().sl.maxValue =
				AttackPathNumber[i] + MaximumSpawnNum - selectedNumber;
		}
	}

	// 공격로 선택이 끝난 이후 공격로에 얼만큼 지정을 하였는지 확인.
	public void CheckingSelectMon()
	{
		// 공격로 설정 턴이 끝난 이후.. 남은 포인트가 존재한다면... -> 가장 적게 설정된 공격로(But Top -> Mid -> Bottom순으로 확인.)에 추가.
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
			if (is_Change_tatic) { GameManager.Instance.DisplayAdd(this.team.ToString() + " 팀의 전략이 변경되었습니다."); }
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
