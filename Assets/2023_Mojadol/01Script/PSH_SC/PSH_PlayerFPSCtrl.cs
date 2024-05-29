using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using static MoonHeader;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using System.Runtime.CompilerServices;

public class PSH_PlayerFPSCtrl : MonoBehaviourPunCallbacks, I_Actor, IPunObservable, I_Characters
{
    // ���� �÷��̾� ����
    // �÷��̾� ����
    enum State { Normal, Attacking, Blocking, Casting, Exhausting };
    State state = State.Normal;

    // �÷��̾� ����
    public float Health = 100.0f;
    private float basicdamage = 20.0f;
    public float currentdamage = 20.0f;
    private float basicattackDelay = 1.0f;

    // ���ݱ�� ���� ����
    public GameObject handpos;
    public GameObject sword;
    public GameObject attackRange;
    public GameObject swordball_prefab;
    private bool canAttack = true;

    // ��ų���� ����
    private float qDamage = 30.0f;
    private bool canUseQ = true;
    public float eDamage = 25.0f;
    private float ePlusDamage = 0.0f;
    private bool ePressed = false;
    private bool canUseE = true;

    // ��ų ������ ����
    private int basicLevel = 1;
    private int qLevel = 1;
    private int eLevel = 1;

    // �̵� ���� ����
    public bool canMove = true; // ������ �� �ִ��� ������
    public float movespeed = 5.0f; // �̵��ӵ� ����

    // ī�޶� ���� ������
    public Camera playerCamera;
    public GameObject camerapos;
    public bool canSee = true;
    bool cameraCanMove = true;
    bool invertCamera = false;
    float yaw = 0.0f;
    float pitch = 0.0f;
    public float mouseSensitivity = 3f; // ���콺 ����
    public float maxLookAngle = 50f; // ���� �þ߰�

    // Ÿ�̸�
    private float timer = 0.0f;

    // LSM ���� �Ʒ� �߰� ���� /*

    public string playerName;
    public MoonHeader.S_ActorState actorHealth;
    private GameObject playerIcon;

    private Rigidbody rigid;
    public LSM_PlayerCtrl myPlayerCtrl;
    public MoonHeader.State_P_Minion state_p;

    private Vector3 networkPosition, networkVelocity;

    MeshRenderer icon_ren;
    List<Material> icon_materialL;
    bool selected_e;

    // LSM */

