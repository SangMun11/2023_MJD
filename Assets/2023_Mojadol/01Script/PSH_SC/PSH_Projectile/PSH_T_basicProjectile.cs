using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PSH_T_basicProjectile : MonoBehaviour
{
    public ParticleSystem hit;

    private void OnCollisionEnter(Collision collision)
    {
        // ������ ���� �˰���
        //Instantiate(hit, transform.position, transform.rotation);
        //Destroy(this.gameObject);
    }
}
