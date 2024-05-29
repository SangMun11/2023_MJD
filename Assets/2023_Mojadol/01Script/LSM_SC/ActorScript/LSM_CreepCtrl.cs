using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using Newtonsoft.Json.Bson;
using Unity.Burst.CompilerServices;

public class LSM_CreepCtrl : MonoBehaviourPunCallbacks, I_Actor, IPunObservable, I_Characters
{
    public MoonHeader.S_CreepStats stat;

    private Rigidbody rigid;
    private GameObject icon;
    private Renderer[] bodies;  // 색상을 변경할 렌더러.
    private I_Creep mainCtrl;
    public int inPlayerNum;
    private bool once;

    public byte level;
    public MoonHeader.ActorType ac_type;

    public short[] add_Hp_Atk;

    public GameObject[] Sounds;// 0: 걷기, 1: 피격, 2: 죽음
    

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(this.gameObject.activeSelf);

            // maxHealth 2byte, health 2byte, team 8bit, atk 8bit, state 8bit
            ulong send_dummy = stat.SendDummyMaker();
            //stream.SendNext(send_dummy);

            int dummy_int1 = (int)(send_dummy & (ulong)uint.MaxValue);
            int dummy_int2 = (int)((send_dummy >> 32) & (ulong)uint.MaxValue);
            stream.SendNext(dummy_int1);
            stream.SendNext(dummy_int2);
            stream.SendNext(inPlayerNum);
        }
        else
        {
            bool isActive_ = (bool)stream.ReceiveNext();
            this.gameObject.SetActive(isActive_);

            ulong receive_dummy = ((ulong)(int)stream.ReceiveNext() & (ulong)uint.MaxValue);
            receive_dummy += (((ulong)(int)stream.ReceiveNext() & (ulong)uint.MaxValue) << 32);

            this.stat.ReceiveDummy(receive_dummy);
            inPlayerNum = (int)stream.ReceiveNext();
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        rigid = this.GetComponent<Rigidbody>();
        once = false;

        icon = GameObject.Instantiate(PrefabManager.Instance.icons[11], transform);
        icon.transform.localPosition = new Vector3(0, 80 / this.transform.localScale.y, 0);
        icon.transform.localScale = icon.transform.localScale / this.transform.localScale.x * 1.5f;
        icon.GetComponent<Renderer>().material.color = Color.yellow;
        bodies = this.transform.GetComponentsInChildren<Renderer>();

        mainCtrl = this.GetComponent<I_Creep>();
        stat = new MoonHeader.S_CreepStats();

        object[] lv_d = LSM_SettingStatus.Instance.lvStatus[(int)ac_type].getStatus_LV(level);

        // 디버그용 maxHealth, Atk, Exp, Gold
        stat.Setting((short)lv_d[0], (short)lv_d[1], 600 + 25*level, 300);
        inPlayerNum = 0;
    }

    public void ResetCreep()
    {
        photonView.RPC("RC_RPC",RpcTarget.All);
    }
    [PunRPC] private void RC_RPC() 
    {
        this.stat.state = MoonHeader.CreepStat.Idle;
        stat.actorHealth.health = stat.actorHealth.maxHealth;
        this.mainCtrl.RegenProcessing();
        icon.GetComponent<Renderer>().material.color = Color.yellow;
        foreach (Renderer item in bodies) { item.enabled = true; }
    }

    // Update is called once per frame
    void Update()
    {
        if (ac_type == MoonHeader.ActorType.Creep_Golem && rigid.velocity.magnitude >= 0.5f)
        { PlaySFX(0); }

        if (!once && GameManager.Instance.onceStart && !PhotonNetwork.IsMasterClient)
        {
            mainCtrl.Setting();
            //mainCtrl.spellFieldGenerator.GetComponentInChildren<HSH_SpellFieldGenerator>().enabled = false;
        }
        if (PhotonNetwork.IsMasterClient && stat.state != MoonHeader.CreepStat.Death)
        {
            LevelUp((int)ac_type);
        }
    }
    public void Damaged(short dam, Vector3 origin, MoonHeader.Team t, GameObject other)
    {
        // 죽음 혹은 무적 상태일 경우 데미지를 입지않음. 바로 return
        if (stat.state == MoonHeader.CreepStat.Death || !PhotonNetwork.IsMasterClient)
            return;

        stat.actorHealth.health -= dam;

        if (stat.actorHealth.health > 0)
            photonView.RPC("DamMinion_RPC", RpcTarget.All);
        //StartCoroutine(DamagedEffect());

        //Debug.Log("Minion Damaged!! : " +stats.health);
        // 체력이 0 이하라면 DeadProcessing
        if (stat.actorHealth.health <= 0 && stat.state != MoonHeader.CreepStat.Death)
        {
            StartCoroutine(DeadProcessing(other));
        }
        return;
    }

    public void Enable_Generator(bool b) { photonView.RPC("E_RPC", RpcTarget.All, b); }
    [PunRPC] private void E_RPC(bool b) {
        //mainCtrl.spellFieldGenerator.SetActive(false); 
        mainCtrl.AttackEffectEnable(false);
    }

    [PunRPC]
    protected void DamMinion_RPC()
    {StartCoroutine(DamagedEffect());}

    private IEnumerator DamagedEffect()
    {
        Color damagedColor = new Color32(255, 150, 150, 255);
        PlaySFX(1);
        //Vector3 knockbackDirection = Vector3.Scale(this.transform.position - origin, Vector3.zero - Vector3.up).normalized * 500 + Vector3.up * 100;

        foreach (Renderer r in bodies)
        { r.material.color = damagedColor; }
        //this.rigid.AddForce(knockbackDirection);

        yield return new WaitForSeconds(0.25f);
        foreach (Renderer r in bodies)
        { r.material.color = Color.white; }
    }


    private IEnumerator DeadProcessing(GameObject other)
    {
        stat.state = MoonHeader.CreepStat.Death;
        //nav_ob.enabled = false;

        //anim.SetBool("Dead", true);
        //this.mainCtrl.lichstat = LichStat.Death;
        mainCtrl.StatSetting(3);
        photonView.RPC("DeadAnim", RpcTarget.All);


        if (other.transform.CompareTag("PlayerMinion"))
        {
            other.GetComponent<I_Characters>().AddEXP((short)stat.exp);        // 잡은 미니언이 플레이어 미니언이라면 경험치를 한번 더 줌.
                                                                               //other.GetComponent<PSH_PlayerFPSCtrl>().myPlayerCtrl.GetExp(50);   // 디버깅용으로 현재 경험치를 50으로 고정 지급.
                                                                               // 디버깅용 플레이어가 미니언을 처치하였다면..
            GameManager.Instance.DisplayAdd(string.Format("{0}가 {1}를 처치하였습니다.", other.name, this.name));
            GameManager.Instance.teamManagers[((int)other.GetComponent<I_Actor>().GetTeam())].AddingAtkHp(add_Hp_Atk[0], add_Hp_Atk[1]);
            other.GetComponent<LSM_PlayerBase>().SettingUpdate();
        }

        else if (other.transform.CompareTag("DamageArea"))
        {
            other.GetComponent<LSM_BasicProjectile>().orner.GetComponent<I_Characters>().AddEXP((short)stat.exp);
            GameManager.Instance.DisplayAdd(string.Format("{0}가 {1}를 처치하였습니다.", other.GetComponent<LSM_BasicProjectile>().orner.name, this.name));
            GameManager.Instance.teamManagers[((int)other.GetComponent<I_Actor>().GetTeam())].AddingAtkHp(add_Hp_Atk[0], add_Hp_Atk[1]);
            other.GetComponent<LSM_PlayerBase>().SettingUpdate();
        }

        yield return new WaitForSeconds(2f);
        // 골드주는 오브젝트 생성.
        //dummy_item.transform.position = this.transform.position;

        for (int i = 0; i < 5; i++)
        {
            GameObject dummy_item = PoolManager.Instance.Get_Item(0);
            dummy_item.GetComponent<LSM_ItemSC>().SpawnSetting(this.stat.gold / 5, this.transform.position, 2.3f);
        }
        //hit.transform.GetComponent<I_Characters>().AddEXP((short)stat.exp);
        //GiveExp();

        yield return new WaitForSeconds(1f);
        //this.gameObject.SetActive(false);
        //photonView.RPC("DeadP", RpcTarget.All);
    }
    [PunRPC]
    protected void DeadAnim()
    { 
        mainCtrl.DeadProcessing();
        PlaySFX(2);
        stat.state = MoonHeader.CreepStat.Death;
        PoolManager.Instance.Get_Local_Item(1).transform.position = this.transform.position + Vector3.up * 1.5f;
        Invoke("Dead_renderer_disable", 5f);
    }
    private void Dead_renderer_disable() {
        if (stat.state == MoonHeader.CreepStat.Death)
        {
            foreach (Renderer item in bodies) { item.enabled = false; }
        }
    }

    public void GiveExp()
    {
        RaycastHit[] hits;
        float expRadius = 10f;
        hits = Physics.SphereCastAll(transform.position, expRadius, Vector3.up, 0, 1 << LayerMask.NameToLayer("Minion"));
        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.CompareTag("PlayerMinion"))
            {
                hit.transform.GetComponent<I_Characters>().AddEXP((short)stat.exp);
            }
        }
    }

    protected void LevelUp(int type)
    {
        if (type != (int)MoonHeader.ActorType.Turret_Nexus)
        {
            if (LSM_SettingStatus.Instance.lvStatus[type].canLevelUp((byte)(level+1), (short)Mathf.CeilToInt(GameManager.Instance.timer_inGameTurn)))
            {
                Debug.Log("Level Up!");
                level++;
                object[] lv_d = LSM_SettingStatus.Instance.lvStatus[type].getStatus_LV(level);
                short lvup_heal = (short)((short)lv_d[0] - this.stat.actorHealth.maxHealth);

                this.stat.actorHealth.maxHealth = (short)lv_d[0];
                this.stat.actorHealth.Atk = (short)lv_d[1];
                this.stat.actorHealth.health = (short)Mathf.Min(this.stat.actorHealth.maxHealth, this.stat.actorHealth.health + lvup_heal);
                this.stat.exp = 600 + 25*level;
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

    // 아이콘 및 몸체 색 변경.
    #region ChangeTeamColors
    // 오버로드. 매개변수가 존재하지 않을경우 미니언의 아이콘의 색상을 변경.
    public void ChangeTeamColor() { photonView.RPC("ChangeTC_RPC", RpcTarget.All); }

    // 시작 혹은 생성할 때 미니언의 아이콘 등의 색상을 변경.
    [PunRPC]
    public void ChangeTC_RPC()
    {
        Color dummy_color;
        switch (stat.actorHealth.team)
        {
            case MoonHeader.Team.Red:
                dummy_color = Color.red;
                break;
            case MoonHeader.Team.Blue:
                dummy_color = Color.blue;
                break;
            case MoonHeader.Team.Yellow:
                dummy_color = Color.yellow;
                break;
            default: dummy_color = Color.gray; break;
        }
        icon.GetComponent<Renderer>().material.color = dummy_color;
    }
    #endregion

    #region I_Actor
    public short GetHealth() { return this.stat.actorHealth.health; }
    public short GetMaxHealth() { return this.stat.actorHealth.maxHealth; }
    public MoonHeader.Team GetTeam() { return this.stat.actorHealth.team; }
    public void AddEXP(short exp) { }
    public MoonHeader.S_ActorState GetActor() { return this.stat.actorHealth; }
    public GameObject GetCameraPos() { return this.transform.gameObject; }
    public int GetState() { return (int)stat.state; }
    public void Selected()
    {
        this.icon.GetComponent<Renderer>().material.color = MoonHeader.SelectedColors[(int)this.stat.actorHealth.team];
    }

    public void Unselected()
    {    }
    #endregion
}
