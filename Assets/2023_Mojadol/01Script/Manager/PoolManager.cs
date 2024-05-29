using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.VisualScripting;


public class PoolManager : MonoBehaviour
{
	// 싱글톤 //
	private static PoolManager instance;
	public static PoolManager Instance { get { return instance; } }
	private void Awake()
	{
		if (instance == null) instance = this;
		Awake_Function();
	}
	// ///

	// 프리펩 저장 변수
	public GameObject[] minions;		// # 0: LSM 폴더 내의 Minion1, 1: LSM 폴더 내의 Minion2
	public GameObject[] playerMinions;	// # 0: PSH 폴더 내의 MeleeCharacter
	public GameObject[] UIs;            // # 0: Icon 폴더 내의 display
	public GameObject[] particles;      // # 0: Explosion
	public GameObject[] Items;          // # 0: Coin
	public GameObject[] Local_Items;	// # 0: CollectObj

	private GameObject alwaysEnableUI;

	// 풀 저장 변수
	public List<GameObject>[] poolList_Minion;
	public List<GameObject>[] poolList_PlayerMinions;
	public List<GameObject>[] poolList_UIs;
	public List<GameObject>[] poolList_Particles;
	public List<GameObject>[] poolList_Items;
	public List<GameObject>[] poolList_Local_Items;

	private bool once;

	public int ReadyToStart_SpawnNum, ReadyToStart_SpawnNum_Particles, ReadyToStart_SpawnNum_Item;

	private void Start()
	{
		alwaysEnableUI = GameObject.Find("AlwaysEnableUI");
	}

    // 싱글톤으로 인해 Awake를 위로 배치하였기에 미관상 아래의 함수를 사용.
    private void Awake_Function()
	{
		// 미니언 종류의 개수만큼 배열의 크기를 지정.
		poolList_Minion = new List<GameObject>[minions.Length];
		for (int i = 0; i < poolList_Minion.Length; i++)
			poolList_Minion[i] = new List<GameObject>();
		poolList_PlayerMinions = new List<GameObject>[playerMinions.Length];
		for (int i = 0; i < poolList_PlayerMinions.Length; i++)
			poolList_PlayerMinions[i] = new List<GameObject>();
		poolList_UIs = new List<GameObject>[UIs.Length];
		for (int i = 0; i < poolList_UIs.Length;i++)
			poolList_UIs[i] = new List<GameObject>();
        poolList_Particles = new List<GameObject>[particles.Length];
        for (int i = 0; i < poolList_Particles.Length; i++)
            poolList_Particles[i] = new List<GameObject>();
		poolList_Items = new List<GameObject>[Items.Length];
		for (int i = 0; i < poolList_Items.Length; i++)
			poolList_Items[i] = new List<GameObject>();
		poolList_Local_Items = new List<GameObject>[Local_Items.Length];
		for (int i = 0; i < poolList_Local_Items.Length; i++)
			poolList_Local_Items[i] = new List<GameObject>();


		ReadyToStart_SpawnNum = 50;
		ReadyToStart_SpawnNum_Particles = 3;
		ReadyToStart_SpawnNum_Item = 50;
    }

	public IEnumerator ReadyToStart_Spawn()
	{
		if (PhotonNetwork.IsMasterClient)
		{
			// 미니언 미리 소환.
			GameManager.Instance.LoadingTxtUpdate("좀비 생성 중..");
			for (int i = 0; i < minions.Length; i++)
			{
				for (int j = 0; j < ReadyToStart_SpawnNum; j++)
				{
					yield return new WaitForSeconds(Time.deltaTime*2);
					Get_Minion(i);
					GameManager.Instance.LoadingUpdate();
				}
				foreach (GameObject item in poolList_Minion[i])
				{
					yield return new WaitForSeconds(Time.deltaTime);
					item.GetComponent<LSM_MinionCtrl>().MinionDisable();
				}
				GameManager.Instance.LoadingUpdate();
			}
            // 파티클 미리 소환.
            GameManager.Instance.LoadingTxtUpdate("마법 구현 중..");
            for (int i = 0; i < particles.Length; i++)
			{
				for (int j = 0; j < ReadyToStart_SpawnNum_Particles; j++)
				{
					yield return new WaitForSeconds(Time.deltaTime*2);
					Get_Particles(i, Vector3.zero);
					GameManager.Instance.LoadingUpdate();
				}
				foreach (GameObject item in poolList_Particles[i])
				{
					yield return new WaitForSeconds(Time.deltaTime);
					item.GetComponent<ParticleAutoDisable>().ParticleDisable();
				}
				GameManager.Instance.LoadingUpdate();
			}
            // 아이템 미리 소환.
            GameManager.Instance.LoadingTxtUpdate("아이템 생성 중..");
			for (int i = 0; i < 10; i++)
			{ yield return null; Get_Item(0); }
			foreach (GameObject item in poolList_Items[0])
			{ yield return null; item.GetComponent<LSM_ItemSC>().ItemDisable(); }
            for (int i = 0; i < Items.Length; i++)
			{
				for (int j = 0; j < ReadyToStart_SpawnNum_Item; j++)
				{
					yield return new WaitForSeconds(Time.deltaTime*2);
					Get_Item(i);
					GameManager.Instance.LoadingUpdate();
				}
				foreach(GameObject item in poolList_Items[i])
				{
					yield return new WaitForSeconds(Time.deltaTime);
					item.GetComponent<LSM_ItemSC>().ItemDisable();
				}
				GameManager.Instance.LoadingUpdate();
			}

            GameManager.Instance.PlayerReady();
        }
		else
		{
			while (true)
			{
				yield return new WaitForSeconds(Time.deltaTime);
				if (GameManager.Instance.LoadingGauge >= GameManager.Instance.ReadyToStart_LoadingGauge)
				{ break; }
			}
			yield return new WaitForSeconds(3f);				// 디버깅용. 삭제해도 무방.
            GameManager.Instance.PlayerReady();
        }
	}

