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
        private Text playerNameText; // 플레이어 이름

        [SerializeField]
        private Slider playerHealthSlider; // 플레이어 체력바

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
            // UI 요소는 Canvas게임 오브젝트 내에 위치해야한다
            // 이 코드를 통해 신이 로드, 언로드 될 때 프리팹도 같이 로드 언로드되며 캔버스가 매번 다르게 되는 현상을 막는다.
            this.transform.SetParent(GameObject.Find("Canvas").GetComponent<Transform>(), false);
        }

        void Update()
        {
            // 슬라이더 체력과 연결
            if (playerHealthSlider != null)
            {
                playerHealthSlider.value = target.Health;
            }

            // 타겟이 없으면 삭제
            if(target == null)
            {
                Destroy(this.gameObject);
                return;
            }
        }

        // WorldToScreenPoint함수를 이용하여 characterControllerHeight을 더함
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
            // 효율을 위한 캐시 레퍼런스
            target = _target;

            CharacterController _characterController = _target.GetComponent<CharacterController>();
            if(_characterController != null)
            {
                characterControllerHeight = _characterController.height;
            }

            if(playerNameText != null)
            {
                playerNameText.text = target.photonView.Owner.NickName; // 이름 지정
            }
        }

        #endregion
    }
}

