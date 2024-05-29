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
    private byte codeline = 0; // 0은 전체 채팅을 위한 것, 1은 팀 채팅을 위한 것
    private byte myTeam; // 팀 구성

    void Awake() => Screen.SetResolution(960, 600, false);

    // Start is called before the first frame update
    void Start() // 연결 및 초기화
    {
        PhotonNetwork.ConnectUsingSettings(); // 시작하자마자 연결
        m_ContentText = m_Content.transform.GetChild(0).gameObject;
        this.gameObject.GetComponent<PhotonView>();
    }

    // 방 생성 및 연결
    #region Photon Override

    public override void OnConnectedToMaster() // 연결되었을 때
    {
        base.OnConnected();
        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 5;

        int nRandomKey = Random.Range(0, 100);

        m_strUserName = "user" + nRandomKey; // 랜덤한 닉네임 설정

        PhotonNetwork.LocalPlayer.NickName = m_strUserName;
        PhotonNetwork.JoinOrCreateRoom("Room1", options, null);
    }

    public override void OnJoinedRoom() // 입장했을 때 채팅에 알림
    {
        base.OnJoinedRoom();
        AddChatMessage("You are : " + PhotonNetwork.LocalPlayer.NickName);

        if (PhotonNetwork.LocalPlayer.ActorNumber % 2 == 1)
            myTeam = 1; // 홀수 팀
        else
            myTeam = 0; // 짝수팀

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


    // 엔터를 통해 입력 및 전송

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) && m_inputField.isFocused == false) // 엔터 치면 InputField로 키보드 커서를 옮김
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

    // 채팅 후 전송 함수
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

    // 채팅 관련 알고리즘

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
