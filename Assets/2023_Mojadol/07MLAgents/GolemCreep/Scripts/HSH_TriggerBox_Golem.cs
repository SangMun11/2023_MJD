using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HSH_TriggerBox_Golem : MonoBehaviour
{
    public GameObject GolemCreep;
    public bool isTherePlayer;
    public int playerCount;
    private List<GameObject> player;
    // Start is called before the first frame update
    void Start()
    {
        isTherePlayer = false;
        playerCount = 0;
        player = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = player.Count - 1; i >= 0; i--)
        {
            if (!player[i].activeSelf)
            {
                player.Remove(player[i]);
            }
        }

        isTherePlayer = player.Count > 0;

        GolemCreep.GetComponent<HSH_GolemAgent>().creepinfo.isHero = isTherePlayer;
    }

    private void OnTriggerExit(Collider c)
    {
        //TriggerExit할 때 플레이어의 위치가 방 안쪽인가?
        //크립 룸 각도에 따라 새로 설정해야 할 수도 있습니다.
        //if ((c.CompareTag("RedTeam") || c.CompareTag("BlueTeam")) && c.transform.position.z >this.transform.position.z)
        if (c.CompareTag("PlayerMinion") && Mathf.Sign(Vector3.Dot(this.transform.forward, (c.transform.position - this.transform.position).normalized)) > 0)
        {
            PlayerAdding(c.gameObject);
            //GolemCreep.GetComponent<HSH_LichCreepController>().Player.Add(c.gameObject);
        }

        else if (c.CompareTag("PlayerMinion") && Mathf.Sign(Vector3.Dot(this.transform.forward, (c.transform.position - this.transform.position).normalized)) < 0)
        {
            player.Remove(c.gameObject);
            //GolemCreep.GetComponent<HSH_LichCreepController>().Player.Remove(c.gameObject);
        }
    }

    public void PlayerAdding(GameObject obj)
    {
        bool isExist = false;
        foreach (GameObject item in player)
        {
            if (item.Equals(obj))
            {
                isExist = true;
                break;
            }
        }
        if (!isExist)
            player.Add(obj);
    }
}
