using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LSM_HoleSC : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerMinion"))
        {
            other.GetComponent<I_Actor>().Damaged(short.MaxValue, this.transform.position, MoonHeader.Team.Yellow, this.gameObject);
        }
    }
}
