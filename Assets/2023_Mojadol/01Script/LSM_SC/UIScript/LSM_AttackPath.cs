using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 공격로 화살표 아이콘
// 본래 클릭으로 미니언의 이동을 구현하였으나, 현재 슬라이더를 이용하였기에 해당 스크립트는 폐기 가능.
public class LSM_AttackPath : MonoBehaviour
{
    public GameObject thisSpawnPoint;
	public LSM_SpawnPointSc thisSpawnPointSC;
    public int number;

	private Renderer rend;

	private void Awake()
	{
		rend = this.GetComponent<Renderer>();
		rend.material.color = Color.red;
	}

	
	private void LateUpdate()
	{
		
		if (!ReferenceEquals(thisSpawnPointSC, null))
			rend.material.color = ((thisSpawnPointSC.isClicked) ? Color.blue : Color.red);
	}
	

	public void SetVariable(GameObject s, int n)
	{
		thisSpawnPoint = s; number = n;
		thisSpawnPointSC = s.GetComponent<LSM_SpawnPointSc>();
	}
}
