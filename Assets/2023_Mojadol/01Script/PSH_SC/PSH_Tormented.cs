using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// using Photon.Pun;
// using Unity.VisualScripting;
// PunCallbacks, I_Actor, IPunObservable, I_Characters, I_Playable

public class PSH_Tormented : MonoBehaviour
{
    GameObject playerCharacter;
    Rigidbody rigid;

    // 이동속도, 점프 판별 함수
    public float speed = 15.0f, currentSpeed;
    bool isGrounded;

    // 애니메이션 부분
    Transform myspine;
    Animator anim;
    int attackcode = 0;

    // 조작 부분
    bool canMove = true;


    // 조작 - 공격 관련 부분
    bool canAttack = true;
    bool canQ = true;
    bool canE = true;

    // 투사체
    public GameObject spawnPoint;
    public GameObject basicProjectile;
    public GameObject qProjectile;
    public GameObject eProjectilepos;

    #region Camera Variants
    // 카메라 관련 변수들
    public Camera playerCamera;
    public GameObject camerapos; // eyes 연결
    bool cameraCanMove = true;
    bool invertCamera = false;
    float yaw = 0.0f;
    float pitch = 0.0f;
    public float mouseSensitivity = 3f; // 마우스 감도
    public float maxLookAngle = 50f; // 상하 시야각
    #endregion

    // 쿨타임 관련 변수.
    private float CoolTime_Q, CoolTime_E;
    private float timer_Q, timer_E, timer_Combo;

    private float time;

    #region LSM Variables, OnPhotonSerialize, Send Receive Dummy 주석처리 

    /*

    #region LSM Variable
    public string playerName;
    public MoonHeader.S_ActorState actorHealth;
    private GameObject playerIcon;

    public LSM_PlayerCtrl myPlayerCtrl;
    public MoonHeader.State_P_Minion state_p;

    private Vector3 networkPosition, networkVelocity;

    private LSM_WeaponSC weaponSC;
    private MeshCollider weapon_C;
    private MeshRenderer icon_ren;
    private List<Material> icon_materialL;
    private bool selected_e;

    public float CollectingRadius, timer_collect;
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

    */
    #endregion



    private void Awake()
    {
        rigid = this.gameObject.GetComponent<Rigidbody>();
        playerCharacter = this.gameObject;
        playerCamera = Camera.main;
        anim = this.gameObject.GetComponent<Animator>();
        myspine = anim.GetBoneTransform(HumanBodyBones.Spine);
        cameraCanMove = true;
        invertCamera = false;

        /*
        // LSM
        playerIcon = GameObject.Instantiate(PrefabManager.Instance.icons[4], transform);
        playerIcon.transform.localPosition = new Vector3(0, 33, 0);
        weaponSC = this.transform.GetComponentInChildren<LSM_WeaponSC>();
        weapon_C = weaponSC.transform.GetComponent<MeshCollider>();
        weapon_C.enabled = false;
        //
        CoolTime_E = 5f;
        CoolTime_Q = 3f;
        icon_materialL = new List<Material>();
        selected_e = false;
        CollectingRadius = 5f;
        */

    }

    // Update is called once per frame
    void Update()
    {
        Move();
        CoolManager();

        if (Input.GetMouseButtonDown(0) && canAttack)
        {
            canAttack = false;
            StartCoroutine(basicAttackDelay());
            Invoke("BasicTormentedProjectile", 0.75f);
        }

        if (Input.GetKeyDown(KeyCode.Q) && canQ && canAttack)
        {
            StartCoroutine(Qskill());
            Invoke("QSkill_Effect", 1f);
        }

        if (Input.GetKeyDown(KeyCode.E) && canE && canAttack)
        {
            StartCoroutine(Eskill());
            ESkill_Effect();
        }

        eProjectilepos.transform.localRotation = Quaternion.Euler(pitch, eProjectilepos.transform.rotation.y, eProjectilepos.transform.rotation.z);
    }

