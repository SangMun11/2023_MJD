using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HSH_Minion_Melee : HSH_LSM_Minion_Base
{
    /*
    protected override void Awake()
    {
        base.Awake();
        SetRadius(7f, 0.75f, 1.5f); //��Ÿ��� �ٰŸ��� �°� �缳��
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
        // ���� �̴Ͼ��� Ÿ���� Ȯ�� �Ͽ�����.
        if (ReferenceEquals(target_attack, null) && !PlayerSelect)
        {

            timer_Searching += Time.deltaTime;
            if (timer_Searching >= SEARCHTARGET_DELAY)
            {
                timer_Searching = 0;

                // ���Ǿ�ĳ��Ʈ�� ����Ͽ� ���� ������ ���� ���� �ִ��� Ȯ��.
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
                        { different_Team = (stats.team != hit.transform.GetComponent<HSH_LSM_Minion_Base>().stats.team) && hit.transform.GetComponent<HSH_LSM_Minion_Base>().minionBelong == minionBelong; }    //(�ٰŸ� ����) �ڽŰ� ���� ���ݷ��� �̴Ͼ� ������� ����
                        else if (hit.transform.CompareTag("Turret"))
                        { different_Team = (stats.team != hit.transform.GetComponent<LSM_TurretSc>().stats.team) && hit.transform.GetComponent<LSM_TurretSc>().TurretBelong == minionBelong; }  //(�ٰŸ� ����) �ڽŰ� ���� ���ݷ��� �ͷ��� ������� ����

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
            // Ÿ���� MaxDistance�̻� �������ִٸ� null
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
