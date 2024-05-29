using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

using Photon.Pun;

// 5번 플레이어 매니저 빔 부분을 만들게 됨
// 7번 네트워크에서 카메라 제어
namespace Com.MyCompany.Game
{
    public class PSH_PlayerManager : MonoBehaviourPunCallbacks, IPunObservable
    {
        #region IPunObservable implementation
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            
            if (stream.IsWriting)
            {
                stream.SendNext(isFiring);
                stream.SendNext(Health); // 체력값도 동기화 함, isFiring과 같은 원리
            }
            else
            {
                this.isFiring = (bool)stream.ReceiveNext();
                this.Health = (float)stream.ReceiveNext();
            }
        }
        #region OnPhotonSerializeView설명
        // 스트림 변수 -> 네트워크를 통해 전송되며, 이 호출로 데이터를 읽고 씀
        // photonView.IsMine == true 일 때 write 할 수 있으며 그렇지 않으면 read
        // stream.isWriting을 통하여 현재 인스턴스 케이스에서 무엇이 올지 예측

        // 데이터를 write하는 것이 예측된다면 stream.SendNext를 이용하여 isFiring Value를
        // data의 스트림에 추가합니다. 데이터를 read하는 것이 예상되면 stream.ReceiveNext()를 사용
        #endregion
        #endregion


        #region Private Fields
        [SerializeField]
        private GameObject beams; // 게임 오브젝트로 만든 빔
        bool isFiring;

        [SerializeField]
        private GameObject playerUiPrefab;
        #endregion

        #region Public Fields
        public float Health = 1f;
        public static GameObject LocalPlayerInstance; // 또 싱글톤
        #endregion

        #region MonoBehaviour Callbacks
        void Awake()
        {
            
            if (beams == null)
            {
                Debug.LogError("<Color=Red><a>Missing</a></Color> Beams Reference.", this);
            }
            else
            {
                beams.SetActive(false);
            }

            if (photonView.IsMine)
            {
                PSH_PlayerManager.LocalPlayerInstance = this.gameObject; // 싱글톤 생성
            }
            DontDestroyOnLoad(this.gameObject);
        }
        void Start()
        {
            // 카메라 제어
            // PSH_CameraWork컴포넌트를 가져옵니다.
            PSH_CameraWork _cameraWork = this.gameObject.GetComponent<PSH_CameraWork>();
            if(_cameraWork != null)
            {
                if(photonView.IsMine)// 로컬이라면 이 인스턴스를 따라갑니다
                {
                    _cameraWork.OnStartFollowing(); // 그 따라가는 함수 호출
                }
                // 로컬 플레이어가 아니라면 아무것도 하지 않습니다.
            }
            else
            {
                Debug.LogError("<Color=Red><a>Missing</a></Color> CameraWork Component on playerPrefab.", this);
            }

            if(playerUiPrefab != null)
            {
                GameObject _uiGo = Instantiate(playerUiPrefab);
                _uiGo.SendMessage("SetTarget", this, SendMessageOptions.RequireReceiver);
            }
            else
            {
                Debug.LogWarning("<Color=Red><a>Missing</a></Color> PlayerUiPrefab reference on player Prefab.", this);
            }

            // 경기장의 크기는 플레이어 수에 따라 변하는데 플레이어가 경기장 크기의 제한에 가까울 경우가 발생 가능(이 예제게임의 특징 때문임
            // 경기장의 중심부로 플레이어들의 위치를 재조정하는 것 -> 게임플레이와 레벨 디자인에 이슈가 발생
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, loadingMode) =>
            {
                this.CalledOnLevelWasLoaded(scene.buildIndex); // 커스텀 함수
            };
        }


        void Update()
        {
            if (photonView.IsMine) // 로컬 플레이어 일때만...
            {
                ProcessInputs(); // 근데 이렇게 한다면 로컬 플레이어만 볼 수 있음
                // 네트워크를 경유하여 발사를 동기화 해주는 메카니즘이 필요
                // 이것을 위해서 isFiring값을 동기화 할 것
                // 해당 메커니즘은 특수한 것이므로, 수동적인 방법으로 구현 (IPunObservable)
            }
            if (Health <= 0f)
            {
                PSH_GameManager.Instance.LeaveRoom(); // 싱글톤을 통해 나가는 함수 호출
            }
            if (beams != null && isFiring != beams.activeInHierarchy)
            {
                beams.SetActive(isFiring);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            // PhotonView.IsMine -> 이게 로컬 플레이어인지 아닌지를 부울로 리턴해줍니다
            if (!photonView.IsMine)
            {
                return; // 이 플레이어가 로컬 플레이어가 아니라면 아무것도 안함
            }
            if (!other.name.Contains("Beam"))
            {
                return; // 충돌한 오브젝트가 "빔"이 아니라면 아무것도 안함
            }
            Health -= 0.1f;
        }

        void OnTriggerStay(Collider other) // 충돌하는동안 도트딜
        {
            if (!photonView.IsMine)
            {
                return;
            }
            if (!other.name.Contains("Beam"))
            {
                return;
            }
            Health -= 0.1f * Time.deltaTime;
        }


        // 플레이어들이 아레나 밖에 있는지 체크한다, 그렇다면 안전구역 주위에 스폰되게함
        void CalledOnLevelWasLoaded(int level)
        {
            // 플레이어 수에 따라 UI프리팹 생성
            GameObject _uiGo = Instantiate(this.playerUiPrefab);
            _uiGo.SendMessage("SetTarget", this, SendMessageOptions.RequireReceiver);

            // 레이캐스트를 통해 공중에 떠있는지 아닌지를 확인
            if (!Physics.Raycast(transform.position, -Vector3.up, 5f))
            {
                transform.position = new Vector3(0f, 5f, 0f);
            }
        }
        
        #endregion

        #region Custom
        // 플레이어가 쏠 빔을 활성화하는 코드
        void ProcessInputs()
        {
            if (Input.GetButtonDown("Fire1"))
            {
                if(!isFiring)
                {
                    isFiring = true;
                }
            }
            if (Input.GetButtonUp("Fire1"))
            {
                if (isFiring)
                {
                    isFiring = false;
                }
            }
        }

        #endregion
    }
}

