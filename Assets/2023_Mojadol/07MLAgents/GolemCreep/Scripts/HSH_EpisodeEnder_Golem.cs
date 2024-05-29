using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PatternAvoider_GolemInfo
{
    public HSH_PatternAvoider_Golem PA;
    public Vector3 InitPos;
}

public class HSH_EpisodeEnder_Golem : MonoBehaviour
{
    public GameObject GolemCreep;
    Vector3 golemInitPos;
    public List<PatternAvoider_GolemInfo> PatternAvoiders = new List<PatternAvoider_GolemInfo>();

    int DeathCount; //얼마나 많은 pattern avoider들이 죽었는가?
    // Start is called before the first frame update

    private void Start()
    {
        DeathCount = 0;
        golemInitPos = GolemCreep.transform.position;
        foreach (var item in PatternAvoiders)
        {
            item.InitPos = item.PA.transform.position;
        }
    }
    // Update is called once per frame
    void Update()
    {
        Again();
    }

    public void Again()
    {
        foreach (var item in PatternAvoiders)
        {
            if (!item.PA.gameObject.active)
            {
                DeathCount++;
            }
        }

        if (DeathCount < PatternAvoiders.Capacity)
        {
            DeathCount = 0;
        }

        else
        {
            GolemCreep.GetComponent<HSH_GolemAgent>().AddReward(1);
            GolemCreep.GetComponent<HSH_GolemAgent>().EndEpisode();
            GolemCreep.transform.position = golemInitPos;

            foreach (var item in PatternAvoiders)
            {
                item.PA.gameObject.SetActive(true);
                item.PA.EndEpisode();
                item.PA.transform.position = item.InitPos;
            }

            DeathCount = 0;
        }
    }
}
