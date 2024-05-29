using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using Photon.Pun;

public class LSM_UI_Result : MonoBehaviour
{
    public TextMeshProUGUI result;
    public TextMeshProUGUI K,D;
    public TextMeshProUGUI Level,Exp;
    public TextMeshProUGUI CS, destroy;

    private void Start()
    {
        this.gameObject.SetActive(false);
    }

    // true -> ÀÌ±ט
    public void Setting(bool re)
    {
        LSM_PlayerCtrl p = GameManager.Instance.mainPlayer;
        result.text = (re) ? "You Win !" : "Lose...";
        K.text = p.kd[0].ToString();
        D.text = p.kd[1].ToString();
        Level.text = p.GetLevel().ToString();
        Exp.text = p.GetTotalExp().ToString();
        CS.text = p.GetCS().ToString();
        destroy.text = p.GetTD().ToString(); 
    }

    public void GoToLobby() { StartCoroutine(LobbyAnim()); }
    private IEnumerator LobbyAnim() {
        yield return StartCoroutine( GameManager.Instance.ScreenFade(false));
        SceneManager.LoadScene(1);
        PhotonNetwork.Disconnect();
    }
}
