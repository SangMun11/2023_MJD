using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LSM_ItemCollectAnim : MonoBehaviour
{
    [SerializeField] private GameObject target;
    [SerializeField] private float speed, timer_accelation;
    [SerializeField] private bool canMove;
    [SerializeField] private int size;
	private void Awake()
	{
        speed = 9f;
        canMove = false;
        timer_accelation = 0f;
    }
	
	public void TargetLockOn(GameObject t, int size_d)
    {
        target = t;
        size = size_d;
        canMove = true;
        timer_accelation = 1f;
        //this.transform.rotation = Quaternion.LookRotation(t.transform.position);
    }
	private void Update()
	{
        if (canMove)
        {
            timer_accelation += Time.deltaTime;
            this.transform.position = Vector3.MoveTowards(this.transform.position, target.transform.position + Vector3.up * 1, speed * Time.deltaTime * (Mathf.Round(timer_accelation) / 2));
            if (Vector3.SqrMagnitude(this.transform.position - target.transform.position) <= 1)
            {
                Debug.Log("Collecting! : " +size);
                target.GetComponent<I_Playable>().AddCollector(size);
                canMove = false;
                this.gameObject.SetActive(false);
            }
        }
	}
}
