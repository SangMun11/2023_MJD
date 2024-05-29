using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;


//Ż�� ����, ���������� ������ ���.

// ��ü ���ӿ� ���� �Ŵ���.
// LSM��� ��ũ��Ʈ���� ��Ϲ��� �������� ������.. �Ӿ�
public class GameManager : MonoBehaviourPunCallbacks,IPunObservable
{
	// �̱���///
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
	// SEARCHATTACKPATHTIME: ���ݷ� ���� �ð�. ROUNDTIME: ���� ���� �ð�.

	[Header("GameManager States")]
	public MoonHeader.ManagerState state;		// ���� ���ӸŴ����� ����. --> ���ӸŴ����� ���� � �������� ex: �غ���, ó����, ó���Ϸ�
	public MoonHeader.GameState gameState;      // ���� ������ ���� ex: ���ݷ� ���� �ð�, ���� ���� ��, ���
												// # ���� ���� �� gameState�� SettingAttackPath�� ���� -> ������
	[Header("Involved Photon")]
	public int numOfPlayer;
	public int numOfSkipClickedPlayer;             // ���� �÷��̾��� ��.
    public int MaxPlayerNum;

    [Header("For UI Control")]
    public TextMeshProUGUI turnText;    // ���� ���� ������ ���Ͽ� ����ڿ��� �����ִ� UI. �Ŀ� �ٲ� ����.
                                        // # �ش� ������ �ν��Ͻ����� ���� �����������. Canvas ���� �ִ� Turn Object�� ����.
    public GameObject canvas;						// ���� �����ϴ� ĵ����. �ϳ��� �ִٰ� �����Ͽ� Awake���� ã�� ����.
	public GameObject selectAttackPathUI, mapUI, gameUI, loadingUI, tabUI;    // selectAttackPathUI: ���ݷ� ���� �� ����ڿ��� �����ִ� UI���� ����� ������Ʈ.  mapUI: TopView ������ �� ����ڿ��� �����ִ� UI���� ����� ������Ʈ.
															// gameUI: ���� ���� �� ǥ�õǴ� UI
	public LSM_GameUI gameUI_SC;

	public Image deadScreen;
    private Image screen;                           // ���̵� IN, OUT�� �� �� ����ϴ� �̹���.
    private List<GameObject> logUIs;
    public int LoadingGauge, ReadyToStart_Player;
    public int ReadyToStart_LoadingGauge;
    public Image LoadingImage;
    public TextMeshProUGUI pingText;

    [Header("For Games")]
    public GameObject[] spawnPoints;    // ���� �����ϴ� "������ ������"�� ����.
    public GameObject[] wayPoints;      // ���� �����ϴ� ��� "��������Ʈ"�� ����

    public LSM_TimerSc timerSc;         // Ÿ�̸� ��ũ��Ʈ. ���� ���� �� Ÿ�̸Ӱ� �ʿ���(ex: ���� ���ݷ� �����ð�, ���� ����ð�) ��� ����ϴ� ��ũ��Ʈ.
    public LSM_PlayerCtrl[] players;				// ��� �÷��̾���� �����ϴ� �迭
	public LSM_PlayerCtrl mainPlayer;               // ���� �����ϰ��ִ� �÷��̾ �����ϴ� ����
	public String mainPlayerName;
	public byte mainPlayerSelectNum;
	public float timer_AllRunning, timer_inGameTurn;
	public GameObject shopUI, endingUI, errorUI;

	public LSM_CreepCtrl[] creeps;

	public TeamManager[] teamManagers;				// ��� ���� ���Ŵ���

