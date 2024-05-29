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

// �÷��̾� ��ũ��Ʈ
// TopView������ �÷��̾� ��Ʈ��
public class LSM_PlayerCtrl : MonoBehaviourPunCallbacks, IPunObservable
{
    public bool isMainPlayer;               // ���� �������� �÷��̾����� Ȯ��.
    public string playerName;               // ��Ƽ������ �÷��̾� �̸�
                                            // # �̸� ����
    public MoonHeader.S_PlayerState player;   // �÷��̾� ���¿� ���� ����ü
    const float MapCamBaseSize = 60;        // TopView ī�޶��� OrthogonalSize
    float canMapCamSize;

    public GameObject mySpawner;            // ���� ������ ������


    // TopView������ �̵��ӵ� �ʱ�ȭ
    private float wheelSpeed = 15f;
    private float map_move = 90f;
    private float timer_Death, deathPenalty;
    private bool death;
    private bool is_zoomIn;                 // ������ �̴Ͼ𿡰� Ȯ���ϰ� �ִ���
    private IEnumerator zoomIn;             // StopCorutine�� ����ϱ����� �̸� ����.
    [HideInInspector] public Camera mapCamCamera;            // TopView�� ���Ǵ� ī�޶�
    [HideInInspector] public GameObject MainCam, MapCam, MapSubCam, MiniMapCam;       // �÷��̾� ������Ʈ ���� �����ϴ� ī�޶��.

    public Vector3 mapCamBasePosition;                  // TopViewī�޶��� �ʱ���ġ
                                                        // # Y�ุ 95�� ����
    [HideInInspector] public GameObject minionStatsPannel, minionStatsPannel_SelectButton;                // �÷��̾ ������ �̴Ͼ��� ������ ǥ�����ִ� UI
                                                                                                          // # Canvas�� �ڽ� ������Ʈ �� MinionStatpanel
    private LSM_MinionCtrl subTarget_minion;            // Ÿ������ ������ �̴Ͼ��� ��ũ��Ʈ
    private I_Actor subTarget_Actor;

    private TextMeshProUGUI minionStatsPannel_txt;      // �̴Ͼ� ������ ǥ���ϴ� UI - �� �� �ؽ�Ʈ.
    private GameObject playerMinion;                    // �÷��̾ ������ �̴Ͼ�.
    //private PSH_PlayerFPSCtrl playerMinionCtrl;         // �÷��̾� �̴Ͼ��� ��ũ��Ʈ
    private GameObject playerWatchingTarget;            // �÷��̾�̴Ͼ��� ���� �̴Ͼ� Ȥ�� ��������.

    private GameObject mapcamSub_Target, mapsubcam_target;  // TopViewī�޶��� Ÿ�� ����� ����ī�޶��� Ÿ�� ����


    [SerializeField] public byte PlayerType;
    [SerializeField] private short exp, gold, total_exp, total_gold;
    [SerializeField] private byte level;
    public ushort[] kd; // 0 : kill 1 : death
    public ushort minionK, turretK;
    public float this_player_ping;

    // �÷��̾ �����ִ� ������
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
            // ��� �����ʸ� �޾ƿ� �� ���� �ش��ϴ� �����ʸ� �޾ƿ�. �Ѱ��ۿ� ���ٴ� �������� �ϳ��� �����ͽ����ʸ� �޾ƿ�.
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

    // TopView���� �� �̺�Ʈ
    private void MapEv()
    {
        // ���� �÷��̾ �ƹ� Ŭ��, �̵� ���� ���� �ʴ´ٸ�
        if (player.statep == MoonHeader.State_P.None && GameManager.Instance.gameState != MoonHeader.GameState.Ending)
        {
            // ���콺 �ٿ� ���� Ȯ��, ���
            float scroll = Input.GetAxis("Mouse ScrollWheel") * wheelSpeed;
            float camOrthoSize = Mathf.Clamp(mapCamCamera.orthographicSize - scroll, MapCamBaseSize - canMapCamSize, MapCamBaseSize + canMapCamSize);
            mapCamCamera.orthographicSize = camOrthoSize;
            if (!(MapMoveInBox(0, mapCamCamera.transform.position) || MapMoveInBox(1, mapCamCamera.transform.position)) || !(MapMoveInBox(2, mapCamCamera.transform.position) || MapMoveInBox(3, mapCamCamera.transform.position)))
            { canMapCamSize--; mapCamCamera.orthographicSize -= 2; }


            // ����Ű �̵��� ���� ���� �̵�
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
            // ���� �̴Ͼ��� Ŭ���Ͽ� �ش� �̴Ͼ��� ������ �������� �� �� �Ǵ� Ű���� ��ư�� Ŭ���Ѵٸ�, Ŭ���ߴ� Ÿ�� �̴Ͼ��� null�� ����. �ٽ� �Ϲ� ���·� �����.
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
            // ����
            case 0:
                float cam_left = mapcamPosition.x - size_width;
                return cam_left >= LSM_MapInfo.Instance.Left;
            // ������
            case 1:
                float cam_right = mapcamPosition.x + size_width;
                return cam_right <= LSM_MapInfo.Instance.Right;
            // ��
            case 2:
                float cam_top = mapcamPosition.z + size_height;
                return cam_top <= LSM_MapInfo.Instance.Top;
            // �Ʒ�
            case 3:
                float cam_bottom = mapcamPosition.z - size_height;
                return cam_bottom >= LSM_MapInfo.Instance.Bottom;
            // ������
            case 4:
                return (MapMoveInBox(0, camPosition) && MapMoveInBox(1, camPosition) && MapMoveInBox(2, camPosition) && MapMoveInBox(3, camPosition));
        }
        return false;
    }

