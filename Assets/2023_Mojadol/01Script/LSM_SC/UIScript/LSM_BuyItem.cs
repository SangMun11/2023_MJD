using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LSM_BuyItem : MonoBehaviour
{
    public LSM_ItemData item;
    public LSM_UI_Shop shop;

    private void Awake()
    {
        shop = this.GetComponentInParent<LSM_UI_Shop>();
    }

    public void CanBuy()
    {
        LSM_PlayerCtrl mainPlayer = GameManager.Instance.mainPlayer;
        byte hasNum = mainPlayer.hasItems.NumOfItem(item.code);
        if (hasNum < item.maxCountable && mainPlayer.GetGold() >= item.price)
        {
            mainPlayer.hasItems.AddItem(item.code);
            mainPlayer.SpendGold(item.price);
            mainPlayer.PlayerItemSynchronize();
            shop.SettingTxt();
            Debug.Log("Buy Items");
        }
        else { Debug.Log("Cant Buy"); }
        
    }

}
