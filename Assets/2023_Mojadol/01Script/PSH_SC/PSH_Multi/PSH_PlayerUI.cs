using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Com.MyCompany.Game
{
    public class PSH_PlayerUI : MonoBehaviour
    {
        #region Private Fields
        [SerializeField]
        private Text playerNameText; // �÷��̾� �̸�

        [SerializeField]
        private Slider playerHealthSlider; // �÷��̾� ü�¹�

        PSH_PlayerManager target;
        #endregion

        #region Public Fields
        [SerializeField]
        private Vector3 screenOffset = new Vector3(0f, 30f, 0f); // 
        #endregion

        #region Private Fields Messages 
        float characterControllerHeight = 0f;
        Transform targetTransform;
        Vector3 targetPosition;
        #endregion



        #region MonoBehaviour CallBacks
        private void Awake()
        {
            // UI ��Ҵ� Canvas���� ������Ʈ ���� ��ġ�ؾ��Ѵ�
            // �� �ڵ带 ���� ���� �ε�, ��ε� �� �� �����յ� ���� �ε� ��ε�Ǹ� ĵ������ �Ź� �ٸ��� �Ǵ� ������ ���´�.
            this.transform.SetParent(GameObject.Find("Canvas").GetComponent<Transform>(), false);
        }

        void Update()
        {
            // �����̴� ü�°� ����
            if (playerHealthSlider != null)
            {
                playerHealthSlider.value = target.Health;
            }

            // Ÿ���� ������ ����
            if(target == null)
            {
                Destroy(this.gameObject);
                return;
            }
        }

        // WorldToScreenPoint�Լ��� �̿��Ͽ� characterControllerHeight�� ����
        private void LateUpdate()
        {
            if (targetTransform != null)
            {
                targetPosition = targetTransform.position;
                targetPosition.y += characterControllerHeight;
                this.transform.position = Camera.main.WorldToScreenPoint(targetPosition) + screenOffset;
            }
        }
        #endregion

        #region Public Methods
        public void SetTarget(PSH_PlayerManager _target)
        {
            if(_target == null)
            {
                Debug.LogError("<Color=Red><a>Missing</a></Color> PlayMakerManager target for PlayerUI.SetTarget.", this);
                return;
            }
            // ȿ���� ���� ĳ�� ���۷���
            target = _target;

            CharacterController _characterController = _target.GetComponent<CharacterController>();
            if(_characterController != null)
            {
                characterControllerHeight = _characterController.height;
            }

            if(playerNameText != null)
            {
                playerNameText.text = target.photonView.Owner.NickName; // �̸� ����
            }
        }

        #endregion
    }
}

