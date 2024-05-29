using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LSM_GoldExp : MonoBehaviour
{
    public TextMeshProUGUI Gold, Exp;
    private bool once;
    private LSM_PlayerCtrl player;

    private void Start()
    {
        once = false;
    }

    private void Update()
    {

        if (GameManager.Instance.onceStart)
        {
            if (!once)
            {once = true; player = GameManager.Instance.mainPlayer; }

            this.transform.SetAsLastSibling();
            Gold.text = player.GetGold() + "G";
            Exp.text = player.GetExp().ToString();
        }
    }
}