	public List<GameObject>[] playerMinions;        // ��� �÷��̾���� �̴Ͼ��� ����. �ش� �κ� ���� PoolManager���� ������� ��� ��..
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
	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) // �Ǵ� �� �����ѵ� ������ ����Ǵ����� Ȯ���ϱ� ���� 
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
		loadingUI.GetComponentInChildren<TextMeshProUGUI>().text = "���� �Ϸ�!!";
		//if(!PhotonNetwork.IsMasterClient)
        //{
		mainPlayer=PhotonNetwork.Instantiate("Playerobj", this.transform.position, this.transform.rotation).GetComponent<LSM_PlayerCtrl>();     // LSM -> ������ Ŭ���̾�Ʈ ���� ���ö� �÷��̾� ������Ʈ ��ȯ.
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
	// �÷��̾ �����ٸ� ����...
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




    // �̱������� ���� Awake�� ���� ��ġ�Ͽ��⿡ �̰��� �Ʒ��� �Լ��� ���.
    private void Awake_Function()
	{
		// ���ӸŴ����� �����ϴ� TimerSC�� �޾ƿ�.
		timerSc = this.GetComponent<LSM_TimerSc>();

		logUIs = new List<GameObject>();                // �α� ǥ�ÿ� ���Ͽ�. 5�� ���ϸ� ǥ���Ϸ��� �ش� ����Ʈ�� ����.
		logUIs_Reservation = new List<string>();        // �ټ����̻� ���ʹ� ���� ����Ʈ�� �����Ͽ� �� ����Ʈ �ȿ� ����.
		timer_log = 0;

		mainPlayerName = (PlayerPrefs.HasKey("PlayerLocalName") ? PlayerPrefs.GetString("PlayerLocalName") : "UnknownPlayer");
		mainPlayerSelectNum = (byte)(PlayerPrefs.HasKey("PlayerSelectType") ? PlayerPrefs.GetInt("PlayerSelectType") : 0);


		// �ش� ������ �´� ���� ������Ʈ���� ����.
		spawnPoints = GameObject.FindGameObjectsWithTag("Spawner");
		canvas = GameObject.Find("Canvas");
		selectAttackPathUI = GameObject.Find("AttackPathUIs");
		selectAttackPathUI.GetComponentInChildren<Button>().onClick.AddListener(SkipClick);		// ��ŵ��ư�� �ش�. Ŭ�� �� TimerSC�� �����ϴ� TimerOut�Լ��� ����ǵ��� ����.

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
		screen.transform.SetAsLastSibling();    // ��ũ���� �ٸ� UI�� �������� ���� �������� ��ġ�ϴ� �ڵ�.
        loadingUI = GameObject.Find("Loading");
		loadingUI.transform.SetAsLastSibling();
		LoadingImage = GameObject.Find("LoadingBar").GetComponent<Image>();
        GameObject[] teammdummy = GameObject.FindGameObjectsWithTag("TeamManager");
		teamManagers = new TeamManager[teammdummy.Length];
		foreach (GameObject t in teammdummy)
		{ teamManagers[(int)t.GetComponent<TeamManager>().team] = t.GetComponent<TeamManager>(); }
		starting_ = false;

        loadingUI.GetComponentInChildren<TextMeshProUGUI>().text = "���� ã�� ��...";
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
		// ��� �÷��̾���� �����ϴ� ��. FindGameObjectsWithTag�� ����Ͽ� ������Ʈ�� ã��, �ش� ��ũ��Ʈ�� �����ϰ� ����.
		// �� �κ� ������ �÷��̾ ��ȯ�ϴ� ������ �ʿ�!, ���� �÷��̾���� �ʿ�.


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
				players[i].SettingTeamAndName(i % 2);              // �� ������.
			}

		}

		GameObject[] creepDummys = GameObject.FindGameObjectsWithTag("Creep");
		creeps = new LSM_CreepCtrl[creepDummys.Length];
		for (int i = 0; i < creepDummys.Length; i++) 
		{ creeps[i] = creepDummys[i].GetComponent<LSM_CreepCtrl>(); }

		// ���� ���ӸŴ����� ���� �ʱ�ȭ. Default�� Ready.
		state = MoonHeader.ManagerState.Ready;

		// �÷��̾� �̴Ͼ��� ����Ʈ ����.
		playerMinions = new List<GameObject>[2];    // ���� ������ŭ �迭�� ũ�⸦ ����. ���� ���������� 2�� ����.
		for (int i = 0; i < 2; i++) { playerMinions[i] = new List<GameObject>(); }


		// ���������� �÷��̾ �Ѹ����� ����.
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

	// �����ϱ� �� �ణ�� �ε��ð�.
	protected IEnumerator StartProcessing() {
        loadingUI.GetComponentInChildren<TextMeshProUGUI>().text = "�ε� ��...";
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

	// �ε� ���� �Լ� ����.
	#region For Loading Function
	// �ε� �ִϸ��̼�. �ٸ� �÷��̾ �غ�ɶ����� ��� ��ٸ��� �Լ�.
	protected IEnumerator CheckingPlayerReady() {
		float timer_Loading_Dummy = 0;
		string dummy_for_loading_Message = "";
		TextMeshProUGUI LoadingUI_Text =loadingUI.GetComponentInChildren<TextMeshProUGUI>();

        while (true)
		{
			yield return new WaitForSeconds(Time.deltaTime);
			timer_Loading_Dummy+= Time.deltaTime;

			if (timer_Loading_Dummy >= 4) timer_Loading_Dummy = 0;
			else if (timer_Loading_Dummy >= 3) dummy_for_loading_Message = "�ٸ� �÷��̾ ��ٸ��� ��...";
			else if (timer_Loading_Dummy >= 2) dummy_for_loading_Message = "�ٸ� �÷��̾ ��ٸ��� ��..";
			else if (timer_Loading_Dummy >= 1) dummy_for_loading_Message = "�ٸ� �÷��̾ ��ٸ��� ��.";
			else dummy_for_loading_Message = "�ٸ� �÷��̾ ��ٸ��� ��";

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

	// ���� ���� �� ��� ��Ȳ�� ó���ϴ� �Լ�.
	// ���� ó�� ����, Ȥ�� ó�� �Ϸ��� ��쿡 �����.
	private void Game()
	{
		if (!PhotonNetwork.IsMasterClient)
			return;
		// ���ӸŴ����� ���°� ���� Ready�ϰ��.
		if (state == MoonHeader.ManagerState.Ready)
		{
			switch (gameState)  // ������ ���� ���¿� ���� ó��.
			{
				// �÷��̾ ���� ���ݷθ� �����ϴ� ��
				case MoonHeader.GameState.SettingAttackPath:

					state = MoonHeader.ManagerState.Processing;
					timerSc.TimerStart(SELECTATTACKPATHTIME);   // ���ӸŴ����� ������ ���ݷ� ���� �ð���ŭ Ÿ�̸� ����.
					photonView.RPC("SettingAttackPathReady_RPC", RpcTarget.AllBuffered);
					numOfSkipClickedPlayer = 0;
					break;

				// ���ݷ� ������ ��� �Ϸ� �� ������ �����ϱ� �� ī��Ʈ �ٿ�
				case MoonHeader.GameState.StartGame:
					timerSc.TimerStart(3.5f, true);             //Ÿ�̸� ����. 3.5���� ���� �ð�.
					state = MoonHeader.ManagerState.Processing;
					photonView.RPC("StartGameReady_RPC", RpcTarget.AllBuffered);
					break;

				// ���� ������ ����
				case MoonHeader.GameState.Gaming:

					state = MoonHeader.ManagerState.Processing;
					timerSc.TimerStart(ROUNDTIME);  // ���ӸŴ����� ������ ���� ���� �ð���ŭ Ÿ�̸� ����.
													//MapCam.SetActive(false); MainCam.SetActive(true);
					photonView.RPC("GamingReady_RPC", RpcTarget.AllBuffered);
					break;
			}
		}
		// ���ӸŴ����� ���°� Processing(���� ��)�� ���
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
		// ���ӸŴ����� ���°� End(ó���Ϸ�)������ ���.
		else if (state == MoonHeader.ManagerState.End)
		{
			switch (gameState)  // ������ ���� ���¿� ���� ó��.
			{
				// ���ݷ� ������ �ð��� ����Ǿ��ٸ�, �ణ�� �ð��� �帥 �� ���۵ǵ��� ����.
				case MoonHeader.GameState.SettingAttackPath:

					state = MoonHeader.ManagerState.Ready;
					gameState = MoonHeader.GameState.StartGame;
					photonView.RPC("SettingAttackPathEnd_RPC", RpcTarget.AllBuffered);
					break;
				// ���� ���� ����Ǿ��ٸ�.
				case MoonHeader.GameState.Gaming:


					ChangeRound_AllRemover();

					state = MoonHeader.ManagerState.Ready;
					gameState = MoonHeader.GameState.SettingAttackPath;
					photonView.RPC("GamingEnd_RPC", RpcTarget.AllBuffered);
					break;
			}
		}
	}

	// ���ӸŴ��� RPC ó��.
    #region Gamemanager Process
    [PunRPC]private void SettingAttackPathReady_RPC()
	{
		gameState = MoonHeader.GameState.SettingAttackPath;
        state = MoonHeader.ManagerState.Processing;
        StartCoroutine(ScreenFade(true));
        SettingAttack();        // �������� ���¸� ����.
        SettingTurnText();      // �� ���� UI�� ����
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
        SettingTurnText();      // �� ���� UI�� ����.
        foreach (GameObject s in spawnPoints)   // ��� ������ �����ʿ��� ���� ��������� �˸�.
        { s.GetComponent<LSM_Spawner>().ChangeTurn(); }
        ScreenFade(true);
    }
	[PunRPC]private void GamingReady_RPC()
	{
        state = MoonHeader.ManagerState.Processing;
        SettingTurnText();      // �� ���� UI�� ����.
        foreach (GameObject s in spawnPoints)   // ��� ������ �������� ���¸� ����.
        { s.GetComponent<LSM_Spawner>().state = MoonHeader.SpawnerState.Spawn; }
    }

	[PunRPC]private void SettingAttackPathEnd_RPC()
	{
        state = MoonHeader.ManagerState.Ready;
        gameState = MoonHeader.GameState.StartGame;
        foreach (GameObject s in spawnPoints)       // ��� ������ �����ʿ��� ���� �ִ� ���� ������ ����Ʈ ��ŭ �����Ͽ����� Ȯ���ϴ� �Լ� ����.
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

    // ���� ���� ���ݷ� ���� �ð��̹Ƿ�, ��� �����ͽ����ʿ��� ���ݷ� ������ �� �� ����ϴ� �Լ�.
    private void SettingAttack()
	{
		if (MoonHeader.GameState.SettingAttackPath != gameState) return;

		// ��� �����ʸ� ã��, �������� ���� ���¸� ���ݷ� �������� �ٲ۴�.
		foreach (GameObject s in spawnPoints)
		{
			s.GetComponent<LSM_Spawner>().state = MoonHeader.SpawnerState.Setting;
			s.GetComponent<LSM_Spawner>().ChangeTurn();
		}
	}

	// Ÿ�Ӿƿ�. �����صξ��� Ÿ�̸Ӱ� ������ ���.
	public void TimeOutProcess()
	{
		if (gameState == MoonHeader.GameState.SettingAttackPath)	//���ݷ� ���� ���� ���. Ÿ�̸Ӱ� ����Ǿ��� ���.
			state = MoonHeader.ManagerState.End;

		else if (gameState == MoonHeader.GameState.StartGame && state == MoonHeader.ManagerState.Processing)	// ���� ���� �� ���¿��� Ÿ�̸Ӱ� ����Ǿ��� ���.
		{ gameState = MoonHeader.GameState.Gaming; state = MoonHeader.ManagerState.Ready; }						// ������ ���¸� ���� ������ ����. ���ӸŴ����� ���¸� �غ������� ����.

		else if (gameState == MoonHeader.GameState.Gaming && state == MoonHeader.ManagerState.Processing)		// ���� ���߿� Ÿ�̸Ӱ� ����Ǿ��� ���.
		{state = MoonHeader.ManagerState.End; }			// ������ ���¸� ���ݷ� �������� ����. ���ӸŴ����� ���¸� �غ������� ����.
	}

	// ���� ������ ���¸� ��Ÿ���� UI�� �ؽ�Ʈ�� ����. �ش� �Լ��� ����׿����� �����ϰ� ����.
	private void SettingTurnText()
	{
		switch (gameState)
		{
			case MoonHeader.GameState.SettingAttackPath:
				turnText.text = "���� ����";
				break;
			case MoonHeader.GameState.StartGame:
				turnText.text = "�غ�..";
				break;
			case MoonHeader.GameState.Gaming:
				turnText.text = "���� ��";
				break;
		}
	}

	// ��ũ�� ���̵� ��/�ƿ��� �����ϴ� �Լ�.
	// �Ű������� true�� ��� FadeIn (���� �����.)
	// �Ű������� false�� ��� FadeOut (���� ��ο���.)
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

	// ���ӸŴ����� ����Ǿ��ִ� �÷��̾� �̴Ͼ��� �����ϴ� �Լ�.
	// ���� �÷��̾��� �̴Ͼ��� ���� ���� �׾��� ��� ���Ǵ� �Լ�.
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

	// ���� �Ŵ����� ����Ǿ��ִ� �÷��̾� �̴Ͼ���� ���� �ı��ϴ� �Լ�.
	// ���� ���� ���尡 ����Ǿ��� ��� ���.
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
			DisplayAdd(string.Format("{0} ���� {1} ����ġ�� ������ϴ�.", ((MoonHeader.Team)i).ToString(), dummy_exp_sum[i]));
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


	// log�� �ִ� 5�� ǥ���ϰ� �����ϱ� ���� �Լ�.
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

	// ������ ����Ǿ��� ��� ����.
	// �Ű������� �޴� ���� �й� ��.
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
