using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ���ݷ� ȭ��ǥ ������
// ���� Ŭ������ �̴Ͼ��� �̵��� �����Ͽ�����, ���� �����̴��� �̿��Ͽ��⿡ �ش� ��ũ��Ʈ�� ��� ����.
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
