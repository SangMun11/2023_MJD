using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PSH_LobbyUI : MonoBehaviour
{
    public GameObject lobbyCtrl;

    // Panels
    public GameObject inputPanel;
    public GameObject selectPanel;

    // Input
    public InputField inputName;
    public Button submitButton;
    public string playerName = "";

    // Text -- Hero, Magician, Shaman
    public Text characterShow;
    public string[] characterName = new string[] { "Knight", "Corrupted One", "Shaman" };
    public int characterCode = 0;

    private void Awake()
    {
        inputPanel.SetActive(true);
        selectPanel.SetActive(false);
    }

    private void Update()
    {
        characterShow.text = characterName[characterCode];
    }

    public void NameInput()
    {
        if(inputName.text.Length > 0 && inputName.text.Length < 8)
        {
            playerName = inputName.text;
            PlayerPrefs.SetString("PlayerLocalName", playerName);
            Debug.Log($"Player Name : {PlayerPrefs.GetString("PlayerLocalName")}");
            inputPanel.SetActive(false);
            selectPanel.SetActive(true);
            lobbyCtrl.GetComponent<PSH_LobbyCtrl>().keyEnable = true;
            GameObject.Find("LobbyManager").GetComponent<PSH_LobbyCtrl>().once = true;
        }
        else 
        {
            inputName.text = "";
        }
    }

    public void GameSceneStart()
    {
        Debug.Log("start button");
        PlayerPrefs.SetInt("PlayerSelectType", characterCode);
        SceneManager.LoadScene(2);
    }
}
