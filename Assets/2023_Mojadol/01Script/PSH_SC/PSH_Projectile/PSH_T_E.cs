using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 상하로 회전 가능하게 만들어야 함
public class PSH_T_E : MonoBehaviour
{
    public float timer1;
    float timer2; // 블럭 연사속도 관련 타이머

    float fireRate = 0.5f;
    LSM_PlayerBase myCtrl;
    GameObject thisObj;
    GameObject playerCam;
    [SerializeField] private GameObject[] soundeffect;
    bool oncePlaySoundBeam;

    private void Awake()
    {
        timer1 = 0;
        timer2 = 0;
        myCtrl = this.GetComponentInParent<LSM_PlayerBase>();
        thisObj = myCtrl.gameObject;
        fireRate = Time.deltaTime;
        playerCam = GameObject.FindGameObjectWithTag("MainCamera");
    }
    private void Start()
    {
        timer1 = 0.0f;
    }

    public void Setting()
    {
        timer1 = 0;
        timer2 = 0;
        oncePlaySoundBeam= false;
        soundeffect[0].GetComponent<AudioSource>().Play();
    }

    void Update()
    {
        timer1 += Time.deltaTime;
        timer2 += Time.deltaTime;

        //Debug.DrawRay(this.transform.position, this.transform.forward * 50f, Color.red);
        //this.transform.localRotation = Quaternion.Euler(playerCam.transform.rotation.eulerAngles.x, transform.rotation.y, transform.rotation.z);
        //eProjectilepos.transform.localRotation = Quaternion.Euler(pitch, eProjectilepos.transform.rotation.y, eProjectilepos.transform.rotation.z);
        if (!oncePlaySoundBeam && timer1 >= 5.0f)
        { oncePlaySoundBeam = true; soundeffect[1].GetComponent<AudioSource>().Play(); }
        if (timer1 >= 8.0f)
        {
            this.gameObject.SetActive(false);
        }
        if (!PhotonNetwork.IsMasterClient)
            return;
        //if (!PhotonNetwork.IsMasterClient)
            //return;

        if(timer1 >= 5.0f && timer1 <= 8f) // 5초 후 부터 초당 2발의 연사속도로 블럭을 발사
        {
                Debug.DrawRay(this.transform.position, this.transform.forward * 35f, Color.blue, 1f);
                RaycastHit[] hits = Physics.RaycastAll(this.transform.position, this.transform.forward,35f,
                    1<< LayerMask.NameToLayer("Minion") | 1 << LayerMask.NameToLayer("Turret"));
                foreach(RaycastHit hit in hits)
                {
                    I_Actor dummy_ac = hit.transform.GetComponent<I_Actor>();
                    if (!ReferenceEquals(dummy_ac, null) && dummy_ac.GetTeam() != myCtrl.actorHealth.team)
                    {
                        PoolManager.Instance.Get_Local_Item(2).transform.position = hit.point;
                        if (PhotonNetwork.IsMasterClient)
                        {
                            short dam_ = (short)Mathf.RoundToInt(myCtrl.actorHealth.Atk * 3 * Time.deltaTime);
                            dummy_ac.Damaged(dam_, this.transform.position, myCtrl.actorHealth.team, thisObj);
                        }
                    }

                }

        }
    }


}
