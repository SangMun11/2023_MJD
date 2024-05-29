using PacketDotNet.Ieee80211;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LSM_BasicProjectile : MonoBehaviourPunCallbacks
{
    public GameObject orner;
    public I_Actor orner_ac;
    public I_Characters orner_ch;
    public int dam;
    public bool isDestroed_, isPenetration;

    protected float speed = 1f;
    protected float timer_o, setting_timer_o;
    protected float power;
    protected Collider c;
    protected ParticleAutoDisable PAD;

    protected void Awake()
    {
        c = GetComponent<Collider>();
        PAD = GetComponent<ParticleAutoDisable>();
    }
    protected void OnEnable()
    {
        setting_timer_o = 0;
        power = 0;
    }
    protected void Update()
    {
        this.transform.position = this.transform.position + this.transform.forward * Time.deltaTime * speed;
        if (setting_timer_o > 0)
        {
            timer_o += Time.deltaTime;
            if (timer_o >= setting_timer_o)
            {
                timer_o = 0;
                if (isDestroed_) { PAD.ParticleDisable(); }
                c.enabled = false;
            }
        }
    }
    // player obj, damage, I_Actor, Speed
    public virtual void Setting(GameObject obj, int d, I_Actor ac, float v)
    { orner = obj; dam = d; orner_ac = ac; speed = v;
        orner_ch = obj.GetComponent<I_Characters>(); c.enabled = true; 
        Rigidbody rigid = this.GetComponent<Rigidbody>();
        if (rigid != null) rigid.velocity = Vector3.zero;
    }
    public void Setting_Trigger_Exist_T(float T, float po) { setting_timer_o = T; timer_o = 0; power = po; }
    protected void OnTriggerEnter(Collider other)
    {
        Debug.Log("Player Slash Effect Dectected : " + other.name);
        if (!other.gameObject.Equals(orner) && PhotonNetwork.IsMasterClient && !ReferenceEquals(null, other.GetComponent<I_Actor>()))
        {
            Debug.Log("Player Effect Detect Other");
            I_Playable other_play = other.GetComponent<I_Playable>();
            if (ReferenceEquals(other_play, null))
                other.GetComponent<I_Actor>().Damaged((short)dam, this.transform.position, orner_ac.GetTeam(), orner);
            else
            {
                if (power != 0)
                    other_play.Damaged((short)dam, this.transform.position, orner_ac.GetTeam(), orner, power);
                else
                    other_play.Damaged((short)dam, this.transform.position, orner_ac.GetTeam(), orner, 0);
            }
            if (!isPenetration)
                PAD.ParticleDisable();
        }
    }
}
