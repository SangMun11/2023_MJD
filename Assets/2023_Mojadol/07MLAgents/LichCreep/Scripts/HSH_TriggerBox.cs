using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//크립 룸안에 플레이어가 있는지 확인하는 간단한 코드입니다.
//플레이어가 크립룸 안에서 사망했을 경우는 플레이어 스크립트랑 상호작용해야 해서 처리하지 않았으니 추가 요망.
public class HSH_TriggerBox : MonoBehaviour
{
    public GameObject LichCreep;
    public bool isTherePlayer;
    int playerCount;

    // Start is called before the first frame update
    void Start()
    {
        isTherePlayer = false;
        playerCount = 0;
        
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log(Mathf.Sign(Vector3.Dot(this.transform.forward, (d.transform.position - this.transform.position).normalized)));

        if(playerCount > 0)
        {
            isTherePlayer = true;
        }

        else
        {
            isTherePlayer = false;
        }
    }

    private void OnTriggerExit(Collider c)
    {
        //TriggerExit할 때 플레이어의 위치가 방 안쪽인가?
        //크립 룸 각도에 따라 새로 설정해야 할 수도 있습니다.
        //if ((c.CompareTag("RedTeam") || c.CompareTag("BlueTeam")) && c.transform.position.z >this.transform.position.z)
        if (c.CompareTag("PlayerMinion") && Mathf.Sign(Vector3.Dot(this.transform.forward,(c.transform.position - this.transform.position).normalized)) > 0)
        {
            playerCount++;
            //LichCreep.GetComponent<HSH_LichCreepController>().Player.Add(c.gameObject);
            LichCreep.GetComponent<HSH_LichCreepController>().PlayerAdding(c.gameObject);
        }

        else if (c.CompareTag("PlayerMinion") && Mathf.Sign(Vector3.Dot(this.transform.forward, (c.transform.position - this.transform.position).normalized)) < 0)
        {
            playerCount--;
            LichCreep.GetComponent<HSH_LichCreepController>().Player.Remove(c.gameObject);
        }
        
    }
}
