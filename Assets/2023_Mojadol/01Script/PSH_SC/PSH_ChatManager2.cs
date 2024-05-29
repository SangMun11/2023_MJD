using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Pun;
using Photon.Realtime;
using TMPro;

[RequireComponent(typeof(PhotonView))]
public class PSH_ChatManager2 : MonoBehaviourPunCallbacks
{
    string myname = "user";

    public TMP_InputField inputField;
    

    #region Photon
    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();
        RoomOptions rop = new RoomOptions();
        rop.MaxPlayers = 4;

        PhotonNetwork.LocalPlayer.NickName = myname;
        PhotonNetwork.JoinOrCreateRoom("room", rop, null);
    }

    

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
    }
    #endregion


    #region MonoBehviour, Private Methods
    private void Awake()
    {
        int randomID = Random.Range(0, 100);
        myname += randomID.ToString();
    }

    // Start is called before the first frame update
    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
    }

    void SendChatMessage()
    {

    }
    #endregion
    /*
     using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class ChatManager : MonoBehaviourPunCallbacks, IPunObservable
{
    public string teamChatPrefix = "(Team)";
    public string globalChatPrefix = "(Global)";
    public string playerName;
    public string message;
    public bool isTeamChat;
    
    private readonly byte teamChatKey = 1;
    private readonly byte globalChatKey = 2;
    
    private void Start()
    {
        playerName = PhotonNetwork.LocalPlayer.NickName;
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Chat");
        
        message = GUILayout.TextField(message);
        
        if(GUILayout.Button("Send"))
        {
            if(!string.IsNullOrEmpty(message))
            {
                if(isTeamChat)
                {
                    SendTeamChat();
                }
                else
                {
                    SendGlobalChat();
                }
            }
        }
    }
    
    private void SendGlobalChat()
    {
        photonView.RPC("ReceiveMessage", RpcTarget.All, $"{globalChatPrefix} {playerName}: {message}");
    }
    
    private void SendTeamChat()
    {
        Hashtable hash = new Hashtable();
        hash.Add("message", $"{teamChatPrefix} {playerName}: {message}");
        PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
    }
    
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if(targetPlayer.IsLocal)
        {
            return;
        }
        
        if(changedProps.TryGetValue("message", out object messageObj))
        {
            string message = (string)messageObj;
            photonView.RPC("ReceiveMessage", targetPlayer, message);
        }
    }
    
    [PunRPC]
    private void ReceiveMessage(string message)
    {
        Debug.Log(message);
    }
    
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if(stream.IsWriting)
        {
            stream.SendNext(isTeamChat);
        }
        else
        {
            isTeamChat = (bool)stream.ReceiveNext();
        }
    }
}
     */
}
