using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class MoonHeader : MonoBehaviour
{
	public enum GameState { ChangeRound, SettingAttackPath, StartGame, Gaming, Ending };	// ������ ���� ���¸� ��Ÿ���� ���� ���.
																					// ChangeRound: ���� ����, SettingAttackPath: ���ݷ� �̴Ͼ� ����, StartGame: ������ �����ϱ� �� ������ ����, Gaming: �������� ��
	public enum ManagerState { Ready, Processing, End };	// ���� �Ŵ����� ���� ���¸� ��Ÿ���� ���� ���. GameState ó���� ������ ��.
															// Ready: ���� �ش� GameState�� ������ �� ����, Processing: �ش� GameState�� ���� ��, End: �ش� GameState�� ����ħ.
	public enum SpawnerState { None, Setting, Spawn };      // �������� ���� ���¸� ��Ÿ���� ���� ���.
															// None: �ƹ��͵� ����, Setting: SettingAttackPath �����϶� �����ʸ� �����ϴ� �ܰ�, Spawn: �����ʰ� �����ϴ� ��.
	public enum CreepStat { Idle = 0, Idle_Combat = 1, Attack = 2, Death = 3, Groggy = 4 }
	public enum State_P { None, Selected , Possession};     // �÷��̾��� ���� ���¸� ��Ÿ���� ���� ���.
															// None: �ƹ��͵� ���ϴ� ��. �ַ� TopView ���������� ����, Seleted: �̴Ͼ��� Ŭ���� ����, Possession: ���� ��.
	public enum State_P_Minion { Normal=0, Dead=1 };			// �÷��̾�̴Ͼ��� ���� ���¸� ��Ÿ���� ���� ���.
	public enum Team { Red = 0, Blue = 1, Yellow = 2 };		// ���� ������ ���/
	
	public enum State { Normal = 0, Dead = 1, Attack = 2, Invincibility = 3 , Thinking = 4};	// �̴Ͼ��� ���� ���¸� ��Ÿ���� ���� ���.
																// Normal: ���� ����, Dead: ����, Attack: �����ϴ� ��, Invincibility: ���� ����.
	public enum MonType { Melee = 0, Range = 1};   // ���� Ÿ��
											// Melee: ����, Range: ���Ÿ�
	public enum ActorType
	{ Knight, Magicion, Shaman,  Minion_Melee, Minion_Range, Turret, Turret_Base, Creep_Golem, Creep_Magition, Turret_Nexus};
	public enum AttackType { None = 0, Melee = 1, Range = 2, Turret = 3 };

	//���õ� ��� ����ϴ� ������ ����. ���Ŀ� ���� ����.
	public static Color32[] SelectedColors = {new Color32(120,0,0,255), new Color32(0,0,120,255), new Color32(120,120,0,255) };

	[Serializable]
	public struct S_ActorState		// ��� ���͵��� ���� ����ü. ü��, ���ݷ�, �� ���� �����ְ� ����.
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
	public struct S_PlayerState	// �÷��̾� ���� ����ü. TopView������ �÷��̾� ���� ����ü
	{
		public State_P statep;	// ���� �÷��̾��� ���¿� ���� ����
		public Team team;		// ���� �÷��̾��� ��
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
	public struct S_MinionStats		// �̴Ͼ��� ���¿� ���õ� ����ü.
	{
		public State state;			// �̴Ͼ��� ���� ���¸� ��Ÿ���� ����
		//public Team team;			// �̴Ͼ��� ��
		//public int maxHealth;		// �̴Ͼ��� �ִ� ü��.
		//public int health;			// �̴Ͼ��� ü��.
		public float speed;			// �̵� �ӵ�
		//public int Atk;				// ���ݷ�
		//public MonType type;        // Ÿ�� -> ����, ���Ÿ� ����
		public S_ActorState actorHealth;
		public short exp, gold;
		public GameObject[] destination;	// �̴Ͼ��� �̵� ���. �迭�� �޾ƿ�.

		public void Setting(short mh, float sp, short atk, GameObject[] des, Team t)	// �Ʒ� �Լ��� �����ε�. MonType ���� �Ű������� ���� ����.
		{ this.Setting(mh,sp,atk,des,t,AttackType.Melee, 100, 100); }
		public void Setting(short mh, float sp, short atk, GameObject[] des, Team t, AttackType type_d)
		{ this.Setting(mh, sp, atk, des, t, type_d, 100, 100); }

		// �̴Ͼ��� ��ȯ�� ���� ���� ������ ���� �Լ�. 
		// mh: �ִ� ü��, sp: ���ǵ�, atk: ���ݷ�, des: �����ʷκ��� �޾ƿ� �̵����, t: �̴Ͼ��� ��, type_d: �̴Ͼ��� Ÿ��,
		// e: �׾�����, Ȥ�� ������ ����Ǿ��� �� ��� ����ġ
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
	public struct S_SpawnerPaths    // �ش� �����ʰ� ���� �ִ� ��������Ʈ�� ���� ����ü. �迭�� �ش� ����ü�� ���.
	{
		public GameObject path;	// ��������Ʈ. �ش� �����ʰ� �����մ� ��������Ʈ �� �ϳ�.
		public int num;			// �� ���̺� �� �ش� ��������Ʈ�� ��ȯ�ϴ� �ִ� ������.
		public int summon_;		// �ش� ��������Ʈ�� �� ���̺꿡 ��ȯ�� ������. ���̺갡 ������ 0���� �ʱ�ȭ.

		public S_SpawnerPaths(GameObject p ) { path = p; num = 0; summon_ = 0; }		// ������. p: ��������Ʈ.
		
	}

	[Serializable]
	public struct S_TurretStats	// �ͷ�(��ž)�� ���¿� ���� ����ü.
	{
		//public Team team;		// �ͷ��� ��.
		//public int Health;		// �ͷ��� �ִ� ü��.
		//public int Atk;         // �ͷ��� ���ݷ�
		public S_ActorState actorHealth;

		public S_TurretStats(short h, short a) { actorHealth = new S_ActorState(h, a, Team.Yellow); }       // ������. h: �ִ�ü��, a: ���ݷ�,  ���� ó�� ���� �� �߸�.
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
		public S_Status GetPlusStatus() // Q��, E��, ���ǵ�, HP, Atk
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


// �������̽�.
public interface I_Actor		// ��� �����̴� ��ü���� ���� �� �������̽�.
{
	public void Damaged(short dam, Vector3 origin, MoonHeader.Team t, GameObject other);    // ��� ĳ���ʹ� �������� �ޱ⿡ �߻��Լ��� ����.

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
    public void Damaged(short dam, Vector3 origin, MoonHeader.Team t, GameObject other, float power);    // ��� ĳ���ʹ� �������� �ޱ⿡ �߻��Լ��� ����. �÷��̾���� �˹��� ����.
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
