using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HSH_Minion_Melee : HSH_LSM_Minion_Base
{
    /*
    protected override void Awake()
    {
        base.Awake();
        SetRadius(7f, 0.75f, 1.5f); //사거리를 근거리에 맞게 재설정
    }

    protected void OnEnable()
    {
        base.OnEnable();
    }

    protected void LateUpdate()
    {
        base.LateUpdate();
    }

    protected override void SearchingTarget()
    {
        // 현재 미니언이 타겟을 확인 하였는지.
        if (ReferenceEquals(target_attack, null) && !PlayerSelect)
        {

            timer_Searching += Time.deltaTime;
            if (timer_Searching >= SEARCHTARGET_DELAY)
            {
                timer_Searching = 0;

                // 스피어캐스트를 사용하여 일정 반지름 내에 적이 있는지 확인.
                RaycastHit[] hits;
                hits = Physics.SphereCastAll(transform.position, searchRadius, Vector3.up, 0);
                float dummyDistance = float.MaxValue;
                foreach (RaycastHit hit in hits)
                {
                    float hit_dummy_distance = Vector3.Distance(transform.position, hit.transform.position);
                    if (dummyDistance > hit_dummy_distance)
                    {
                        bool different_Team = false;
                        if (hit.transform.CompareTag("Minion"))
                        { different_Team = (stats.team != hit.transform.GetComponent<HSH_LSM_Minion_Base>().stats.team) && hit.transform.GetComponent<HSH_LSM_Minion_Base>().minionBelong == minionBelong; }    //(근거리 한정) 자신과 같은 공격로의 미니언만 대상으로 지정
                        else if (hit.transform.CompareTag("Turret"))
                        { different_Team = (stats.team != hit.transform.GetComponent<LSM_TurretSc>().stats.team) && hit.transform.GetComponent<LSM_TurretSc>().TurretBelong == minionBelong; }  //(근거리 한정) 자신과 같은 공격로의 터렛만 대상으로 지정

                        if (different_Team)
                        {
                            dummyDistance = hit_dummy_distance;
                            target_attack = hit.transform.gameObject;
                            if (nav.enabled)
                                nav.destination = target_attack.transform.position;
                        }

                    }
                }
            }
        }

        if (!ReferenceEquals(target_attack, null) && !PlayerSelect && nav.enabled)
        {

            nav.destination = target_attack.transform.position;
            // 타겟이 MaxDistance이상 떨어져있다면 null
            if (Vector3.Distance(target_attack.transform.position, this.transform.position) > maxAtkRadius)
            { StartCoroutine(AttackFin()); }

            else if (Vector3.Distance(target_attack.transform.position, this.transform.position) <= minAtkRadius)
            {
                stats.state = MoonHeader.State.Attack;
            }

        }
    }

    */
}
