using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Photon.Pun;
using Photon.Realtime;

// 2번 로비 UI
// Control Panel과 Progress Label을 만든 이유는 플레이어들에게 게임의 진척 상황을 알려주기 위함
namespace Com.MyCompany.Game
{
    [RequireComponent(typeof(InputField))] // InputField 컴포넌트를 붙이는 코드.?
    public class PSH_PlayerNameInputField : MonoBehaviour
    {
        #region Private Constants

        const string playerNamePrefKey = "PlayerName"; // 플레이어 이름

        #endregion

        #region MonoBehaviour Callbacks 

        void Start()
        {
            // 플레이어의 이름을 PlayerPrefs를 통해 지정하는 코드
            string defaultName = string.Empty;
            InputField _inputField = this.GetComponent<InputField>();
            if(_inputField != null) // 아무 이름도 없으면 PlayerPrefs를 통해 이름을 가짐
            {
                if(PlayerPrefs.HasKey(playerNamePrefKey))
                {
                    defaultName = PlayerPrefs.GetString(playerNamePrefKey);
                    _inputField.text = defaultName;
                }
            }

            PhotonNetwork.NickName = defaultName; // 네트워크상의 플레이어 이름
        }

        #endregion

        #region Public Methods
        public void SetPlayerName(string value) // InputField를 통하여 플레이어 이름을 지정, 네트워크의 이름까지 / 이 함수는 InputField OnValuedChange에서 호출
        {
            if(string.IsNullOrEmpty(value))
            {
                Debug.LogError("No name"); // 이 부분에서 문자가 아무것도 없으면 버그가 발생하긴 함
                return;
            }
            PhotonNetwork.NickName = value;

            PlayerPrefs.SetString(playerNamePrefKey, value);
            Debug.Log($"PhotonNetwork.NickName = {PhotonNetwork.NickName}");
        }

        #endregion
    }

}