    // �÷��̾ �ش� �̴Ͼ� ����/���� �ϰ��ִٸ� ���� 
    private void PlayerInMinion()
    {
        if (player.statep == MoonHeader.State_P.Selected)
        {
            //MainCam.transform.position = mapsubcam_target.transform.position;
            //MainCam.transform.rotation = mapsubcam_target.transform.rotation;
            // �÷��̾ ������ ���¿�����, �÷��̾��� �̴Ͼ��� ��󠺴ٸ� �ʱ�ȭ
            if (!playerMinion.activeSelf)
            {
                StartCoroutine(AttackPathSelectSetting());
                Cursor.lockState = CursorLockMode.None;
            }
            // �÷��̾� �̴Ͼ��� ������� ��� �Ʒ� ������ ����.
            else
            {
                // �̴ϸ�ķ�� �÷��̾� ��ġ�� �̵�.
                MiniMapCam.transform.position = Vector3.Scale(playerMinion.transform.position, Vector3.one - Vector3.up) + Vector3.up * mapCamBasePosition.y;

                // ����ī�޶� ����. ����ī�޶� �����ִ� �������� ���̸� ��, �̴Ͼ� Ȥ�� �÷��̾�, �ͷ� ���� �ĺ�.
                // ���� ����UI�� ������ ����.
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
                    // ���� �÷��̾� ĳ���Ͱ� Ž�� ���̿� �߰ߵƴٸ�, ���.
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



    // �÷��̾��� �� ���¸� ����. ���ݷ� �������� �����Ͽ����� ���
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

    // TopView������ Ŭ�� �̺�Ʈ
    private void ClickEv()
    {
        // �̴Ͼ��� ó�� Ŭ���� ���
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
                    // ���� ������ ���ݷ� ���� ���̶��
                    case MoonHeader.GameState.SettingAttackPath:
                        break;

                    // ���� �� �÷��̾ �̴Ͼ� ��ũ�� Ŭ���ߴٸ�,
                    case MoonHeader.GameState.Gaming:
                        // �̴Ͼ�, �÷��̾�, ��ž ���� �������� Ŭ���� ���� �ȵ�.
                        //if (!hit.transform.CompareTag("Minion") && !hit.transform.CompareTag("PlayerMinion") && !hit.transform.CompareTag("Turret") && !hit.transform.CompareTag("Nexus")) { return; }

                        I_Actor dummy = hit.transform.GetComponentInParent<I_Actor>();

                        if (ReferenceEquals(dummy, null)) { return; }
                        if (dummy.GetTeam() != this.player.team) { return; }

                        // ���� �̴Ͼ��� Ŭ���Ǿ� ������, �ٸ� �̴Ͼ��� Ŭ���Ͽ��ٸ�, ���� Ŭ���ߴ� �̴Ͼ��� �������� ���� ���·� ����
                        if (!ReferenceEquals(mapcamSub_Target, null) && !ReferenceEquals(mapcamSub_Target, hit.transform.gameObject))
                            subTarget_Actor.ChangeTeamColor();
                        //subTarget_Actor.Unselected();

                        // Ŭ���� �̴Ͼ� ���Ͽ�.. ī�޶��� ��ġ�� �̵� �� ����. õõ�� �����ϴ� �ڷ�ƾ ����
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
                            // ���� �̴Ͼ��� Ŭ���Ǿ� ������, �ٸ� �̴Ͼ��� Ŭ���Ͽ��ٸ�, ���� Ŭ���ߴ� �̴Ͼ��� �������� ���� ���·� ����
                            if (!ReferenceEquals(mapcamSub_Target, null) && !ReferenceEquals(mapcamSub_Target, hit.transform.gameObject))
                            {
                                //if (!ReferenceEquals(mapcamSub_Target, hit.transform.gameObject))
                                    subTarget_minion.ChangeTeamColor();
                            }

                            // Ŭ���� �̴Ͼ� ���Ͽ�.. ī�޶��� ��ġ�� �̵� �� ����. õõ�� �����ϴ� �ڷ�ƾ ����
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

    // TopView ���¿��� Ŭ���� �̴Ͼ��� ������ ����ϴ� ī�޶� ���� �Լ�
    private void SubMapCamMove()
    {
        if (!ReferenceEquals(mapcamSub_Target, null) && !is_zoomIn) {
            // �̴Ͼ� Ȯ�� �� ���� ����� ���� ó���� ����.
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
                // �̴Ͼ� �̸����� â �۾�.
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

    // �ʿ��� �ش� �̴Ͼ𿡰� õõ�� ��������� �ڵ�
    // ���� Lerp�� ����Ͽ�����, �ӵ��� ���� �̵��ϰ� �������� ���.
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

    // select��ư Ŭ�� �� 
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

    // ���� �ڷ�ƾ
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
        // ī�޶� ����ī�޶� ���� ��� ����.
        GameManager.Instance.mapUI.SetActive(false);
        mapCamCamera.transform.gameObject.SetActive(false);
        MapSubCam.transform.gameObject.SetActive(false);
        MainCam.SetActive(true);
        MiniMapCam.SetActive(true);

        yield return new WaitForSeconds(1f);
        // ������ �̴Ͼ��� ��Ȱ��ȭ�� �� �÷��̾� ���� ������ ��ȯ.

        playerMinion = PoolManager.Instance.Get_PlayerMinion(PlayerType);
        //playerMinion = GameObject.Instantiate(PrefabManager.Instance.players[0],PoolManager.Instance.transform);

        //playerMinionCtrl = playerMinion.GetComponent<PSH_PlayerFPSCtrl>();

        // ī�޶� ����. �� �ʱ⼼��
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
