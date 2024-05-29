using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class LSM_Player_Tormented : LSM_PlayerBase
{
    // 쿨타임 관련 변수.
    public Transform spawnPosition;

    private float timer_Combo, timer_E_Holding;
    private bool casting_E, input_E;

    private IEnumerator basic_IE;
    private PSH_T_E E_SkillSC;

    protected override void Awake()
    {
        base.Awake();
        CoolTime_E = 6f;
        CoolTime_Q = 3f;
        basic_IE = basicAttack();
        E_SkillSC = this.GetComponentInChildren<PSH_T_E>();
        E_SkillSC.transform.gameObject.SetActive(false);
    }

    //private void FixedUpdate()
    //{
        //E_SkillSC.transform.localRotation = Quaternion.Euler(pitch, E_SkillSC.transform.localRotation.y, E_SkillSC.transform.localRotation.z);
        //this.transform.localRotation = Quaternion.Euler(playerCam.transform.rotation.eulerAngles.x, transform.rotation.y, transform.rotation.z);
    //}

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
        //eProjectilepos.transform.localRotation = Quaternion.Euler(pitch, eProjectilepos.transform.rotation.y, eProjectilepos.transform.rotation.z);
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
        yield return new WaitForSeconds(0.55f);
        photonView.RPC("BasicE", RpcTarget.MasterClient, this.playerCamera.transform.forward);

        yield return new WaitForSeconds(0.8f);
        AnimatorLayerReset();
        yield return new WaitForSeconds(0.2f);
        this.currentSpeed = speed;
        canAttack = true;
    }
    [PunRPC]
    private void basicAnim_RPC()
    {
        PlaySFX(3);
        if (photonView.IsMine)
            return;

        anim.SetLayerWeight(1, 1f);

        anim.SetTrigger("basicAttack0");
        Invoke("AnimatorLayerReset", 1.3f);
    }
    [PunRPC]
    private void BasicE(Vector3 forward_cam)
    {
        GameObject effect_d = PoolManager.Instance.Get_Particles
            (8, spawnPosition.position, Quaternion.LookRotation(forward_cam).eulerAngles);
        effect_d.GetComponent<LSM_BasicProjectile>().Setting
            (this.gameObject, this.actorHealth.Atk, this.GetComponent<I_Actor>(), 30f);
        effect_d.GetComponent<LSM_BasicProjectile>().Setting_Trigger_Exist_T(1.5f, 3f);
    }

    #endregion

    #region Q Skill
    protected override IEnumerator QSkill()
    {
        canQ = false;
        canAttack = false;
        canMove = false;
        timer_Q = 0;

        photonView.RPC("QAnim_RPC", RpcTarget.All);
        yield return new WaitForSeconds(1f);
        photonView.RPC("QE", RpcTarget.MasterClient, this.playerCamera.transform.forward);

        yield return new WaitForSeconds(1.3f);
        canQ = false;
        timer_Q = 0;
        canAttack = true;
        canMove = true;
    }
    [PunRPC]
    private void QAnim_RPC()
    {
        anim.SetTrigger("skillQ_Trigger");
    }
    [PunRPC]private void QE(Vector3 forward_cam)
    {
        GameObject effect_d = PoolManager.Instance.Get_Particles
            (9, spawnPosition.position, Quaternion.LookRotation(forward_cam).eulerAngles);
        effect_d.GetComponent<LSM_BasicProjectile>().Setting
            (this.gameObject, (short)Mathf.CeilToInt(this.actorHealth.Atk * 1f), this.GetComponent<I_Actor>(), 10f);
        effect_d.GetComponent<LSM_BasicProjectile>().Setting_Trigger_Exist_T(2f, 5f);
    }

    #endregion

    #region E Skill
    protected override IEnumerator ESkill()
    {

        photonView.RPC("EAnim_RPC", RpcTarget.All);
        canAttack = false;
        canE = false;
        timer_E = 0;

        currentSpeed = speed / 4;
        yield return new WaitForSeconds(5f);
        canMove = false;
        yield return new WaitForSeconds(3.5f);

        canE = false;
        timer_E = 0;
        canMove = true;
        currentSpeed = speed;
        canAttack = true;
    }

    [PunRPC]
    private void EAnim_RPC()
    {

        anim.SetLayerWeight(1, 1f);
        anim.SetTrigger("skillE_Trigger");
        E_SkillSC.gameObject.SetActive(true);
        E_SkillSC.Setting();


        Invoke("AnimatorLayerReset",7f);
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

        CoolTime_E = 6f + alpha.plusECool;
        CoolTime_Q = 3f + alpha.plusQCool;
        speed = 10 + alpha.plusSpeed;
        timer_E = 0;
        timer_Q = 0;

        object[] dummy_o = LSM_SettingStatus.Instance.lvStatus[(int)MoonHeader.ActorType.Magicion].getStatus_LV(GetLV());
        short[] add = GameManager.Instance.teamManagers[(int)t].GetAtkHp();

        this.actorHealth.Atk =(short)((short)dummy_o[1] + add[1] + alpha.plusATk);

        photonView.RPC("SpawnSetting_RPC", RpcTarget.All, (short)((short)dummy_o[0] + add[0] + alpha.plusHP),
            (short)((short)dummy_o[0] + add[0] + alpha.plusHP), pname, (int)t);
    }
}
