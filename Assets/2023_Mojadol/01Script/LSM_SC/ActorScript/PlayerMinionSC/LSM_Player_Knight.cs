using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LSM_Player_Knight : LSM_PlayerBase
{
    // 쿨타임 관련 변수.

    private float  timer_Combo, timer_E_Holding;
    private bool casting_E, input_E;

    protected LSM_WeaponSC weaponSC;
    protected MeshCollider weapon_C;

    private IEnumerator basic_IE;

    protected override void Awake()
    {
        base.Awake();

        basic_IE = basicAttack();
        
        weaponSC = this.transform.GetComponentInChildren<LSM_WeaponSC>();
        weapon_C = weaponSC.transform.GetComponent<MeshCollider>();
        weapon_C.enabled = false;
    }

    protected override void AttackFunction()
    {
        base.AttackFunction();
        if (Input.GetMouseButtonDown(0) && canAttack)
        {
            canAttack = false;
            StopCoroutine(basic_IE);
            basic_IE = basicAttack();
            StartCoroutine(basic_IE);
        }

        if (Input.GetKeyDown(KeyCode.Q) && canQ && canAttack)
        {
            StartCoroutine(QSkill());
        }

        

        StartCoroutine(ESkill());
        CoolManager();

        if (attackcode == 1 || attackcode == 2 || attackcode == 3)
        {
            timer_Combo += Time.deltaTime;
            if (timer_Combo >= 5f)
            {
                attackcode = 0;
                timer_Combo = 0f;
            }
        }
    }

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

    // 일반 공격
    #region BasicAttack
    protected override IEnumerator basicAttack()
    {
        canAttack = false;

        anim.SetLayerWeight(1, 1f);
        anim.SetTrigger("basicAttack" + attackcode.ToString());
        StartCoroutine(weaponSC.SwordEffect(attackcode));

        photonView.RPC("basicAnim_RPC", RpcTarget.All, attackcode);

        attackcode++;
        this.currentSpeed = speed / 2;
        //photonView.RPC("WeaponTriggerEnable", RpcTarget.MasterClient, true);
        yield return new WaitForSeconds(1.3f);
        //if (photonView.IsMine) 
        AnimatorLayerReset();
        yield return new WaitForSeconds(0.2f);
        this.currentSpeed = speed;
        yield return new WaitForSecondsRealtime(attackcode == 4 ? 2f : 0f);
        attackcode %= 4;
        timer_Combo = 0;

        //anim.SetLayerWeight(1, 0f);
        canAttack = true;
        //StopCoroutine(basicAttackDelay());
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
        effect_d.GetComponent<LSM_BasicProjectile>().Setting(this.gameObject, this.actorHealth.Atk, this.GetComponent<I_Actor>(), v + (rigid.velocity.magnitude));
        effect_d.GetComponent<LSM_BasicProjectile>().Setting_Trigger_Exist_T(0.7f, 5f);
        //effect_d.transform.LookAt(forward_ + dummypos, n);
        effect_d.transform.localScale = Vector3.one * s;
        effect_d.transform.position = dummypos + forward_ * 3f;
    }
    #endregion

    // Q Skill
    #region Qskill
    protected override IEnumerator QSkill()
    {
        canQ = false;
        canAttack = false;
        canMove = false;
        timer_Q = 0;
        //anim.applyRootMotion = true;
        //anim.SetTrigger("skillQ_Trigger");
        photonView.RPC("QAnim_RPC", RpcTarget.All);
        yield return new WaitForSecondsRealtime(1.3f);
        //photonView.RPC("AttackEffect_Q", RpcTarget.MasterClient, this.playerCamera.transform.forward);
        photonView.RPC("AttackEffect_Q", RpcTarget.MasterClient);
        yield return new WaitForSecondsRealtime(0.7f);
        //canQ = true;
        canQ = false;
        timer_Q = 0;
        canAttack = true;
        canMove = true;
        //anim.applyRootMotion = false;
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
    private void AttackEffect_Q()
    {
        Vector3 dummy_position = this.transform.position + Vector3.up*0.3f + this.transform.forward * 1.8f;

        GameObject effect_d = PoolManager.Instance.Get_Particles(2, dummy_position);
            //, Quaternion.LookRotation(forward, this.transform.right).eulerAngles);

        //effect_d.transform.LookAt(forward + effect_d.transform.position, this.transform.right);
        effect_d.transform.localScale = Vector3.one * 2.3f;
        effect_d.GetComponent<LSM_BasicProjectile>().Setting(this.gameObject, Mathf.CeilToInt((float)this.actorHealth.Atk * 0.6f), this.GetComponent<I_Actor>(), 0f);
        effect_d.GetComponent<LSM_BasicProjectile>().Setting_Trigger_Exist_T(0.1f, 10f);

    }
    #endregion

    // E Skill
    #region E Skill
    protected override IEnumerator ESkill()
    {
        yield return null;
        if (Input.GetKeyDown(KeyCode.E) && canAttack && canE)
        {
            photonView.RPC("EAnim_RPC", RpcTarget.All);
            canMove= false;
            canAttack= false;
            canE= false;
            timer_E = 0;
            casting_E= true;
            input_E = true;
        }

        if (Input.GetKeyUp(KeyCode.E)) { input_E = false; }
        if (casting_E) { timer_E += Time.deltaTime; }
        if (anim.GetCurrentAnimatorStateInfo(1).normalizedTime >= 0.35f && anim.GetCurrentAnimatorStateInfo(1).IsName("casting1") &&
            casting_E)
        { photonView.RPC("EAnim_Pause", RpcTarget.All); }

        if ((!input_E && timer_E >= 1f || timer_E >= 4f) && casting_E)
        {
            input_E = false;
            casting_E= false;
            photonView.RPC("EAnimE_RPC", RpcTarget.All, this.playerCamera.transform.forward, timer_E);
            yield return new WaitForSeconds(1.8f);
            canE = false;
            timer_E = 0;
            EskillOver();
        }
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
    private void EAnimE_RPC(Vector3 forward, float timer)
    {
        StartCoroutine(EAnimE_IE(forward, timer));
        //AnimatorLayerReset();
    }
    private IEnumerator EAnimE_IE(Vector3 forward, float timer)
    {
        anim.speed = 1f;
        yield return new WaitForSeconds(1f);
        if (PhotonNetwork.IsMasterClient) AttackEffect_E(forward, timer);
        yield return new WaitForSeconds(1f);
        AnimatorLayerReset();
    }
    private void AttackEffect_E(Vector3 forward, float timer)
    {
        Vector3 dummy_position = this.transform.position + this.transform.forward * 1 + Vector3.up;
        GameObject effect_d = PoolManager.Instance.Get_Particles(3, dummy_position
            , Quaternion.LookRotation(forward, this.transform.right).eulerAngles);
        //effect_d.transform.position = this.transform.position + this.transform.forward * 1 + Vector3.up;
        //effect_d.transform.LookAt(forward + effect_d.transform.position, this.transform.right);
        effect_d.transform.localScale = Vector3.one * (1f + 1f * (timer / 4f));


        effect_d.GetComponent<LSM_BasicProjectile>().Setting(this.gameObject, Mathf.CeilToInt((float)this.actorHealth.Atk * (1.2f + 0.5f*(timer/4f)))
            , this.GetComponent<I_Actor>(), 4f + (timer / 4f) * 4f);
    }
    #endregion

    protected override void CoolManager()
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

    public override void SpawnSetting(MoonHeader.Team t, short monHealth, string pname, LSM_PlayerCtrl pctrl)
    {
        base.SpawnSetting(t, monHealth, pname, pctrl);

        MoonHeader.S_Status alpha = pctrl.hasItems.GetPlusStatus();

        CoolTime_E = 5f + alpha.plusECool;
        CoolTime_Q = 3f + alpha.plusQCool;
        speed = 11 + alpha.plusSpeed;
        timer_E = 0;
        timer_Q = 0;
        object[] dummy_o = LSM_SettingStatus.Instance.lvStatus[(int)MoonHeader.ActorType.Knight].getStatus_LV(GetLV());
        short[] add = GameManager.Instance.teamManagers[(int)t].GetAtkHp();

        this.actorHealth.Atk = (short)((short)dummy_o[1] + add[1] + alpha.plusATk);
        

        photonView.RPC("SpawnSetting_RPC", RpcTarget.All, (short)((short)dummy_o[0] + add[0] + alpha.plusHP),
            (short)((short)dummy_o[0] +add[0] + alpha.plusHP), pname, (int)t);
    }
}
