using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LSM_CameraPosMove : MonoBehaviour
{
    public Transform eye;
    void Update()
    {
        this.transform.localPosition = new Vector3(0,eye.localPosition.y + 1.6f, eye.localPosition.z);
        this.transform.rotation = eye.rotation;
    }
}
