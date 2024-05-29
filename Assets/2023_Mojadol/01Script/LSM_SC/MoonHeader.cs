using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class MoonHeader : MonoBehaviour
{
	public enum GameState { ChangeRound, SettingAttackPath, StartGame, Gaming, Ending };	// 게임의 현재 상태를 나타내기 위해 사용.
																					// ChangeRound: 라운드 변경, SettingAttackPath: 공격로 미니언 지정, StartGame: 게임이 시작하기 전 마무리 세팅, Gaming: 게임진행 중
	public enum ManagerState { Ready, Processing, End };	// 게임 매니저의 현재 상태를 나타내기 위해 사용. GameState 처리에 도움을 줌.
															// Ready: 현재 해당 GameState를 진행할 수 있음, Processing: 해당 GameState를 진행 중, End: 해당 GameState를 끝마침.
	public enum SpawnerState { None, Setting, Spawn };      // 스포너의 현재 상태를 나타내기 위해 사용.
															// None: 아무것도 안함, Setting: SettingAttackPath 상태일때 스포너를 조정하는 단계, Spawn: 스포너가 동작하는 중.
	public enum CreepStat { Idle = 0, Idle_Combat = 1, Attack = 2, Death = 3, Groggy = 4 }
	public enum State_P { None, Selected , Possession};     // 플레이어의 현재 상태를 나타내기 위해 사용.
															// None: 아무것도 안하는 중. 주로 TopView 시점에서의 상태, Seleted: 미니언을 클릭한 시점, Possession: 빙의 중.
	public enum State_P_Minion { Normal=0, Dead=1 };			// 플레이어미니언의 현재 상태를 나타내기 위해 사용.
	public enum Team { Red = 0, Blue = 1, Yellow = 2 };		// 팀을 나눌때 사용/
	
	public enum State { Normal = 0, Dead = 1, Attack = 2, Invincibility = 3 , Thinking = 4};	// 미니언의 현재 상태를 나타내기 위해 사용.
																// Normal: 현재 생존, Dead: 죽음, Attack: 공격하는 중, Invincibility: 무적 상태.
	public enum MonType { Melee = 0, Range = 1};   // 몬스터 타입
											// Melee: 근접, Range: 원거리
	public enum ActorType
	{ Knight, Magicion, Shaman,  Minion_Melee, Minion_Range, Turret, Turret_Base, Creep_Golem, Creep_Magition, Turret_Nexus};
	public enum AttackType { None = 0, Melee = 1, Range = 2, Turret = 3 };

	//선택될 경우 사용하는 아이콘 색상. 이후에 변경 가능.
	public static Color32[] SelectedColors = {new Color32(120,0,0,255), new Color32(0,0,120,255), new Color32(120,120,0,255) };

	[Serializable]
	public struct S_ActorState		// 모든 액터들이 갖는 구조체. 체력, 공격력, 팀 등을 갖고있게 설정.
	{
		public Team team;
		public short maxHealth;
		public short health;
		public short Atk;
		public AttackType type;

		public S_ActorState(short hp, short at, Team t) { team = t; maxHealth = hp; health = maxHealth; Atk = at; type = AttackType.None; }
		public S_ActorState(short hp, short at, Team t, AttackType ty) { team = t; maxHealth = hp; health = maxHealth; Atk = at; type = ty; }

	}

	[Serializable]
	public struct S_PlayerState	// 플레이어 관련 구조체. TopView상태의 플레이어 관련 구조체
	{
		public State_P statep;	// 현재 플레이어의 상태에 대한 변수
		public Team team;		// 현재 플레이어의 팀
	}

	[Serializable]
	public struct S_CreepStats
	{
		public CreepStat state;
		public S_ActorState actorHealth;
		public int exp, gold;

		public void Setting(short mh, short atk, int e, int g) 
		{ actorHealth = new S_ActorState(mh, atk, Team.Yellow, AttackType.None);
			exp = e; gold = g;
		}

		public ulong SendDummyMaker()
		{
            ulong send_dummy = 0;
            send_dummy += ((ulong)actorHealth.maxHealth & (ulong)ushort.MaxValue);
            send_dummy += ((ulong)(actorHealth.health) & (ulong)ushort.MaxValue) << 16;
            send_dummy += ((ulong)(actorHealth.Atk) & (ulong)ushort.MaxValue) << 32;
            send_dummy += ((ulong)(state) & (ulong)byte.MaxValue) << 48;
            return send_dummy;
        }

        public void ReceiveDummy(ulong receive_dummy)
        {
            actorHealth.maxHealth = (short)(receive_dummy & (ulong)ushort.MaxValue);
            actorHealth.health = (short)((receive_dummy >> 16) & (ulong)ushort.MaxValue);
            actorHealth.Atk = (short)((receive_dummy >> 32) & (ulong)ushort.MaxValue);
            state = (MoonHeader.CreepStat)((receive_dummy >> 48) & (ulong)byte.MaxValue);

        }
    }

	[Serializable]
	public struct S_MinionStats		// 미니언의 상태에 관련된 구조체.
	{
		public State state;			// 미니언의 현재 상태를 나타내는 변수
		//public Team team;			// 미니언의 팀
		//public int maxHealth;		// 미니언의 최대 체력.
		//public int health;			// 미니언의 체력.
		public float speed;			// 이동 속도
		//public int Atk;				// 공격력
		//public MonType type;        // 타입 -> 근접, 원거리 구현
		public S_ActorState actorHealth;
		public short exp, gold;
		public GameObject[] destination;	// 미니언의 이동 경로. 배열로 받아옴.

		public void Setting(short mh, float sp, short atk, GameObject[] des, Team t)	// 아래 함수의 오버로드. MonType 관련 매개변수를 받지 않음.
		{ this.Setting(mh,sp,atk,des,t,AttackType.Melee, 100, 100); }
		public void Setting(short mh, float sp, short atk, GameObject[] des, Team t, AttackType type_d)
		{ this.Setting(mh, sp, atk, des, t, type_d, 100, 100); }

		// 미니언이 소환될 때의 기초 설정을 위한 함수. 
		// mh: 최대 체력, sp: 스피드, atk: 공격력, des: 스포너로부터 받아올 이동경로, t: 미니언의 팀, type_d: 미니언의 타입,
		// e: 죽었을때, 혹은 게임이 종료되었을 때 얻는 경험치
		public void Setting(short mh, float sp, short atk, GameObject[] des, Team t, AttackType type_d, short e, short g)
		{ speed = sp; destination = des; state = State.Normal; actorHealth = new S_ActorState(mh, atk, t,type_d); exp = e; gold = g; }

		public ulong SendDummyMaker()
		{
            // maxHealth 2byte, health 2byte, team 8bit, atk 8bit, state 8bit
            ulong send_dummy = 0;
            send_dummy += ((ulong)actorHealth.maxHealth & (ulong)ushort.MaxValue);
            send_dummy += ((ulong)(actorHealth.health) & (ulong)ushort.MaxValue) << 16;
            send_dummy += ((ulong)(actorHealth.team) & (ulong)byte.MaxValue) << 32;
            send_dummy += ((ulong)(actorHealth.Atk) & (ulong)ushort.MaxValue) << 40;
            send_dummy += ((ulong)(state) & (ulong)byte.MaxValue) << 56;
			return send_dummy;
		}

		public void ReceiveDummy(ulong receive_dummy)
		{
            actorHealth.maxHealth = (short)(receive_dummy & (ulong)ushort.MaxValue);
            actorHealth.health = (short)((receive_dummy >> 16) & (ulong)ushort.MaxValue);
            actorHealth.team = (MoonHeader.Team)((receive_dummy >> 32) & (ulong)byte.MaxValue);
            actorHealth.Atk = (short)((receive_dummy >> 40) & (ulong)ushort.MaxValue);
            state = (MoonHeader.State)((receive_dummy >> 56) & (ulong)byte.MaxValue);
			
        }
    }

	[Serializable]
	public struct S_SpawnerPaths    // 해당 스포너가 갖고 있는 스폰포인트에 대한 구조체. 배열로 해당 구조체를 사용.
	{
		public GameObject path;	// 스폰포인트. 해당 스포너가 갖고잇는 스폰포인트 중 하나.
		public int num;			// 한 웨이브 당 해당 스폰포인트가 소환하는 최대 마릿수.
		public int summon_;		// 해당 스폰포인트가 현 웨이브에 소환한 마릿수. 웨이브가 끝나면 0으로 초기화.

		public S_SpawnerPaths(GameObject p ) { path = p; num = 0; summon_ = 0; }		// 생성자. p: 스폰포인트.
		
	}

	[Serializable]
	public struct S_TurretStats	// 터렛(포탑)의 상태에 대한 구조체.
	{
		//public Team team;		// 터렛의 팀.
		//public int Health;		// 터렛의 최대 체력.
		//public int Atk;         // 터렛의 공격력
		public S_ActorState actorHealth;

		public S_TurretStats(short h, short a) { actorHealth = new S_ActorState(h, a, Team.Yellow); }       // 생성자. h: 최대체력, a: 공격력,  팀은 처음 시작 시 중립.
		public S_TurretStats(short h, short a, Team t) { actorHealth = new S_ActorState(h, a, t); }

	}

	[Serializable]
	public struct S_ActorStatus_LV
	{
		public ActorType pT;
		public short[] needExp;
		public short[] hp;
		public short[] atk;

		public S_ActorStatus_LV(ActorType t, int maxLv) { pT = t; hp = new short[maxLv]; atk = new short[maxLv]; needExp = new short[maxLv]; }

		public void setStatus_LV(int lv,short exp, short health, short attack) {
			if (lv >= hp.Length || lv < 0) return;
			needExp[lv] = exp;
			hp[lv] = health; atk[lv] = attack; 
		}
		public object[] getStatus_LV(int lv)
		{
			if (hp.Length < lv)
				return new object[] { hp[hp.Length-1], atk[atk.Length-1] };
			return new object[] { hp[lv], atk[lv] };
		}
		public bool canLevelUp(byte lv,short exp) {
			if (lv >= needExp.Length)
				return false;
			return needExp[lv] <= exp; 
		}
	}

	[Serializable]
	public struct S_Status
	{
        public float plusQCool;
        public float plusECool;
        public float plusSpeed;
        public short plusHP;
        public short plusATk;
    }

	[Serializable]
	public struct S_ShopItems
	{
        private LSM_ItemData[] item_num;
        private byte[] num_has;

		private S_Status alphaStat;

		public S_ShopItems(LSM_ItemData[] i) { item_num = i; num_has = new byte[item_num.Length];
            alphaStat = new S_Status();
        }
		public S_Status GetPlusStatus() // Q쿨, E쿨, 스피드, HP, Atk
		{
			alphaStat = new S_Status();

			for (int i = 0; i < item_num.Length; i++) 
			{
				S_Status ob = item_num[i].GetEffect();
				alphaStat.plusQCool += ob.plusQCool * num_has[i];
                alphaStat.plusECool += ob.plusECool * num_has[i];
                alphaStat.plusSpeed += ob.plusSpeed * num_has[i];
                alphaStat.plusHP += (short)(ob.plusHP * num_has[i]);
                alphaStat.plusATk += (short)(ob.plusATk * num_has[i]);
            }
			return alphaStat;
		}

		public byte NumOfItem(int code) { return num_has[code]; }
		public byte[] GetHasItems() { return num_has; }
		public void AddItem(int code) { num_has[code]++; }
		public void SetItem(int code, byte num) { num_has[code] = num; }
    }
}