    PhotonView pv;

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) // �Ǵ� �� �����ѵ� ������ ����Ǵ����� Ȯ���ϱ� ���� 
    {
        if (stream.IsWriting)
        {
            stream.SendNext(playerName);
            //stream.SendNext(this.gameObject.activeSelf);
            //stream.SendNext(actorHealth.maxHealth);
            //stream.SendNext(actorHealth.health);
            //stream.SendNext(actorHealth.team);
            //stream.SendNext(actorHealth.Atk);
            ulong send_dummy = SendDummyMaker_LSM();
            int dummy_int1 = (int)(send_dummy & (ulong)uint.MaxValue);
            int dummy_int2 = (int)((send_dummy >> 32) & (ulong)uint.MaxValue);
            stream.SendNext(dummy_int1);
            stream.SendNext(dummy_int2);
            stream.SendNext(rigid.velocity);
        }
        else
        {
            this.playerName = (string)stream.ReceiveNext();
            //this.gameObject.SetActive((bool)stream.ReceiveNext());
            /*
            this.actorHealth.maxHealth = (short)stream.ReceiveNext();
            this.actorHealth.health = (short)stream.ReceiveNext();
            this.actorHealth.team = (MoonHeader.Team)stream.ReceiveNext();
            this.actorHealth.Atk = (short)stream.ReceiveNext();
            */
            int d1 = (int)stream.ReceiveNext();
            int d2 = (int)stream.ReceiveNext();

            ulong receive_dummy = (ulong)(d1) & (ulong)uint.MaxValue;
            receive_dummy += ((ulong)(d2) << 32);
            ReceiveDummyUnZip(receive_dummy);
            networkVelocity = (Vector3)stream.ReceiveNext();
            rigid.velocity = networkVelocity;
        }
    }

    private ulong SendDummyMaker_LSM()
    {
        ulong send_dummy = 0;
        send_dummy += ((ulong)actorHealth.maxHealth & (ulong)ushort.MaxValue);
        send_dummy += ((ulong)(actorHealth.health) & (ulong)ushort.MaxValue) << 16;

        send_dummy += ((ulong)(actorHealth.team) & (ulong)byte.MaxValue) << 32;
        send_dummy += ((ulong)(actorHealth.Atk) & (ulong)byte.MaxValue) << 40;
        send_dummy += ((ulong)(state_p) & (ulong)byte.MaxValue) << 48;
        return send_dummy;
    }
    private void ReceiveDummyUnZip(ulong receive_dummy)
    {
        //actorHealth.maxHealth = (short)(receive_dummy & (ulong)ushort.MaxValue);
        actorHealth.maxHealth = (short)(receive_dummy & (ulong)ushort.MaxValue);
        actorHealth.health = (short)((receive_dummy >> 16) & (ulong)ushort.MaxValue);
        actorHealth.team = (MoonHeader.Team)((receive_dummy >> 32) & (ulong)byte.MaxValue);
        actorHealth.Atk = (short)((receive_dummy >> 40) & (ulong)byte.MaxValue);
        state_p = (MoonHeader.State_P_Minion)((receive_dummy >> 48) & (ulong)byte.MaxValue);
        
    }

    private void Awake()
	{
        // LSM
        rigid = GetComponent<Rigidbody>();
        playerIcon = GameObject.Instantiate(PrefabManager.Instance.icons[4], transform);
        playerIcon.transform.localPosition = new Vector3(0, 60, 0);
        //

        pv = this.GetComponent<PhotonView>();
        int id = PhotonNetwork.AllocateViewID(pv.ViewID);
        pv.ViewID = id;
        icon_materialL = new List<Material>();
        selected_e = false;
    }


    // ���� ĳ�� �����ؼ� ����
    // Start is called before the first frame update
    void Start()
    {
        // ���� �� �ʱ�ȭ
        attackRange.SetActive(false);

        //Cursor.lockState = CursorLockMode.Locked;
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!photonView.IsMine)
        {
            rigid.velocity = networkVelocity;
            return; 
        }

        // �̵�, ī�޶� ����
        if (canMove)
            Move();
        if (canSee)
            LookAround();

        // �⺻����
        if (Input.GetMouseButtonDown(0) && canAttack)
        {
            StartCoroutine(BasicAttack(basicattackDelay));
            StartCoroutine(BasicAttackVolume(0.2f));
        }

        // ����
        if (canAttack)
            Block();

        // ��ų 1
        if (Input.GetKeyDown(KeyCode.Q) && canUseQ)
        {
            StartCoroutine(QskillActive(0.3f));
            StartCoroutine(QskillCool(3.0f));
        }

        // ��ų 2
        if (canUseE)
        {
            EskillActive();
        }

    }

    private void LateUpdate()
    {
        RecoverMoveSpeed();
    }


    // �̵�
    void Move()
    {
        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");

        // movespeed = Input.GetKey(KeyCode.LeftShift) ? 8.0f : 5.0f; // �޸���
        this.transform.Translate(new Vector3(x, 0, y) * movespeed * Time.deltaTime);
    }

    // ī�޶� �̵�
    void LookAround()
    {
        playerCamera.transform.position = camerapos.transform.position;
        playerCamera.transform.rotation = camerapos.transform.rotation;


        if (cameraCanMove)
        {
            yaw = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * mouseSensitivity;

            if (!invertCamera)
            {
                pitch -= mouseSensitivity * Input.GetAxis("Mouse Y");
            }
            else
            {
                // Inverted Y
                pitch += mouseSensitivity * Input.GetAxis("Mouse Y");
            }

            // Clamp pitch between lookAngle
            pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);

            this.transform.localEulerAngles = new Vector3(0, yaw, 0);
            camerapos.transform.localEulerAngles = new Vector3(pitch, 0, 0);
            playerCamera.transform.localEulerAngles = new Vector3(pitch, yaw, 0);
        }

    }

    // �����̻� ȸ��(�̵��ӵ�)
    void RecoverMoveSpeed()
    {
        if (movespeed <= 2.0f)
        {
            timer += Time.deltaTime;

            if (timer >= 2.0f)
            {
                movespeed = 5.0f;
                timer = 0.0f;
            }
        }
    }

    // �⺻���� �ڷ�ƾ
    IEnumerator BasicAttack(float delay)
    {
        canAttack = false;
        currentdamage = basicdamage;
        state = State.Attacking;
        handpos.transform.localEulerAngles = new Vector3(0, 0, 0);
        handpos.transform.localEulerAngles += new Vector3(70.0f, 0, 0);
        movespeed = 3.0f;

        yield return new WaitForSecondsRealtime(delay);

        canAttack = true;
        state = State.Normal;
        handpos.transform.localEulerAngles = new Vector3(0, 0, 0);
        movespeed = 5.0f;
    }

    IEnumerator BasicAttackVolume(float delay)
    {
        attackRange.SetActive(true);
        yield return new WaitForSecondsRealtime(delay);
        attackRange.SetActive(false);
    }

    // ����
    void Block()
    {
        if (Input.GetMouseButtonDown(1))
        {
            handpos.transform.localEulerAngles = new Vector3(0, 0, 70);
            state = State.Blocking;
            movespeed = 3.0f;
        }

        if (Input.GetMouseButtonUp(1))
        {
            handpos.transform.localEulerAngles = new Vector3(0, 0, 0);
            state = State.Normal;
            movespeed = 5.0f;
        }
    }

    // ��ų Q
    IEnumerator QskillActive(float delay)
    {
        currentdamage = qDamage;
        handpos.transform.localPosition = new Vector3(0, 0, 1);
        handpos.transform.localEulerAngles = new Vector3(90, 0, 0);
        canAttack = false;
        sword.GetComponent<Collider>().enabled = true;

        yield return new WaitForSecondsRealtime(delay);

        currentdamage = basicdamage;
        handpos.transform.localPosition = new Vector3(0.6f, -0.2f, 0);
        handpos.transform.localEulerAngles = new Vector3(0, 0, 0);
        canAttack = true;
        sword.GetComponent<Collider>().enabled = false;
    }

    IEnumerator QskillCool(float delay)
    {
        canUseQ = false;
        yield return new WaitForSecondsRealtime(delay);
        canUseQ = true;
    }

    // ��ų E
    void EskillActive()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            state = State.Casting;
            canMove = false;
            ePressed = true;
            handpos.transform.localPosition = new Vector3(0, 0.1f, 0);
        }

        if (Input.GetKey(KeyCode.E))
        {
            timer += Time.deltaTime;
            if (timer <= 7.0f && ePressed)
            {
                ePlusDamage += 3.0f * Time.deltaTime;
            }
        }

        if (Input.GetKeyUp(KeyCode.E))
        {
            if (ePressed)
            {
                state = State.Normal;
                canMove = true;
                eDamage += ePlusDamage;
                ePlusDamage = 0.0f;
                handpos.transform.localPosition = new Vector3(0.6f, -0.2f, 0);

                GameObject sprefab = Instantiate(swordball_prefab, attackRange.transform.position, attackRange.transform.rotation);
                sprefab.gameObject.GetComponent<PSH_SwordProjectile>().damage = eDamage;
                sprefab.GetComponent<PSH_SwordProjectile>().script = this.GetComponent<PSH_PlayerFPSCtrl>();

                eDamage = 25.0f;
                timer = 0.0f;
                ePressed = false;
                StartCoroutine(EskillCool(8.0f));
            }
        }
    }

    IEnumerator EskillCool(float delay)
    {
        canUseE = false;
        yield return new WaitForSecondsRealtime(delay);
        canUseE = true;
    }

    void LevelFunc(float attackExp, float qExp, float eExp)
    {
        if (attackExp > 50.0f)
            basicLevel = 2;
        else if (attackExp > 100.0f)
            basicLevel = 3;

        if (qExp > 50.0f)
            qLevel = 2;
        else if (qExp > 70.0f)
            qLevel = 3;

        if (eExp > 30.0f)
            eLevel = 2;
        else if (eExp > 50.0f)
            eLevel = 3;

        switch (basicLevel)
        {
            case 1:
                break;
            case 2:
                basicattackDelay = 0.7f;
                break;
            case 3:
                basicattackDelay = 0.3f;
                break;
        }

        switch (qLevel)
        {
            case 1:
                break;

        }
    }

    // LSM Spawn Setting
    public void SpawnSetting(MoonHeader.Team t, short monHealth, string pname, LSM_PlayerCtrl pctrl)
    {
        //Health = monHealth * 10;
        // ����׿�. ���� �����ϴ� �̴Ͼ��� ü���� 10������ ����, ���ݷ��� 10���� ����Ʈ. ���� �÷��̾� ���ݷ����� ������ ����
        this.photonView.RequestOwnership();
        actorHealth = new MoonHeader.S_ActorState(100, 10, t);
        actorHealth.health = (short)(monHealth * 10);
        playerName = pname;
        myPlayerCtrl = pctrl;
        state_p = MoonHeader.State_P_Minion.Normal;

        photonView.RPC("SpawnSetting_RPC", RpcTarget.All, (short)100, (short)(monHealth * 10), pname, (int)t);

        // �ʱ�ȭ
        canAttack = true;
        canMove= true;
        state = State.Normal;
        handpos.transform.localEulerAngles = new Vector3(0, 0, 0);
        movespeed = 5.0f;
        attackRange.SetActive(false);
        canUseQ = true;
        canUseE = true;
        canSee = true;
        cameraCanMove = true;
        invertCamera = false;
    }

    [PunRPC]private void SpawnSetting_RPC(short mh, short h, string name, int t)
    {
        this.actorHealth.maxHealth = mh;
        this.actorHealth.health = h;
        this.playerName= name;
        this.actorHealth.team = (Team)t;

        this.transform.name = playerName;
        GameManager.Instance.playerMinions[(int)actorHealth.team].Add(this.gameObject);
        ChangeTeamColor(playerIcon);
    }


    // LSM Damaged �߰�.
    public void Damaged(short dam, Vector3 origin, MoonHeader.Team t, GameObject other)
    {
        
        if (t == actorHealth.team || state_p == MoonHeader.State_P_Minion.Dead)
            return;
        //actorHealth.health -= dam;
        // �˹��� �Ǵ� ���⺤�͸� ����.
        //Vector3 direction_knock = Vector3.Scale(this.transform.position - origin, Vector3.one - Vector3.up).normalized;
        //float scale_knock = 100f;
        //rigid.AddForce(direction_knock * scale_knock);
        if (this.actorHealth.health - dam <= 0)
        { StartCoroutine(DeadProcessing(other)); }
        else
        {
            photonView.RPC("Dam_RPC", RpcTarget.All, dam);
        }
        return;
    }
    [PunRPC]private void Dam_RPC(short dam)
    {
        if (photonView.IsMine)
        {
            actorHealth.health -= dam;
            //if (actorHealth.health <= 0)
            //{ actorHealth.health = 0; }
        }
    }
    [PunRPC] private void Dead_RPC() { 
        state_p = MoonHeader.State_P_Minion.Dead;
        if (photonView.IsMine)
            myPlayerCtrl.PlayerMinionDeadProcessing();
    }
    // LSM DeadProcessing
    public IEnumerator DeadProcessing(GameObject other)
    {
        photonView.RPC("Dead_RPC", RpcTarget.All);
        Debug.Log("PlayerMinion Dead");
        GameManager.Instance.PlayerMinionRemover(actorHealth.team, playerName);
        // ������ Ÿ���� �÷��̾���, ����ġ �� �α�â ����.
        if (other.transform.CompareTag("PlayerMinion"))
        {
            other.GetComponent<PSH_PlayerFPSCtrl>().myPlayerCtrl.SetExp( 50);   // ���������� ���� ����ġ�� 50���� ���� ����.
        }
        GameManager.Instance.DisplayAdd(string.Format("{0} Killed {1}",other.gameObject.name, this.name));
        yield return new WaitForSeconds(0.5f);
        MinionDisable();

        
    }

    // �÷��̾� ������ ������.
    public void ChangeTeamColor() { ChangeTeamColor(playerIcon); }
    public void ChangeTeamColor(GameObject obj)
    {
        Color dummy_color;
        switch (actorHealth.team)
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
        obj.GetComponent<Renderer>().material.color = dummy_color;
    }

    public void ParentSetting_Pool(int index) { photonView.RPC("ParentSetting_Pool_RPC", RpcTarget.AllBuffered, index); }
    [PunRPC]private void ParentSetting_Pool_RPC(int index)
    {
        this.transform.parent = PoolManager.Instance.gameObject.transform;
        PoolManager.Instance.poolList_PlayerMinions[index].Add(this.gameObject);
    }

    public void MinionDisable() { photonView.RPC("DeadProcessing", RpcTarget.All); }
    [PunRPC]protected void DeadProcessing()
    {
        if (photonView.IsMine)
            myPlayerCtrl.PlayerMinionDeadProcessing(); 
        this.gameObject.SetActive(false);
    }

    public void MinionEnable() { photonView.RPC("MinionEnable_RPC", RpcTarget.All); }
    [PunRPC] protected void MinionEnable_RPC() { this.gameObject.SetActive(true); }

    // I_Actor �������̽��� �̸� �����ص� �Լ��� ����
    public short GetHealth() { return this.actorHealth.health; }
    public short GetMaxHealth() { return this.actorHealth.maxHealth; }
    public MoonHeader.Team GetTeam() { return this.actorHealth.team; }
    public void AddEXP(short exp) { }
    public MoonHeader.S_ActorState GetActor() { return this.actorHealth; }
    public bool IsCanUseE() { return canUseE; }
    public bool IsCanUseQ() { return canUseQ; }
    public GameObject GetCameraPos() { return null; }
    public void Selected()
    {
        icon_ren = playerIcon.GetComponent<MeshRenderer>();

        icon_materialL.Clear();
        icon_materialL.AddRange(icon_ren.materials);
        //icon_materialL.Add(PrefabManager.Instance.outline);

        icon_ren.materials = icon_materialL.ToArray();
        selected_e = true;
    }

    public void Unselected()
    {
        MeshRenderer renderer_d = playerIcon.GetComponent<MeshRenderer>();

        icon_materialL.Clear();
        icon_materialL.AddRange(renderer_d.materials);
        //icon_materialL.Remove(PrefabManager.Instance.outline);

        icon_ren.materials = icon_materialL.ToArray();
        selected_e = false;
    }
    public int GetState() { return 0; }
}
