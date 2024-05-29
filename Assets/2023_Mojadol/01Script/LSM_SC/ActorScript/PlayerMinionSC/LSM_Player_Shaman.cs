using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LSM_Player_Shaman : LSM_PlayerBase
{
    // 쿨타임 관련 변수.
    private float CoolTime_Q, CoolTime_E;
    private float timer_Q, timer_E, timer_Combo, timer_E_Holding;
    private bool casting_E, input_E;

    private IEnumerator basic_IE;

    protected override void Awake()
    {
        base.Awake();
        CoolTime_E = 5f;
        CoolTime_Q = 3f;
        basic_IE = basicAttack();
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


        if (Input.GetKeyDown(KeyCode.E) && canE && canAttack)
        {
            StartCoroutine(ESkill());
        }
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

    #region BasicAttack
    protected override IEnumerator basicAttack()
    {
        canAttack = false;
        anim.SetLayerWeight(1, 1f);
        anim.SetTrigger("basicAttack0");

        photonView.RPC("basicAnim_RPC", RpcTarget.All);
        yield return new WaitForSeconds(0.2f);


        this.currentSpeed = speed / 2;
        yield return new WaitForSeconds(1.3f);
        AnimatorLayerReset();
        yield return new WaitForSeconds(0.2f);
        this.currentSpeed = speed;
        canAttack= true;
    }
    [PunRPC]private void basicAnim_RPC()
    {
        if (photonView.IsMine)
            return;

        anim.SetLayerWeight(1, 1f);

        anim.SetTrigger("basicAttack0");
        Invoke("AnimatorLayerReset", 1.3f);
    }
    [PunRPC]private void BasicE()
    {
        GameObject effect_d = PoolManager.Instance.Get_Particles(1, this.transform.position + this.transform.forward*0.7f);
        //effect_d
    }

    #endregion

    #region Q Skill
    protected override IEnumerator QSkill()
    {
        canQ = false;
        canAttack= false;
        canMove= false;
        timer_Q = 0;

        photonView.RPC("QAnim_RPC", RpcTarget.All);
        yield return new WaitForSeconds(1.3f);
        canAttack= true;
        canMove= true;
    }
    [PunRPC]private void QAnim_RPC()
    {
        anim.SetTrigger("skillQ_Trigger");
    }
    #endregion

    #region E Skill
    protected override IEnumerator ESkill()
    {
        anim.SetLayerWeight(1, 1f);
        anim.SetTrigger("skillE_Trigger");
        canMove = false;
        canAttack = false;
        canE = false;

        yield return new WaitForSeconds(1f);

        canMove = true;
        canAttack = true;
        canE = true;        
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
        timer_E = 0;
        timer_Q = 0;
    }
}
