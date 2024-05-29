using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Pun;

// 5�� �÷��̾� �����
// �̰� �׳� ��׵��� PlayerCtrl��ũ��Ʈ ����� �ǵ�
// �ۼ������ ������ ���� �ٸ��� ����淡 �ű��ؼ� �־���� �ּ��޾���
// ī�޶�� �߿��մϴ�. �̰� �̻��ϰ��ϸ� ���� ȭ���� ���Ⱜ�� ����ΰ��� ��������� ���� �ϴ��󱸿�
// ���⼭�� �߰��� CameraWork��ũ��Ʈ�� �׳� ī�޶� ���۰� ���õ� ��ũ��Ʈ���󱸿�
namespace Com.MyCompany.Game
{
    public class PSH_PlayerAnimatorManager : MonoBehaviourPun
    {
        #region Private Fields
        [SerializeField]
        private float directionDampTime = 0.25f; // ȸ���ϴµ� �ɸ��� �ð�

        #endregion

        #region MonoBehaviour Callbacks

        private Animator animator;
        void Start()
        {
            animator = GetComponent<Animator>(); // animator�� �����ɴϴ�
            if(!animator)
            {
                Debug.LogError("PlayerAnimator is Missing", this);
            }
        }

        // Update is called once per frame
        void Update()
        {
            // 7�� �÷��̾� ��Ʈ��ŷ
            // �ν��Ͻ��� client���� ����ǰ� �ִٸ� PhotonView.IsMine �� true
            // �׷��ϱ� ���������� �� ��ǻ�� �տ��� �÷����ϴ� ����� ��Ÿ��
            if(photonView.IsMine == false && PhotonNetwork.IsConnected == true)
            {
                return; // ������ ������ �����÷��̾ �ƴ϶�� �ƹ��͵� ����
            }
            if(!animator)
            {
                return;
            }

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0); // state������ �����ɴϴ�
            if (stateInfo.IsName("Base Layer.Run")) // ĳ���Ͱ� �޸��� �ִ����� �Ǵ�, Base Layer���� Run����
            {
                if (Input.GetButtonDown("Fire2")) //Fire2 Input�� ����, JumpƮ���� �߻�
                {
                    animator.SetTrigger("Jump");
                }
            }

            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");// �Ϲ����� �Է¹ޱ�
            if (v < 0)
            {
                v = 0;
            }
            
            animator.SetFloat("Speed", h * h + v * v); // animator�� apply motion root�� �����Ͽ� �װɷ� �̵�
            animator.SetFloat("Direction", h, directionDampTime, Time.deltaTime); // �̰� ����ȸ��
                                                                                  // damping Time�� ���ϴ� ������ �����ϴµ� �ɸ��� �ð�
                                                                                  // deltaTime�� Update�Ѽ� ������ 
        }

        #endregion

    }
}

