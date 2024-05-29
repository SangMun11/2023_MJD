using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HSH_FireBall : MonoBehaviour
{
    public float dmg;

    public ParticleSystem Explosion;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if(this.transform.position.y < -1000f)
        {
            Destroy(this.gameObject);
        }
    }
    
    private void OnTriggerEnter(Collider c)
    {
        //if(c.gameObject.CompareTag("RedTeam") || c.gameObject.CompareTag("BlueTeam"))
        if (c.CompareTag("PlayerMinion"))
        {
            // ^플레이어 캐릭터와 호환하는 스크립트로 교체해야 함.
            //c.GetComponent<HSH_PatternAvoider2>().Damaged(dmg);
            c.GetComponent<I_Actor>().Damaged((short)dmg, this.transform.position ,  MoonHeader.Team.Yellow, this.gameObject);
        }

        Transform spawntrans = this.transform;

        if (!c.gameObject.CompareTag("Creep"))
        {
            Instantiate(Explosion, spawntrans.position, spawntrans.rotation);
            Destroy(this.gameObject);
        }
    }
}
