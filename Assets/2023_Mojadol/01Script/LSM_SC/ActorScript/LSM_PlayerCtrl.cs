using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Photon.Pun;
using static MoonHeader;
using UnityEngine.EventSystems;
using System.IO;
using System.Runtime.InteropServices.ComTypes;

// 플레이어 스크립트
// TopView에서의 플레이어 컨트롤
public class LSM_PlayerCtrl : MonoBehaviourPunCallbacks, IPunObservable
{
    public bool isMainPlayer;               // 현재 게임중인 플레이어인지 확인.
    public string playerName;               // 멀티에서의 플레이어 이름
                                            // # 이름 설정
    public MoonHeader.S_PlayerState player;   // 플레이어 상태에 대한 구조체
    const float MapCamBaseSize = 60;        // TopView 카메라의 OrthogonalSize
    float canMapCamSize;

    public GameObject mySpawner;            // 팀의 마스터 스포너


    // TopView에서의 이동속도 초기화
    private float wheelSpeed = 15f;
    private float map_move = 90f;
    private float timer_Death, deathPenalty;
    private bool death;
    private bool is_zoomIn;                 // 선택한 미니언에게 확대하고 있는지
    private IEnumerator zoomIn;             // StopCorutine을 사용하기위해 미리 선언.
    [HideInInspector] public Camera mapCamCamera;            // TopView에 사용되는 카메라
    [HideInInspector] public GameObject MainCam, MapCam, MapSubCam, MiniMapCam;       // 플레이어 오브젝트 내에 존재하는 카메라들.

    public Vector3 mapCamBasePosition;                  // TopView카메라의 초기위치
                                                        // # Y축만 95로 설정
    [HideInInspector] public GameObject minionStatsPannel, minionStatsPannel_SelectButton;                // 플레이어가 선택한 미니언의 스탯을 표기해주는 UI
                                                                                                          // # Canvas의 자식 오브젝트 중 MinionStatpanel
    private LSM_MinionCtrl subTarget_minion;            // 타겟으로 지정한 미니언의 스크립트
    private I_Actor subTarget_Actor;

    private TextMeshProUGUI minionStatsPannel_txt;      // 미니언 스탯을 표기하는 UI - 그 중 텍스트.
    private GameObject playerMinion;                    // 플레이어가 선택한 미니언.
    //private PSH_PlayerFPSCtrl playerMinionCtrl;         // 플레이어 미니언의 스크립트
    private GameObject playerWatchingTarget;            // 플레이어미니언이 보는 미니언 혹은 여러가지.

    private GameObject mapcamSub_Target, mapsubcam_target;  // TopView카메라의 타겟 저장과 메인카메라의 타겟 저장


    [SerializeField] public byte PlayerType;
    [SerializeField] private short exp, gold, total_exp, total_gold;
    [SerializeField] private byte level;
    public ushort[] kd; // 0 : kill 1 : death
    public ushort minionK, turretK;
    public float this_player_ping;

    // 플레이어가 갖고있는 아이템
    public MoonHeader.S_ShopItems hasItems;

    private float timer_photon;

