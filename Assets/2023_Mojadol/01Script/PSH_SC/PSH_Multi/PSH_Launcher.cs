using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

// 1번 로비 구성
namespace Com.MyCompany.Game // 다른 개발자 코드와의 충돌을 막기위해 지정했다하네요. 굳이 안써도 되긴 할 듯
{
    // MonoBehaviour -> MonoBehaviourPunCallbacks로 바꿉니당
    // PUN 콜백(대충 멀티에서 써먹을 것들이라고 이해하셈)은 2가지로 나뉩니다
    // 둘 중 하나가 MonoBehaviour -> MonoBehaviourPunCallbacks 이렇게 바꾸는 것
    public class PSH_Launcher : MonoBehaviourPunCallbacks
    {
        string gameVersion = "1"; // 게임 버전을 스트링으로 기술, 같은 버전들 끼리만 연결됩니당

        [SerializeField]
        private byte maxPlayersPerRoom = 4; // 룸당 최대 인원

        #region 2번 로비 UI구성에 쓰인 변수들
        [SerializeField]
        private GameObject controlPanel; // 플레이어들이 이름을 입력하고, 연결 후 플레이할 때 쓰일 것임

        [SerializeField]
        private GameObject progressLabel; // 플레이어들에게 연결상태에 대해 알려줄 때 쓰일 것임

        #endregion

        #region 4번 게임매니저~~에서 작성된 변수들
        bool isConnecting;
        #endregion

        #region MonoBehaviour Callbacks

        void Awake()
        {
            PhotonNetwork.AutomaticallySyncScene = true; // 이 값이 true가 되면 PhotonNetwork.LoadLevel()을 호출, 연결된 플레이어들은 동일한 레벨을 자동적으로 로드
        }

        void Start()
        {
            #region 2번 로비 UI에 쓰임
            progressLabel.SetActive(false);
            controlPanel.SetActive(true);
            #endregion

        }

        #endregion

        #region Public Methods

        public void Connect() // 내가 기술한 게임 룸에 입장하는 함수
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
            #region 2번 로비 UI에 쓰임
            progressLabel.SetActive(true);
            controlPanel.SetActive(false);
            #endregion
        }
        #region Connect()함수 설명
        /*
            1. 만약 포톤네트워크에 연결되었다면 --> 무작위 룸에 입장합니다
            2.                    아니라면 --> 게임버전을 선언한 게임버전으로 지정 후, 연결합니다
            3. *ConnectUsingSettings() 포톤서버에 PhotonServerSettings 파일에 설정된 상태로 입장합니다
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
        #region OnConnectedToMaster() 설명
        /*
        1. 마스터클라이언트에 연결되었을 때 호출됩니다.
        2. JoinRandomRoom()을 통해 무작위 룸에 입장
        */

        #endregion


        public override void OnDisconnected(DisconnectCause cause) // 연결이 끊어졌을 때
        {
            // base.OnDisconnected(cause);
            Debug.LogWarningFormat("OnDisconnected() was called by reason {0}", cause);
            #region 2번 로비 UI에 쓰임
            progressLabel.SetActive(false);
            controlPanel.SetActive(true);
            #endregion
        }

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            // base.OnJoinRandomFailed(returnCode, message);
            PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = maxPlayersPerRoom });
        }
        #region OnJoinRandomFailed(~~) 설명
        /*
        1. 룸에 입장 실패할 때 호출되는 함수
        2. CreateRoom을 통해 룸을 생성합니다
        3. 이 때 룸 생성은 구조체로 생성되는데 해당 부분은 나중에 기술될 거에요
         */
        #endregion


        public override void OnJoinedRoom() // 룸에 입장했을 때
        {
            // base.OnJoinedRoom();
            Debug.Log("Room Joined!");

            #region 4번 게임 매니저에서 작성됨
            if(PhotonNetwork.CurrentRoom.PlayerCount == 1) // 이것만 작성하며 다시 로비로 돌아갈시 자동으로 다시 게임연결하게됨 그래서 Launcher에 부울변수를 하나 추가합니다(isConnecting)
            {
                Debug.Log("Room for 1 loaded!");

                PhotonNetwork.LoadLevel("PSH_M_Room_for_1");
            }
            #endregion
        }
        #endregion
    }


}
