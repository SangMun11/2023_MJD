using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabManager : MonoBehaviour
{
	// 싱글톤///
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

	// 0: 미니언(Melee) 1: 미니언(Range)
	// # 0: LSM 폴더 내의 Minion1을 연결. 1: LSM 폴더 내의 Minion2를 연결
	public GameObject[] minions;
	// 0: 미니언마크 1: 공격로마크 2: 공격로UI 3: 포탑마크 4: 플레이어마크
	// # 0: Icon 폴더 내의 MinionMark, 1: Icon 폴더 내의 Direction, 2: Icon 폴더 내의 AttackPathUI
	// # 3: Icon 폴더 내의 TurretMark, 4: Icon 폴더 내의 PlayerMark
	public GameObject[] icons;
	// 0: 플레이어캐릭터(Melee)
	// # 0: PSH 폴더 내의 MeleeCharacter
	public GameObject[] players;
	// OutLine 마테리얼
	//public Material outline;
}
