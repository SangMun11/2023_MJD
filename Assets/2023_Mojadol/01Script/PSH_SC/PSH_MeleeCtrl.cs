using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PSH_MeleeCtrl : MonoBehaviour
{
    public GameObject head;
    public bool isSkill = false;

    // 자주 쓰일것이니, 미리 변수를 생성.
    private PSH_PlayerFPSCtrl fpsc;
    private void Awake()
    {
        fpsc = this.gameObject.GetComponentInParent<PSH_PlayerFPSCtrl>();
    }
    

	private void OnTriggerEnter(Collider other)
    {
        
        float thisdamage = fpsc.currentdamage;

        if (other.transform.tag == "PlayerMinion")
        {
            other.GetComponent<PSH_PlayerFPSCtrl>().Health -= thisdamage;
            if (isSkill)
                other.GetComponent<PSH_PlayerFPSCtrl>().movespeed -= 3.0f;
        }
        // LSM 추가.
        // 미니언 관련하여 공격이 가능하게 변경.
        else if (other.transform.CompareTag("Minion"))
        {
            LSM_Attack<LSM_MinionCtrl>(other.gameObject);
        }
        else if (other.transform.CompareTag("Turret") || other.transform.CompareTag("Nexus"))
        {
            LSM_Attack<LSM_TurretSc>(other.gameObject);
        }
        
    }

    private void LSM_Attack<T>(GameObject obj) where T : I_Actor
    {
        Debug.Log("Player Attack!");
        T ctrl = obj.GetComponent<T>();
        ctrl.Damaged((short)fpsc.currentdamage, fpsc.transform.position, fpsc.actorHealth.team, fpsc.gameObject);
    }

}
