using System.Collections;
using System.Collections.Generic;

using System;

using UnityEngine;
using UnityEngine.SceneManagement;

using Photon.Pun;
using Photon.Realtime;

// 3�� ���Ӿ�
// 4�� ���� �Ŵ����� ����
namespace Com.MyCompany.Game
{
    public class PSH_GameManager : MonoBehaviourPunCallbacks
    {
        #region Public Variables region
        public GameObject playerPrefab;
        #endregion

        #region Photon Callbacks
        public override void OnLeftRoom() // �������̵�� �ݹ��Լ�, ���� ������ Launcher ������ �̵���Ų��
        {
            SceneManager.LoadScene(0);
        }

        #region 4�� ���� �Ŵ����� �������� �ۼ�
        public override void OnPlayerEnteredRoom(Player other)
        {
            Debug.LogFormat("OnPlayerEneteredRoom() {0}", other.NickName);

            if(PhotonNetwork.IsMasterClient) // ���� �÷��̾ MasterClient�� �� ȣ��˴ϴ� (�ٸ� �÷��̾ ���� ��
            {
                Debug.LogFormat("OnPlayerEnteredRoom IsMasterClient {0}", PhotonNetwork.IsMasterClient);

                LoadArena();
            }
        }

        public override void OnPlayerLeftRoom(Player other)
        {
            Debug.LogFormat("OnPlayerLeftRoom() {0}", other.NickName); // seen when other disconnects

            if (PhotonNetwork.IsMasterClient) // �÷��̾ ���� ��, MasterClient�� �� ȣ��
            {
                Debug.LogFormat("OnPlayerLeftRoom IsMasterClient {0}", PhotonNetwork.IsMasterClient); // called before OnPlayerLeftRoom

                LoadArena();
            }
        }

        // OnPlayerEnterRoom, OnPlayerLeftRoom�� �÷��̾� ���� ���� LoadArena�� ȣ���ϱ� ���� �����ǵǾ� �������
        // �ֳĸ� �� ������ �÷��̾� ���� ���� ���� ũ�Ⱑ ���ϴ� ���̱� �����̴ϱ�
        #endregion

        #endregion

        #region Private Methods

        #region 4�� ���� �Ŵ����� �������� �ۼ�
        void LoadArena()
        {
            if(!PhotonNetwork.IsMasterClient)
            {
                Debug.LogError("we are not the masterClient");
                return;
            }

            Debug.LogFormat("Loading Level : {0}", PhotonNetwork.CurrentRoom.PlayerCount);
            PhotonNetwork.LoadLevel("PSH_M_Room_for_" + PhotonNetwork.CurrentRoom.PlayerCount); // �÷��̾��� ���� ���� ���� ���� ������
        }
        #region LoadArena ����
        /*
         * 1. PhotonNetwork.LoadLevel()�� ������Ŭ���̾�Ʈ�� ��쿡�� ȣ�� <- PhotonNetwork.IsMasterClient�� ���� üũ
         * 2. PhotonNetwork.automaticallySyncScene�� ���� Photon�� ����� ��� Ŭ���̾�Ʈ�鿡�� ������ �ε��ϰ���(����Ƽ�� �ƴ϶�)
         */
        #endregion


        #endregion

        #endregion

        #region Public Methods

        // 5�� �÷��̾� ����� �߿� �ۼ��Ǹ�, �÷��̾� ü���� 0�� �Ǿ��� ���� ���� �������
        public static PSH_GameManager Instance; // �̱��� start ����

        public void LeaveRoom()
        {
            PhotonNetwork.LeaveRoom(); // �� ������ �޼ҵ�
        }
        #endregion

        void Start()
        {
            Instance = this;


            if (playerPrefab == null) // playerPrefab�� ������ �ȵǾ��ִٸ�
            {
                Debug.LogError("No playerPrefab Reference", this); // �ƹ��͵� ���ϰ� ���� ���
            }
            else
            {
                // PlayerManager�� localPlayer�� ���� �ν��Ͻ��� ������ ���� ���� �ν��Ͻ� ������ �ϰ� ��
                if(PSH_PlayerManager.LocalPlayerInstance == null)
                {
                    Debug.LogFormat("Instantiating LocalPlayer from {0}", Application.loadedLevelName); // �ε�� ������ ĳ���� �����Ѵٴ� �α�
                    PhotonNetwork.Instantiate(this.playerPrefab.name, new Vector3(0f, 5f, 0f), Quaternion.identity, 0); // ĳ���� ����
                }
                //���⼭ �������� �����ϴ� �� PhotonNetwork.Instantiate�� ���� �����մϴ�.
                // �����Ǵ� ������Ʈ �̸��� ���ڿ� �������� �Է��� �޽��ϴ�.
            }
        }
    }
}


