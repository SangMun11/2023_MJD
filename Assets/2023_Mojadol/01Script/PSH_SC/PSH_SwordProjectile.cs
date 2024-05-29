using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PSH_SwordProjectile : MonoBehaviour
{
    public GameObject head;
    private float timer = 0.0f;
    private float power = 500.0f;
    public float damage = 0.0f;

    // LSM ���� ���� ������ ������ ����.. ��ƿø� ĳ������ ��ũ��Ʈ�� �޾ƿ�.
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
        // LSM �߰�.
        // �̴Ͼ� �����Ͽ� ������ �����ϰ� ����.
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
