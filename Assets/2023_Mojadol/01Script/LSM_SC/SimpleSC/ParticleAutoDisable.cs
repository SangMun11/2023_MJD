using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Unity.VisualScripting.Antlr3.Runtime.Misc;

public class ParticleAutoDisable : MonoBehaviourPunCallbacks, IPunObservable
{
    ParticleSystem ps;
    float timer_autoDisable = 0;
    //bool alive;
    //float timer = 0;

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            //stream.SendNext(this.gameObject.activeSelf);
        }
        else
        {
           // bool isActive_ = (bool)stream.ReceiveNext();
         //   this.gameObject.SetActive(isActive_);
        }
    }
    private void Update()
    {
        timer_autoDisable += Time.deltaTime;
        if (timer_autoDisable > 3f) { this.gameObject.SetActive(false); }
    }

    private void Awake()
    {
        ps = this.GetComponent<ParticleSystem>();
        if (PhotonNetwork.IsMasterClient)
        { }//StartCoroutine(CheckAlive());
        else
        {
            Rigidbody rigid = this.GetComponent<Rigidbody>();

            if (rigid != null)
            { rigid.useGravity = false; }

        }

    }
    
    private void OnEnable()
    {
        timer_autoDisable = 0;
        //if (PhotonNetwork.IsMasterClient)
        //{ StartCoroutine(CheckAlive()); timer = 0f; }
    }
    /*
    private IEnumerator CheckAlive()
    {
        while (alive)
        {
            yield return new WaitForSeconds(0.5f);
            timer += 0.5f;
            if (!ps.isPlaying || timer >= 10f)
            {
                timer = 0;
                Debug.Log("particle disable!");
                photonView.RPC("ParticleDisable_RPC", RpcTarget.All);
                break;
            }
        }
    }*/
    public void ParticleDisable() { photonView.RPC("ParticleDisable_RPC", RpcTarget.All); }
    [PunRPC] private void ParticleDisable_RPC() { this.gameObject.SetActive(false); }

    public void ParentSetting_Pool(int index) { photonView.RPC("ParentSP_RPC", RpcTarget.AllBuffered, index); }
    [PunRPC]
    private void ParentSP_RPC(int index)
    {
        this.transform.parent = PoolManager.Instance.gameObject.transform;
        PoolManager.Instance.poolList_Particles[index].Add(this.gameObject);
    }

    public void ParticleEnable(Vector3 position_, Vector3 rot)
    {photonView.RPC("ParticleEnable_RPC", RpcTarget.All, position_, rot);}
    [PunRPC]private void ParticleEnable_RPC(Vector3 position_, Vector3 rot)
    {
        this.gameObject.SetActive(true);
        this.transform.position = position_;
        this.transform.rotation = Quaternion.Euler(rot);
    }
    public void Particle_Size_Setting(float s) { photonView.RPC("PS_s", RpcTarget.All, s); }
    [PunRPC] private void PS_s(float s)
    {
        ps.startSize = s;
    }
}
