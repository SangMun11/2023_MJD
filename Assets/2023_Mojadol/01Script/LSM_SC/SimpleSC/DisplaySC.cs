using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisplaySC : MonoBehaviour
{
    float timer;
    void Start()
    {
        timer = 0;
    }
	private void OnEnable()
	{
        
        timer = 0;
	}
	private void Update()
	{
        timer += Time.deltaTime;
        if (timer >= 5)
        { this.gameObject.SetActive(false); GameManager.Instance.DisplayChecking(); }
    }
}
