using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class LSM_WeaponSC : MonoBehaviourPunCallbacks
{
    LSM_Player_Knight myParent;
    public Transform weaponPos;
    

    // Start is called before the first frame update
    void Start()
    {
        myParent = GetComponentInParent<LSM_Player_Knight>();
    }

    private void OnTriggerEnter(Collider other)
    {
        myParent.AttackThem(other.gameObject);
    }
    public IEnumerator SwordEffect(int code)
    {
        Vector3 pos1, pos2, pos3;

        yield return new WaitForSeconds(code == 0 ? 0.65f: code == 1? 0.35f : code == 2? 0.33f : 0.8f);
        pos1 = weaponPos.position;


        yield return new WaitForSeconds(code == 0? 0.15f: code == 1? 0.2f : code == 2? 0.22f : 0.15f);
        pos3 = weaponPos.position;

        Vector3 dummypos = pos3 + (pos1 - pos3).normalized * Vector3.Distance(pos1, pos3) / 2;
        pos2 = dummypos + myParent.playerCamera.transform.forward;

        Vector3 d1 = pos1 - pos2, d2 = pos3 - pos2;
        Vector3 n = new Vector3(d1.y*d2.z - d1.z*d2.y , d1.z*d2.x - d1.x*d2.z , d1.x*d2.y - d1.y*d2.x).normalized;
        if (n.y <= 0) { n = -n; }
        myParent.AttackEffect_B(dummypos, n, Vector3.Distance(pos1, dummypos)*0.7f, code == 3? 5f: 2f, this.myParent.playerCamera.transform.forward);

        //GameObject dummy__ = PoolManager.Instance.Get_Particles(1, dummypos);
        //dummy__.transform.position = dummypos;
        //dummy__.transform.LookAt(myParent.playerCamera.transform.forward + dummypos,n);
        //dummy__.transform.localScale = Vector3.one * Vector3.Distance(pos1, dummypos);
        
    }
}
