using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LSM_TurretIconUI : MonoBehaviour
{
    private GameObject turret_obj;
    public LSM_TurretSc turret;
    public Image currentUI, teamUI;
    private Camera mapcam, minimapcam;
    private bool once;
    private LSM_PlayerCtrl player;

    private void Start_function()
    {
        //num.text = "0";
        once = true;
        mapcam = GameManager.Instance.mainPlayer.MapCam.GetComponent<Camera>();
        minimapcam = GameManager.Instance.mainPlayer.MiniMapCam.GetComponent<Camera>();
        player = GameManager.Instance.mainPlayer;
    }

    public void Setting(GameObject obj) {turret_obj = obj; turret = obj.GetComponent<LSM_TurretSc>(); }

    private void Update()
    {
        // 생성 후 초기화를 하기 전에 실행되는 것을 방지.
        if (!ReferenceEquals(turret, null) && GameManager.Instance.onceStart)
        {
            if (!once)
                Start_function();

            currentUI.fillAmount = (float)turret.GetHealth() / turret.GetMaxHealth();
            if (mapcam.gameObject.activeSelf)
            { this.transform.position = mapcam.WorldToScreenPoint(turret_obj.transform.position); }

            //else if (minimapcam.transform.gameObject.activeSelf)
            //{ this.transform.position = minimapcam.WorldToScreenPoint(turret_obj.transform.position); }
        }
    }
}
