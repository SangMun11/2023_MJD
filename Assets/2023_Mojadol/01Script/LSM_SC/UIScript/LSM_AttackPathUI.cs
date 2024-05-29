using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// 공격로 비율 지정에 사용되는 아이콘 스크립트.
public class LSM_AttackPathUI : MonoBehaviour
{
    public TextMeshProUGUI num;				// 표시되는 숫자.
    public Slider sl;						// 슬라이더
	public LSM_Spawner parentSpawner;		// 해당 UI가 사용되는 마스터스포너
	public LSM_SpawnPointSc spawnPoint;		// 해당 UI가 사용되는 스폰포인트
	private Camera mapcam, minimapcam;                  // 맵을 볼때 사용되는 카메라
	private bool once;
	private Vector3 originSize;

    private void Awake()
    {
        sl = this.GetComponentInChildren<Slider>();
    }
    private void Start_function()
	{
		//num.text = "0";
		once = true;
		num.text = sl.value.ToString();
		mapcam = GameManager.Instance.mainPlayer.MapCam.GetComponent<Camera>();
		minimapcam = GameManager.Instance.mainPlayer.MiniMapCam.GetComponent<Camera>();
		originSize = Vector3.one;
		//transform.SetAsFirstSibling();	// 해당 UI가 다른 UI를 가리지 않기 위해 가장 상단으로 위치. 해당 부분은 EmptyObject내에 자식 오브젝트로 생성하기에 필요없는 부분이 되었음.
	}

	// 다시 UI가 활성화 될 경우, 현재 게임 상태 중 공격로 설정 턴에 슬라이더를 표시하도록 구현.
	private void OnEnable()
	{
		sl.gameObject.SetActive(GameManager.Instance.gameState == MoonHeader.GameState.SettingAttackPath);
	}

	private void Update()
	{
		// 생성 후 초기화를 하기 전에 실행되는 것을 방지.
		if (!ReferenceEquals(spawnPoint, null) && GameManager.Instance.onceStart
			&& GameManager.Instance.onceStart)
		{
			if (!once)
				Start_function();

			// 게임 시작 전, 공격로 선택 중에 스폰포인트와 첫번째 웨이포인트 사이에 UI를 위치하도록 구현.
			if (GameManager.Instance.gameState == MoonHeader.GameState.SettingAttackPath || 
				GameManager.Instance.gameState == MoonHeader.GameState.StartGame)
			{
				if (mapcam.gameObject.activeSelf)
					this.transform.position = mapcam.WorldToScreenPoint(spawnPoint.Paths[0].transform.position);
				else if (minimapcam.gameObject.activeSelf)
                    this.transform.position = minimapcam.WorldToScreenPoint(spawnPoint.Paths[0].transform.position);

                //num.text = GameManager.Instance.teamManagers[(int)parentSpawner.team].AttackPathNumber[spawnPoint.number].ToString();
                num.text = sl.value.ToString();
				originSize = Vector3.one;
			}
			// 게임 중, 스폰포인트의 위치에 UI를 위치하도록 구현.
			else if (GameManager.Instance.gameState == MoonHeader.GameState.Gaming)
			{
				//this.transform.position = Camera.main.WorldToScreenPoint(spawnPoint.transform.position);
				Vector3 dummy_pos = spawnPoint.transform.position + (spawnPoint.Paths[0].transform.position - spawnPoint.transform.position) * 0.4f;
				if (mapcam.gameObject.activeSelf)
					this.transform.position = mapcam.WorldToScreenPoint(dummy_pos);
				//else if (minimapcam.gameObject.activeSelf)
					//this.transform.position = dummy_pos;
                    
                num.text = parentSpawner.spawnpoints[spawnPoint.number].num.ToString();
				originSize = Vector3.one * 0.5f;
			}
			this.transform.localScale = originSize * Mathf.Max(0.1f, Mathf.Min(1, 1 - (mapcam.orthographicSize - 60) * 0.015f));
		}
		
	}

	// 초기화함수. 스폰포인트에서 해당 UI를 생성 후 UI가 소속된 마스터 스포너와, 스폰포인트를 저장하도록 구현.
	public void SetParent(LSM_SpawnPointSc sp)
	{
		spawnPoint = sp;
		parentSpawner = sp.parentSpawner.GetComponent<LSM_Spawner>();


		sl.maxValue = GameManager.Instance.teamManagers[(int)parentSpawner.team].MaximumSpawnNum;

	}

	// 슬라이더의 값이 변할 경우 발동되는 함수.
	// 해당하는 팀의 매니저에서 값을 변경하도록 설정.

	public void OnSpideUp(BaseEventData eventData)
	{
        if (!ReferenceEquals(spawnPoint, null))
        {
            GameManager.Instance.teamManagers[(int)parentSpawner.team].PathingNumberSetting(spawnPoint.number, (int)sl.value);
            num.text = sl.value.ToString();
            // 팀매니저에 존재하는 최대값 변경 함수 호출.
            //GameManager.Instance.teamManagers[(int)parentSpawner.team].PathUI_ChangeMaxValue();
        }
    }

	

	public void CheckingServerValue()
	{
		sl.value = GameManager.Instance.teamManagers[(int)parentSpawner.team].AttackPathNumber[spawnPoint.number];
        num.text = sl.value.ToString();
    }

	// 슬라이더 비활성화
	public void InvisibleSlider(bool change) { sl.gameObject.SetActive(change); }
}
