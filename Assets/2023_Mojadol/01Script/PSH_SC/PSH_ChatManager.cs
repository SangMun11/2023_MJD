using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Pun;
using Photon.Realtime;
using TMPro;
using HashTable = ExitGames.Client.Photon.Hashtable;


[RequireComponent(typeof(PhotonView))]
public class PSH_ChatManager : MonoBehaviourPunCallbacks
{
    public GameObject m_Content;
    public TMP_InputField m_inputField;
    

    GameObject m_ContentText;

    string m_strUserName;
    private byte codeline = 0; // 0�� ��ü ä���� ���� ��, 1�� �� ä���� ���� ��
    private byte myTeam; // �� ����

    void Awake() => Screen.SetResolution(960, 600, false);

    // Start is called before the first frame update
    void Start() // ���� �� �ʱ�ȭ
    {
        PhotonNetwork.ConnectUsingSettings(); // �������ڸ��� ����
        m_ContentText = m_Content.transform.GetChild(0).gameObject;
        this.gameObject.GetComponent<PhotonView>();
    }

    // �� ���� �� ����
    #region Photon Override

    public override void OnConnectedToMaster() // ����Ǿ��� ��
    {
        base.OnConnected();
        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 5;

        int nRandomKey = Random.Range(0, 100);

        m_strUserName = "user" + nRandomKey; // ������ �г��� ����

        PhotonNetwork.LocalPlayer.NickName = m_strUserName;
        PhotonNetwork.JoinOrCreateRoom("Room1", options, null);
    }

    public override void OnJoinedRoom() // �������� �� ä�ÿ� �˸�
    {
        base.OnJoinedRoom();
        AddChatMessage("You are : " + PhotonNetwork.LocalPlayer.NickName);

        if (PhotonNetwork.LocalPlayer.ActorNumber % 2 == 1)
            myTeam = 1; // Ȧ�� ��
        else
            myTeam = 0; // ¦����

        Debug.Log($"myTeam Code : {myTeam}");

        // photonView.Group = myTeam;
        // Debug.Log($"photonView.Group : {photonView.Group}");
        // PhotonNetwork.SetPlayerCustomProperties(new HashTable { { "PlayerTeam", myTeam } });

    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);

        Debug.Log($"newPlayer's UserId : {newPlayer.UserId}");
        Debug.Log($"newPlayer's ActorNumber : {newPlayer.ActorNumber}");
    }
    #endregion


    // ���͸� ���� �Է� �� ����

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) && m_inputField.isFocused == false) // ���� ġ�� InputField�� Ű���� Ŀ���� �ű�
        {
            m_inputField.ActivateInputField();
        }

        if(Input.GetKeyDown(KeyCode.Tab))
        {
            codeline++;
            codeline %= 2;
            Debug.Log($"Chatting Line Code : {codeline}");
        }
    }

    // ä�� �� ���� �Լ�
    public void OnEndEditEvent() 
    {
        if(Input.GetKeyDown(KeyCode.Return))
        {
            string strMessage = m_strUserName + " : " + m_inputField.text;

            /*
            switch (code)
            {
                case 0:
                    photonView.RPC("RPC_Chat", RpcTarget.All, strMessage);
                    break;
                case 1:
                    

                    break;
            }
            */

            photonView.RPC("RPC_Chat", RpcTarget.All, strMessage);
            m_inputField.text = "";
        }
    }

    // ä�� ���� �˰���

    [PunRPC]
    void RPC_Chat(string message)
    {
        AddChatMessage(message);
    }

    void AddChatMessage(string message)
    {
        GameObject goText = Instantiate(m_ContentText, m_Content.transform);
        
        goText.GetComponent<TextMeshProUGUI>().text = message;
        goText.GetComponent<TextMeshProUGUI>().fontSize = 8;
        m_Content.GetComponent<RectTransform>().anchoredPosition = Vector3.zero;
    }

}
