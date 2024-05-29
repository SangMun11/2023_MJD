using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HSH_SpellField : MonoBehaviour
{
    public float dmg;
    private bool is_S;
    private SphereCollider s;

    private void Awake()
    {
        s = GetComponent<SphereCollider>();
        s.enabled = false;
    }
    private void OnEnable()
    {
        s.enabled = false;
    }

    // Start is called before the first frame update
    void Start()
    {
        //dmg = 2;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void Setting(int dam) { dmg= dam; is_S = true; s.enabled = true; }

    //^ 이하의 스크립트를 HSH_PatternAvoider가 아닌 playerscript와 호환되도록 바꿔야 함.
    private void OnTriggerEnter(Collider c)
    {
        if (c.CompareTag("PlayerMinion") && is_S)
        {
            //c.GetComponent<HSH_PatternAvoider>().Damaged(dmg);
            c.GetComponent<I_Actor>().Damaged((short)dmg, this.transform.position, MoonHeader.Team.Yellow, this.gameObject);
        }
    }
}
