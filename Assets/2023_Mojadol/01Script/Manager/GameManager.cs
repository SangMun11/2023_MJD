using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;


//탈출 제작, 낭떠러지에 빠지면 즉사.

// 전체 게임에 대한 매니저.
// LSM담당 스크립트지만 톱니바퀴 아이콘이 맘에들어서.. 머쓱
public class GameManager : MonoBehaviourPunCallbacks,IPunObservable
{
	// 싱글톤///
    private static GameManager instance;
	private void Awake()
	{
		if (instance == null) { instance = this; }
		else { Destroy(this); }
		Awake_Function();
	}
	public static GameManager Instance{ get{ return instance; } }
	// ///

	const float SELECTATTACKPATHTIME = 60f, ROUNDTIME = 1800;
	// SEARCHATTACKPATHTIME: 공격로 설정 시간. ROUNDTIME: 게임 진행 시간.

	[Header("GameManager States")]
	public MoonHeader.ManagerState state;		// 현재 게임매니저의 상태. --> 게임매니저가 현재 어떤 상태인지 ex: 준비중, 처리중, 처리완료
	public MoonHeader.GameState gameState;      // 현재 게임의 상태 ex: 공격로 설정 시간, 게임 시작 전, 등등
												// # 게임 시작 전 gameState를 SettingAttackPath로 설정 -> 디버깅용
	[Header("Involved Photon")]
	public int numOfPlayer;
	public int numOfSkipClickedPlayer;             // 현재 플레이어의 수.
    public int MaxPlayerNum;

    [Header("For UI Control")]
    public TextMeshProUGUI turnText;    // 현재 턴의 종류에 대하여 사용자에게 보여주는 UI. 후에 바꿀 예정.
                                        // # 해당 변수는 인스턴스에서 직접 연결해줘야함. Canvas 내에 있는 Turn Object를 연결.
    public GameObject canvas;						// 씬에 존재하는 캔버스. 하나만 있다고 가정하여 Awake에서 찾아 저장.
	public GameObject selectAttackPathUI, mapUI, gameUI, loadingUI, tabUI;    // selectAttackPathUI: 공격로 설정 때 사용자에게 보여주는 UI들이 저장된 오브젝트.  mapUI: TopView 상태일 때 사용자에게 보여주는 UI들이 저장된 오브젝트.
															// gameUI: 게임 진행 중 표시되는 UI
	public LSM_GameUI gameUI_SC;

	public Image deadScreen;
    private Image screen;                           // 페이드 IN, OUT을 할 때 사용하는 이미지.
    private List<GameObject> logUIs;
    public int LoadingGauge, ReadyToStart_Player;
    public int ReadyToStart_LoadingGauge;
    public Image LoadingImage;
    public TextMeshProUGUI pingText;

    [Header("For Games")]
    public GameObject[] spawnPoints;    // 씬에 존재하는 "마스터 스포너"의 모음.
    public GameObject[] wayPoints;      // 씬에 존재하는 모든 "웨이포인트"의 모음

    public LSM_TimerSc timerSc;         // 타이머 스크립트. 게임 진행 중 타이머가 필요한(ex: 게임 공격로 설정시간, 게임 진행시간) 경우 사용하는 스크립트.
    public LSM_PlayerCtrl[] players;				// 모든 플레이어들을 저장하는 배열
	public LSM_PlayerCtrl mainPlayer;               // 현재 접속하고있는 플레이어를 저장하는 변수
	public String mainPlayerName;
	public byte mainPlayerSelectNum;
	public float timer_AllRunning, timer_inGameTurn;
	public GameObject shopUI, endingUI, errorUI;

	public LSM_CreepCtrl[] creeps;

	public TeamManager[] teamManagers;				// 모든 팀의 팀매니저

	public List<GameObject>[] playerMinions;        // 모든 플레이어들의 미니언을 저장. 해당 부분 또한 PoolManager에서 사용할지 고민 중..
    private bool isClickSkip;

    private List<string> logUIs_Reservation;
	private float timer_log;

	public bool onceStart;
	private bool starting_;
	private float ping;
	[Header("Debugging")]
	public bool debugging_bool;
	private int preMasterClientID;

	// private 

