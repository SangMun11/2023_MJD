using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Pun;

/* 2023_03_20_HSH_수정사항 : 터렛이 팀에 종속 시 Scene에서도 Color가 변경되도록 함.
 * ㄴ 터렛 피격 시 분홍색으로 하이라이트(DamagedEffect())
 */

// 포탑에 대하여 간단하게 구현
public class LSM_TurretSc : MonoBehaviourPunCallbacks, I_Actor, IPunObservable
{
	// 포탑의 탐색, 공격에 대한 딜레이 상수화 혹시 모를 변경에 대비하여 const는 생략
	protected float ATTACKDELAY = 3f, SEARCHINGDELAY = 0.5f;
	const float LAST_ATTACK_DELAY = 10f;

    public MoonHeader.S_TurretStats stats;			// 터렛의 상태에 대한 구조체
	public GameObject mark, mark_obj;						// TopView에서 플레이어에게 보여질 아이콘
	protected float timer, timer_attack;				// 탐색, 공격에 사용될 타이머
	protected float searchRadius;                       // 탐색 범위
	protected float maxAttackRadius;

	[SerializeField]protected GameObject target;        // 공격 타겟
	protected I_Actor target_Actor;

	protected Renderer[] bodies;  // 색상을 변경할 렌더러.

	public GameObject waypoint;
	//public int TurretBelong;                        // 터렛의 위치
													// # 터렛의 경로에 따라서 번호를 다르게 설정. 해당 경로와 동일하게 숫자가 같도록 설정.
													//protected PhotonView photonView;
	public GameObject CameraPos;

    MeshRenderer icon_ren;
    List<Material> icon_materialL;
	bool selected_e;

	protected byte level;
	protected MoonHeader.ActorType ac_type;

    protected GameObject last_Attack_Player;
    protected float timer_lastAttack;