    private void LateUpdate()
    {
        LookAround();
    }

    void Move()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        bool sprint = Input.GetKey(KeyCode.LeftShift);

        // 애니메이션
        /*
        anim.SetBool("isRunFront", Input.GetKey(KeyCode.W));
        anim.SetBool("isRunBack", Input.GetKey(KeyCode.S));
        anim.SetBool("isRunRight", Input.GetKey(KeyCode.D));
        anim.SetBool("isRunLeft", Input.GetKey(KeyCode.A));
        */
        anim.SetFloat("Front", y);
        anim.SetFloat("Right", x);

        Vector3 moveX = transform.right * x;
        Vector3 moveY = transform.forward * y;

        Vector3 thisVelocity = (moveX + moveY).normalized;
        //rigid.MovePosition(transform.position + thisVelocity * Time.deltaTime * speed);       // 웬지 모르게 fps차이에 따라서 속도가 다름...
        this.transform.position = this.transform.position + thisVelocity * currentSpeed * Time.deltaTime * (sprint ? 1.5f : 1f);
        // this.transform.position = this.transform.position + thisVelocity * currentSpeed * Time.deltaTime;

        // 점프
        isGrounded = Physics.Raycast(this.transform.position + new Vector3(0f, 0.5f, 0f), Vector3.down, 1f, 1 << LayerMask.NameToLayer("Map"));
        Debug.DrawRay(this.transform.position + Vector3.up * 0.5f, Vector3.down * 1f, Color.red);
        //bool canJump = !isGrounded;

        //anim.SetBool("InAir", !isGrounded);


