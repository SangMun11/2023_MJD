using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static MoonHeader;
/*
public class LSM_PSHPlayerFPSCtrl : MonoBehaviour, I_Actor
{
    enum State { Normal, Attacking, Blocking, Casting, Exhausting};
    State state = State.Normal;

    protected float BASIC_ATTACKDELAY = 1.0f;

    // 플레이어 변수
    protected int basicDamage = 20;
    protected int currentDamage;

    // 공격 기능 관련 변수
    public GameObject attackRange;
    public GameObject swordBall_Prefab;
    protected bool canAttack;

    // 스킬 관련 변수
    protected int qDamage_ = 30, eDamage_ = 25;
    protected float qDelay_, eDelay_;
    protected bool canUseQ, canUseE;

    // 스킬 레벨 변수
    protected int basicLevel = 1;
    protected int qLevel, eLevel;

    // 이동 관련 변수
    public bool canMove;
    public float basicMoveSpeed = 5.0f;
    public float moveSpeed = 5.0f;
    public bool isSpeedDown;

    // 카메라 관련 변수
    public Camera playerCamera;
    public GameObject camerapos;
    public bool canSee = true;
    protected bool cameraCanMove;
    protected bool invertCamera;
    protected float yaw, pitch;
    public float mouseSensitivity = 3f;
    public float maxLookAngle = 50f;

    protected float timer_RecoverSpeed, timer_BA, timer_EC, timer_QC, timer_dummy_Casting;

    public string playerName;
    public MoonHeader.S_ActorState actorHealth;
    protected GameObject playerIcon;

    protected Rigidbody rigid;
    public LSM_PlayerCtrl myPlayerCtrl;
    public MoonHeader.State_P_Minion state_p;
    protected Animator anim;

    protected void Awake()
    {
        ResetVariable();

        anim = this.GetComponentInChildren<Animator>();
        rigid = this.GetComponent<Rigidbody>();
        //playerIcon = GameObject.Instantiate(PrefabManager.Instance.icons[4], this.transform);
        //playerIcon.transform.localPosition = new Vector3(0,60,0);
    }
    protected void Update()
    {
        if (canMove)
        { Move(); }
        if (canSee)
        { LookAround(); }
        PlayerInput();
        Recharging();
    }
    protected void LateUpdate()
    {
        RecoverMoveSpeed();
    }

    protected void PlayerInput()
    {
        if (Input.GetButtonDown("Fire1") && canAttack) { StartCoroutine(BasicAttack()); }
        if (canAttack) { Block(); }

        if (Input.GetKeyDown(KeyCode.Q) && canUseQ) { }
        if (canUseE) { }


    }

    protected void Move()
    {
        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");

        // movespeed = Input.GetKey(KeyCode.LeftShift) ? 8.0f : 5.0f; // 달리기
        this.transform.Translate(new Vector3(x, 0, y) * moveSpeed * Time.deltaTime);

        anim.SetFloat("Velocity", rigid.velocity.magnitude);
        Debug.Log(rigid.velocity.magnitude);
    }

    protected void LookAround()
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

    protected void RecoverMoveSpeed()
    {
        if (isSpeedDown)
        {
            timer_RecoverSpeed += Time.deltaTime;
            if (timer_RecoverSpeed >= 2.0f)
            {
                moveSpeed = 5.0f;
                timer_RecoverSpeed = 0.0f;
                isSpeedDown= false;
            }
        }
    }

    // 기본 공격
    protected IEnumerator BasicAttack()
    {
        canAttack = false;
        currentDamage = basicDamage;
        state = State.Attacking;
        moveSpeed = basicMoveSpeed - 2f;

        // 애니메이션 실행, 애니메이션에 짧은 시간이 흐른 후 공격 범위 활성화.
        //anim.SetTrigger("Attack");
        yield return new WaitForSeconds(0f);

        attackRange.SetActive(true);
        yield return new WaitForSeconds(0.3f);
        attackRange.SetActive(false);
    }
    protected void BasicAttackOver()
    {
        moveSpeed = basicMoveSpeed;
        state = State.Normal;
    }

    // 막기
    protected void Block()
    {
        if (Input.GetButtonDown("Fire2"))
        {
            //anim.SetBool("Blocking",true);
            state = State.Blocking;
            moveSpeed = basicMoveSpeed - 2.0f;
        }
        if (Input.GetButtonUp("Fire2"))
        {
            //anim.SetBool("Blocking",false);
            state = State.Normal;
            moveSpeed = basicMoveSpeed;
        }
    }

    // Q 스킬
    protected IEnumerator QSkillActive()
    {
        currentDamage = qDamage_;
        //anim.SetTrigger("Q");
        canAttack = false;
        canUseQ = false;

        yield return new WaitForSeconds(0f);
        currentDamage = basicDamage;
        canAttack = true;
        state = State.Normal;
    }

    // E 스킬
    protected void ESkillActive()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            state = State.Casting;
            canMove = false;
            anim.SetBool("Casting",true);
            timer_dummy_Casting = 0;
        }
        if (Input.GetKey(KeyCode.E))
        {
            timer_dummy_Casting += Time.deltaTime;
        }
        if (Input.GetKeyUp(KeyCode.E))
        {
            state = State.Normal;
            canMove = true;

            GameObject dummy_prefab = GameObject.Instantiate(swordBall_Prefab, attackRange.transform.position, attackRange.transform.rotation);
            dummy_prefab.GetComponent<PSH_SwordProjectile>().damage = eDamage_ + (timer_dummy_Casting / 1);
            dummy_prefab.GetComponent<PSH_SwordProjectile>().script = this.GetComponent<PSH_PlayerFPSCtrl>();

            timer_dummy_Casting = 0;
            canUseE = false;
        }
    }

    // 딜레이 계산하는 구문.
    protected void Recharging()
    {
        if (!canAttack)
        {
            switch (state)
            {
                case State.Attacking:
                    timer_BA += Time.deltaTime;
                    if (timer_BA >= BASIC_ATTACKDELAY)
                    { timer_BA = 0; BasicAttackOver(); }
                    break;
            }
        }
        if (!canUseQ)
        {
            timer_QC += Time.deltaTime;
            if (timer_QC >= qDelay_)
            { timer_QC = 0; canUseQ = true; }
        }
        if (!canUseE)
        {
            timer_EC += Time.deltaTime;
            if (timer_EC >= eDelay_)
            { timer_EC = 0; canUseE = true; }
        }
    }



    protected void ResetVariable()
    {
        // 변수 초기화
        qLevel = basicLevel; eLevel = basicLevel;
        canMove = true; canAttack = true;
        canUseQ = true; canUseE = true;
        currentDamage = basicDamage;
        state = State.Normal;
        moveSpeed = basicMoveSpeed;
        attackRange.SetActive(false);
        cameraCanMove = true;
        invertCamera = false;
        canSee = true;
        isSpeedDown = false;
        qDelay_ = 3.0f; eDelay_ = 7.0f;
    }

    public void SpawnSetting(MoonHeader.Team t, short monHealth, string pname, LSM_PlayerCtrl pctrl)
    {
        //Health = monHealth * 10;
        // 디버그용. 현재 강령하는 미니언의 체력의 10배율로 강령, 공격력을 10으로 디폴트. 이후 플레이어 공격력으로 변경할 예정
        actorHealth = new MoonHeader.S_ActorState(100, 10, t);
        actorHealth.health = (short)(monHealth * 10);
        playerName = pname;
        myPlayerCtrl = pctrl;
        ChangeTeamColor(playerIcon);
        state_p = MoonHeader.State_P_Minion.Normal;

        // 초기화


        //handpos.transform.localEulerAngles = new Vector3(0, 0, 0);

        ResetVariable();
        
    }

    public void Damaged(short dam, Vector3 origin, MoonHeader.Team t, GameObject other)
    {
        if (t == actorHealth.team || state_p == MoonHeader.State_P_Minion.Dead)
            return;
        actorHealth.health -= dam;
        // 넉백이 되는 방향벡터를 구함.
        //Vector3 direction_knock = Vector3.Scale(this.transform.position - origin, Vector3.one - Vector3.up).normalized;
        //float scale_knock = 100f;
        //rigid.AddForce(direction_knock * scale_knock);
        if (this.actorHealth.health <= 0)
            StartCoroutine(DeadProcessing(other));
        return;
    }

    public IEnumerator DeadProcessing(GameObject other)
    {
        state_p = MoonHeader.State_P_Minion.Dead;
        Debug.Log("PlayerMinion Dead");
        GameManager.Instance.PlayerMinionRemover(actorHealth.team, playerName);
        // 마지막 타격이 플레이어라면, 경험치 및 로그창 띄우기.
        if (other.transform.CompareTag("PlayerMinion"))
        {
            other.GetComponent<PSH_PlayerFPSCtrl>().myPlayerCtrl.GetExp(50);   // 디버깅용으로 현재 경험치를 50으로 고정 지급.
        }
        GameManager.Instance.DisplayAdd(string.Format("{0} Killed {1}", other.gameObject.name, this.name));
        yield return new WaitForSeconds(0.5f);
        this.gameObject.SetActive(false);
        myPlayerCtrl.PlayerMinionDeadProcessing();
    }

    // 플레이어 아이콘 색변경.
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

    // I_Actor 인터페이스에 미리 선언해둔 함수들 구현
    public short GetHealth() { return this.actorHealth.health; }
    public short GetMaxHealth() { return this.actorHealth.maxHealth; }
    public MoonHeader.Team GetTeam() { return this.actorHealth.team; }

    public bool IsCanUseE() { return canUseE; }
    public bool IsCanUseQ() { return canUseQ; }
}
*/