using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PSH_T_E_Projectile : MonoBehaviour
{
    public GameObject hit_effect;
    float timer = 0.0f;

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= 3.0f)
            Destroy(this.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 데미지 전달
        Instantiate(hit_effect, transform.position, transform.rotation);
        Destroy(this.gameObject);
    }
}
