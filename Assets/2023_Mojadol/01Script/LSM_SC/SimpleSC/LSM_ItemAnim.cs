using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LSM_ItemAnim : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        this.transform.rotation = Quaternion.Euler(0,this.transform.rotation.eulerAngles.y + 0.2f,0);
    }
}
