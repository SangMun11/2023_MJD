using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 넥서스가 갖고있는 스크립트. 터렛 스크립트를 상속받게 설정.
public class LSM_NexusSC : LSM_TurretSc
{
	private LSM_Spawner parentSpawner;

	protected override void Start()
	{
		parentSpawner = this.GetComponentInParent<LSM_Spawner>();
		base.Start();

		GameObject.Destroy(mark);
		GameObject.Destroy(mark_obj);

        mark = GameObject.Instantiate(PrefabManager.Instance.icons[7], GameManager.Instance.mapUI.transform);
        mark.GetComponent<LSM_TurretIconUI>().Setting(this.gameObject);
        mark_obj = GameManager.Instantiate(PrefabManager.Instance.icons[8], transform);
        mark_obj.transform.localPosition = new Vector3(5, 30, -5);
		mark_obj.transform.rotation = Quaternion.Euler(90, 0, 0);

        stats = new MoonHeader.S_TurretStats(3000, 0, parentSpawner.team);
		base.ChangeTeamColor();
		ATTACKDELAY = 1.5f;
		stats.actorHealth.type = MoonHeader.AttackType.Turret;
		ac_type = MoonHeader.ActorType.Turret_Nexus;
	}
    protected override void Update()
    {
        if (GameManager.Instance.onceStart)
		{
            if (GameManager.Instance.mainPlayer.MapCam.activeSelf) { mark_obj.SetActive(false); }
            else { mark_obj.SetActive(true); }
        }
    }

    public override void Damaged(short dam, Vector3 origin, MoonHeader.Team t, GameObject other)
	{
		if (t == this.stats.actorHealth.team || !PhotonNetwork.IsMasterClient)
			return;
		this.stats.actorHealth.health -= dam;
		StartCoroutine(DamagedEffect());
		if (this.stats.actorHealth.health <= 0)
		{
			DestroyProcessing(other);
			GameManager.Instance.GameEndingProcess(this.stats.actorHealth.team);
		}

		return;
	}
	protected override void DestroyProcessing(GameObject other)
	{
		GameManager.Instance.DisplayAdd(string.Format("{0} Destroyed {1}", other.name, this.name));
	}
}
