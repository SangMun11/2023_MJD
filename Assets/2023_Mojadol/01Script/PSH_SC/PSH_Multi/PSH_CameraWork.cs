using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 6번 CameraWork - 이건 로컬플레이어에게만 붙여야함
// My Robot Kyle오브젝트에 부착됨
namespace Com.MyCompany.Game
{
    public class PSH_CameraWork : MonoBehaviour
    {
        #region Private Fields

        [SerializeField] // 물체와의 거리 x-z 평면
        private float distance = 7.0f;

        [SerializeField] // 카메라 높이
        private float height = 3.0f;
        
        [SerializeField] // 중간지점 오프ㅡ셋
        private Vector3 centerOffset = Vector3.zero;

        [SerializeField]
        private bool followOnStart = false;
        // 네트워크 환경이 아닌 곳에서 사용할 때 true로 설정 가능

        [SerializeField]
        private float smoothSpeed = 0.125f;

        // 카메라 위치
        Transform cameraTransform;
        
        // 따라갈지 말지를 정해주는 부울변수
        bool isFollowing;

        // 카메라 오프셋
        Vector3 cameraOffset = Vector3.zero;

        #endregion

        #region MonoBehaviour Callbacks
        void Start()
        {
            if (followOnStart)
            {
                OnStartFollowing();
            }
        }


        void LateUpdate()
        {
            if (cameraTransform == null && isFollowing)
            {
                OnStartFollowing();
            }

            if (isFollowing)
            {
                Follow();
            }
        }
        #endregion

        #region Public Methods
        // 이 메소드는 플레이어 프리팹 네트워킹 챕터에서 설정하고 생성한 PlayerManager스크립트에서 수행
        public void OnStartFollowing()
        {
            cameraTransform = Camera.main.transform;
            isFollowing = true;
            Cut();
        }
        #endregion

        #region Private Methods
        void Follow()
        {
            cameraOffset.z = -distance;
            cameraOffset.y = height;

            cameraTransform.position = Vector3.Lerp(cameraTransform.position, this.transform.position + this.transform.TransformVector(cameraOffset), smoothSpeed * Time.deltaTime);
            // Lerping과 LookAt함수를 통해 부드럽게 플레이어를 바라봄
            cameraTransform.LookAt(this.transform.position + centerOffset);
        }


        void Cut()
        {
            cameraOffset.z = -distance;
            cameraOffset.y = height;

            cameraTransform.position = this.transform.position + this.transform.TransformVector(cameraOffset);

            cameraTransform.LookAt(this.transform.position + centerOffset);
        }
        #endregion

        // 로컬플레이어에게 카메라를 할당하기 위한 3가지 방법
        // 1. CameraWork의 스크립트를 로컬 플레이어에게만 붙이던가
        // 2. CameraWork의 동작은 따라가야하는 플레이어가 로컬플레이어인지 아닌지에 따라
        // 3. 껐다 켰다하며 조절
        // 아니면 해당씬에 로컬 플레이어 인스턴스가 있는지 확인하고 해당 인스턴스만 따름
    }
}

