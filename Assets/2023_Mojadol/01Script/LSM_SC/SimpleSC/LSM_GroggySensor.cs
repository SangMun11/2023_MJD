using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LSM_GroggySensor : MonoBehaviour
{
	HSH_GolemAgent mainC;
	private void Awake()
	{
		mainC = this.GetComponentInParent<HSH_GolemAgent>();
	}
	private void OnTriggerEnter(Collider other)
	{
		if (other.transform.CompareTag("iWall"))
		{ StartCoroutine(mainC.Groggy()); }
	}
}
