using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 팀 마스터 스포너 아래의 미니언이 생성되는 지점의 스폰 포인트
public class LSM_SpawnPointSc : MonoBehaviour
{
    public GameObject[] Ways;				// 미니언이 갖는 공격로 웨이포인트 지점.
											// # GameManager의 자식오브젝트에 있는 WayPoint를 하나씩 연결. 연결 시 기즈모를 이용한 실선이 보임. 마지막은 상대팀 스폰포인트.
	public GameObject[] Paths;				// 공격로를 플레이어에게 화살표로 보여주는 UI둘
	public int number;						// 스폰포인트의 번호를 알려줌. 
											// # 스폰포인트마다 번호를 다르게 설정해야함. 0 1 2 로 설정을해야 같은 경로의 포탑을 공격. 팀마다 대칭으로 설정.
	public bool isClicked;					// 이전 비율로 정하던 때의 변수. 아직은 쓸 일이 없음.
	public GameObject parentSpawner;		// 이 스폰포인트의 마스터 스포너
	private LSM_Spawner parentSpawnerSC;	// 마스터 스포너의 스크립트

	public GameObject pathUI;               // 플레이어가 조작할 공격로 설정 ui
	public LSM_AttackPathUI pathUI_SC;
	private bool once;

	// 기즈모
	private void OnDrawGizmos()
	{
		
		for (int i = 0; i < Ways.Length; i++)
		{
			Vector3 one;
			one = ((i == 0) ? this.transform.position : Ways[i - 1].transform.position);

			Gizmos.color = Color.red;
			Gizmos.DrawRay(one, Ways[i].transform.position - one);
		}
		
	}

	private void Start()
	{
		// 변수 초기화
		isClicked = false;
		parentSpawner = transform.parent.gameObject;
		parentSpawnerSC = parentSpawner.GetComponent<LSM_Spawner>();
		once = false;

		// 플레이어가 조작할 UI
		pathUI = GameObject.Instantiate(PrefabManager.Instance.icons[2], GameManager.Instance.mapUI.transform);
		pathUI_SC = pathUI.GetComponent<LSM_AttackPathUI>();
		pathUI_SC.SetParent(this);
	}

	private void Start_function()
	{
		//if (GameManager.Instance.mainPlayer.player.team == parentSpawnerSC.team)
		// 화살표 아이콘
		Paths = new GameObject[Ways.Length];
		for (int i = 0; i < Paths.Length; i++)
		{
			Paths[i] = GameObject.Instantiate(PrefabManager.Instance.icons[1], transform);
			Paths[i].GetComponent<LSM_AttackPath>().SetVariable(this.gameObject, number);
			if (parentSpawnerSC.team != GameManager.Instance.mainPlayer.player.team) Paths[i].SetActive(false);
		}
        if (parentSpawnerSC.team != GameManager.Instance.mainPlayer.player.team) pathUI.SetActive(false);

    }

	//public void Click(bool change) { isClicked = change; }

	private void Update()
	{
		if (GameManager.Instance.onceStart && !once)
		{
			Start_function();
			once = true;
		}

		// 공격로 아이콘의 위치 조정.
		for (int i = 0; i < Paths.Length; i++)
		{
			Vector3 origin;
			origin = ((i == 0) ? this.transform.position : Ways[i - 1].transform.position);

			Paths[i].transform.position = (Ways[i].transform.position - origin)*0.5f + origin;
			Paths[i].transform.LookAt(Ways[i].transform.position);
			Paths[i].transform.rotation = Quaternion.Euler(Paths[i].transform.rotation.eulerAngles + (Vector3.right * 90));
			Paths[i].transform.localPosition += Vector3.up * 50;
			float dummy_distance = Vector3.Distance(origin, Ways[i].transform.position);
			Paths[i].transform.localScale = Vector3.one + (Vector3.up * Mathf.Min(50,dummy_distance * 0.8f)) + (Vector3.right * Mathf.Min(10,dummy_distance * 0.3f)); 

		}
	}
}