    public GameObject[] localSounds;

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(playerName);
            //stream.SendNext(player.team);
            //stream.SendNext(player.statep);
            //stream.SendNext(exp);
            /*
            ulong send_dummy = SendDummyMaker();
            int dummy_i1 = (int)(send_dummy & (ulong)uint.MaxValue);
            int dummy_i2 = (int)(send_dummy >> 32 & (ulong)uint.MaxValue);
            stream.SendNext(dummy_i1);
            stream.SendNext(dummy_i2);

            uint dummy_i3 = ((uint)kd[0] & (uint)ushort.MaxValue);
            dummy_i3 += ((uint)kd[1] & (uint)ushort.MaxValue) << 16;
            stream.SendNext((int)dummy_i3);
            */

        }
        else
        {
            this.playerName = (string)stream.ReceiveNext();
            //this.player.team = (MoonHeader.Team)stream.ReceiveNext();
            //this.player.statep = (MoonHeader.State_P)stream.ReceiveNext();
            //this.exp = (short)stream.ReceiveNext();
            /*
            ulong receive_d = ((ulong)(int)stream.ReceiveNext() & (ulong)uint.MaxValue);
            receive_d += (((ulong)(int)stream.ReceiveNext() & (ulong)uint.MaxValue) << 32);
            ReceiveDummy(receive_d);

            int dummy_r = (int)stream.ReceiveNext();
            kd[0] = (ushort)(dummy_r & (ushort)ushort.MaxValue);
            kd[1] = (ushort)(dummy_r >> 16 & (ushort)ushort.MaxValue);
            */
            this_player_ping = (float)(PhotonNetwork.Time - info.SentServerTime);
            
        }
    }

    public ulong SendDummyMaker()
    {
        ulong send_dummy = 0;
        send_dummy += ((ulong)exp & (ulong)ushort.MaxValue);
        send_dummy += ((ulong)gold & (ulong)ushort.MaxValue) << 16;
        send_dummy += ((ulong)PlayerType & (ulong)byte.MaxValue) << 32;
        send_dummy += ((ulong)level & (ulong)byte.MaxValue) << 40;
        send_dummy += ((ulong)player.team & (ulong)byte.MaxValue) << 48;

        return send_dummy;
    }
    public void ReceiveDummy(ulong receive_dummy)
    {
        exp = (short)(receive_dummy & (ulong)ushort.MaxValue);
        gold = (short)(receive_dummy >> 16 & (ulong)ushort.MaxValue);
        PlayerType = (byte)(receive_dummy >> 32 & (ulong)byte.MaxValue);
        level = (byte)(receive_dummy >> 40 & (ulong)byte.MaxValue);
        player.team = (MoonHeader.Team)(receive_dummy >> 48 & (ulong)byte.MaxValue);
    }


    private void Awake()
    {
        canMapCamSize = 30;
        if (mySpawner == null)
        {
            mySpawner = GameObject.Find("Spawner");
        }
        if (photonView.IsMine)
        {
            //LSM_PlayerCtrl.LocalPlayerInstance = this.gameObject;
        }
        kd = new ushort[2];
        minionK = 0;

        hasItems = new S_ShopItems(GameManager.Instance.shopUI.GetComponent<LSM_UI_Shop>().items);

    }

    public void Start_fuction()
    {
        if (isMainPlayer)
        {
            MapCam = GameObject.FindGameObjectWithTag("MapCamera");
            MainCam = GameObject.FindGameObjectWithTag("MainCamera");
            MiniMapCam = GameObject.FindGameObjectWithTag("MiniMapCamera");
            MapSubCam = GameObject.FindGameObjectWithTag("SubCamera");

            mapCamCamera = MapCam.GetComponent<Camera>();
            zoomIn = ZoomInMinion();
            is_zoomIn = false;
            MapCam.transform.position = mapCamBasePosition;
            MapCam.GetComponent<Camera>().orthographicSize = MapCamBaseSize;

            minionStatsPannel = GameObject.Find("MinionStatPanel");


            if (minionStatsPannel != null)
            {
                minionStatsPannel_SelectButton = minionStatsPannel.GetComponentInChildren<Button>().transform.gameObject;
                minionStatsPannel.SetActive(false);
                minionStatsPannel_txt = minionStatsPannel.GetComponentInChildren<TextMeshProUGUI>();

            }

            playerWatchingTarget = null;
            player.statep = MoonHeader.State_P.None;

            if (photonView.IsMine)
            { MapCam.SetActive(true); MapSubCam.SetActive(true); MainCam.SetActive(false); MiniMapCam.SetActive(false); MapCam.GetComponent<AudioListener>().enabled = true; }
            // 모든 스포너를 받아온 후 팀에 해당하는 스포너를 받아옴. 한개밖에 없다는 가정으로 하나의 마스터스포너를 받아옴.
            GameObject[] dummySpawners = GameObject.FindGameObjectsWithTag("Spawner");
            foreach (GameObject s in dummySpawners)
            {
                LSM_Spawner sSC = s.GetComponent<LSM_Spawner>();
                if (sSC.team == this.player.team) { mySpawner = s; break; }
            }
            timer_Death = 0;
            death = false;
            deathPenalty = 10f;
        }
    }
    public void SettingTeamAndName(int t) {
        photonView.RPC("SettingTN_RPC", RpcTarget.AllBuffered, t);
    }
    [PunRPC] protected void SettingTN_RPC(int t)
    {
        this.player.team = (MoonHeader.Team)t;
        if (photonView.IsMine)
        { photonView.RPC("SN_RPC", RpcTarget.AllBuffered, this.playerName, this.PlayerType); }
    }
    [PunRPC] protected void SN_RPC(string n, byte t) {
        if (photonView.IsMine)
            return;
        this.playerName = n; this.PlayerType = t; }


    public void SettingPlayer_(string n, byte t) { playerName = n; PlayerType = t; }

    void Update()
    {
        if (isMainPlayer && GameManager.Instance.onceStart)
        {
            ClickEv();
            MapEv();
            SubMapCamMove();
            PlayerInMinion();
            ReGeneration();

            debugging();
            PlayerInput_M();

            timer_photon += Time.deltaTime;
            if (timer_photon >= 0.5f) { timer_photon = 0; SendPhoton_Variable(); }
        }
    }

    private void SendPhoton_Variable()
    {
        ulong send_dummy = SendDummyMaker();
        int dummy_i1 = (int)(send_dummy & (ulong)uint.MaxValue);
        int dummy_i2 = (int)(send_dummy >> 32 & (ulong)uint.MaxValue);
        //stream.SendNext(dummy_i1);
        //stream.SendNext(dummy_i2);

        uint dummy_i3 = ((uint)kd[0] & (uint)ushort.MaxValue);
        dummy_i3 += ((uint)kd[1] & (uint)ushort.MaxValue) << 16;
        //stream.SendNext((int)dummy_i3);
        photonView.RPC("ReceivePV", RpcTarget.All, dummy_i1, dummy_i2, (int)dummy_i3);
    }

    [PunRPC]private void ReceivePV(int d1, int d2, int d3)
    {
        if (photonView.IsMine)
            return;
        ulong receive_d = ((ulong)(int)d1 & (ulong)uint.MaxValue);
        receive_d += (((ulong)(int)d2 & (ulong)uint.MaxValue) << 32);
        ReceiveDummy(receive_d);

        int dummy_r = (int)d3;
        kd[0] = (ushort)(dummy_r & (ushort)ushort.MaxValue);
        kd[1] = (ushort)(dummy_r >> 16 & (ushort)ushort.MaxValue);

    }

    private void PlayerInput_M()
    {

        GameManager.Instance.tabUI.SetActive(Input.GetKey(KeyCode.Tab));

    }

    private void ReGeneration()
    {
        GameManager.Instance.deadScreen.gameObject.SetActive(death);
        if (death)
        {
            timer_Death += Time.deltaTime;
            if (timer_Death % 0.5f <= 0.1f)
                GameManager.Instance.deadScreen.GetComponentInChildren<TextMeshProUGUI>().text = Mathf.CeilToInt(deathPenalty - timer_Death).ToString();

            if (timer_Death >= deathPenalty)
            {
                ReChargingEnerge();
            }
        }
    }
    public void ReChargingEnerge()
    { death = false; timer_Death = 0; }


    private void debugging()
    {

    }

    // TopView때의 맵 이벤트
    private void MapEv()
    {
        // 현재 플레이어가 아무 클릭, 이동 등을 하지 않는다면
        if (player.statep == MoonHeader.State_P.None && GameManager.Instance.gameState != MoonHeader.GameState.Ending)
        {
            // 마우스 휠에 따라 확대, 축소
            float scroll = Input.GetAxis("Mouse ScrollWheel") * wheelSpeed;
            float camOrthoSize = Mathf.Clamp(mapCamCamera.orthographicSize - scroll, MapCamBaseSize - canMapCamSize, MapCamBaseSize + canMapCamSize);
            mapCamCamera.orthographicSize = camOrthoSize;
            if (!(MapMoveInBox(0, mapCamCamera.transform.position) || MapMoveInBox(1, mapCamCamera.transform.position)) || !(MapMoveInBox(2, mapCamCamera.transform.position) || MapMoveInBox(3, mapCamCamera.transform.position)))
            { canMapCamSize--; mapCamCamera.orthographicSize -= 2; }


            // 방향키 이동에 따라서 맵의 이동
            Vector3 mapcamPosition = MapCam.transform.position;
            Vector3 move_f = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")) * map_move * Time.deltaTime;

            Vector3 mapcamPosition_dummy = mapcamPosition + move_f;
            float width = Screen.width, height = Screen.height;
            float size_width = camOrthoSize * (width / height), size_height = camOrthoSize;
            Vector3 size_V = new Vector3(size_width, 0, size_height);
            float cam_left = mapcamPosition_dummy.x - size_width, cam_right = mapcamPosition_dummy.x + size_width,
                cam_top = mapcamPosition_dummy.z + size_height, cam_bottom = mapcamPosition_dummy.z - size_height;


            MapCam.transform.position = new Vector3(
                mapcamPosition.x + ((MapMoveInBox(0, mapcamPosition_dummy) && MapMoveInBox(1, mapcamPosition_dummy)) ? move_f.x :
                    (!(MapMoveInBox(0, mapcamPosition)) ? LSM_MapInfo.Instance.Left - (mapcamPosition.x - size_width) :
                    !(MapMoveInBox(1, mapcamPosition)) ? LSM_MapInfo.Instance.Right - (mapcamPosition.x + size_width) : 0)),

                mapcamPosition.y,

                mapcamPosition.z + ((MapMoveInBox(2, mapcamPosition_dummy) && MapMoveInBox(3, mapcamPosition_dummy)) ? move_f.z :
                (!(MapMoveInBox(3, mapcamPosition)) ? LSM_MapInfo.Instance.Bottom - (mapcamPosition.z - size_height) :
                !(MapMoveInBox(2, mapcamPosition)) ? LSM_MapInfo.Instance.Top - (mapcamPosition.z + size_height) : 0))
                );

            /*
            MapCam.transform.position = new Vector3(
                mapcamPosition.x + ((cam_left >= LSM_MapInfo.Instance.Left && cam_right <= LSM_MapInfo.Instance.Right)? move_f.x : 
                    (((mapcamPosition.x - size_width) < LSM_MapInfo.Instance.Left)? LSM_MapInfo.Instance.Left - (mapcamPosition.x - size_width) :
                    ((mapcamPosition.x + size_width) > LSM_MapInfo.Instance.Right) ? LSM_MapInfo.Instance.Right - (mapcamPosition.x + size_width) : 0)),
                mapcamPosition.y,
                mapcamPosition.z+ ((cam_top <= LSM_MapInfo.Instance.Top && cam_bottom >= LSM_MapInfo.Instance.Bottom)?move_f.z:
                (((mapcamPosition.z - size_height) < LSM_MapInfo.Instance.Bottom) ? LSM_MapInfo.Instance.Bottom - (mapcamPosition.z - size_height) :
                    ((mapcamPosition.z + size_height) > LSM_MapInfo.Instance.Top) ? LSM_MapInfo.Instance.Top - (mapcamPosition.z + size_height)  : 0))
                );
            */
            // 만약 미니언을 클릭하여 해당 미니언의 스탯을 보고있을 때 휠 또는 키보드 버튼을 클릭한다면, 클릭했던 타겟 미니언을 null로 변경. 다시 일반 상태로 변경됨.
            if (!ReferenceEquals(mapcamSub_Target, null) && (scroll != 0 || move_f != Vector3.zero))
            {
                //subTarget_minion.ChangeTeamColor();
                subTarget_Actor.ChangeTeamColor();
                //subTarget_Actor.Unselected();

                mapcamSub_Target = null;
                is_zoomIn = false;
                StopCoroutine(zoomIn);
                minionStatsPannel.SetActive(false);
            }
        }
    }

    private bool MapMoveInBox(int n, Vector3 camPosition)
    {
        Vector3 mapcamPosition = camPosition;

        float camOrthoSize = mapCamCamera.orthographicSize;
        float width = Screen.width, height = Screen.height;
        float size_width = camOrthoSize * (width / height), size_height = camOrthoSize;

        switch (n)
        {
            // 왼쪽
            case 0:
                float cam_left = mapcamPosition.x - size_width;
                return cam_left >= LSM_MapInfo.Instance.Left;
            // 오른쪽
            case 1:
                float cam_right = mapcamPosition.x + size_width;
                return cam_right <= LSM_MapInfo.Instance.Right;
            // 위
            case 2:
                float cam_top = mapcamPosition.z + size_height;
                return cam_top <= LSM_MapInfo.Instance.Top;
            // 아래
            case 3:
                float cam_bottom = mapcamPosition.z - size_height;
                return cam_bottom >= LSM_MapInfo.Instance.Bottom;
            // 전방위
            case 4:
                return (MapMoveInBox(0, camPosition) && MapMoveInBox(1, camPosition) && MapMoveInBox(2, camPosition) && MapMoveInBox(3, camPosition));
        }
        return false;
    }

    // 플레이어가 해당 미니언에 빙의/강림 하고있다면 실행 
    private void PlayerInMinion()
    {
        if (player.statep == MoonHeader.State_P.Selected)
        {
            //MainCam.transform.position = mapsubcam_target.transform.position;
            //MainCam.transform.rotation = mapsubcam_target.transform.rotation;
            // 플레이어가 선택한 상태였으나, 플레이어의 미니언이 사라졋다면 초기화
            if (!playerMinion.activeSelf)
            {
                StartCoroutine(AttackPathSelectSetting());
                Cursor.lockState = CursorLockMode.None;
            }
            // 플레이어 미니언이 살아있을 경우 아래 구문이 실행.
            else
            {
                // 미니맵캠을 플레이어 위치로 이동.
                MiniMapCam.transform.position = Vector3.Scale(playerMinion.transform.position, Vector3.one - Vector3.up) + Vector3.up * mapCamBasePosition.y;

                // 메인카메라를 기준. 메인카메라가 보고있는 방향으로 레이를 쏴, 미니언 혹은 플레이어, 터렛 등을 식별.
                // 이후 게임UI에 정보를 전달.
                RaycastHit[] hits;
                Debug.DrawRay(MainCam.transform.position + MainCam.transform.forward * 0.15f, MainCam.transform.forward * 10, Color.green, Time.deltaTime);
                //if (Physics.Raycast(MainCam.transform.position + MainCam.transform.forward * 0.15f, MainCam.transform.forward, out hit, 10, 1 << LayerMask.NameToLayer("Minion") | 1 << LayerMask.NameToLayer("Turret")))
                hits = Physics.RaycastAll(MainCam.transform.position, MainCam.transform.forward, 10, 1 << LayerMask.NameToLayer("Minion") | 1 << LayerMask.NameToLayer("Turret"));
                GameObject dummy = null;
                float dist = float.MaxValue;

                foreach (RaycastHit hit in hits)
                {
                    if (hit.transform.name.Equals(this.playerName)) { continue; }
                    else
                    {
                        if (dist > hit.distance)
                        {
                            dist = hit.distance;
                            dummy = hit.transform.gameObject;
                        }
                    }
                }

                if (!ReferenceEquals(dummy, null))
                {
                    // 만약 플레이어 캐릭터가 탐색 레이에 발견됐다면, 취소.
                    if (!ReferenceEquals(dummy, playerWatchingTarget))
                    {
                        playerWatchingTarget = dummy;
                        GameManager.Instance.gameUI_SC.enableTargetUI(true, playerWatchingTarget);
                    }
                }
                else if (!ReferenceEquals(playerWatchingTarget, null))
                {
                    playerWatchingTarget = null;
                    GameManager.Instance.gameUI_SC.enableTargetUI(false);
                }

            }
        }
    }



    // 플레이어의 현 상태를 리셋. 공격로 선택턴이 시작하였을때 사용
    public IEnumerator AttackPathSelectSetting()
    {
        if (isMainPlayer)
        {
            SoundManager.Instance.BGMMute(false);
            player.statep = MoonHeader.State_P.None;
            yield return StartCoroutine(GameManager.Instance.ScreenFade(false));
            playerMinion = null;
            MainCam.SetActive(false);
            MiniMapCam.SetActive(false);
            MapCam.SetActive(true);
            MapCam.GetComponent<AudioListener>().enabled= true;
            MapCam.transform.position = mapCamBasePosition;
            mapCamCamera.orthographicSize = MapCamBaseSize;
            yield return null;
            MapSubCam.SetActive(true);
            is_zoomIn = false;
            //subTarget_minion = null;
            subTarget_Actor = null;
            GameManager.Instance.mapUI.SetActive(true);
            StartCoroutine(GameManager.Instance.ScreenFade(true));
        }
    }

    // TopView에서의 클릭 이벤트
    private void ClickEv()
    {
        // 미니언을 처음 클릭할 경우
        if (Input.GetMouseButtonDown(0) && player.statep == MoonHeader.State_P.None)
        {
            if (EventSystem.current.IsPointerOverGameObject()) { return; }
            Ray ray = mapCamCamera.ScreenPointToRay(Input.mousePosition);
            Debug.DrawRay(ray.origin, ray.direction * 1000, Color.green, 3f);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 1000, 1 << LayerMask.NameToLayer("Icon")))
            {
                Debug.Log(hit.transform.name + " : " + hit.transform.tag);

                switch (GameManager.Instance.gameState)
                {
                    // 현재 게임이 공격로 지정 턴이라면
                    case MoonHeader.GameState.SettingAttackPath:
                        break;

                    // 게임 중 플레이어가 미니언 마크를 클릭했다면,
                    case MoonHeader.GameState.Gaming:
                        // 미니언, 플레이어, 포탑 제외 나머지를 클릭시 실행 안됨.
                        //if (!hit.transform.CompareTag("Minion") && !hit.transform.CompareTag("PlayerMinion") && !hit.transform.CompareTag("Turret") && !hit.transform.CompareTag("Nexus")) { return; }

                        I_Actor dummy = hit.transform.GetComponentInParent<I_Actor>();

                        if (ReferenceEquals(dummy, null)) { return; }
                        if (dummy.GetTeam() != this.player.team) { return; }

                        // 현재 미니언이 클릭되어 있으나, 다른 미니언을 클릭하였다면, 전에 클릭했던 미니언의 아이콘을 원래 상태로 복구
                        if (!ReferenceEquals(mapcamSub_Target, null) && !ReferenceEquals(mapcamSub_Target, hit.transform.gameObject))
                            subTarget_Actor.ChangeTeamColor();
                        //subTarget_Actor.Unselected();

                        // 클릭된 미니언에 대하여.. 카메라의 위치를 이동 및 고정. 천천히 줌인하는 코루틴 실행
                        if (ReferenceEquals(mapcamSub_Target, null) || (!ReferenceEquals(mapcamSub_Target, null) &&
                            !ReferenceEquals(mapcamSub_Target, hit.transform.gameObject)))
                        {
                            is_zoomIn = true;
                            //subTarget_Actor = hit.transform.GetComponent<I_Actor>();
                            subTarget_Actor = dummy;

                            mapcamSub_Target = hit.transform.gameObject;
                            mapsubcam_target = subTarget_Actor.GetCameraPos();
                            minionStatsPannel.SetActive(false);
                            StopCoroutine(zoomIn);
                            zoomIn = ZoomInMinion();
                            StartCoroutine(zoomIn);
                            subTarget_Actor.Selected();
                        }

                        /*
                        if (hit.transform.CompareTag("Minion"))
                        {
                            // 현재 미니언이 클릭되어 있으나, 다른 미니언을 클릭하였다면, 전에 클릭했던 미니언의 아이콘을 원래 상태로 복구
                            if (!ReferenceEquals(mapcamSub_Target, null) && !ReferenceEquals(mapcamSub_Target, hit.transform.gameObject))
                            {
                                //if (!ReferenceEquals(mapcamSub_Target, hit.transform.gameObject))
                                    subTarget_minion.ChangeTeamColor();
                            }

                            // 클릭된 미니언에 대하여.. 카메라의 위치를 이동 및 고정. 천천히 줌인하는 코루틴 실행
                            if (ReferenceEquals(mapcamSub_Target, null) || (!ReferenceEquals(mapcamSub_Target, null)&& 
                                !ReferenceEquals(mapcamSub_Target,hit.transform.gameObject))) {
                                is_zoomIn = true;
                                subTarget_minion = hit.transform.GetComponent<LSM_MinionCtrl>();


                                mapcamSub_Target = hit.transform.gameObject;
                                mapsubcam_target = subTarget_minion.CameraPosition;
                                minionStatsPannel.SetActive(false);
                                StopCoroutine(zoomIn);
                                zoomIn = ZoomInMinion();
                                StartCoroutine(zoomIn);
                                //subTarget_minion.icon.GetComponent<Renderer>().material.color = Color.green;
                                subTarget_minion.PlayerSelected();
                            }

                            //GameManager.Instance.MapCam.GetComponent<Camera>().orthographicSize = 20;
                        }
                        */
                        break;
                }

            }
        }
    }

    // TopView 상태에서 클릭한 미니언의 시점을 담당하는 카메라에 대한 함수
    private void SubMapCamMove()
    {
        if (!ReferenceEquals(mapcamSub_Target, null) && !is_zoomIn) {
            // 미니언 확대 중 죽을 경우의 예외 처리도 포함.
            if (!mapcamSub_Target.activeSelf)
            {
                mapcamSub_Target = null;
                is_zoomIn = false;
                StopCoroutine(zoomIn);
                minionStatsPannel.SetActive(false);
            }
            else
            {
                //minionStatsPannel_SelectButton.SetActive(subTarget_minion.stats.actorHealth.team == this.player.team);
                minionStatsPannel_SelectButton.SetActive(subTarget_Actor.GetActor().team == this.player.team && mapcamSub_Target.CompareTag("Minion"));
                // 미니언 미리보기 창 글씨.
                //minionStatsPannel_txt.text = string.Format("Minion : {0}\nHealth : {1}\nATK : {2}",
                //subTarget_minion.stats.actorHealth.type, subTarget_minion.stats.actorHealth.health, subTarget_minion.stats.actorHealth.Atk);

                minionStatsPannel_txt.text = string.Format("Type : {0}\nHealth : {1}\nATK : {2}",
                                subTarget_Actor.GetActor().type, subTarget_Actor.GetActor().health, subTarget_Actor.GetActor().Atk);

                MapCam.transform.position = (mapcamSub_Target.transform.position + Vector3.up * mapCamBasePosition.y);
                MapSubCam.transform.position = mapsubcam_target.transform.position;
                MapSubCam.transform.rotation = mapsubcam_target.transform.rotation;
            }
        }
    }

    // 맵에서 해당 미니언에게 천천히 가까워지는 코드
    // 현재 Lerp를 사용하였지만, 속도에 따라 이동하게 설정할지 고민.
    private IEnumerator ZoomInMinion()
    {

        Vector3 originV = MapCam.transform.position;
        float originSize = mapCamCamera.orthographicSize;

        /*for (int i = 0; i < 50; i++)
        {
            MapCam.transform.position = Vector3.Lerp(originV, (mapcamSub_Target.transform.position + Vector3.up * 95), 0.02f * i);
            mapCamCamera.orthographicSize = Mathf.Lerp(originSize, 20, 0.02f * i);
            yield return new WaitForSeconds(0.01f);
        }*/


        while (true)
        {
            if (ReferenceEquals(mapcamSub_Target, null))
                break;
            Vector3 targetPosition = mapcamSub_Target.transform.position + Vector3.up * mapCamBasePosition.y;
            Vector3 dummy_position = Vector3.MoveTowards(MapCam.transform.position,
                targetPosition, map_move * 2 * Time.deltaTime);
            bool dummy_inBox = MapMoveInBox(4, dummy_position);
            MapCam.transform.position = (dummy_inBox) ? dummy_position : MapCam.transform.position;

            mapCamCamera.orthographicSize = (mapCamCamera.orthographicSize > MapCamBaseSize - (canMapCamSize - 3)) ?
                mapCamCamera.orthographicSize - map_move * Time.deltaTime : MapCamBaseSize - (canMapCamSize - 3);

            yield return new WaitForSeconds(Time.deltaTime);

            if ((Vector3.Distance(MapCam.transform.position, targetPosition) <= 5 || !dummy_inBox) && mapCamCamera.orthographicSize <= MapCamBaseSize - (canMapCamSize - 3))
                break;
        }
        is_zoomIn = false;
        minionStatsPannel.SetActive(true);
    }

    // select버튼 클릭 시 
    public void SelectPlayerMinion()
    {
        if (ReferenceEquals(mapcamSub_Target, null) || ReferenceEquals(mapcamSub_Target.GetComponent<LSM_MinionCtrl>(), null) || death) { return; }

        LSM_MinionCtrl dummy_minion = mapcamSub_Target.GetComponent<LSM_MinionCtrl>();


        if (player.statep == MoonHeader.State_P.None && dummy_minion.stats.state != MoonHeader.State.Dead &&
            dummy_minion.stats.actorHealth.team == this.player.team)
        {
            player.statep = MoonHeader.State_P.Possession;
            dummy_minion.PlayerConnect();
            playerMinion = dummy_minion.transform.gameObject;
            StartCoroutine(ZoomPossession());
        }
    }

    // 빙의 코루틴
    private IEnumerator ZoomPossession()
    {
        StartCoroutine(GameManager.Instance.ScreenFade(false));
        float originSize = mapCamCamera.orthographicSize;


        minionStatsPannel.SetActive(false);
        PlaySFX(0);
        float dummy_time_in = 0;
        while (true)
        {
            yield return new WaitForSeconds(Time.deltaTime);
            dummy_time_in += Time.deltaTime;
            mapCamCamera.orthographicSize = Mathf.Lerp(originSize, 5, dummy_time_in >= 1 ? 1 : dummy_time_in);
            if (dummy_time_in >= 1) break;
        }
        /*
        for (int i = 0; i < 100; i++)
        {
            yield return new WaitForSeconds(0.01f);
            mapCamCamera.orthographicSize = Mathf.Lerp(originSize, 5, 0.01f * i);
        }
        */
        // 카메라 메인카메라 제외 모두 끄기.
        GameManager.Instance.mapUI.SetActive(false);
        mapCamCamera.transform.gameObject.SetActive(false);
        MapSubCam.transform.gameObject.SetActive(false);
        MainCam.SetActive(true);
        MiniMapCam.SetActive(true);

        yield return new WaitForSeconds(1f);
        // 기존의 미니언을 비활성화한 후 플레이어 전용 프리펩 소환.

        playerMinion = PoolManager.Instance.Get_PlayerMinion(PlayerType);
        //playerMinion = GameObject.Instantiate(PrefabManager.Instance.players[0],PoolManager.Instance.transform);

        //playerMinionCtrl = playerMinion.GetComponent<PSH_PlayerFPSCtrl>();

        // 카메라 지정. 및 초기세팅
        //PSH_PlayerFPSCtrl player_dummy = playerMinion.GetComponent<PSH_PlayerFPSCtrl>();
        I_Playable player_dummy = playerMinion.GetComponent<I_Playable>();
        player_dummy.SpawnSetting(player.team, subTarget_Actor.GetActor().health, playerName, this.GetComponent<LSM_PlayerCtrl>());
        //player_dummy.playerCamera = MainCam.GetComponent<Camera>();
        mapsubcam_target = player_dummy.CameraSetting(MainCam);
        GameManager.Instance.gameUI.SetActive(true);
        GameManager.Instance.gameUI_SC.playerHealth(playerMinion);

        Transform dummy_m = mapcamSub_Target.transform;
        mapcamSub_Target.GetComponent<LSM_MinionCtrl>().MinionDisable();

        Vector3 dummyPosition = dummy_m.position;
        Quaternion dummyRotation = dummy_m.rotation;


        playerMinion.transform.position = dummyPosition;
        playerMinion.transform.rotation = dummyRotation;

        StartCoroutine(GameManager.Instance.ScreenFade(true));
        player.statep = MoonHeader.State_P.Selected;
        Cursor.lockState = CursorLockMode.Locked;

        SoundManager.Instance.BGMMute(true);

        yield return new WaitForSeconds(3f);
        StartCoroutine(GameManager.Instance.ScreenFade(true));
        //subTarget_minion.stats.state = MoonHeader.State.Normal;
    }

    public void PlayerMinionDeadProcessing()
    {
        if (isMainPlayer) {
            death = true;
            Cursor.lockState = CursorLockMode.None;
            GameManager.Instance.gameUI.SetActive(false);
        }
    }

    public void SetExp(short exp_dummy)
    { photonView.RPC("ExpPlus", RpcTarget.All, exp_dummy); }
    [PunRPC] private void ExpPlus(short d) {
        if (photonView.IsMine)
        {
            exp += d;
            total_exp += d;
            if (level < LSM_SettingStatus.Instance.maxLV - 1 &&
                LSM_SettingStatus.Instance.lvStatus[PlayerType].canLevelUp(level, exp))
            {
                level += 1;
                exp = 0;
            }
        }
    }

    protected void PlaySFX(int num)
    {
        AudioSource dummy_s = localSounds[num].GetComponent<AudioSource>();
        if (dummy_s.isPlaying) { return; }
        else dummy_s.Play();
    }
    protected void StopSFX(int num)
    {
        AudioSource dummy_s = localSounds[num].GetComponent<AudioSource>();
        if (dummy_s.isPlaying) { dummy_s.Stop(); }
        else { return; }
    }

    public void AddingKD(byte i) { kd[i] += 1; }
    public void AddingCS() { minionK += 1; }
    public void AddingTD() { turretK += 1; }
    public short GetExp() { return exp; }
    public short GetTotalExp() { return total_exp; }
    public short GetGold() { return gold; }
    public ushort GetCS() { return minionK; }
    public ushort GetTD() { return turretK; }
    public void SpendGold(short b) { gold -= b; }
    public void GetGold(short gold_dummy) { gold += gold_dummy; total_gold += gold_dummy; }
    public byte GetLevel() { return level; }
    public void PlayerItemSynchronize() { 
        for (int i = 0; i < GameManager.Instance.shopUI.GetComponent<LSM_UI_Shop>().items.Length; i++)
        {
            photonView.RPC("ItemRPC", RpcTarget.All, i, (int)hasItems.NumOfItem(i));
        }
    }
    [PunRPC] private void ItemRPC(int code, int num) { hasItems.SetItem(code, (byte)num); }
}