	#region IPunObservable Implementation 0408-PSH
	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) // 되는 것 같긴한데 실제로 적용되는지는 확인하기 힘듬 
	{
		if (stream.IsWriting)
		{
			stream.SendNext(state);
			stream.SendNext(gameState);
        }
		else
		{
			this.state = (MoonHeader.ManagerState)stream.ReceiveNext();
			this.gameState = (MoonHeader.GameState)stream.ReceiveNext();
        }
	}
	#endregion

	#region Multi Variables Region
	string gameVersion = "1.0";

	private byte maxPlayersPerRoom = 2;
	bool isConnecting;

	#endregion

	#region public Multi Methods
	public void Connect()
	{
		isConnecting = true;
		if (PhotonNetwork.IsConnected)
		{
			PhotonNetwork.JoinRandomRoom();
			Debug.Log($"Room Info : {PhotonNetwork.InRoom}");
		}
		else
		{
			PhotonNetwork.GameVersion = gameVersion;
			PhotonNetwork.ConnectUsingSettings();
		}
	}
	#endregion

	#region Multi Callbacks Method Region
	public override void OnConnectedToMaster()
	{
		base.OnConnectedToMaster();
		if (isConnecting)
		{
			PhotonNetwork.JoinRandomRoom();
		}
	}

	public override void OnJoinRandomFailed(short returnCode, string message)
	{
		RoomOptions default_option = new RoomOptions();
		default_option.MaxPlayers = maxPlayersPerRoom;
		default_option.PublishUserId = true;

		PhotonNetwork.CreateRoom(null, default_option);
	}

	public override void OnJoinedRoom()
	{
		base.OnJoinedRoom();
		loadingUI.GetComponentInChildren<TextMeshProUGUI>().text = "연결 완료!!";
		//if(!PhotonNetwork.IsMasterClient)
        //{
		mainPlayer=PhotonNetwork.Instantiate("Playerobj", this.transform.position, this.transform.rotation).GetComponent<LSM_PlayerCtrl>();     // LSM -> 마스터 클라이언트 또한 들어올때 플레이어 오브젝트 소환.
		mainPlayer.SettingPlayer_(mainPlayerName, mainPlayerSelectNum);
		
		//}
	}

	public override void OnPlayerEnteredRoom(Player newPlayer)
	{
		base.OnPlayerEnteredRoom(newPlayer);

		if (PhotonNetwork.IsMasterClient)
		{
			Debug.Log($"Room Player Count : {PhotonNetwork.CurrentRoom.PlayerCount}");
			Debug.Log("PlayerEntered");
			
		}
		else
			return;
	}
	// 플레이어가 나간다면 종료...
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);
        Debug.Log("Client is Left!");
		if (gameState != MoonHeader.GameState.Ending)
		{
            Cursor.lockState = CursorLockMode.None;
            errorUI.SetActive(true);
            StopAllCoroutines();
            PhotonNetwork.Disconnect();
			onceStart = false;
		}
    }

    #endregion




    // 싱글톤으로 인해 Awake를 위로 배치하였기에 미관상 아래의 함수를 사용.
    private void Awake_Function()
	{
		// 게임매니저에 존재하는 TimerSC를 받아옴.
		timerSc = this.GetComponent<LSM_TimerSc>();

		logUIs = new List<GameObject>();                // 로그 표시에 대하여. 5개 이하만 표시하려고 해당 리스트를 생성.
		logUIs_Reservation = new List<string>();        // 다섯개이상 부터는 예약 리스트를 생성하여 그 리스트 안에 저장.
		timer_log = 0;

		mainPlayerName = (PlayerPrefs.HasKey("PlayerLocalName") ? PlayerPrefs.GetString("PlayerLocalName") : "UnknownPlayer");
		mainPlayerSelectNum = (byte)(PlayerPrefs.HasKey("PlayerSelectType") ? PlayerPrefs.GetInt("PlayerSelectType") : 0);


		// 해당 변수에 맞는 게임 오브젝트들을 저장.
		spawnPoints = GameObject.FindGameObjectsWithTag("Spawner");
		canvas = GameObject.Find("Canvas");
		selectAttackPathUI = GameObject.Find("AttackPathUIs");
		selectAttackPathUI.GetComponentInChildren<Button>().onClick.AddListener(SkipClick);		// 스킵버튼에 해당. 클릭 시 TimerSC에 존재하는 TimerOut함수가 실행되도록 설정.

		selectAttackPathUI.SetActive(false);
		mapUI = GameObject.Find("MapUIs");
		gameUI = GameObject.Find("GameUI");
		tabUI = GameObject.Find("TabUI");
		shopUI = GameObject.Find("ShopPannel");
		endingUI = GameObject.Find("ResultPannel");
		errorUI = GameObject.Find("ErrorPannel"); errorUI.SetActive(false);

        gameUI_SC = gameUI.GetComponent<LSM_GameUI>();
		gameUI.SetActive(false);

		wayPoints = GameObject.FindGameObjectsWithTag("WayPoint");
		screen = GameObject.Find("Screen").GetComponent<Image>();
		deadScreen = GameObject.Find("DeadScreen").GetComponent<Image>();
		screen.transform.SetAsLastSibling();    // 스크린이 다른 UI를 가리도록 가장 마지막에 배치하는 코드.
        loadingUI = GameObject.Find("Loading");
		loadingUI.transform.SetAsLastSibling();
		LoadingImage = GameObject.Find("LoadingBar").GetComponent<Image>();
        GameObject[] teammdummy = GameObject.FindGameObjectsWithTag("TeamManager");
		teamManagers = new TeamManager[teammdummy.Length];
		foreach (GameObject t in teammdummy)
		{ teamManagers[(int)t.GetComponent<TeamManager>().team] = t.GetComponent<TeamManager>(); }
		starting_ = false;

        loadingUI.GetComponentInChildren<TextMeshProUGUI>().text = "방을 찾는 중...";
		MaxPlayerNum = 2;
		numOfSkipClickedPlayer = 0;
		isClickSkip = false;
		timer_AllRunning = 0;
		timer_inGameTurn= 0;

		Invoke("Connecting_", 2f);
	}

	private void Connecting_() 
	{ Connect(); if (debugging_bool) { Debugging_Setting(); } }

	private void Start_function()
	{
		// 모든 플레이어들을 저장하는 중. FindGameObjectsWithTag를 사용하여 오브젝트를 찾고, 해당 스크립트를 저장하게 구현.
		// 이 부분 이전에 플레이어를 소환하는 절차가 필요!, 로컬 플레이어또한 필요.


		GameObject[] playerdummys = GameObject.FindGameObjectsWithTag("Player");
		players = new LSM_PlayerCtrl[playerdummys.Length];
		for (int i = 0; i < playerdummys.Length; i++)
		{
			players[i] = playerdummys[i].transform.GetComponent<LSM_PlayerCtrl>();
			players[i].isMainPlayer = false;
        }

		mainPlayer.isMainPlayer = true;
		for (int i = 0; i < players.Length; i++)
		{
			players[i].Start_fuction();
			if (PhotonNetwork.IsMasterClient)
			{
				players[i].SettingTeamAndName(i % 2);              // 팀 나누기.
			}

		}

		GameObject[] creepDummys = GameObject.FindGameObjectsWithTag("Creep");
		creeps = new LSM_CreepCtrl[creepDummys.Length];
		for (int i = 0; i < creepDummys.Length; i++) 
		{ creeps[i] = creepDummys[i].GetComponent<LSM_CreepCtrl>(); }

		// 기존 게임매니저의 상태 초기화. Default값 Ready.
		state = MoonHeader.ManagerState.Ready;

		// 플레이어 미니언의 리스트 저장.
		playerMinions = new List<GameObject>[2];    // 팀의 개수만큼 배열의 크기를 지정. 현재 디버깅용으로 2로 설정.
		for (int i = 0; i < 2; i++) { playerMinions[i] = new List<GameObject>(); }


		// 디버깅용으로 플레이어를 한명으로 설정.
		numOfPlayer = players.Length ;

		gameState = MoonHeader.GameState.SettingAttackPath;

		Invoke("TabPlayerGenerator", 3f);
    }

	private void TabPlayerGenerator() 
	{
        for (int i = 0; i < players.Length; i++)
        {
            GameObject dummy_UI = PoolManager.Instance.Get_UI(1);
            dummy_UI.transform.parent = tabUI.transform;
            dummy_UI.GetComponent<RectTransform>().anchoredPosition = new Vector3(0, 80 - 130 * i, 0);
            dummy_UI.GetComponent<LSM_TabUI>().Setting(players[i].PlayerType, players[i].playerName, players[i].gameObject);
        }
        tabUI.SetActive(false);
    }

	// 시작하기 전 약간의 로딩시간.
	protected IEnumerator StartProcessing() {
        loadingUI.GetComponentInChildren<TextMeshProUGUI>().text = "로딩 중...";
        yield return new WaitForSeconds(1f);
        Start_function();
        ReadyToStart_LoadingGauge = PoolManager.Instance.minions.Length * PoolManager.Instance.ReadyToStart_SpawnNum
			+ PoolManager.Instance.particles.Length * PoolManager.Instance.ReadyToStart_SpawnNum_Particles
			+ PoolManager.Instance.Items.Length * PoolManager.Instance.ReadyToStart_SpawnNum_Item
			+ PoolManager.Instance.minions.Length - 1
			+ PoolManager.Instance.particles.Length - 1
			+ PoolManager.Instance.Items.Length - 1;
		LoadingGauge = 0;
		//yield return new WaitForSeconds(3f);
		yield return StartCoroutine(PoolManager.Instance.ReadyToStart_Spawn());
		yield return StartCoroutine(CheckingPlayerReady());
        loadingUI.SetActive(false);
		onceStart = true;
		for (int i = 0; i < wayPoints.Length; i++)
		{
			LSM_TurretSc dummy_sc = wayPoints[i].GetComponentInChildren<LSM_TurretSc>();
			if (!ReferenceEquals(dummy_sc, null))
			{
				dummy_sc.ResetTurret();
			}
		}
	}

	// 로딩 관련 함수 모음.
	#region For Loading Function
	// 로딩 애니메이션. 다른 플레이어가 준비될때까지 잠시 기다리는 함수.
	protected IEnumerator CheckingPlayerReady() {
		float timer_Loading_Dummy = 0;
		string dummy_for_loading_Message = "";
		TextMeshProUGUI LoadingUI_Text =loadingUI.GetComponentInChildren<TextMeshProUGUI>();

        while (true)
		{
			yield return new WaitForSeconds(Time.deltaTime);
			timer_Loading_Dummy+= Time.deltaTime;

			if (timer_Loading_Dummy >= 4) timer_Loading_Dummy = 0;
			else if (timer_Loading_Dummy >= 3) dummy_for_loading_Message = "다른 플레이어를 기다리는 중...";
			else if (timer_Loading_Dummy >= 2) dummy_for_loading_Message = "다른 플레이어를 기다리는 중..";
			else if (timer_Loading_Dummy >= 1) dummy_for_loading_Message = "다른 플레이어를 기다리는 중.";
			else dummy_for_loading_Message = "다른 플레이어를 기다리는 중";

			LoadingUI_Text.text = dummy_for_loading_Message;

			if (ReadyToStart_Player >= players.Length)
			{ break; }
		}
	}

	public void LoadingUpdate()
	{
		photonView.RPC("LoadingUpdate_RPC", RpcTarget.AllBuffered);
	}

	public void LoadingTxtUpdate(string update_) { photonView.RPC("LTU", RpcTarget.All, update_); }
	[PunRPC] private void LTU(string u) { loadingUI.GetComponentInChildren<TextMeshProUGUI>().text = u; }

	[PunRPC]public void LoadingUpdate_RPC()
	{
		LoadingGauge++;
		LoadingImage.fillAmount = (float)LoadingGauge / ReadyToStart_LoadingGauge;
	}
	public void PlayerReady() { photonView.RPC("PlayerReady_RPC", RpcTarget.AllBuffered); }
	[PunRPC] public void PlayerReady_RPC() { ReadyToStart_Player++; }
    #endregion


    #region ForDebugging~~
	private void Debugging_Setting()
	{
		PoolManager.Instance.ReadyToStart_SpawnNum = 0;
		PoolManager.Instance.ReadyToStart_SpawnNum_Particles = 0;
		PoolManager.Instance.ReadyToStart_SpawnNum_Item = 0;
		MaxPlayerNum = 1;
	}
    #endregion

    private void Update()
	{
		//if (Input.GetKeyDown(KeyCode.O) && !onceStart)
		//{ Connect(); if (debugging_bool) { Debugging_Setting(); } }

		if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
		{
			
			if (!starting_ && PhotonNetwork.CurrentRoom.PlayerCount == MaxPlayerNum)
			{

				starting_ = true;
				if (PhotonNetwork.IsMasterClient)
					PhotonNetwork.CurrentRoom.IsOpen = false;
                preMasterClientID = PhotonNetwork.MasterClient.ActorNumber;
                StartCoroutine(StartProcessing());
			}
            if(starting_)
            {
                if (preMasterClientID != PhotonNetwork.MasterClient.ActorNumber)
                    Debug.Log("MasterClient Switched!!");
            }


            if (onceStart)
			{
				if (gameState != MoonHeader.GameState.Ending)
					timer_AllRunning += Time.deltaTime;
                Game();
                DisplayEnable();
                PingCalculator();
            }
		}
	}

	private void PingCalculator()
	{
		float dummy_ping = 0;
		foreach(LSM_PlayerCtrl item in players)
		{
			if (item.isMainPlayer)
				continue;
			else
			{
				dummy_ping += item.this_player_ping;
			}
		}
		ping = Mathf.Ceil(dummy_ping / (players.Length - 1) * 1000);
		pingText.text = ping.ToString() + " ms";
	}

	public void SkipClick()
	{
		if (isClickSkip)
			return;
		isClickSkip = true;
		numOfSkipClickedPlayer += 1;
		photonView.RPC("SkipNumSynch", RpcTarget.All, numOfSkipClickedPlayer);
		//selectAttackPathUI.GetComponentInChildren<Button>().gameObject.SetActive(false);
		selectAttackPathUI.GetComponentInChildren<Button>().interactable = false ;
        selectAttackPathUI.transform.Find("WaitingPannel").gameObject.SetActive(true);
		spawnPoints[(int)mainPlayer.player.team].GetComponent<LSM_Spawner>().SliderDisable();
    }
	[PunRPC] private void SkipNumSynch(int num) { numOfSkipClickedPlayer = num; }

	// 게임 진행 중 모든 상황을 처리하는 함수.
	// 보통 처음 시작, 혹은 처리 완료일 경우에 실행됨.
	private void Game()
	{
		if (!PhotonNetwork.IsMasterClient)
			return;
		// 게임매니저의 상태가 현재 Ready일경우.
		if (state == MoonHeader.ManagerState.Ready)
		{
			switch (gameState)  // 게임의 현재 상태에 따라 처리.
			{
				// 플레이어가 각각 공격로를 설정하는 턴
				case MoonHeader.GameState.SettingAttackPath:

					state = MoonHeader.ManagerState.Processing;
					timerSc.TimerStart(SELECTATTACKPATHTIME);   // 게임매니저에 설정된 공격로 설정 시간만큼 타이머 시작.
					photonView.RPC("SettingAttackPathReady_RPC", RpcTarget.AllBuffered);
					numOfSkipClickedPlayer = 0;
					break;

				// 공격로 지정이 모두 완료 후 게임을 시작하기 전 카운트 다운
				case MoonHeader.GameState.StartGame:
					timerSc.TimerStart(3.5f, true);             //타이머 세팅. 3.5초의 설정 시간.
					state = MoonHeader.ManagerState.Processing;
					photonView.RPC("StartGameReady_RPC", RpcTarget.AllBuffered);
					break;

				// 현재 게임을 시작
				case MoonHeader.GameState.Gaming:

					state = MoonHeader.ManagerState.Processing;
					timerSc.TimerStart(ROUNDTIME);  // 게임매니저에 설정된 게임 진행 시간만큼 타이머 시작.
													//MapCam.SetActive(false); MainCam.SetActive(true);
					photonView.RPC("GamingReady_RPC", RpcTarget.AllBuffered);
					break;
			}
		}
		// 게임매니저으 상태가 Processing(진행 중)일 경우
		else if (state == MoonHeader.ManagerState.Processing)
		{
			switch (gameState)
			{
				case MoonHeader.GameState.SettingAttackPath:
					if (numOfSkipClickedPlayer >= numOfPlayer)
					{
						timerSc.TimerOut();
					}
					break;
				case MoonHeader.GameState.Gaming:
					timer_inGameTurn += Time.deltaTime;
					break;
			}
		}
		// 게임매니저의 상태가 End(처리완료)상태일 경우.
		else if (state == MoonHeader.ManagerState.End)
		{
			switch (gameState)  // 게임의 현재 상태에 따라 처리.
			{
				// 공격로 선택의 시간이 종료되었다면, 약간의 시간이 흐른 후 시작되도록 설정.
				case MoonHeader.GameState.SettingAttackPath:

					state = MoonHeader.ManagerState.Ready;
					gameState = MoonHeader.GameState.StartGame;
					photonView.RPC("SettingAttackPathEnd_RPC", RpcTarget.AllBuffered);
					break;
				// 게임 턴이 종료되었다면.
				case MoonHeader.GameState.Gaming:


					ChangeRound_AllRemover();

					state = MoonHeader.ManagerState.Ready;
					gameState = MoonHeader.GameState.SettingAttackPath;
					photonView.RPC("GamingEnd_RPC", RpcTarget.AllBuffered);
					break;
			}
		}
	}

	// 게임매니저 RPC 처리.
    #region Gamemanager Process
    [PunRPC]private void SettingAttackPathReady_RPC()
	{
		gameState = MoonHeader.GameState.SettingAttackPath;
        state = MoonHeader.ManagerState.Processing;
        StartCoroutine(ScreenFade(true));
        SettingAttack();        // 스포너의 상태를 변경.
        SettingTurnText();      // 턴 상태 UI를 변경
		shopUI.transform.SetAsLastSibling();
        selectAttackPathUI.SetActive(true);
        isClickSkip = false;
		numOfSkipClickedPlayer = 0;
        selectAttackPathUI.GetComponentInChildren<Button>().interactable = true;
        //selectAttackPathUI.GetComponentInChildren<Button>().gameObject.SetActive(true);
        selectAttackPathUI.transform.Find("WaitingPannel").gameObject.SetActive(false);
        foreach (LSM_CreepCtrl item in creeps) { item.ResetCreep(); }
		ScreenFade(true);
    }
	[PunRPC]private void StartGameReady_RPC()
	{
        state = MoonHeader.ManagerState.Processing;
        SettingTurnText();      // 턴 상태 UI를 변경.
        foreach (GameObject s in spawnPoints)   // 모든 마스터 스포너에게 턴이 변경됐음을 알림.
        { s.GetComponent<LSM_Spawner>().ChangeTurn(); }
        ScreenFade(true);
    }
	[PunRPC]private void GamingReady_RPC()
	{
        state = MoonHeader.ManagerState.Processing;
        SettingTurnText();      // 턴 상태 UI를 변경.
        foreach (GameObject s in spawnPoints)   // 모든 마스터 스포너의 상태를 변경.
        { s.GetComponent<LSM_Spawner>().state = MoonHeader.SpawnerState.Spawn; }
    }

	[PunRPC]private void SettingAttackPathEnd_RPC()
	{
        state = MoonHeader.ManagerState.Ready;
        gameState = MoonHeader.GameState.StartGame;
        foreach (GameObject s in spawnPoints)       // 모든 마스터 스포너에게 현재 최대 설정 가능한 포인트 만큼 설정하였는지 확인하는 함수 실행.
        { s.GetComponent<LSM_Spawner>().CheckingSelectMon(); }
        selectAttackPathUI.SetActive(false);

    }
	[PunRPC]private void GamingEnd_RPC()
	{
        state = MoonHeader.ManagerState.Ready;
        gameState = MoonHeader.GameState.SettingAttackPath;
        ScreenFade(false);
        StartCoroutine(mainPlayer.AttackPathSelectSetting());
        Cursor.lockState = CursorLockMode.None;

    }
    #endregion

    // 현재 턴이 공격로 선택 시간이므로, 모든 마스터스포너에게 공격로 설정을 할 때 사용하는 함수.
    private void SettingAttack()
	{
		if (MoonHeader.GameState.SettingAttackPath != gameState) return;

		// 모든 스포너를 찾아, 스포너의 현재 상태를 공격로 설정으로 바꾼다.
		foreach (GameObject s in spawnPoints)
		{
			s.GetComponent<LSM_Spawner>().state = MoonHeader.SpawnerState.Setting;
			s.GetComponent<LSM_Spawner>().ChangeTurn();
		}
	}

	// 타임아웃. 설정해두었던 타이머가 끝났을 경우.
	public void TimeOutProcess()
	{
		if (gameState == MoonHeader.GameState.SettingAttackPath)	//공격로 설정 턴일 경우. 타이머가 종료되었을 경우.
			state = MoonHeader.ManagerState.End;

		else if (gameState == MoonHeader.GameState.StartGame && state == MoonHeader.ManagerState.Processing)	// 게임 시작 전 상태에서 타이머가 종료되었을 경우.
		{ gameState = MoonHeader.GameState.Gaming; state = MoonHeader.ManagerState.Ready; }						// 게임의 상태를 게임 중으로 변경. 게임매니저의 상태를 준비중으로 변경.

		else if (gameState == MoonHeader.GameState.Gaming && state == MoonHeader.ManagerState.Processing)		// 게임 도중에 타이머가 종료되었을 경우.
		{state = MoonHeader.ManagerState.End; }			// 게임의 상태를 공격로 설정으로 변경. 게임매니저의 상태를 준비중으로 변경.
	}

	// 현재 게임의 상태를 나타내는 UI의 텍스트를 변경. 해당 함수는 디버그용으로 간략하게 구현.
	private void SettingTurnText()
	{
		switch (gameState)
		{
			case MoonHeader.GameState.SettingAttackPath:
				turnText.text = "전략 설정";
				break;
			case MoonHeader.GameState.StartGame:
				turnText.text = "준비..";
				break;
			case MoonHeader.GameState.Gaming:
				turnText.text = "게임 중";
				break;
		}
	}

	// 스크린 페이드 인/아웃을 구현하는 함수.
	// 매개변수가 true일 경우 FadeIn (점점 밝아짐.)
	// 매개변수가 false일 경우 FadeOut (점점 어두워짐.)
	public IEnumerator ScreenFade(bool inout)
	{
		if ((inout && screen.color.a >= 0.9f) || (!inout && screen.color.a <= 0.1f))
		{
			int time = 50;
			float origin = inout ? 1 : 0;//, alpha = ((float)1 / time ) * (inout ? -1 : 1);

			screen.color = new Color(0, 0, 0, origin);
			for (int i = 0; i < ((float)time / 100) / Time.deltaTime; i++)
			{
				yield return new WaitForSeconds(Time.deltaTime);
				float plustAlpha = Time.deltaTime * (inout ? -1 : 1) * 2;
				origin += plustAlpha;
				screen.color = new Color(0, 0, 0, origin);
			}
			screen.color = new Color(0, 0, 0, (inout ? 0 : 1));
		}
		else
		{ yield return new WaitForSeconds(1); screen.color = new Color(0, 0, 0, (inout ? 0 : 1)); }
	}

	// 게임매니저에 저장되어있는 플레이어 미니언을 삭제하는 함수.
	// 보통 플레이어의 미니언이 게임 도중 죽었을 경우 사용되는 함수.
	public void PlayerMinionRemover(MoonHeader.Team t, string nam)
	{
		photonView.RPC("PlayerMinionRemover_RPC",RpcTarget.All, (int)t, nam);
	}
	[PunRPC]private void PlayerMinionRemover_RPC(int t, string n)
	{
		for (int i = 0; i < playerMinions[t].Count; i++)
		{
			if (playerMinions[t][i].name.Equals(n))
			{
				playerMinions[t].Remove(playerMinions[t][i]);
			}
		}
	}

	// 게임 매니저에 저장되어있는 플레이어 미니언들을 전부 파괴하는 함수.
	// 보통 게임 라운드가 변경되었을 경우 사용.
	private void ChangeRound_AllRemover()
	{
		for (int i = 0; i < playerMinions.Length; i++)
		{
			foreach (GameObject obj in playerMinions[i])
			{
				obj.GetComponent<I_Playable>().MinionDisable();
			}
		}

		photonView.RPC("ChangeRound_AllRemover_RPC", RpcTarget.All);

		short[] dummy_exp_sum = new short[teamManagers.Length];

		for (int i = 0; i < PoolManager.Instance.minions.Length; i++) {
			foreach (GameObject minion in PoolManager.Instance.poolList_Minion[i])
			{
				if (minion.activeSelf)
				{
					LSM_MinionCtrl dummyCtrl = minion.GetComponent<LSM_MinionCtrl>();
					dummy_exp_sum[(int)dummyCtrl.stats.actorHealth.team] += dummyCtrl.stats.exp;
					//teamManagers[(int)dummyCtrl.stats.actorHealth.team].exp += dummyCtrl.stats.exp;
					//minion.SetActive(false);
					dummyCtrl.MinionDisable();
				}
			}
		}
		for (int i = 0; i < dummy_exp_sum.Length; i++)
		{
			DisplayAdd(string.Format("{0} 팀이 {1} 경험치를 얻었습니다.", ((MoonHeader.Team)i).ToString(), dummy_exp_sum[i]));
			teamManagers[i].ExpAdding(dummy_exp_sum[i]);
		}


		for (int i = 0; i < wayPoints.Length; i++)
		{
			LSM_TurretSc dummy_sc = wayPoints[i].GetComponentInChildren<LSM_TurretSc>();
			if (!ReferenceEquals(dummy_sc, null))
			{
				dummy_sc.ResetTurret();
			}
		}
	}
	[PunRPC]private void ChangeRound_AllRemover_RPC() { playerMinions[0].Clear(); playerMinions[1].Clear(); mainPlayer.ReChargingEnerge(); }


	// log를 최대 5개 표시하게 관리하기 위한 함수.
	private void DisplayEnable()
	{
		timer_log += Time.deltaTime;
		if (logUIs.Count < 3 && logUIs_Reservation.Count > 0 && timer_log >= 1f)
		{
			timer_log = 0;
			GameObject dummy = PoolManager.Instance.Get_UI(0);
			dummy.GetComponentInChildren<TextMeshProUGUI>().text = logUIs_Reservation[0];
			dummy.GetComponent<RectTransform>().anchoredPosition = new Vector3(-205, -300, 0);
			logUIs.Add(dummy);
			logUIs_Reservation.RemoveAt(0);
		}
		
	}
	public void DisplayAdd(string content)
	{
		photonView.RPC("DisplayAdd_RPC", RpcTarget.All, content);
	}

	[PunRPC]public void DisplayAdd_RPC(string content)
	{
		logUIs_Reservation.Add(content);
		Debug.Log(content);
	}
	public void DisplayChecking() {
		for (int i = logUIs.Count-1; i >= 0; i--)
		{
			if (!logUIs[i].activeSelf)
				logUIs.RemoveAt(i);
		}
	}

	// 게임이 종료되었을 경우 실행.
	// 매개변수로 받는 팀은 패배 팀.
	public void GameEndingProcess(MoonHeader.Team t)
	{
		photonView.RPC("GameEP_RPC", RpcTarget.AllBuffered, (int)t);
		/*
		ScreenFade(false);
		gameState = MoonHeader.GameState.Ending;
		StartCoroutine(mainPlayer.AttackPathSelectSetting());
		Cursor.lockState = CursorLockMode.None;
		Debug.Log("Team " +t.ToString() + " Lose");
		*/
	}
	[PunRPC] private void GameEP_RPC(int t) 
	{
        ScreenFade(false);
        gameState = MoonHeader.GameState.Ending;
		StartCoroutine(EndGame(t));
    }
	private IEnumerator EndGame(int t)
	{
        
        yield return StartCoroutine(mainPlayer.AttackPathSelectSetting());
        endingUI.SetActive(true);
        endingUI.GetComponent<LSM_UI_Result>().Setting((MoonHeader.Team)t != mainPlayer.player.team);
        Cursor.lockState = CursorLockMode.None;
        Debug.Log("Team " + ((MoonHeader.Team)t).ToString() + " Lose");
		yield return new WaitForSeconds(3f);
		ScreenFade(true);
    }

    public void GoToLobby() { StartCoroutine(LobbyAnim()); }
    private IEnumerator LobbyAnim()
    {
        yield return StartCoroutine(GameManager.Instance.ScreenFade(false));
        SceneManager.LoadScene(1);
        PhotonNetwork.Disconnect();
    }
}
