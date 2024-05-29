using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

//흑마법사 크립의 설정 및 제어 스크립트

public enum LichStat
{
    Idle = 0,
    Idle_Combat = 1,
    Attack = 2,
    Death = 3
}

public class HSH_LichCreepController : MonoBehaviour, I_Creep
{
    public CreepInfo lichinfo; //크립 관련 정보
    public LichStat lichstat;

    public bool doOnlyOnce; //coroutine을 한 번만 실행
    Transform initTrans;
    private LSM_CreepCtrl creepCtrl;

    public GameObject triggerBox;
    public GameObject spellFieldGenerator, fireBallThrower;  //투사체, 장판 패턴을 담당하는 Agent들
    public List<GameObject> Player;


    Animator anim;
    Rigidbody rb;

    private void Awake()    
    {
        lichinfo = new CreepInfo();
        lichinfo.hp = 100f;
        lichinfo.isHero = false;
        lichstat = LichStat.Idle;

        doOnlyOnce = true;
        initTrans = this.transform;

        Player = new List<GameObject>();

        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        creepCtrl = this.GetComponent<LSM_CreepCtrl>();

        //플레이어가 없으면 모든 패턴 비활성화
        fireBallThrower.SetActive(false);
        spellFieldGenerator.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            if (creepCtrl.inPlayerNum > 0)
            {
                spellFieldGenerator.SetActive(true);
            }
            else
            {
                spellFieldGenerator.SetActive(false);
            }
            this.lichstat = (LichStat)creepCtrl.stat.state;

            if (lichstat != LichStat.Death)
                AnimCtrl();

            return;
        }

        // in Master Client
        for (int i = Player.Count-1; i >= 0; i--)
        {
            if (!Player[i].activeSelf)
            {
                Player.Remove(Player[i]);
            }
        }


        //AnimCtrl();
        //lichinfo.isHero = triggerBox.GetComponent<HSH_TriggerBox>().isTherePlayer;  //트리거 박스로부터 플레이어 존재 여부를 받아옴
        lichinfo.isHero = Player.Count > 0;
        creepCtrl.inPlayerNum = Player.Count;
        if (lichstat != LichStat.Death)
        {
            //크립룸 안에 플레이어가 있는가?
            if (lichinfo.isHero)    //네
            {
                lichstat = LichStat.Idle_Combat;
                AnimCtrl();
                LookAtMostCloseOne();

                //패턴 활성화
                fireBallThrower.SetActive(true);

                //if (!spellFieldGenerator.activeSelf)
                //creepCtrl.Enable_Generator(true);
                spellFieldGenerator.SetActive(true);

                if (!fireBallThrower.GetComponent<HSH_FireBallThrower>().pinfo.isCool && doOnlyOnce)
                {
                    doOnlyOnce = false;
                    StartCoroutine(DelayedAttack());
                }
            }
            else    //아니요
            {
                lichstat = LichStat.Idle;
                AnimCtrl();
                //모든 패턴 비활성화
                fireBallThrower.SetActive(false);

                //if (spellFieldGenerator.activeSelf)
                //creepCtrl.Enable_Generator(false);
                spellFieldGenerator.SetActive(false);
            }
            creepCtrl.stat.state = (MoonHeader.CreepStat)this.lichstat;
        }

        if (lichstat == LichStat.Death)
        {
            fireBallThrower.SetActive(false);
            
            //if (spellFieldGenerator.activeSelf)
                //creepCtrl.Enable_Generator(false);
            spellFieldGenerator.SetActive(false);
        }

        if(lichinfo.hp <= 0)
        {
            lichstat = LichStat.Death;
        }
    }

    public void DeadProcessing() 
    {
        lichstat = LichStat.Death;
        anim.SetBool("Death_B", true);
        AnimCtrl();
    }
    public void RegenProcessing()
    {
        lichstat = LichStat.Idle;
        anim.SetBool("Death_B", false);
        spellFieldGenerator.SetActive(true);
        //spellFieldGenerator.GetComponentInChildren<HSH_SpellFieldGenerator>().Setting(creepCtrl.stat.actorHealth.Atk);
        spellFieldGenerator.SetActive(false);
        AnimCtrl();
    }


    public void AnimCtrl()
    {
        switch (lichstat)
        {
            case LichStat.Idle:
                anim.SetTrigger("Idle");
                break;
            case LichStat.Idle_Combat:
                anim.SetTrigger("Idle_Combat");
                break;
            case LichStat.Attack:
                anim.SetTrigger("Attack");
                break;
            case LichStat.Death:
                anim.SetTrigger("Death");   //사망 조건이 충족했을 때 lichstat = LichStat.Death를 넣어주세요.
                break;
        }
    }

    void LookAtMostCloseOne()
    {
        Vector3 mostClose = Vector3.zero;
        float distance = 100000f;

        foreach (var item in Player)
        {
            if (distance > Vector3.Distance(this.transform.position, item.transform.position))
            {
                distance = Vector3.Distance(this.transform.position, item.transform.position);
                mostClose = item.transform.position;
            }
        }

        transform.rotation = Quaternion.LookRotation(Vector3.Scale(mostClose - transform.position, Vector3.one-Vector3.up)).normalized;
    }

    IEnumerator DelayedAttack() //애니메이션과 공격 패턴이 같은 타이밍에 재생되게끔 하는 함수
    {
        doOnlyOnce = false;
        yield return new WaitForSeconds(fireBallThrower.GetComponent<HSH_FireBallThrower>().pinfo.cooltime - 0.73f);

        if (lichinfo.isHero && lichstat != LichStat.Death)
        {
            lichstat = LichStat.Attack;
            AnimCtrl();
        }
        doOnlyOnce = true;
    }

    public void PlayerAdding(GameObject obj)
    {
        bool isExist = false;
        foreach (GameObject item in Player)
        {
            if (item.Equals(obj))
            {
                isExist = true;
                break;
            }
        }
        if (!isExist)
            Player.Add(obj);
    }
    public void AttackEffectEnable(bool b)
    { spellFieldGenerator.GetComponentInChildren<HSH_SpellFieldGenerator>().enabled = b; }
    public void StatSetting(int i)
    { lichstat = (LichStat)i; }
    public void Setting()
    {
        spellFieldGenerator.GetComponentInChildren<HSH_SpellFieldGenerator>().enabled = false;
    }
}
