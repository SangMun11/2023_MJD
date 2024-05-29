using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System;

public class LSM_PlayerBase : MonoBehaviourPunCallbacks, IPunObservable, I_Actor, I_Characters, I_Playable
{
    protected GameObject playerCharacter;
    protected Rigidbody rigid;

    const float LAST_ATTACK_DELAY = 10f;

    // 이동속도, 점프 판별 함수
    protected float speed = 15.0f, currentSpeed;
    protected bool isGrounded;

    // 애니메이션 부분
    protected Transform myspine;
    protected Animator anim;
    protected int attackcode = 0;

    // 조작 부분
    protected bool canMove = true;


    // 조작 - 공격 관련 부분
    protected bool canAttack = true;
    protected bool canQ = true;
    protected bool canE = true;

    #region Camera Variants
    // 카메라 관련 변수들
    [Header("Camera Variable")]
    public Camera playerCamera;
    public GameObject camerapos, deadCamerapos; // eyes 연결
    bool cameraCanMove = false;
    bool invertCamera = false;
    protected float yaw = 0.0f;
    protected float pitch = 0.0f;
    public float mouseSensitivity = 3f; // 마우스 감도
    public float maxLookAngle = 50f; // 상하 시야각
    #endregion

    protected float CoolTime_Q, CoolTime_E;
    protected float timer_Q, timer_E;

    // 자폭 관련 변수
    protected float timer_F_Holder;
    protected bool pushing_F;

    protected GameObject last_Attack_Player;
    protected float timer_lastAttack;

    #region LSM Variable
    [Header("Player Info")]
    public string playerName;
    public MoonHeader.S_ActorState actorHealth;
    protected GameObject playerIcon;

    public LSM_PlayerCtrl myPlayerCtrl;
    public MoonHeader.State_P_Minion state_p;

    protected Vector3 networkPosition, networkVelocity;
    protected Vector3 preCamera;

    protected bool isMove;

    protected MeshRenderer icon_ren;
    protected List<Material> icon_materialL;
    protected bool selected_e;


    public float CollectingRadius;
    private float timer_collect;
    private byte settingLevel;