    // 미니언의 종류에 맞는 미니언을 반환.
	public GameObject Get_Minion(int index)
	{
		if (minions.Length <= index || index < 0)
			return null;
		GameObject result = null;

		// 현재 비활성화 되어있는 미니언이 존재하는지 확인
		foreach (GameObject item in poolList_Minion[index])
		{
			if (!item.activeSelf)
			{
				result = item;
				item.SetActive(true);
				break;
			}
		}

		// 비활성화된 미니언이 존재하지 않는다면 새로 생성.
		if (ReferenceEquals(result, null))
		{
			//result = GameObject.Instantiate(minions[index], this.transform);
			result = PhotonNetwork.Instantiate(minions[index].name, Vector3.zero, Quaternion.identity);
			//result.transform.parent = this.transform;
			result.GetComponent<LSM_MinionCtrl>().ParentSetting_Pool(index);
			//poolList_Minion[index].Add(result);
			
		}

		return result;
	}

	// 플레이어 미니언 반환
	public GameObject Get_PlayerMinion(int index)
	{
		if (index >= playerMinions.Length || index < 0)
			return null;
		GameObject result = null;

		foreach (GameObject item in poolList_PlayerMinions[index])
		{
			if (!item.activeSelf)
			{
				result = item;
				item.SetActive(true);
				item.GetComponent<I_Playable>().MinionEnable();
				break;
			}
		}

		if (ReferenceEquals(result, null))
		{
			//result = GameObject.Instantiate(playerMinions[index], this.transform);
			result = PhotonNetwork.Instantiate(playerMinions[index].name,Vector3.zero, Quaternion.identity);
			result.GetComponent<I_Playable>().ParentSetting_Pool(index);
			//result.transform.parent = this.transform;
			//poolList_PlayerMinions[index].Add(result);
		}
		return result;
	}

	// UI 반환
	public GameObject Get_UI(int index)
	{
		if (index >= UIs.Length || index < 0)
			return null;
		GameObject result = null;

		foreach (GameObject item in poolList_UIs[index])
		{
			if (!item.activeSelf)
			{
				result = item;
				item.SetActive(true);
				break;
			}
		}
		if (ReferenceEquals(result, null))
		{
			result = GameObject.Instantiate(UIs[index], alwaysEnableUI.transform);
			poolList_UIs[index].Add(result);
		}
		return result;
	}

	// 파티클 반환
	public GameObject Get_Particles(int index, Vector3 position_)
	{ return Get_Particles(index, position_, Vector3.one); }
    public GameObject Get_Particles(int index, Vector3 position_, Vector3 rot)
    {
        if (index >= particles.Length || index < 0)
            return null;
        GameObject result = null;

        foreach (GameObject item in poolList_Particles[index])
        {
            if (!item.activeSelf)
            {
                result = item;
                item.SetActive(true);
				item.GetComponent<ParticleAutoDisable>().ParticleEnable(position_, rot);
                break;
            }
        }
        if (ReferenceEquals(result, null))
        {
            //result = GameObject.Instantiate(particles[index], this.transform);
            //poolList_Particles[index].Add(result);
			result = PhotonNetwork.Instantiate(particles[index].name, Vector3.one * 100, Quaternion.identity);
			result.GetComponent<ParticleAutoDisable>().ParentSetting_Pool(index);
            result.GetComponent<ParticleAutoDisable>().ParticleEnable(position_, rot);
        }
        return result;
    }

	// 아이템 반환
	public GameObject Get_Item(int index)
	{
		if (index >= Items.Length || index < 0)
			return null;
		GameObject result = null;

		foreach(GameObject item in poolList_Items[index])
		{
			if (!item.activeSelf)
			{
				result = item;
				item.SetActive(true);
				item.GetComponent<LSM_ItemSC>().ItemEnable();
				break;
			}
		}
		if (ReferenceEquals(result, null))
		{
			result = PhotonNetwork.Instantiate(Items[index].name, Vector3.one * 100, Quaternion.identity);
			result.GetComponent<LSM_ItemSC>().ParentSetting_Pool(index);
		}
		return result;
	}
	// 로컬에서 쓰이는 아이템들 소환.
	public GameObject Get_Local_Item(int index)
	{
		if (index >= Local_Items.Length || index < 0)
			return null;
		GameObject result = null;

		foreach (GameObject item in poolList_Local_Items[index])
		{
			if (!item.activeSelf)
			{
				result = item;
				item.SetActive(true);
				break;
			}
		}
		if (ReferenceEquals(result, null))
		{
			result = GameObject.Instantiate(Local_Items[index], Vector3.one * 100, Quaternion.identity, this.transform);
			poolList_Local_Items[index].Add(result);
		}
		return result;
	}
}
