using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelectButtonSC : MonoBehaviour
{
	Button myButton;
	bool once;

	private void Start()
	{
		once = false;
		myButton = GetComponent<Button>();
		
	}
    private void Update()
    {
		if (GameManager.Instance.onceStart && !once)
        {
			once = true;
			myButton.onClick.AddListener(GameManager.Instance.mainPlayer.SelectPlayerMinion);
		}
	}
}