        //anim.SetBool("isJump", !canJump && canQ);
        if (isGrounded)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                // photonView.RPC("Jump_RPC", RpcTarget.All);
                rigid.AddForce(Vector3.up * 500f);
            }
        }
    }

    void LookAround()
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
            playerCamera.transform.position = camerapos.transform.position + this.transform.forward * 0.2f;
        }

        // playerCamera.transform.rotation = camerapos.transform.rotation;
    }

    #region New Attack & Skills
    void BasicTormentedProjectile()
    {
        GameObject bp_prefab = Instantiate(basicProjectile, spawnPoint.transform.position, 
            Quaternion.Euler(pitch, yaw, spawnPoint.transform.rotation.z));
        
        bp_prefab.GetComponent<Rigidbody>().AddRelativeForce(Vector3.forward * 500f);
    }

    void QSkill_Effect()
    {
        GameObject q_prefab = Instantiate(qProjectile, spawnPoint.transform.position,
            Quaternion.Euler(pitch, yaw, spawnPoint.transform.position.z));
        q_prefab.GetComponent<Rigidbody>().AddRelativeForce(Vector3.forward * 400f);
    }

    void ESkill_Effect()
    {
        eProjectilepos.SetActive(true);
        eProjectilepos.GetComponent<PSH_T_E>().timer1 = 0.0f;
        
        /*
        GameObject e_prefab = Instantiate(eProjectile, spawnPoint.transform.position,
            Quaternion.Euler(pitch, yaw, spawnPoint.transform.rotation.z));
        e_prefab.transform.position = spawnPoint.transform.position;
        e_prefab.transform.rotation = Quaternion.Euler(pitch, yaw, spawnPoint.transform.rotation.z);
        */
    }

    #endregion

    IEnumerator basicAttackDelay()
    {
        canAttack = false;
        anim.SetLayerWeight(1, 1f);
        anim.SetTrigger("basicAttack0");
        //anim.SetLayerWeight(1, 1f);
        //anim.SetTrigger("basicAttack" + attackcode.ToString());
        /*
        if (photonView.IsMine)
        {
            anim.SetLayerWeight(1, 1f);
            anim.SetTrigger("basicAttack" + attackcode.ToString());
            StartCoroutine(weaponSC.SwordEffect(attackcode));
            Invoke("AnimatorLayerReset", 1.3f);
        }

        photonView.RPC("basicAnim_RPC", RpcTarget.All, attackcode);
        */

        attackcode++;
        this.currentSpeed = speed / 2;
        // photonView.RPC("WeaponTriggerEnable", RpcTarget.MasterClient, true);
        yield return new WaitForSeconds(1.5f);
        anim.SetLayerWeight(1, 0f);
        this.currentSpeed = speed;
        // yield return new WaitForSecondsRealtime(attackcode == 4 ? 2f : 0.5f);
        attackcode %= 4;
        timer_Combo = 0;

        //anim.SetLayerWeight(1, 0f);
        canAttack = true;
        //StopCoroutine(basicAttackDelay());
    }

    IEnumerator Qskill()
    {
        canQ = false;
        canAttack = false;
        canMove = false;
        timer_Q = 0;
        // anim.applyRootMotion = true;
        // anim.SetLayerWeight(1, 1f);
        anim.SetTrigger("skillQ_Trigger");
        // photonView.RPC("QAnim_RPC", RpcTarget.All);
        yield return new WaitForSecondsRealtime(1.3f);
        // photonView.RPC("AttackEffect_Q", RpcTarget.MasterClient, this.playerCamera.transform.forward);
        // yield return new WaitForSecondsRealtime(0.7f);
        // canQ = true;
        // anim.SetLayerWeight(1, 0f);
        canAttack = true;
        canMove = true;
        //anim.applyRootMotion = false;
    }

    IEnumerator Eskill()
    {
        anim.SetLayerWeight(1, 1f);
        anim.SetTrigger("skillE_Trigger");
        canMove = false;
        canAttack = false;
        canE = false;

        yield return new WaitForSecondsRealtime(7.0f);

        anim.SetLayerWeight(1, 0f);
        canMove = true;
        canAttack = true;
        canE = true;
        anim.speed = 1f;
        //StopCoroutine(Eskill());
    }

    private void CoolManager()
    {
        if (!canE)
        {
            timer_E += Time.deltaTime;
            if (CoolTime_E <= timer_E)
            {
                timer_E = 0; canE = true;
            }
        }
        if (!canQ)
        {
            timer_Q += Time.deltaTime;
            if (CoolTime_Q <= timer_Q)
            {
                timer_Q = 0; canQ = true;
            }
        }
    }

    #region 따로 모아둠

    /*


    private void AttackFunction()
    {
        if (Input.GetMouseButtonDown(0) && canAttack)
        {
            canAttack = false;
            StartCoroutine(basicAttackDelay());
        }

        if (Input.GetKeyDown(KeyCode.Q) && canQ && canAttack)
        {
            StartCoroutine(Qskill());
        }

        // ESkill();
        CoolManager();

        if (attackcode == 1 || attackcode == 2 || attackcode == 3)
        {
            timer_Combo += Time.deltaTime;
            if (timer_Combo >= 3f)
            {
                attackcode = 0;
                timer_Combo = 0f;
            }
        }
    }

    [PunRPC] private void Jump_RPC() { anim.SetTrigger("Jump"); }

    private void AnimatorLayerReset()
    {
        anim.SetLayerWeight(1, 0f);
        //photonView.RPC("WeaponTriggerEnable", RpcTarget.MasterClient, false); 
    }
    private void AnimatorRootMotionReset()
    {
        anim.applyRootMotion = false;
        //photonView.RPC("WeaponTriggerEnable", RpcTarget.MasterClient, false);
    }

    [PunRPC]
    private void WeaponTriggerEnable(bool b)
    {
        weapon_C.enabled = b;
    }


    [PunRPC]
    private void basicAnim_RPC(int attackcode)
    {
        if (photonView.IsMine)
            return;

        anim.SetLayerWeight(1, 1f);

        anim.SetTrigger("basicAttack" + attackcode.ToString());
        Invoke("AnimatorLayerReset", 1.3f);
    }

    public void AttackEffect_B(Vector3 dummypos, Vector3 n, float s, float v, Vector3 forward_)
    {
        photonView.RPC("AttackEB_RPC", RpcTarget.MasterClient, dummypos, n, s, v, forward_);
    }

    [PunRPC]
    public void AttackEB_RPC(Vector3 dummypos, Vector3 n, float s, float v, Vector3 forward_)
    {
        GameObject effect_d = PoolManager.Instance.Get_Particles(1, dummypos,
            Quaternion.LookRotation(forward_, n).eulerAngles);
        effect_d.GetComponent<LSM_W_Slash>().Setting(this.gameObject, this.actorHealth.Atk, this.GetComponent<I_Actor>(), v + (rigid.velocity.magnitude));
        //effect_d.transform.LookAt(forward_ + dummypos, n);
        effect_d.transform.localScale = Vector3.one * s;
        effect_d.transform.position = dummypos + forward_ * 3f;
    }

    [PunRPC]
    private void QAnim_RPC()
    {
        if (photonView.IsMine)
            anim.applyRootMotion = true;
        //photonView.RPC("WeaponTriggerEnable", RpcTarget.MasterClient, true);
        anim.SetTrigger("skillQ_Trigger"); Invoke("AnimatorRootMotionReset", 1.65f);
    }

    [PunRPC]
    private void AttackEffect_Q(Vector3 forward)
    {
        Vector3 dummy_position = this.transform.position + this.transform.forward * 1 + Vector3.up;

        GameObject effect_d = PoolManager.Instance.Get_Particles(2, dummy_position
            , Quaternion.LookRotation(forward, this.transform.right).eulerAngles);

        //effect_d.transform.LookAt(forward + effect_d.transform.position, this.transform.right);
        effect_d.transform.localScale = Vector3.one * 1.7f;
        effect_d.GetComponent<LSM_W_Slash>().Setting(this.gameObject, Mathf.CeilToInt((float)this.actorHealth.Atk * 1.5f), this.GetComponent<I_Actor>(), 5f);
    }

    private void EskillOver() { canMove = true; canAttack = true; }

    [PunRPC]
    private void EAnim_RPC()
    {
        anim.SetLayerWeight(1, 1f);
        anim.SetTrigger("skillE_Trigger");
    }

    [PunRPC] private void EAnim_Pause() { anim.speed = 0f; }

    [PunRPC]
    private void EAnimE_RPC(Vector3 forward)
    {
        StartCoroutine(EAnimE_IE(forward));
        //AnimatorLayerReset();
    }
    private IEnumerator EAnimE_IE(Vector3 forward)
    {
        anim.speed = 1f;
        yield return new WaitForSeconds(0.5f);
        if (PhotonNetwork.IsMasterClient) AttackEffect_E(forward);
        yield return new WaitForSeconds(1f);
        AnimatorLayerReset();
    }
    private void AttackEffect_E(Vector3 forward)
    {
        Vector3 dummy_position = this.transform.position + this.transform.forward * 1 + Vector3.up;
        GameObject effect_d = PoolManager.Instance.Get_Particles(3, dummy_position
            , Quaternion.LookRotation(forward, this.transform.right).eulerAngles);
        //effect_d.transform.position = this.transform.position + this.transform.forward * 1 + Vector3.up;
        //effect_d.transform.LookAt(forward + effect_d.transform.position, this.transform.right);
        effect_d.transform.localScale = Vector3.one * 1.7f;
        effect_d.GetComponent<LSM_W_Slash>().Setting(this.gameObject, Mathf.CeilToInt((float)this.actorHealth.Atk * 2f), this.GetComponent<I_Actor>(), 8f);
    }

    public void AttackThem(GameObject obj)
    {
        if (!ReferenceEquals(obj.GetComponent<I_Actor>(), null))
        {
            obj.GetComponent<I_Actor>().Damaged(this.actorHealth.Atk, this.transform.position, this.actorHealth.team, this.gameObject);

            Debug.Log("Attack! : " + obj.name);
        }
    }

    private void DamagedRotation(Vector3 origin)
    {
        GameObject dummy_u = GameObject.Instantiate(PrefabManager.Instance.icons[5], GameManager.Instance.gameUI_SC.DamagedDirection.transform);
        LSM_DamagedDirection d = dummy_u.GetComponent<LSM_DamagedDirection>();
        d.SpawnSetting(this.gameObject, origin);
    }

    #region SpawnSetting
    // LSM Spawn Setting
    public void SpawnSetting(MoonHeader.Team t, short monHealth, string pname, LSM_PlayerCtrl pctrl)
    {
        //Health = monHealth * 10;
        // 디버그용. 현재 강령하는 미니언의 체력의 10배율로 강령, 공격력을 10으로 디폴트. 이후 플레이어 공격력으로 변경할 예정
        this.photonView.RequestOwnership();
        actorHealth = new MoonHeader.S_ActorState(100, 10, t);
        actorHealth.health = (short)(monHealth * 10);
        playerName = pname;
        myPlayerCtrl = pctrl;
        state_p = MoonHeader.State_P_Minion.Normal;

        photonView.RPC("SpawnSetting_RPC", RpcTarget.All, (short)100, (short)(monHealth * 10), pname, (int)t);

        // 초기화
        canAttack = true;
        canMove = true;
        speed = 5.0f;
        canE = true;
        canQ = true;
        cameraCanMove = true;
        invertCamera = false;
        currentSpeed = speed;
        timer_E = 0;
        timer_Q = 0;
    }

    [PunRPC]
    private void SpawnSetting_RPC(short mh, short h, string name, int t)
    {
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

        if (t == actorHealth.team || state_p == MoonHeader.State_P_Minion.Dead)
            return;
        if (this.actorHealth.health - dam <= 0 && state_p != MoonHeader.State_P_Minion.Dead)
        { state_p = MoonHeader.State_P_Minion.Dead; StartCoroutine(DeadProcessing(other)); }
        else if (this.actorHealth.health - dam > 0)
        {
            photonView.RPC("Dam_RPC", RpcTarget.All, dam, origin);
        }
        return;
    }
    [PunRPC]
    private void Dam_RPC(short dam, Vector3 origin)
    {
        if (photonView.IsMine)
        {
            actorHealth.health -= dam;
            DamagedRotation(origin);
        }
    }
    [PunRPC]
    private void Dead_RPC()
    {
        state_p = MoonHeader.State_P_Minion.Dead;
        if (photonView.IsMine)
            myPlayerCtrl.PlayerMinionDeadProcessing();
    }
    // LSM DeadProcessing
    public IEnumerator DeadProcessing(GameObject other)
    {
        canMove = false;
        photonView.RPC("Dead_RPC", RpcTarget.All);
        Debug.Log("PlayerMinion Dead");
        GameManager.Instance.PlayerMinionRemover(actorHealth.team, playerName);
        // 마지막 타격이 플레이어라면, 경험치 및 로그창 띄우기.
        if (other.transform.CompareTag("PlayerMinion"))
        {
            other.GetComponent<I_Characters>().AddEXP(50);
            //other.GetComponent<PSH_PlayerFPSCtrl>().myPlayerCtrl.GetExp(50);   // 디버깅용으로 현재 경험치를 50으로 고정 지급.
        }
        GameManager.Instance.DisplayAdd(string.Format("{0} Killed {1}", other.gameObject.name, this.name));
        yield return new WaitForSeconds(0.5f);
        MinionDisable();

        cameraCanMove = false;
        playerCamera = null;

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
    private void ParentSetting_Pool_RPC(int index)
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

        /*
        // this.playerIcon.GetComponent<Renderer>().material.color = MoonHeader.SelectedColors[(int)this.actorHealth.team];
    }
        */
        /*
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
    public bool IsCanUseE() { return canE; }
    public bool IsCanUseQ() { return canQ; }
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

    public void AddCollector(int s) { myPlayerCtrl.GetGold(s); }

    #endregion

    */
    #endregion
}
