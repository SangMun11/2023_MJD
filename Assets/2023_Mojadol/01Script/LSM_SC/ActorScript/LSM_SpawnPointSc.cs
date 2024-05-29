using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// �� ������ ������ �Ʒ��� �̴Ͼ��� �����Ǵ� ������ ���� ����Ʈ
public class LSM_SpawnPointSc : MonoBehaviour
{
    public GameObject[] Ways;				// �̴Ͼ��� ���� ���ݷ� ��������Ʈ ����.
											// # GameManager�� �ڽĿ�����Ʈ�� �ִ� WayPoint�� �ϳ��� ����. ���� �� ����� �̿��� �Ǽ��� ����. �������� ����� ��������Ʈ.
	public GameObject[] Paths;				// ���ݷθ� �÷��̾�� ȭ��ǥ�� �����ִ� UI��
	public int number;						// ��������Ʈ�� ��ȣ�� �˷���. 
											// # ��������Ʈ���� ��ȣ�� �ٸ��� �����ؾ���. 0 1 2 �� �������ؾ� ���� ����� ��ž�� ����. ������ ��Ī���� ����.
	public bool isClicked;					// ���� ������ ���ϴ� ���� ����. ������ �� ���� ����.
	public GameObject parentSpawner;		// �� ��������Ʈ�� ������ ������
	private LSM_Spawner parentSpawnerSC;	// ������ �������� ��ũ��Ʈ

	public GameObject pathUI;               // �÷��̾ ������ ���ݷ� ���� ui
	public LSM_AttackPathUI pathUI_SC;
	private bool once;

	// �����
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
		// ���� �ʱ�ȭ
		isClicked = false;
		parentSpawner = transform.parent.gameObject;
		parentSpawnerSC = parentSpawner.GetComponent<LSM_Spawner>();
		once = false;

		// �÷��̾ ������ UI
		pathUI = GameObject.Instantiate(PrefabManager.Instance.icons[2], GameManager.Instance.mapUI.transform);
		pathUI_SC = pathUI.GetComponent<LSM_AttackPathUI>();
		pathUI_SC.SetParent(this);
	}

	private void Start_function()
	{
		//if (GameManager.Instance.mainPlayer.player.team == parentSpawnerSC.team)
		// ȭ��ǥ ������
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

		// ���ݷ� �������� ��ġ ����.
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
