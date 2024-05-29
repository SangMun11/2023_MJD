using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HSH_AlertField : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnTriggerStay(Collider c)
    {

        //if (c.CompareTag("RedTeam") && c.CompareTag("BlueTeam"))
        if (c.CompareTag("PlayerMinion"))
        {
            c.GetComponent<HSH_PatternAvoider>().AddReward(-0.0005f);   //조준당하면 pattern avoider는 감점
        }

    }
}
