using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 6�� CameraWork - �̰� �����÷��̾�Ը� �ٿ�����
// My Robot Kyle������Ʈ�� ������
namespace Com.MyCompany.Game
{
    public class PSH_CameraWork : MonoBehaviour
    {
        #region Private Fields

        [SerializeField] // ��ü���� �Ÿ� x-z ���
        private float distance = 7.0f;

        [SerializeField] // ī�޶� ����
        private float height = 3.0f;
        
        [SerializeField] // �߰����� �����Ѽ�
        private Vector3 centerOffset = Vector3.zero;

        [SerializeField]
        private bool followOnStart = false;
        // ��Ʈ��ũ ȯ���� �ƴ� ������ ����� �� true�� ���� ����

        [SerializeField]
        private float smoothSpeed = 0.125f;

        // ī�޶� ��ġ
        Transform cameraTransform;
        
        // ������ ������ �����ִ� �οﺯ��
        bool isFollowing;

        // ī�޶� ������
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
        // �� �޼ҵ�� �÷��̾� ������ ��Ʈ��ŷ é�Ϳ��� �����ϰ� ������ PlayerManager��ũ��Ʈ���� ����
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
            // Lerping�� LookAt�Լ��� ���� �ε巴�� �÷��̾ �ٶ�
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

        // �����÷��̾�� ī�޶� �Ҵ��ϱ� ���� 3���� ���
        // 1. CameraWork�� ��ũ��Ʈ�� ���� �÷��̾�Ը� ���̴���
        // 2. CameraWork�� ������ ���󰡾��ϴ� �÷��̾ �����÷��̾����� �ƴ����� ����
        // 3. ���� �״��ϸ� ����
        // �ƴϸ� �ش���� ���� �÷��̾� �ν��Ͻ��� �ִ��� Ȯ���ϰ� �ش� �ν��Ͻ��� ����
    }
}

