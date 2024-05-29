using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using JetBrains.Annotations;
using Unity.MLAgents.Actuators;
using System.Net.NetworkInformation;
using System.Security.AccessControl;
using Unity.MLAgents.Policies;
using System.Diagnostics.Tracing;
using Unity.Barracuda;
using Photon.Pun;

public enum GolemStat
{
    Idle=0,
    Walk=1,
    Charge=2,
    Groggy=4,
    Death = 3
}

public class HSH_GolemAgent : Agent, I_Creep
{
    float spd;
    bool doOnlyOnce; bool doOnlyOnce2; //bool doOnlyOnce3;
    bool doOnlyOnce4;
    const float WALKSPEED = 3f; const float CHARGESPEED = 20f;
    const float WALKCOOL = 4.5f; const float CHARGECOOL = 2.5f;
    const float HP = 4;

    private float timer_groggy;
    public GolemStat stat;
    public CreepInfo creepinfo;
    public GameObject groggySensor;
    private LSM_CreepCtrl creepCtrl;
    PatternInfo patternInfo;

    Vector3 InitPos;

    public Rigidbody rb;
    public Animator anim;

    NNModel golemonnx;

    public override void Initialize()
    {
        spd = WALKSPEED;
        doOnlyOnce = true; doOnlyOnce2 = true; //doOnlyOnce3 = true;
        doOnlyOnce4 = true;

        stat = GolemStat.Walk;

        creepinfo = new CreepInfo();
        creepinfo.hp = HP;   //학습에는 필요하지 않은 데이터
        creepinfo.isHero = false;

        InitPos = this.transform.position;

        patternInfo = new PatternInfo();
        patternInfo.cooltime = WALKCOOL;
        patternInfo.isCool = false; //이 에이전트에서는 사용되지 않음
        patternInfo.dmg = 2f;

        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        groggySensor.SetActive(false);

        creepCtrl = this.GetComponent<LSM_CreepCtrl>();
        timer_groggy = 0;
        //golemonnx = this.GetComponent<BehaviorParameters>().Model;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            this.stat = (GolemStat)creepCtrl.stat.state;
            if (stat != GolemStat.Death)
                StatCtrl();
            spd = 0;
            return;
        }
        PatCoolCtrl();

        

        rb.inertiaTensor = Vector3.zero;
        rb.inertiaTensorRotation = Quaternion.Euler(Vector3.zero);


