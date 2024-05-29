using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PSH_T_Q : MonoBehaviour
{
    float timer = 0.0f;
    float speed = 2.0f;

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        if(timer>= 1.5f)
        {
            Destroy(this.gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 데미지 알고리즘

        Destroy(this.gameObject);
    }
}
