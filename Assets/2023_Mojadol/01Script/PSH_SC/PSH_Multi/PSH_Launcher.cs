using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

// 1�� �κ� ����
namespace Com.MyCompany.Game // �ٸ� ������ �ڵ���� �浹�� �������� �����ߴ��ϳ׿�. ���� �Ƚᵵ �Ǳ� �� ��
{
    // MonoBehaviour -> MonoBehaviourPunCallbacks�� �ٲߴϴ�
    // PUN �ݹ�(���� ��Ƽ���� ����� �͵��̶�� �����ϼ�)�� 2������ �����ϴ�
    // �� �� �ϳ��� MonoBehaviour -> MonoBehaviourPunCallbacks �̷��� �ٲٴ� ��
    public class PSH_Launcher : MonoBehaviourPunCallbacks
    {
        string gameVersion = "1"; // ���� ������ ��Ʈ������ ���, ���� ������ ������ ����˴ϴ�

        [SerializeField]
        private byte maxPlayersPerRoom = 4; // ��� �ִ� �ο�

        #region 2�� �κ� UI������ ���� ������
        [SerializeField]
        private GameObject controlPanel; // �÷��̾���� �̸��� �Է��ϰ�, ���� �� �÷����� �� ���� ����

        [SerializeField]
        private GameObject progressLabel; // �÷��̾�鿡�� ������¿� ���� �˷��� �� ���� ����

        #endregion

        #region 4�� ���ӸŴ���~~���� �ۼ��� ������
        bool isConnecting;
        #endregion

        #region MonoBehaviour Callbacks

        void Awake()
        {
            PhotonNetwork.AutomaticallySyncScene = true; // �� ���� true�� �Ǹ� PhotonNetwork.LoadLevel()�� ȣ��, ����� �÷��̾���� ������ ������ �ڵ������� �ε�
        }

        void Start()
        {
            #region 2�� �κ� UI�� ����
            progressLabel.SetActive(false);
            controlPanel.SetActive(true);
            #endregion

        }

        #endregion

        #region Public Methods

        public void Connect() // ���� ����� ���� �뿡 �����ϴ� �Լ�
        {
            isConnecting = true;
            if(PhotonNetwork.IsConnected) 
            {
                PhotonNetwork.JoinRandomRoom();
            }
            else
            {
                PhotonNetwork.GameVersion = gameVersion;
                PhotonNetwork.ConnectUsingSettings();
            }
            #region 2�� �κ� UI�� ����
            progressLabel.SetActive(true);
            controlPanel.SetActive(false);
            #endregion
        }
        #region Connect()�Լ� ����
        /*
            1. ���� �����Ʈ��ũ�� ����Ǿ��ٸ� --> ������ �뿡 �����մϴ�
            2.                    �ƴ϶�� --> ���ӹ����� ������ ���ӹ������� ���� ��, �����մϴ�
            3. *ConnectUsingSettings() ���漭���� PhotonServerSettings ���Ͽ� ������ ���·� �����մϴ�
         */

        #endregion


        public override void OnConnectedToMaster()
        {
            // base.OnConnectedToMaster();
            if(isConnecting)
            {
                Debug.Log("OnConnectedToMaster() executed");
                PhotonNetwork.JoinRandomRoom();
            }
        }
        #region OnConnectedToMaster() ����
        /*
        1. ������Ŭ���̾�Ʈ�� ����Ǿ��� �� ȣ��˴ϴ�.
        2. JoinRandomRoom()�� ���� ������ �뿡 ����
        */

        #endregion


        public override void OnDisconnected(DisconnectCause cause) // ������ �������� ��
        {
            // base.OnDisconnected(cause);
            Debug.LogWarningFormat("OnDisconnected() was called by reason {0}", cause);
            #region 2�� �κ� UI�� ����
            progressLabel.SetActive(false);
            controlPanel.SetActive(true);
            #endregion
        }

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            // base.OnJoinRandomFailed(returnCode, message);
            PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = maxPlayersPerRoom });
        }
        #region OnJoinRandomFailed(~~) ����
        /*
        1. �뿡 ���� ������ �� ȣ��Ǵ� �Լ�
        2. CreateRoom�� ���� ���� �����մϴ�
        3. �� �� �� ������ ����ü�� �����Ǵµ� �ش� �κ��� ���߿� ����� �ſ���
         */
        #endregion


        public override void OnJoinedRoom() // �뿡 �������� ��
        {
            // base.OnJoinedRoom();
            Debug.Log("Room Joined!");

            #region 4�� ���� �Ŵ������� �ۼ���
            if(PhotonNetwork.CurrentRoom.PlayerCount == 1) // �̰͸� �ۼ��ϸ� �ٽ� �κ�� ���ư��� �ڵ����� �ٽ� ���ӿ����ϰԵ� �׷��� Launcher�� �οﺯ���� �ϳ� �߰��մϴ�(isConnecting)
            {
                Debug.Log("Room for 1 loaded!");

                PhotonNetwork.LoadLevel("PSH_M_Room_for_1");
            }
            #endregion
        }
        #endregion
    }


}
