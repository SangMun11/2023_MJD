using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LSM_W_Slash : LSM_BasicProjectile
{
	public GameObject[] slash_effect;

	public override void Setting(GameObject obj, int d, I_Actor ac, float v)
	{
		base.Setting(obj, d, ac, v);
		float dummy_d = (float)(d - ac.GetActor().Atk * 1.2f) / (ac.GetActor().Atk * 0.5f) ;
        photonView.RPC("Enable_effect", RpcTarget.All, dummy_d);
	}
	[PunRPC] protected void Enable_effect(float dummy_d)
	{
        if (dummy_d >= 0.8f)
        {
            for (int i = 0; i < slash_effect.Length; i++) { slash_effect[i].SetActive(true); }
        }
        else if (dummy_d >= 0.4f)
        {
            for (int i = 0; i < slash_effect.Length; i++) { slash_effect[i].SetActive(i == 2); }
        }
        else
        {
            for (int i = 0; i < slash_effect.Length; i++) { slash_effect[i].SetActive(false); }
        }
    }
}