// 인터페이스.
public interface I_Actor		// 모든 움직이는 객체들이 갖게 한 인터페이스.
{
	public void Damaged(short dam, Vector3 origin, MoonHeader.Team t, GameObject other);    // 모든 캐릭터는 데미지를 받기에 추상함수로 설정.

	public short GetHealth();
	public short GetMaxHealth();
	public MoonHeader.S_ActorState GetActor();
	public MoonHeader.Team GetTeam();
	public void ChangeTeamColor();
	public GameObject GetCameraPos();
	public void Selected();
	public void Unselected();
}
public interface I_Characters
{
	public void AddEXP(short exp);
	public int GetState();
}

public interface I_Playable 
{
    public void Damaged(short dam, Vector3 origin, MoonHeader.Team t, GameObject other, float power);    // 모든 캐릭터는 데미지를 받기에 추상함수로 설정. 플레이어블은 넉백이 가능.
    public float IsCanUseE();
	public float IsCanUseQ();
	public bool IsCanHit();
	public GameObject CameraSetting(GameObject cam);
	public int GetExp();
	public int GetGold();
	public byte GetLV();
	public void SpawnSetting(MoonHeader.Team t, short monHealth, string pname, LSM_PlayerCtrl pctrl);
	public void MinionDisable();
	public void MinionEnable();
	public void ParentSetting_Pool(int index);
	public void CollectingArea();
	public void AddCollector(int s);
	public float GetF();
	public void AddKill();
	public void AddDeath();
	public void AddCS();
	public void AddTD();
}

public interface I_Creep
{
	public void RegenProcessing();
	public void AttackEffectEnable(bool b);
	public void StatSetting(int i);
	public void DeadProcessing();
	public void Setting();
}
