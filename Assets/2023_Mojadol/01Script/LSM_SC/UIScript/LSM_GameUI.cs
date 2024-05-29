using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


// 게임 진행 중에 플레이어에게 보이는 UI를 관리하는 스크립트.
public class LSM_GameUI : MonoBehaviour
{
                                                // # Canvas 안 GameUI의 자식오브젝트
    public Image playerHP, targetHP;            // # Player의 자식 오브젝트 중 CurrentHP        -> playerHP
                                                // # Enemy의 자식 오브젝트 중 CurrentHP         -> targetHP
    public Image playerExp;

    public TextMeshProUGUI targetName;          // # Enemy의 자식 오브젝트 중 TargetName
    public TextMeshProUGUI playerHP_txt, playerGold_txt, playerLevel_txt;

    public GameObject[] playerIcons;

    public GameObject suicide_obj;
    public Image suicide_current_gauge, suicide_screen;
    public GameObject targetUI, playerUI;       // # Enemy      -> targetUI
                                                // # Player     -> playerUI
    public Image QSkillCool, ESkillCool;        // # QSkillCool -> Qcool    ESkillCool -> Ecool
    public GameObject DamagedDirection;
    
    private I_Actor player_ac, target_ac;
    private I_Playable player_playable;
    private GameObject target_obj;
    private int type_;


    private void Awake()
    {
        QSkillCool.color = new Color32(0, 0, 0, 150);
        ESkillCool.color = new Color32(0, 0, 0, 150);
    }

    private void OnEnable()
	{
		playerUI.SetActive(false);
        targetUI.SetActive(false);
	}

	// 타겟 UI 온오프.
	public void enableTargetUI(bool on)
    {
        targetUI.SetActive(on);
    }
    public void enableTargetUI(bool on, GameObject obj)
    {
        enableTargetUI(on);
        target_obj = obj;
        target_ac = obj.GetComponent<I_Actor>();
    }
    public void playerHealth(GameObject ctrl)
    { 
        playerUI.SetActive(true);  
        player_ac = ctrl.GetComponent<I_Actor>(); 
        player_playable = ctrl.GetComponent<I_Playable>();
        type_ = ctrl.GetComponent<LSM_PlayerBase>().myPlayerCtrl.PlayerType;
        for (int i = 0; i < playerIcons.Length; i++) { playerIcons[i].SetActive(i == type_); }
    }
    // 모든 캐릭터들이 갖고있는 공통적인 것. I_Actor를 받아와 구문을 최소화.


    private void LateUpdate()
	{
        if (targetUI.activeSelf)
        {
            targetName.text = (target_ac.GetTeam() != MoonHeader.Team.Yellow ? target_ac.GetTeam().ToString() + " Team " : "")
                + target_obj.name;
            //targetName.text = string.Format("{0} Team {1}", target_ac.GetTeam().ToString(), target_obj.name);
            targetHP.fillAmount = Mathf.Round((float)target_ac.GetHealth() / target_ac.GetMaxHealth() * 100) / 100;
            if (target_ac.GetHealth() <= 0)
                enableTargetUI(false);
        }

        if (playerUI.activeSelf)
        { 
            playerHP.fillAmount = Mathf.Round((float)player_ac.GetHealth() / player_ac.GetMaxHealth() * 100) / 100;
            playerHP_txt.text = player_ac.GetHealth() + " / " + player_ac.GetMaxHealth();
            playerGold_txt.text = player_playable.GetGold() + " G";
            playerLevel_txt.text = player_playable.GetLV().ToString() + "LV";
            playerExp.fillAmount = (Mathf.Round(((float)player_playable.GetExp() / LSM_SettingStatus.Instance.lvStatus[type_].needExp[player_playable.GetLV()]) * 100) / 100) * ((float)45/100) + 0.55f;

            //QSkillCool.color = new Color32(0, 0, 0, (byte)(player_playable.IsCanUseQ() || !player_playable.IsCanHit() ? 0 : 150));
            //ESkillCool.color = new Color32(0, 0, 0, (byte)(player_playable.IsCanUseE() || !player_playable.IsCanHit() ? 0 : 150));
            QSkillCool.fillAmount =(player_playable.IsCanHit())? player_playable.IsCanUseQ() : 1f;
            ESkillCool.fillAmount = (player_playable.IsCanHit()) ? player_playable.IsCanUseE() : 1f;

            if (player_playable.GetF() >= 0.5f)
            {
                suicide_screen.gameObject.SetActive(true);
                suicide_obj.SetActive(true);
                suicide_current_gauge.fillAmount = (float)player_playable.GetF() / 3f;
                suicide_screen.color = new Color32(110,10,10, (byte)Mathf.CeilToInt(((float)player_playable.GetF()/3) * 200));
            }
            else { suicide_obj.SetActive(false); suicide_screen.gameObject.SetActive(false); }
        }
    }

}
