using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LSM_UI_Shop : MonoBehaviour
{
    public GameObject shopUI;

    public LSM_ItemData[] items;

    public TextMeshProUGUI[] numTxts;
    public TextMeshProUGUI[] priceTxts;
    public TextMeshProUGUI hasGold;

    private void Start()
    {
        shopUI.SetActive(false);
    }

    private void OnEnable()
    {
        this.transform.SetAsLastSibling();
    }

    private void Update()
    {


    }

    public void SettingTxt()
    {
        byte[] has = GameManager.Instance.mainPlayer.hasItems.GetHasItems();
        for (int i = 0; i < numTxts.Length; i++)
        {
            numTxts[i].text = string.Format("({0}/{1})", has[i], items[i].maxCountable);
            priceTxts[i].text = items[i].price + "G";
        }
        hasGold.text = GameManager.Instance.mainPlayer.GetGold() + "G";
    }

    public void OpenShop() { shopUI.SetActive(!shopUI.activeSelf); SettingTxt();}
    public void CloseShop() { shopUI.SetActive(false); }
}