    public GameObject[] Sounds; //0 : 걷기, 1: 피격, 2 : 죽음
    #endregion

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) // 되는 것 같긴한데 실제로 적용되는지는 확인하기 힘듬 
    {
        if (stream.IsWriting)
        {
            stream.SendNext(playerName);

            ulong send_dummy = SendDummyMaker_LSM();
            int dummy_int1 = (int)(send_dummy & (ulong)uint.MaxValue);
            int dummy_int2 = (int)((send_dummy >> 32) & (ulong)uint.MaxValue);
            stream.SendNext(dummy_int1);
            stream.SendNext(dummy_int2);
            stream.SendNext(rigid.velocity);
            stream.SendNext(isMove);
            //stream.SendNext(Mathf.RoundToInt(pitch / 3));
        }
        else
        {
            this.playerName = (string)stream.ReceiveNext();

            int d1 = (int)stream.ReceiveNext();
            int d2 = (int)stream.ReceiveNext();

            ulong receive_dummy = (ulong)(d1) & (ulong)uint.MaxValue;
            receive_dummy += ((ulong)(d2) << 32);
            ReceiveDummyUnZip(receive_dummy);
            networkVelocity = (Vector3)stream.ReceiveNext();
            rigid.velocity = networkVelocity;
            isMove = (bool)stream.ReceiveNext();
            //pitch = (int)stream.ReceiveNext();
        }
    }

    // 패킷을 줄이기 위하여 압축해서 데이터를 전송.
    private ulong SendDummyMaker_LSM()
    {
        ulong send_dummy = 0;
        send_dummy += ((ulong)actorHealth.maxHealth & (ulong)ushort.MaxValue);
        send_dummy += ((ulong)(actorHealth.health) & (ulong)ushort.MaxValue) << 16;

        send_dummy += ((ulong)(actorHealth.team) & (ulong)byte.MaxValue) << 32;
        send_dummy += ((ulong)(actorHealth.Atk) & (ulong)ushort.MaxValue) << 40;
        send_dummy += ((ulong)(state_p) & (ulong)byte.MaxValue) << 56;
        return send_dummy;
    }
    // 압축된 데이터를 언집
    private void ReceiveDummyUnZip(ulong receive_dummy)
    {
        //actorHealth.maxHealth = (short)(receive_dummy & (ulong)ushort.MaxValue);
        actorHealth.maxHealth = (short)(receive_dummy & (ulong)ushort.MaxValue);
        actorHealth.health = (short)((receive_dummy >> 16) & (ulong)ushort.MaxValue);
        actorHealth.team = (MoonHeader.Team)((receive_dummy >> 32) & (ulong)byte.MaxValue);
        actorHealth.Atk = (short)((receive_dummy >> 40) & (ulong)ushort.MaxValue);
        state_p = (MoonHeader.State_P_Minion)((receive_dummy >> 56) & (ulong)byte.MaxValue);

    }

    protected virtual void Awake()
    {
        rigid = this.gameObject.GetComponent<Rigidbody>();
        playerCharacter = this.gameObject;
        //playerCamera = Camera.main;
        anim = this.gameObject.GetComponent<Animator>();
        myspine = anim.GetBoneTransform(HumanBodyBones.Spine);
        cameraCanMove = false;
        invertCamera = false;

        // LSM
        playerIcon = GameObject.Instantiate(PrefabManager.Instance.icons[4], transform);
        playerIcon.transform.localPosition = new Vector3(0, 33, 0);

        //
        icon_materialL = new List<Material>();
        selected_e = false;
        CollectingRadius = 10f;
        last_Attack_Player = null;
    }

    protected void Update()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (!ReferenceEquals(last_Attack_Player, null))
            {
                timer_lastAttack += Time.deltaTime;
                if (timer_lastAttack >= LAST_ATTACK_DELAY)
                { timer_lastAttack = 0; last_Attack_Player = null; }
            }
        }

        if (isMove) { PlaySFX(0); }

        // 지연보상에대한 내용.
        if (!photonView.IsMine)
        {
            rigid.velocity = networkVelocity;
            rigid.MovePosition(rigid.position + networkVelocity * Time.deltaTime);
            isGrounded = Physics.Raycast(this.transform.position + Vector3.up * 0.5f, Vector3.down, 0.8f, 1 << LayerMask.NameToLayer("Map"));
            anim.SetBool("InAir", !isGrounded);
            myspine.transform.localEulerAngles = new Vector3(-180, 0, pitch); // 척추 움직에 따른 시야 변경
            return;
        }

        if (canMove)
            Move();
        else
        {
            anim.SetFloat("Front", 0);
            anim.SetFloat("Right", 0);
        }
        if (state_p != MoonHeader.State_P_Minion.Dead)
        {
            AttackFunction();
            CollectingArea();
        }
        if (settingLevel != GetLV())
        { 
            settingLevel= GetLV();
            SettingUpdate();
        }
        // anim.SetBool("skillE_Bool", Input.GetKey(KeyCode.E));

    }

    protected void LateUpdate()
    {
        if (!photonView.IsMine)
            return;
        LookAround(); // 척추 움직임에 따른 시야 움직임이 적용될려면 이 함수가 LateUpdate()에서 호출 되어야함
    }

    public void SettingUpdate()
    {
        object[] dummy_o = LSM_SettingStatus.Instance.lvStatus[myPlayerCtrl.PlayerType].getStatus_LV(settingLevel);
        short[] add = GameManager.Instance.teamManagers[(int)actorHealth.team].GetAtkHp();
        MoonHeader.S_Status alpha = myPlayerCtrl.hasItems.GetPlusStatus();

        this.actorHealth.maxHealth = (short)((short)dummy_o[0] + add[0] + alpha.plusHP);
        this.actorHealth.Atk = (short)((short)dummy_o[1] + add[1] + alpha.plusATk);
    }

    protected virtual void AttackFunction() 
    {
        if (Input.GetKeyDown(KeyCode.F) && !pushing_F)
        {
            pushing_F = true;
            StartCoroutine(FSkill());
        }
    }

    #region Explosion~ F Skill
    protected IEnumerator FSkill()
    {
        GameObject dummy_Glow = PoolManager.Instance.Get_Particles(4, this.transform.position + Vector3.up * 1.5f);
        ParticleAutoDisable dummy_PAD = dummy_Glow.GetComponent<ParticleAutoDisable>();
        short pre_Health = this.actorHealth.health;
        timer_F_Holder = 0;
        float timer_dummy = 0;
        yield return new WaitForSeconds(Time.deltaTime);
        dummy_PAD.Particle_Size_Setting(1);
        while (true)
        {
            yield return new WaitForSeconds(Time.deltaTime);
            if (timer_dummy >= 0.5f)
            { dummy_PAD.Particle_Size_Setting(timer_F_Holder * 6 + 1); timer_dummy = 0; }

            timer_dummy += Time.deltaTime;
            timer_F_Holder += Time.deltaTime;

            if (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0 || pre_Health > this.actorHealth.health)
            {
                dummy_Glow.GetComponent<ParticleAutoDisable>().ParticleDisable();
                timer_F_Holder = 0;
                pushing_F = false;
                break;
            }
            else if (timer_F_Holder >= 3f)
            {
                dummy_Glow.GetComponent<ParticleAutoDisable>().ParticleDisable();
                break;
            }
        }
        pushing_F = false;
        dummy_PAD.Particle_Size_Setting(1);
        if (timer_F_Holder >= 3f)
        {
            state_p = MoonHeader.State_P_Minion.Dead;
            photonView.RPC("Explosion_s", RpcTarget.MasterClient);
            DeadProcessing(this.gameObject);
        }

        yield return new WaitForSeconds(1);
    }
    [PunRPC]
    protected void Explosion_s()
    {
        GameObject dummy_Explosion = PoolManager.Instance.Get_Particles(7, this.transform.position + Vector3.up * 1.5f);
        dummy_Explosion.GetComponent<LSM_BasicProjectile>().Setting(this.gameObject, Mathf.CeilToInt(this.actorHealth.health *0.2f), this.GetComponent<I_Actor>(), 0);
        dummy_Explosion.GetComponent<LSM_BasicProjectile>().Setting_Trigger_Exist_T(0.1f, 50f);
    }
    #endregion

    #region Moving and basic Control

    protected void Move()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        bool sprint = Input.GetButton("Sprint");

        // 애니메이션
        anim.SetFloat("Front", y);
        anim.SetFloat("Right", x);

        Vector3 moveX = transform.right * x;
        Vector3 moveY = transform.forward * y;

        Vector3 thisVelocity = (moveX + moveY).normalized;
        //rigid.MovePosition(transform.position + thisVelocity * Time.deltaTime * speed);       // 웬지 모르게 fps차이에 따라서 속도가 다름...

        // 현재 가려고하는 방향에 맵이 존재하는지 확인.
        //Debug.DrawRay(this.transform.position + Vector3.up * 0.5f, thisVelocity * 1.5f, Color.blue);
        bool isborder = Physics.Raycast(this.transform.position + Vector3.up * 0.5f, thisVelocity, 0.8f, 1 << LayerMask.NameToLayer("Map"));
        // 현재 입력한 방향 중, x방향 즉 왼 오른쪽에 장애물이 있는지, 대각이동시에 사용
        //Debug.DrawRay(this.transform.position + Vector3.up * 0.5f, moveX * 3, Color.red);
        bool isborder_x = Physics.Raycast(this.transform.position + Vector3.up * 0.5f, x * Vector3.right, 1.3f, 1 << LayerMask.NameToLayer("Map"));
        // 현재 입력한 방향 중, z방향 즉 앞 뒤에 장애물이 있는지, 대각이동시에 사용
        //Debug.DrawRay(this.transform.position + Vector3.up * 0.5f, moveY * 3, Color.black);
        bool isborder_z = Physics.Raycast(this.transform.position + Vector3.up * 0.5f, y * Vector3.forward, 1.3f, 1 << LayerMask.NameToLayer("Map"));


        thisVelocity = new Vector3(thisVelocity.x * (isborder_x ? 0.4f : 1), 0, thisVelocity.z * (isborder_z ? 0.4f : 1));
        //if (!isborder)this.transform.position = this.transform.position + thisVelocity * currentSpeed * Time.deltaTime * (sprint?1.5f:1f);
        if (!isborder) rigid.MovePosition(rigid.position + thisVelocity * currentSpeed * Time.deltaTime * (sprint ? 1.5f : 1f));        

        // 점프
        isGrounded = Physics.Raycast(this.transform.position + Vector3.up * 0.5f, Vector3.down, 0.8f, 1 << LayerMask.NameToLayer("Map"));
        Debug.DrawRay(this.transform.position + Vector3.up * 0.5f, Vector3.down * 0.8f, Color.red);
        //bool canJump = !isGrounded;

        if ((x != 0 || y != 0 )&& isGrounded) { isMove = true; }
        else { isMove = false; }

        anim.SetBool("InAir", !isGrounded);


        //anim.SetBool("isJump", !canJump && canQ);
        if (isGrounded)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                photonView.RPC("Jump_RPC", RpcTarget.All);
                rigid.AddForce(Vector3.up * 20f, ForceMode.Impulse);
            }
        }
    }
    [PunRPC] protected void Jump_RPC() { anim.SetTrigger("Jump"); }

    protected void LookAround()
    {
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

            myspine.transform.localEulerAngles = new Vector3(-180, 0, pitch); // 척추 움직에 따른 시야 변경
            // camerapos.transform.localEulerAngles = new Vector3(pitch, 0, 0);
            playerCamera.transform.localEulerAngles = new Vector3(pitch, yaw, 0);

            // 잔흔들림 방지용
            if (Vector3.Distance(camerapos.transform.position + this.transform.forward * 0.2f, preCamera) >= 0.05f)
            {
                playerCamera.transform.position = camerapos.transform.position + this.transform.forward * 0.3f;
                preCamera = playerCamera.transform.position;
            }
        }

        // playerCamera.transform.rotation = camerapos.transform.rotation;
    }
    #endregion

    //기본공격
    protected virtual IEnumerator basicAttack() { yield return null; }

    protected virtual IEnumerator QSkill() { yield return null; }

    protected virtual IEnumerator ESkill() { yield return null; }

    protected virtual void CoolManager() { }

    public void AttackThem(GameObject obj)
    {
        if (!ReferenceEquals(obj.GetComponent<I_Actor>(), null))
        {
            obj.GetComponent<I_Actor>().Damaged(this.actorHealth.Atk, this.transform.position, this.actorHealth.team, this.gameObject);

            Debug.Log("Attack! : " + obj.name);
        }
    }

    // 데미지 입은 위치를 플레이어가 확인할 수 있게 설정.
    private void DamagedRotation(Vector3 origin)
    {
        GameObject dummy_u = GameObject.Instantiate(PrefabManager.Instance.icons[5], GameManager.Instance.gameUI_SC.DamagedDirection.transform);
        LSM_DamagedDirection d = dummy_u.GetComponent<LSM_DamagedDirection>();
        d.SpawnSetting(this.gameObject, origin);
    }

    #region SpawnSetting
    // LSM Spawn Setting
    public virtual void SpawnSetting(MoonHeader.Team t, short monHealth, string pname, LSM_PlayerCtrl pctrl)
    {
        //Health = monHealth * 10;
        // 디버그용. 현재 강령하는 미니언의 체력의 10배율로 강령, 공격력을 10으로 디폴트. 이후 플레이어 공격력으로 변경할 예정
        this.photonView.RequestOwnership();
        //actorHealth = new MoonHeader.S_ActorState(100, 10, t);
        //actorHealth.health = (short)(monHealth * 10);
        playerName = pname;
        myPlayerCtrl = pctrl;
        state_p = MoonHeader.State_P_Minion.Normal;
        this.settingLevel = this.GetLV();

        //photonView.RPC("SpawnSetting_RPC", RpcTarget.All, (short)100, (short)(monHealth * 10), pname, (int)t);

        // 초기화
        canAttack = true;
        canMove = true;
        speed = 10.0f;
        canE = true;
        canQ = true;
        cameraCanMove = true;
        invertCamera = false;
        currentSpeed = speed;
        timer_F_Holder = 0;
        pushing_F = false;
    }

    [PunRPC]
    protected void SpawnSetting_RPC(short mh, short h, string name, int t)
    {
        rigid.useGravity = true;
        this.gameObject.layer = 7;
        this.actorHealth.maxHealth = mh;
        this.actorHealth.health = h;
        this.playerName = name;
        this.actorHealth.team = (MoonHeader.Team)t;
        this.actorHealth.type = MoonHeader.AttackType.Melee;
        foreach (LSM_PlayerCtrl sc in GameManager.Instance.players)
        {
            if (sc.playerName.Equals(name)) { myPlayerCtrl = sc; break; }
        }
        //if (selected_e)
        //Unselected();

        this.transform.name = playerName;
        GameManager.Instance.playerMinions[(int)actorHealth.team].Add(this.gameObject);
        ChangeTeamColor(playerIcon);
    }
    #endregion

    #region Damaged()
    // LSM Damaged 추가.
    public void Damaged(short dam, Vector3 origin, MoonHeader.Team t, GameObject other)
    {

        Damaged(dam, origin, t, other, 0f);
    }
    public void Damaged(short dam, Vector3 origin, MoonHeader.Team t, GameObject other, float power)
    {
        if (t == actorHealth.team || state_p == MoonHeader.State_P_Minion.Dead)
            return;

        PlaySFX(1);
        if (!PhotonNetwork.IsMasterClient)
            return;
        if (other.CompareTag("PlayerMinion"))
        { last_Attack_Player = other; timer_lastAttack = 0; }
        else if (other.CompareTag("DamageArea"))
        {last_Attack_Player = other.GetComponent<LSM_BasicProjectile>().orner; timer_lastAttack = 0; }

        // 죽음 처리
        if (this.actorHealth.health - dam <= 0 && state_p != MoonHeader.State_P_Minion.Dead)
        {
            state_p = MoonHeader.State_P_Minion.Dead;
            //StartCoroutine(DeadProcessing(other)); 
            DeadProcessing(other);
        }
        // 죽지 않을 경우.
        else if (this.actorHealth.health - dam > 0)
        {

            photonView.RPC("Dam_RPC", RpcTarget.All, dam, origin, power);
        }
        return;
    }


    [PunRPC]
    protected void Dam_RPC(short dam, Vector3 origin, float power)
    {
        actorHealth.health -= dam;
        if (photonView.IsMine)
        {
            rigid.AddForce((this.transform.position - origin).normalized * power, ForceMode.Impulse);
            DamagedRotation(origin);
        }
    }
    [PunRPC]
    protected void Dead_RPC()
    {
        this.gameObject.layer = 12;
        state_p = MoonHeader.State_P_Minion.Dead;
        PlaySFX(2);
        if (photonView.IsMine)
        {
            StartCoroutine(DeadInOrner());
            myPlayerCtrl.PlayerMinionDeadProcessing();
        }
    }
    // LSM DeadProcessing
    public void DeadProcessing(GameObject other)
    {
        canMove = false;
        photonView.RPC("Dead_RPC", RpcTarget.All);
        timer_F_Holder = 0;
        pushing_F = false;
        cameraCanMove = false;

        Debug.Log("PlayerMinion Dead");
        GameManager.Instance.PlayerMinionRemover(actorHealth.team, playerName);

        // 마지막 타격이 플레이어라면, 경험치 및 로그창 띄우기.
        if (!other.Equals(this.gameObject))
        {
            //if (other.transform.CompareTag("PlayerMinion"))
            //{
            //    other.GetComponent<I_Characters>().AddEXP(100);
                //other.GetComponent<I_Playable>().AddKill();
                //other.GetComponent<PSH_PlayerFPSCtrl>().myPlayerCtrl.GetExp(50);   // 디버깅용으로 현재 경험치를 50으로 고정 지급.
            //}
            //if (other.transform.CompareTag("DamageArea"))
            //{ other.GetComponent<LSM_W_Slash>().orner_ch.AddEXP(100); }

            if (!ReferenceEquals(last_Attack_Player, null))
            {
                last_Attack_Player.GetComponent<I_Playable>().AddKill();
                last_Attack_Player.GetComponent<I_Characters>().AddEXP(150);
                last_Attack_Player.GetComponent<I_Playable>().AddCollector(300);
                GameManager.Instance.DisplayAdd(string.Format("{0}가 {1}를 처치했습니다.", last_Attack_Player.name, this.name));
            }
            else 
            {
                GameManager.Instance.DisplayAdd(string.Format("{0}가 {1}를 처형하였습니다.", other.gameObject.name, this.name));
            }
        }
        AddDeath();
    }
    private IEnumerator DeadInOrner()
    {
        canMove = false;
        timer_F_Holder = 0;
        pushing_F = false;
        cameraCanMove = false;
        rigid.useGravity = false;

        int d_for = Mathf.CeilToInt(1.5f / Time.deltaTime);
 
        for (int i = 0; i < d_for; i++)
        {

            yield return new WaitForSeconds(Time.deltaTime);
            if (Mathf.CeilToInt(d_for / 3 * 2) == i) { StartCoroutine(GameManager.Instance.ScreenFade(false)); }
            playerCamera.transform.position = Vector3.Lerp(playerCamera.transform.position, deadCamerapos.transform.position, Time.deltaTime * 2);
            playerCamera.transform.rotation = Quaternion.Lerp(playerCamera.transform.rotation, deadCamerapos.transform.rotation, Time.deltaTime * 5);
        }
        yield return new WaitForSeconds(0.5f);
        cameraCanMove = false;
        playerCamera = null;
        MinionDisable();
    }


    #endregion

    #region ChangeTeamColor(obj)
    // 플레이어 아이콘 색변경.
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
    #endregion

    #region ParentSetting
    public void ParentSetting_Pool(int index) { photonView.RPC("ParentSetting_Pool_RPC", RpcTarget.AllBuffered, index); }
    [PunRPC]
    protected void ParentSetting_Pool_RPC(int index)
    {
        this.transform.parent = PoolManager.Instance.gameObject.transform;
        PoolManager.Instance.poolList_PlayerMinions[index].Add(this.gameObject);
    }
    #endregion

    #region MinionDisable()
    public void MinionDisable() { photonView.RPC("DeadProcessing", RpcTarget.All); }
    [PunRPC]
    protected void DeadProcessing()
    {
        if (photonView.IsMine)
            myPlayerCtrl.PlayerMinionDeadProcessing();
        this.gameObject.SetActive(false);
    }
    #endregion

    #region MinionEnable()
    public void MinionEnable() { photonView.RPC("MinionEnable_RPC", RpcTarget.All); }
    [PunRPC] protected void MinionEnable_RPC() { this.gameObject.SetActive(true); }
    #endregion

    protected void PlaySFX(int num)
    {
        AudioSource dummy_s = Sounds[num].GetComponent<AudioSource>();
        if (dummy_s.isPlaying) { return; }
        else dummy_s.Play();
    }
    protected void StopSFX(int num)
    {
        AudioSource dummy_s = Sounds[num].GetComponent<AudioSource>();
        if (dummy_s.isPlaying) { dummy_s.Stop(); }
        else { return; }
    }
    // I_Actor 인터페이스에 미리 선언해둔 함수들 구현
    public short GetHealth() { return this.actorHealth.health; }
    public short GetMaxHealth() { return this.actorHealth.maxHealth; }
    public MoonHeader.Team GetTeam() { return this.actorHealth.team; }
    public void AddEXP(short exp) { this.myPlayerCtrl.SetExp(exp); }
    public MoonHeader.S_ActorState GetActor() { return this.actorHealth; }
    public GameObject GetCameraPos() { return camerapos; }
    public void Selected()
    {
        /*
        icon_ren = playerIcon.GetComponent<MeshRenderer>();

        icon_materialL.Clear();
        icon_materialL.AddRange(icon_ren.materials);
        icon_materialL.Add(PrefabManager.Instance.outline);

        icon_ren.materials = icon_materialL.ToArray();
        selected_e = true;
        */
        this.playerIcon.GetComponent<Renderer>().material.color = MoonHeader.SelectedColors[(int)this.actorHealth.team];
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
    public int GetState() { return (int)state_p; }

    #region I_Playable
    public float IsCanUseE() { return canE? 0 : 1-(timer_E / CoolTime_E); }
    public float IsCanUseQ() { return canQ ? 0 : 1 - (timer_Q / CoolTime_Q); }
    public bool IsCanHit() { return canAttack; }

    public GameObject CameraSetting(GameObject cam)
    {
        playerCamera = cam.GetComponent<Camera>();
        return camerapos;
    }
    public int GetExp()
    {
        return myPlayerCtrl.GetExp();
    }
    public int GetGold() { return myPlayerCtrl.GetGold(); }
    public byte GetLV() { return myPlayerCtrl.GetLevel(); }
    public void CollectingArea()
    {
        timer_collect += Time.deltaTime;
        if (timer_collect >= 0.5f)
        {
            timer_collect = 0;
            RaycastHit[] hits = Physics.SphereCastAll(transform.position, CollectingRadius, Vector3.up, 0, 1 << LayerMask.NameToLayer("Item"));
            foreach (RaycastHit item in hits)
            {
                LSM_ItemSC dummy_i = item.transform.GetComponent<LSM_ItemSC>();
                if (!dummy_i.isCollecting)
                {
                    int size_d = dummy_i.Getting();
                    dummy_i.ItemDisable();
                    GameObject dummy_e = PoolManager.Instance.Get_Local_Item(0);
                    dummy_e.transform.position = item.transform.position + Vector3.up * 1f;
                    dummy_e.GetComponent<LSM_ItemCollectAnim>().TargetLockOn(this.gameObject, size_d);
                }
            }
        }

    }
    public void AddKill()
    { photonView.RPC("AddK_RPC", RpcTarget.AllBuffered); }
    [PunRPC] protected void AddK_RPC() {if (photonView.IsMine) myPlayerCtrl.AddingKD(0); }

    public void AddDeath() 
    { photonView.RPC("AddD_RPC", RpcTarget.AllBuffered); }
    [PunRPC] protected void AddD_RPC() { if (photonView.IsMine) myPlayerCtrl.AddingKD(1); }

    public void AddCS()
    { photonView.RPC("AddCS_RPC", RpcTarget.AllBuffered); }
    [PunRPC] protected void AddCS_RPC() { if (photonView.IsMine) myPlayerCtrl.AddingCS(); }
    public void AddTD()
    { photonView.RPC("AddTD_RPC", RpcTarget.AllBuffered); }
    [PunRPC] protected void AddTD_RPC() { if (photonView.IsMine) myPlayerCtrl.AddingTD(); }

    public void AddCollector(int s) { myPlayerCtrl.GetGold((short)s); }
    public float GetF()
    {
        return timer_F_Holder;
    }
    #endregion

}
