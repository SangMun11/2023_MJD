using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class LSM_BaseTurretSC : LSM_TurretSc
{
    private LSM_Spawner parentSpawner;
    private float timer_regen;
    private bool isDie;

    protected override void Start()
    {
        parentSpawner = this.GetComponentInParent<LSM_Spawner>();
        base.Start();
        this.ac_type = MoonHeader.ActorType.Turret_Base;

        GameObject.Destroy(mark);
        GameObject.Destroy(mark_obj);

        mark = GameObject.Instantiate(PrefabManager.Instance.icons[9], GameManager.Instance.mapUI.transform);
        mark.GetComponent<LSM_TurretIconUI>().Setting(this.gameObject);
        mark_obj = GameManager.Instantiate(PrefabManager.Instance.icons[10], transform);
        mark_obj.transform.localPosition = new Vector3(0, 30, 0);
        mark_obj.transform.rotation = Quaternion.Euler(90, 0, 0);

        object[] lv_d = LSM_SettingStatus.Instance.lvStatus[(int)ac_type].getStatus_LV(level);

        stats = new MoonHeader.S_TurretStats((short)lv_d[0], (short)lv_d[1], parentSpawner.team);
        base.ChangeTeamColor();
        base.ChangeTeamColor(bodies[0].gameObject);
        ATTACKDELAY = 1.5f;
        stats.actorHealth.type = MoonHeader.AttackType.Turret;
        timer_regen = 0;
        isDie = false;
        searchRadius = 23;
        maxAttackRadius = 27;
        
    }

    protected override void Update()
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
                if (!isDie)
                {
                    SearchingTarget();
                    AttackTarget();
                    LevelUp((int)ac_type);
                }
                else
                {
                    timer_regen += Time.deltaTime;
                    if (timer_regen >= 600f)
                    {
                        ReBuild();
                    }
                }
            }

            if (isDie)
            {
                timer_regen += Time.deltaTime;
                if (timer_regen >= 30f)
                {
                    ReBuild();
                }
            }
        }

    }

    private void ReBuild()
    {
        isDie = false;
        timer_regen = 0;
        GameManager.Instance.DisplayAdd(string.Format("{0}팀의 포탑이 재생성되었습니다.", this.stats.actorHealth.team));
        ChangeTeamColor(bodies[0].gameObject);
        this.stats.actorHealth.health = this.stats.actorHealth.maxHealth;
        this.transform.tag = "Turret";
        this.transform.gameObject.layer = 10;
    }


    public override void Damaged(short dam, Vector3 origin, MoonHeader.Team t, GameObject other)
    {
        if (t == this.stats.actorHealth.team || !PhotonNetwork.IsMasterClient || isDie)
            return;
        this.stats.actorHealth.health -= dam;
        photonView.RPC("D_BT_RPC", RpcTarget.All);
        //StartCoroutine(DamagedEffect());
        if (this.stats.actorHealth.health <= 0 && !isDie)
        {
            DestroyProcessing(other);
        }
        return;
    }
    [PunRPC]
    private void D_BT_RPC()
    {PlaySFX(0); StartCoroutine(DamagedEffect()); }
    protected override IEnumerator DamagedEffect()
    {
        Color damagedColor = new Color32(255, 150, 150, 255);
        Color recovered = Color.white;

        foreach (Renderer item in bodies)
            item.material.color = damagedColor;

        yield return new WaitForSeconds(0.25f);

        foreach (Renderer item in bodies)
            item.material.color = recovered;

        if (!isDie)
            ChangeTeamColor(bodies[0].gameObject);
        else
            bodies[0].GetComponent<Renderer>().material.color = Color.black;
    }

    protected override void DestroyProcessing(GameObject other)
    {
        GameManager.Instance.DisplayAdd(string.Format("{0} Destroyed {1}", other.name, this.name));
        photonView.RPC("Destroy_BT_RPC",RpcTarget.All);
    }
    [PunRPC]private void Destroy_BT_RPC() 
    {
        PlaySFX(1);
        isDie = true;
        bodies[0].GetComponent<Renderer>().material.color = Color.black;
        //this.transform.tag = "Untagged";
        this.transform.gameObject.layer = 6;
    }
}
