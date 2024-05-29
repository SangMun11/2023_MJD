using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PSH_SwordProjectile : MonoBehaviour
{
    public GameObject head;
    private float timer = 0.0f;
    private float power = 500.0f;
    public float damage = 0.0f;

    // LSM 팀에 대한 정보를 얻어오기 위해.. 쏘아올린 캐릭터의 스크립트를 받아옴.
    public PSH_PlayerFPSCtrl script;

    private void Start()
    {
        this.gameObject.GetComponent<Rigidbody>().AddRelativeForce(Vector3.forward * power);
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= 5.0f)
            Destroy(this.gameObject);

    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.transform.tag == "Player")
        {
            collision.transform.GetComponent<PSH_PlayerFPSCtrl>().Health -= damage;
        }

        Destroy(this.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {

        if (other.transform.tag == "PlayerMinion")
        {
            //other.GetComponent<PSH_PlayerFPSCtrl>().Health -= damage;
            LSM_Attack<PSH_PlayerFPSCtrl>(other.gameObject);
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
        ctrl.Damaged((short)damage, this.transform.position, script.actorHealth.team, script.gameObject);
        Destroy(this.gameObject);
    }


}
