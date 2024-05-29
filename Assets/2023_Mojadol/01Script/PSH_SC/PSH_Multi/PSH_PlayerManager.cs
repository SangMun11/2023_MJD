using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

using Photon.Pun;

// 5�� �÷��̾� �Ŵ��� �� �κ��� ����� ��
// 7�� ��Ʈ��ũ���� ī�޶� ����
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
                stream.SendNext(Health); // ü�°��� ����ȭ ��, isFiring�� ���� ����
            }
            else
            {
                this.isFiring = (bool)stream.ReceiveNext();
                this.Health = (float)stream.ReceiveNext();
            }
        }
        #region OnPhotonSerializeView����
        // ��Ʈ�� ���� -> ��Ʈ��ũ�� ���� ���۵Ǹ�, �� ȣ��� �����͸� �а� ��
        // photonView.IsMine == true �� �� write �� �� ������ �׷��� ������ read
        // stream.isWriting�� ���Ͽ� ���� �ν��Ͻ� ���̽����� ������ ���� ����

        // �����͸� write�ϴ� ���� �����ȴٸ� stream.SendNext�� �̿��Ͽ� isFiring Value��
        // data�� ��Ʈ���� �߰��մϴ�. �����͸� read�ϴ� ���� ����Ǹ� stream.ReceiveNext()�� ���
        #endregion
        #endregion


        #region Private Fields
        [SerializeField]
        private GameObject beams; // ���� ������Ʈ�� ���� ��
        bool isFiring;

        [SerializeField]
        private GameObject playerUiPrefab;
        #endregion

        #region Public Fields
        public float Health = 1f;
        public static GameObject LocalPlayerInstance; // �� �̱���
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
                PSH_PlayerManager.LocalPlayerInstance = this.gameObject; // �̱��� ����
            }
            DontDestroyOnLoad(this.gameObject);
        }
        void Start()
        {
            // ī�޶� ����
            // PSH_CameraWork������Ʈ�� �����ɴϴ�.
            PSH_CameraWork _cameraWork = this.gameObject.GetComponent<PSH_CameraWork>();
            if(_cameraWork != null)
            {
                if(photonView.IsMine)// �����̶�� �� �ν��Ͻ��� ���󰩴ϴ�
                {
                    _cameraWork.OnStartFollowing(); // �� ���󰡴� �Լ� ȣ��
                }
                // ���� �÷��̾ �ƴ϶�� �ƹ��͵� ���� �ʽ��ϴ�.
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

            // ������� ũ��� �÷��̾� ���� ���� ���ϴµ� �÷��̾ ����� ũ���� ���ѿ� ����� ��찡 �߻� ����(�� ���������� Ư¡ ������
            // ������� �߽ɺη� �÷��̾���� ��ġ�� �������ϴ� �� -> �����÷��̿� ���� �����ο� �̽��� �߻�
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, loadingMode) =>
            {
                this.CalledOnLevelWasLoaded(scene.buildIndex); // Ŀ���� �Լ�
            };
        }


        void Update()
        {
            if (photonView.IsMine) // ���� �÷��̾� �϶���...
            {
                ProcessInputs(); // �ٵ� �̷��� �Ѵٸ� ���� �÷��̾ �� �� ����
                // ��Ʈ��ũ�� �����Ͽ� �߻縦 ����ȭ ���ִ� ��ī������ �ʿ�
                // �̰��� ���ؼ� isFiring���� ����ȭ �� ��
                // �ش� ��Ŀ������ Ư���� ���̹Ƿ�, �������� ������� ���� (IPunObservable)
            }
            if (Health <= 0f)
            {
                PSH_GameManager.Instance.LeaveRoom(); // �̱����� ���� ������ �Լ� ȣ��
            }
            if (beams != null && isFiring != beams.activeInHierarchy)
            {
                beams.SetActive(isFiring);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            // PhotonView.IsMine -> �̰� ���� �÷��̾����� �ƴ����� �ο�� �������ݴϴ�
            if (!photonView.IsMine)
            {
                return; // �� �÷��̾ ���� �÷��̾ �ƴ϶�� �ƹ��͵� ����
            }
            if (!other.name.Contains("Beam"))
            {
                return; // �浹�� ������Ʈ�� "��"�� �ƴ϶�� �ƹ��͵� ����
            }
            Health -= 0.1f;
        }

        void OnTriggerStay(Collider other) // �浹�ϴµ��� ��Ʈ��
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


        // �÷��̾���� �Ʒ��� �ۿ� �ִ��� üũ�Ѵ�, �׷��ٸ� �������� ������ �����ǰ���
        void CalledOnLevelWasLoaded(int level)
        {
            // �÷��̾� ���� ���� UI������ ����
            GameObject _uiGo = Instantiate(this.playerUiPrefab);
            _uiGo.SendMessage("SetTarget", this, SendMessageOptions.RequireReceiver);

            // ����ĳ��Ʈ�� ���� ���߿� ���ִ��� �ƴ����� Ȯ��
            if (!Physics.Raycast(transform.position, -Vector3.up, 5f))
            {
                transform.position = new Vector3(0f, 5f, 0f);
            }
        }
        
        #endregion

        #region Custom
        // �÷��̾ �� ���� Ȱ��ȭ�ϴ� �ڵ�
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