	public GameObject[] Sounds; // 0: 피격, 1: 죽음

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
		if(stream.IsWriting)
        {
            ulong send_dummy = SendDummyMaker();
            //stream.SendNext(send_dummy);

            int dummy_int1 = (int)(send_dummy & (ulong)uint.MaxValue);
            int dummy_int2 = (int)((send_dummy >> 32) & (ulong)uint.MaxValue);
            stream.SendNext(dummy_int1);
            stream.SendNext(dummy_int2);

        }
		else
        {
            ulong receive_dummy = ((ulong)(int)stream.ReceiveNext() & (ulong)uint.MaxValue);
            receive_dummy += (((ulong)(int)stream.ReceiveNext() & (ulong)uint.MaxValue) << 32);

            this.ReceiveDummy(receive_dummy);
        }
    }

    public ulong SendDummyMaker()
    {
        ulong send_dummy = 0;
        send_dummy += ((ulong)stats.actorHealth.maxHealth & (ulong)ushort.MaxValue);
        send_dummy += ((ulong)(stats.actorHealth.health) & (ulong)ushort.MaxValue) << 16;
        send_dummy += ((ulong)(stats.actorHealth.Atk) & (ulong)ushort.MaxValue) << 32;
        send_dummy += ((ulong)(stats.actorHealth.team) & (ulong)byte.MaxValue) << 48;
        return send_dummy;
    }

    public void ReceiveDummy(ulong receive_dummy)
    {
        stats.actorHealth.maxHealth = (short)(receive_dummy & (ulong)ushort.MaxValue);
        stats.actorHealth.health = (short)((receive_dummy >> 16) & (ulong)ushort.MaxValue);
        stats.actorHealth.Atk = (short)((receive_dummy >> 32) & (ulong)ushort.MaxValue);
        stats.actorHealth.team = (MoonHeader.Team)((receive_dummy >> 48) & (ulong)byte.MaxValue);

    }


    protected virtual void Start()
	{
		// 초기화
		bodies = this.transform.GetComponentsInChildren<Renderer>();
		mark = GameObject.Instantiate(PrefabManager.Instance.icons[3], GameManager.Instance.mapUI.transform);
		mark_obj = GameManager.Instantiate(PrefabManager.Instance.icons[6], transform);
		mark.GetComponent<LSM_TurretIconUI>().Setting(this.gameObject);
		//mark.transform.parent = GameManager.Instance.teamManagers[(int)stats.actorHealth.team].transform;
		mark_obj.transform.localPosition = new Vector3(0,30,0);
		mark_obj.transform.rotation= Quaternion.Euler(90,0,0);

		waypoint = this.transform.parent.gameObject;

		// health, atk

		// 디버그용으로 미리 설정.
		ResetTurret();
		
		searchRadius = 14f;
		maxAttackRadius = 16f;
		

		// 선택시 하이라이트 주기 위한 변수였음. 하지만 실패 후 더미로 남김.
        icon_materialL = new List<Material>();
		selected_e = false;
		ac_type = MoonHeader.ActorType.Turret;
    }

	// 포탑 초기화
	public void ResetTurret() 
	{
		object[] lv_d = LSM_SettingStatus.Instance.lvStatus[(int)MoonHeader.ActorType.Turret].getStatus_LV(level);
		stats = new MoonHeader.S_TurretStats((short)lv_d[0], (short)lv_d[1]);
		stats.actorHealth.type = MoonHeader.AttackType.Turret;
		
		timer = 0;
		target = null;
		if (GameManager.Instance.onceStart)
			photonView.RPC("ResetRPC",RpcTarget.All, (int)level);

	}
	[PunRPC] private void ResetRPC(int l) {
        object[] lv_d = LSM_SettingStatus.Instance.lvStatus[(int)MoonHeader.ActorType.Turret].getStatus_LV(l);
        stats = new MoonHeader.S_TurretStats((short)lv_d[0], (short)lv_d[1]);
        stats.actorHealth.type = MoonHeader.AttackType.Turret;

        ChangeTeamColor();
        ChangeTeamColor(bodies[0].gameObject);
    }

	// 팀에 해당하는 색으로 변경.
	public void ChangeTeamColor() {
		Color dummy_c = ChooseColor();
		mark.GetComponent<LSM_TurretIconUI>().currentUI.color = dummy_c;
		mark.GetComponent<LSM_TurretIconUI>().teamUI.color= dummy_c;
		mark_obj.GetComponent<Renderer>().material.color = dummy_c;
	}
	public void ChangeTeamColor(GameObject obj)
	{
		Color dummy_c = ChooseColor();
		obj.GetComponent<Renderer>().material.color = dummy_c;
		//bodies[0].GetComponent<Renderer>().material.color = dummy_c;
	}
	private Color ChooseColor() 
	{
        Color dummy_c = Color.white;

        switch (stats.actorHealth.team)
        {
            case MoonHeader.Team.Red:
                dummy_c = Color.red;
                break;
            case MoonHeader.Team.Blue:
                dummy_c = Color.blue;
                break;
            default:
                dummy_c = Color.yellow;
                break;
        }
		return dummy_c;
    }

	protected virtual void Update()
	{
		if (GameManager.Instance.onceStart)
		{
			if (GameManager.Instance.mainPlayer.MapCam.activeSelf) { mark_obj.SetActive(false); }
			else { mark_obj.SetActive(true); }

			if (!PhotonNetwork.IsMasterClient)
				return;
			// 게임 중일때만 실행되도록 설정
			if (GameManager.Instance.gameState == MoonHeader.GameState.Gaming)
			{
				SearchingTarget();
				AttackTarget();
				LevelUp((int)ac_type);

				if (!ReferenceEquals(last_Attack_Player, null))
				{
					timer_lastAttack += Time.deltaTime;
					if (timer_lastAttack >= LAST_ATTACK_DELAY)
					{ timer_lastAttack = 0; last_Attack_Player = null; }
				}
			}
		}
	}

	// I_Actor 인터페이스에 포함되어잇는 함수.
	// 공격을 받을 시 데미지를 입음.
	public virtual void Damaged(short dam, Vector3 origin, MoonHeader.Team t, GameObject other)
	{
		if (!PhotonNetwork.IsMasterClient || t== this.stats.actorHealth.team)
			return;

		this.stats.actorHealth.health -= dam;
		photonView.RPC("Damaged_RPC_Turret", RpcTarget.All);

        if (other.CompareTag("PlayerMinion"))
        { last_Attack_Player = other; timer_lastAttack = 0; }
        else if (other.CompareTag("DamageArea"))
        { last_Attack_Player = other.GetComponent<LSM_BasicProjectile>().orner; timer_lastAttack = 0; }


        if (this.stats.actorHealth.health <= 0) {
			this.stats.actorHealth.team = t;
			this.stats.actorHealth.health = (short)(this.stats.actorHealth.maxHealth/2);
			DestroyProcessing(other);
			photonView.RPC("DestroyProcessing_RPC", RpcTarget.All, (int)t);
		}
		return;
		
	}

	// 체력은 동기화되게 설정하였기에, 외적인 면만 보여주면 될듯..
	[PunRPC]protected void Damaged_RPC_Turret()
    {
		PlaySFX(0);
        StartCoroutine(DamagedEffect());
	}

	[PunRPC] protected void DestroyProcessing_RPC(int t) {
		PlaySFX(1);
		this.stats.actorHealth.team = (MoonHeader.Team)t;
		ChangeTeamColor();
		ChangeTeamColor(bodies[0].gameObject);
	}


	// 데미지를 입을 경우 색상 변경.
    protected virtual IEnumerator DamagedEffect()
    {
		Color damagedColor = new Color32(255, 150, 150, 255);
		Color recovered = Color.white;

		foreach (Renderer item in bodies)
			item.material.color = damagedColor;

        yield return new WaitForSeconds(0.25f);

		foreach (Renderer item in bodies)
			item.material.color = recovered;
		ChangeTeamColor();
		ChangeTeamColor(bodies[0].gameObject);
		
	}

	protected virtual void DestroyProcessing(GameObject other)
	{
		if (!ReferenceEquals(last_Attack_Player, null))
		{
			last_Attack_Player.GetComponent<I_Playable>().AddTD();
            GameManager.Instance.DisplayAdd(string.Format("{0}가 {1}를 파괴하였습니다.", last_Attack_Player.name, this.name));
        }
		else { GameManager.Instance.DisplayAdd(string.Format("{0}가 {1}를 파괴하였습니다.", other.name, this.name)); }
	}

    // 일정 범위 내에 적이 있는지를 확인하는 코드.
    protected void SearchingTarget()
	{
		// 타겟이 존재하지 않을 때만 탐색.
		if (ReferenceEquals(target, null)){
			timer += Time.deltaTime;
			if (timer >= SEARCHINGDELAY && ReferenceEquals(target, null))
			{
				timer = 0;
				// 구형 캐스트를 사용하여 탐지.
				RaycastHit[] hits = Physics.SphereCastAll(transform.position, searchRadius, Vector3.up, 0, 1 << LayerMask.NameToLayer("Minion"));

				// 가장 가까운 미니언을 찾는 함수.
                float minDistance = float.MaxValue;
                foreach (RaycastHit hit in hits)
				{
					MoonHeader.S_ActorState dummy_actor = new MoonHeader.S_ActorState();
					bool dummy_bool = false;
					if (hit.transform.CompareTag("Minion"))
					{
						LSM_MinionCtrl dummyCtr = hit.transform.GetComponent<LSM_MinionCtrl>();
						dummy_actor = dummyCtr.stats.actorHealth;
						if (dummy_actor.health > 0)
							dummy_bool = true;
					}
					else if (hit.transform.CompareTag("PlayerMinion"))
					{
						Debug.Log("Player Find!");
						I_Actor dummyCtr = hit.transform.GetComponent<I_Actor>();
						dummy_actor = dummyCtr.GetActor();
						if (dummyCtr.GetHealth() > 0)
							dummy_bool = true;
					}


					// 혹시 모를 오류를 방지하기 위하여 논리 변수를 확인.
					if (dummy_bool && dummy_actor.team != this.stats.actorHealth.team)
					{
						float dummydistance = Vector3.Distance(transform.position, hit.transform.position);
						// 가장 거리가 적은 타겟을 찾는 구문
						if (minDistance > dummydistance)
						{
							target = hit.transform.gameObject;
							minDistance = dummydistance;
						}
					}
				}

				if (!ReferenceEquals(target, null))
				{target_Actor = target.GetComponent<I_Actor>();}


				//if (!ReferenceEquals(target, null)) Debug.Log("Minion Searching!!");
			}
		}
		else timer = 0;
	}

	// 공격 함수.
	// 현재 미니언만 공격이 가능하도록 설정되어있음. 후에 플레이어블 미니언 또한 가능하도록 설정할것임.
	protected void AttackTarget()
	{
		if (timer_attack < ATTACKDELAY) timer_attack += Time.deltaTime;
		if (!ReferenceEquals(target, null))
		{
			if (!target.activeSelf || target_Actor.GetTeam() == this.GetTeam() || target_Actor.GetHealth() <= 0)
			{ target = null;}
			else
			{
				if (timer_attack >= ATTACKDELAY)
				{
					//Debug.Log("Attack Minion!");
					timer_attack = 0;
					bool can_attack_dummy = false;

					RaycastHit[] hits=Physics.RaycastAll(this.transform.position, (target.transform.position - this.transform.position).normalized, searchRadius, 1 << LayerMask.NameToLayer("Minion"));

					foreach(RaycastHit hit in hits)
					{
						if (target == hit.transform.gameObject)
						{
							if (hit.distance <= maxAttackRadius)
							{
								can_attack_dummy = true;
								GameObject dummy = PoolManager.Instance.Get_Particles(0, hit.point);
								dummy.transform.position = hit.point;
								break;
							}
						}
					}
					if (!can_attack_dummy) {target = null; return; }

					// 팀에 따라 제너릭 함수를 따로 사용.
					switch (target.tag)
					{
						case "Minion":
							Attack_Actor<LSM_MinionCtrl>(target);
							break;
						case "PlayerMinion":
							Debug.Log("PlayerMinion Attack!");
							//Attack_Actor<PSH_PlayerFPSCtrl>(target);
							I_Playable dummy_Playable = target.GetComponent<I_Playable>();
							I_Actor dummy_Actor = target.GetComponent<I_Actor>();
							dummy_Playable.Damaged(this.stats.actorHealth.Atk, transform.position, this.stats.actorHealth.team, this.gameObject, 10f);
							if (dummy_Actor.GetHealth() <= 0)
								target = null;
							break;
						default:
							break;
					}

				}
			}
		}
	}

	// Generic함수와 Interface를 결합해서 사용.
	// 미니언 및 플레이어의 오브젝트를 받아와서 사용함.
	private void Attack_Actor<T>(GameObject obj) where T : I_Actor
	{
		T script = obj.GetComponent<T>();
		script.Damaged(this.stats.actorHealth.Atk, transform.position, this.stats.actorHealth.team, this.gameObject);
		int remain_health = script.GetHealth();
		if (remain_health <= 0)
			target = null;
	}

	public virtual short GetHealth() { return this.stats.actorHealth.health; }
	public virtual short GetMaxHealth() { return this.stats.actorHealth.maxHealth; }
	public MoonHeader.S_ActorState GetActor() { return this.stats.actorHealth; }
	public virtual MoonHeader.Team GetTeam() { return this.stats.actorHealth.team; }
	public GameObject GetCameraPos() { return CameraPos; }
    public void Selected()
    {
        /*
        icon_ren = mark.GetComponent<MeshRenderer>();

        icon_materialL.Clear();
        icon_materialL.AddRange(icon_ren.materials);
        icon_materialL.Add(PrefabManager.Instance.outline);

        icon_ren.materials = icon_materialL.ToArray();
		selected_e = true;
		*/
        this.mark.GetComponent<LSM_TurretIconUI>().currentUI.color = MoonHeader.SelectedColors[(int)this.stats.actorHealth.team];
    }

    public void Unselected()
    {
        MeshRenderer renderer_d = mark.GetComponent<MeshRenderer>();

        icon_materialL.Clear();
        icon_materialL.AddRange(renderer_d.materials);
        //icon_materialL.Remove(PrefabManager.Instance.outline);

        icon_ren.materials = icon_materialL.ToArray();
		selected_e = false;
    }

	protected void LevelUp(int type)
	{
		if (type != (int)MoonHeader.ActorType.Turret_Nexus)
		{
			if (LSM_SettingStatus.Instance.lvStatus[type].canLevelUp((byte)(level+1), (short)Mathf.CeilToInt(GameManager.Instance.timer_inGameTurn)))
			{
				level++;
				object[] lv_d = LSM_SettingStatus.Instance.lvStatus[type].getStatus_LV(level);
				short lvup_heal = (short)((short)lv_d[0] - this.stats.actorHealth.maxHealth);

				this.stats.actorHealth.maxHealth = (short)lv_d[0];
				this.stats.actorHealth.Atk = (short)lv_d[1];
				this.stats.actorHealth.health = (short)Mathf.Min(this.stats.actorHealth.maxHealth, this.stats.actorHealth.health + lvup_heal);
			}
		}
	}

    protected void PlaySFX(int num)
    {
        AudioSource dummy_s = Sounds[num].GetComponent<AudioSource>();
        if (dummy_s.isPlaying) { return; }
        else dummy_s.Play();
    }
    protected void StopSFX(int num)
    {
        AudioSource dummy_s = Sounds[num].GetComponent<AudioSource>();
        if (dummy_s.isPlaying) { dummy_s.Stop(); }
        else { return; }
    }

}
