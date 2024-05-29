using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using static MoonHeader;

public class LSM_ItemSC : MonoBehaviourPunCallbacks, IPunObservable
{
    public int size;
    public bool isCollecting;
    private Rigidbody rigid;
    private IEnumerator discard_IE;

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) // 되는 것 같긴한데 실제로 적용되는지는 확인하기 힘듬 
    {
        if (stream.IsWriting)
        {
            stream.SendNext(this.gameObject.activeSelf);
        }
        else
        {
            this.gameObject.SetActive((bool)stream.ReceiveNext());
        }
    }

    private void Awake()
	{
        rigid = GetComponent<Rigidbody>();
        size = 0;
        photonView.RPC("ReadyCollect", RpcTarget.All, true);
        discard_IE = Discard();
    }
	private void OnEnable()
	{
        photonView.RPC("ReadyCollect", RpcTarget.All, true);
    }
	private void SpawnAnim(float x, float z,float power) 
    {
        rigid.useGravity = true;
        rigid.AddExplosionForce(500*power, this.transform.position + new Vector3(x,-1f,z), 8, 5);
    }

    [PunRPC] private void SettingItem(int s,Vector3 position, float x, float z, float power)
    {
        rigid.useGravity = false;
        size = s;
        this.transform.position = position;
        //Debug.Log("Gold Position" +position);
        StartCoroutine(CollectSetting(x,z, power));
    }
    public void SpawnSetting(int s, Vector3 position)
    {
        SpawnSetting(s, position, 1);
    }

    public void SpawnSetting(int s, Vector3 position, float power)
    {
        photonView.RPC("ReadyCollect", RpcTarget.All, true);
        float dummyx = Random.Range(-5f, 5f), dummyz = Random.Range(-5f, 5f);
        photonView.RPC("SettingItem", RpcTarget.All, s, position, dummyx, dummyz, power);
        //photonView.RPC("SpawnS_RPC", RpcTarget.All, s);
        //StartCoroutine(CollectSetting());
    }

    private IEnumerator CollectSetting(float x, float z, float power)
    {
        yield return new WaitForSeconds(0.1f);
        SpawnAnim(x,z,power);
        
        yield return new WaitForSeconds(1f);
        //photonView.RPC("ReadyCollect", RpcTarget.All, false);
        isCollecting = false;
        if (PhotonNetwork.IsMasterClient)
        {
            StopCoroutine(discard_IE);
            discard_IE = Discard();
            StartCoroutine(discard_IE);
        }
    }

    private IEnumerator Discard()
    {
        yield return new WaitForSeconds(10f);
        if (!isCollecting)
            ItemDisable();
    }
    [PunRPC] private void ReadyCollect(bool c) { isCollecting = c; }
    [PunRPC] private void SpawnS_RPC(int s) { size = s; }

    public void ItemEnable() { photonView.RPC("ItemE_RPC", RpcTarget.All); }
    [PunRPC] private void ItemE_RPC() { this.gameObject.SetActive(true); }

    public int Getting() { photonView.RPC("Getting_RPC", RpcTarget.All); return size; }
    [PunRPC] private void Getting_RPC() { isCollecting = true; Invoke("ItemDisable", 1f); }
    public void ItemDisable() { photonView.RPC("ItemD_RPC",RpcTarget.All); }
    [PunRPC] private void ItemD_RPC() { this.transform.gameObject.SetActive(false); }

    public void ParentSetting_Pool(int index) { photonView.RPC("ParentSetting_Pool_RPC", RpcTarget.AllBuffered, index); }
    [PunRPC]private void ParentSetting_Pool_RPC(int index)
    {
        this.transform.parent = PoolManager.Instance.gameObject.transform;
        PoolManager.Instance.poolList_Items[index].Add(this.gameObject);
    }
}