        // 트리거 박스 안에 플레이어가 존재하지 않을 경우.
        // 다시 원래 자리로 돌아가기 위한 함수.
        if (stat != GolemStat.Death)
        {
            if (!creepinfo.isHero)
            {
                doOnlyOnce4 = true;
                creepinfo.hp = HP;
                if (Mathf.Abs(transform.position.x - InitPos.x) > 0.1f &&
                    Mathf.Abs(transform.position.z - InitPos.z) > 0.1f)
                {
                    //Debug.Log("1");
                    stat = GolemStat.Walk; patternInfo.cooltime = WALKCOOL;
                    transform.rotation = Quaternion.LookRotation(InitPos - transform.position).normalized;
                    transform.position = Vector3.MoveTowards(transform.position, InitPos, spd * Time.fixedDeltaTime);
                    StatCtrl();
                }

                else
                {
                    //Debug.Log("2");
                    stat = GolemStat.Idle;
                    StatCtrl();
                }
            }

            else
            {
                //doOnlyOnce3 = true;

                if (doOnlyOnce4)
                {
                    Debug.Log("3");
                    doOnlyOnce4 = false;
                    stat = GolemStat.Walk;
                    patternInfo.cooltime = WALKCOOL;
                    StatCtrl();
                }
            }
            creepCtrl.stat.state = (MoonHeader.CreepStat)this.stat;

            if (stat == GolemStat.Groggy)
            {
                timer_groggy += Time.deltaTime;
                if (timer_groggy >= 4f)
                {
                    patternInfo.cooltime = WALKCOOL;
                    stat = GolemStat.Walk;
                    creepCtrl.stat.state = (MoonHeader.CreepStat)this.stat;
                    StatCtrl();
                    timer_groggy = 0;
                    transform.rotation = Quaternion.LookRotation(InitPos - transform.position);
                    doOnlyOnce = true;
                }
            }
        }
        else { spd = 0; rb.velocity = Vector3.zero; }

    }

    private void OnCollisionEnter(Collision c)
    {
        if (stat == GolemStat.Death || !PhotonNetwork.IsMasterClient)
            return;
        if (c.transform.CompareTag("PlayerMinion"))
        {
            AddReward(0.1f);

            if (GolemStat.Walk == this.stat)
            {
                c.transform.GetComponent<I_Playable>().Damaged((short)Mathf.CeilToInt(patternInfo.dmg/3), this.transform.position, MoonHeader.Team.Yellow, this.gameObject, 10);
            }
            else if (this.stat == GolemStat.Charge)
            {
                c.transform.GetComponent<I_Playable>().Damaged((short)Mathf.CeilToInt(patternInfo.dmg), this.transform.position, MoonHeader.Team.Yellow, this.gameObject, 20f);
            }

            //c.transform.GetComponent<HSH_PatternAvoider_Golem>().Damaged(patternInfo.dmg);

            /*원래 이 부분에 피격 대상을 넉백시키는 코드를 넣었었는데 플레이어랑 어떻게 호환될지 몰라서 주석처리 했습니다.
             
             c.gameObject.GetComponent<Rigidbody>().AddForce(this.transform.position - c.transform.position * 50f, ForceMode.Impulse);
             */

        }

        if (c.transform.CompareTag("iWall") && doOnlyOnce && stat == GolemStat.Charge)
        {
            //groggySensor.SetActive(false);
            //doOnlyOnce = false;
            StartCoroutine(Groggy());
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        AddReward(-0.0005f);

        WalkAndCharge(actions.DiscreteActions);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        //x축 이동
        var discreteActionsOut = actionsOut.DiscreteActions;

        if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[0] = 0;
        }

        else if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[0] = 2;
        }

        else
        {
            discreteActionsOut[0] = 1;
        }
    }
    /*
    public override void OnEpisodeBegin()
    {
        stat = GolemStat.Walk; patternInfo.cooltime = WALKCOOL;
    }*/

    void PatCoolCtrl()
    {
        if (stat == GolemStat.Death)
            return;
        if (stat != GolemStat.Groggy && stat != GolemStat.Idle && creepinfo.hp > 0f)
        {
            patternInfo.cooltime -= Time.fixedDeltaTime;

            if (stat == GolemStat.Walk && patternInfo.cooltime < 0.5f)
            {
                stat = GolemStat.Charge; patternInfo.cooltime = CHARGECOOL;
                //groggySensor.SetActive(true);
                StatCtrl();
            }

            else if (stat == GolemStat.Charge && patternInfo.cooltime < 0.5f)
            {
                stat = GolemStat.Walk; patternInfo.cooltime = WALKCOOL;
                //groggySensor.SetActive(false);
                StatCtrl();
            }
        }

        /*
        else if (creepinfo.hp <= 0)
        {           

            if (doOnlyOnce2)
            {
                doOnlyOnce2 = false;
                stat = GolemStat.Death;
                StartCoroutine(Death());
            }
        }
        */
    }

    void StatCtrl()
    {
        switch (stat)
        {
            case GolemStat.Idle:
                spd = 0;
                anim.ResetTrigger("Charge");
                anim.ResetTrigger("Groggy");
                anim.ResetTrigger("Death");
                anim.ResetTrigger("Walk");
                anim.SetTrigger("Idle");
                break;
            case GolemStat.Walk:
                spd = WALKSPEED;
                anim.ResetTrigger("Charge");
                anim.ResetTrigger("Groggy");
                anim.ResetTrigger("Death");
                anim.ResetTrigger("Idle");
                anim.SetTrigger("Walk");
                break;
            case GolemStat.Charge:
                spd = CHARGESPEED;
                anim.ResetTrigger("Walk");
                anim.ResetTrigger("Groggy");
                anim.ResetTrigger("Death");
                anim.ResetTrigger("Idle");
                anim.SetTrigger("Charge");
                break;
            case GolemStat.Groggy:
                spd = 0;
                anim.ResetTrigger("Walk");
                anim.ResetTrigger("Charge");
                anim.ResetTrigger("Death");
                anim.ResetTrigger("Idle");
                anim.SetTrigger("Groggy");
                break;
            case GolemStat.Death:
                spd = 0;
                anim.ResetTrigger("Walk");
                anim.ResetTrigger("Charge");
                anim.ResetTrigger("Groggy");
                anim.ResetTrigger("Idle");
                anim.SetTrigger("Death");
                break;
        }
    }

    void WalkAndCharge(ActionSegment<int> act)
    {
        if (!PhotonNetwork.IsMasterClient || stat == GolemStat.Death) return;

        var rotateDir = Vector3.zero;

        switch (act[0])
        {
            case 0:
                rotateDir = transform.up * 1f;
                break;
            case 1:
                break;
            case 2:
                rotateDir = transform.up * -1f;
                break;
        }

        if (stat != GolemStat.Groggy && creepinfo.isHero && stat != GolemStat.Death)
        {
            transform.Rotate(rotateDir, Time.fixedDeltaTime * 100f);
        }
        rb.velocity = transform.forward * spd * 50f * Time.fixedDeltaTime;
    }

    public IEnumerator Groggy()
    {
        if (doOnlyOnce)
        {

            groggySensor.SetActive(false);
            doOnlyOnce = false;
            stat = GolemStat.Groggy;
            StatCtrl();

            yield return new WaitForSeconds(5f);
            /*
            if (stat != GolemStat.Death)
            {
                patternInfo.cooltime = WALKCOOL;
                stat = GolemStat.Walk;
                creepCtrl.stat.state = (MoonHeader.CreepStat)this.stat;
                StatCtrl();
            }*/

            //doOnlyOnce = true;

            transform.rotation = Quaternion.LookRotation(InitPos - transform.position);
        }
    }

    IEnumerator Death()
    {
        GetComponent<BehaviorParameters>().Model = null;

        yield return new WaitForSeconds(4.586f);

        Destroy(this.gameObject);
    }

    public void RegenProcessing() 
    {
        stat = GolemStat.Idle;
        anim.SetBool("Death_B", false);
        patternInfo.cooltime = WALKCOOL;
        stat = GolemStat.Walk;
        doOnlyOnce = true;
        groggySensor.SetActive(false);
        StatCtrl();
    }
    public void AttackEffectEnable(bool b) { }
    public void StatSetting(int i) { stat = (GolemStat)i; }
    public void DeadProcessing() 
    {
        stat = GolemStat.Death;
        StatCtrl();
        spd = 0f;
        anim.SetBool("Death_B", true);
        //this.GetComponent<BehaviorParameters>().Model = null;

    }
    public void Setting() { }
}
