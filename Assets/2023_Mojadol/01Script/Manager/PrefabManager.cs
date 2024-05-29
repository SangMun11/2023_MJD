using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabManager : MonoBehaviour
{
	// �̱���///
	private static PrefabManager instance;
	private void Awake()
	{
		if (instance == null)
			instance = this;
        //outline = new Material(Shader.Find("Draw/OutlineShader"));
    }
	public static PrefabManager Instance
	{
		get { return instance; }
	}
	// ///

	// 0: �̴Ͼ�(Melee) 1: �̴Ͼ�(Range)
	// # 0: LSM ���� ���� Minion1�� ����. 1: LSM ���� ���� Minion2�� ����
	public GameObject[] minions;
	// 0: �̴Ͼ�ũ 1: ���ݷθ�ũ 2: ���ݷ�UI 3: ��ž��ũ 4: �÷��̾ũ
	// # 0: Icon ���� ���� MinionMark, 1: Icon ���� ���� Direction, 2: Icon ���� ���� AttackPathUI
	// # 3: Icon ���� ���� TurretMark, 4: Icon ���� ���� PlayerMark
	public GameObject[] icons;
	// 0: �÷��̾�ĳ����(Melee)
	// # 0: PSH ���� ���� MeleeCharacter
	public GameObject[] players;
	// OutLine ���׸���
	//public Material outline;
}
