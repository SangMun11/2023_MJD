using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HSH_AimBox : MonoBehaviour
{
    public GameObject FireBallThrower;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerStay(Collider c)
    {
        //if (c.gameObject.CompareTag("RedTeam") || c.gameObject.CompareTag("BlueTeam"))
        if (c.CompareTag("PlayerMinion"))
        {
            FireBallThrower.GetComponent<HSH_FireBallThrower>().AddReward(0.01f);
        }
        
    }
}
