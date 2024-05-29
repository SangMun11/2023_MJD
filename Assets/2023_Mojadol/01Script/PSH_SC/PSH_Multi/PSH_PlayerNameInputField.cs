using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Photon.Pun;
using Photon.Realtime;

// 2�� �κ� UI
// Control Panel�� Progress Label�� ���� ������ �÷��̾�鿡�� ������ ��ô ��Ȳ�� �˷��ֱ� ����
namespace Com.MyCompany.Game
{
    [RequireComponent(typeof(InputField))] // InputField ������Ʈ�� ���̴� �ڵ�.?
    public class PSH_PlayerNameInputField : MonoBehaviour
    {
        #region Private Constants

        const string playerNamePrefKey = "PlayerName"; // �÷��̾� �̸�

        #endregion

        #region MonoBehaviour Callbacks 

        void Start()
        {
            // �÷��̾��� �̸��� PlayerPrefs�� ���� �����ϴ� �ڵ�
            string defaultName = string.Empty;
            InputField _inputField = this.GetComponent<InputField>();
            if(_inputField != null) // �ƹ� �̸��� ������ PlayerPrefs�� ���� �̸��� ����
            {
                if(PlayerPrefs.HasKey(playerNamePrefKey))
                {
                    defaultName = PlayerPrefs.GetString(playerNamePrefKey);
                    _inputField.text = defaultName;
                }
            }

            PhotonNetwork.NickName = defaultName; // ��Ʈ��ũ���� �÷��̾� �̸�
        }

        #endregion

        #region Public Methods
        public void SetPlayerName(string value) // InputField�� ���Ͽ� �÷��̾� �̸��� ����, ��Ʈ��ũ�� �̸����� / �� �Լ��� InputField OnValuedChange���� ȣ��
        {
            if(string.IsNullOrEmpty(value))
            {
                Debug.LogError("No name"); // �� �κп��� ���ڰ� �ƹ��͵� ������ ���װ� �߻��ϱ� ��
                return;
            }
            PhotonNetwork.NickName = value;

            PlayerPrefs.SetString(playerNamePrefKey, value);
            Debug.Log($"PhotonNetwork.NickName = {PhotonNetwork.NickName}");
        }

        #endregion
    }

}
