using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PatternAvoider2Info
{
    public HSH_PatternAvoider2 PA;
    public Vector3 InitPos;
}

public class HSH_EpisodeEnder2 : MonoBehaviour
{
    public GameObject FireBallThrower;
    Vector3 sfgInitPos;
    public List<PatternAvoider2Info> PatternAvoiders = new List<PatternAvoider2Info>();

    int DeathCount; //얼마나 많은 pattern avoider들이 죽었는가?
    // Start is called before the first frame update

    private void Start()
    {
        DeathCount = 0;
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
            FireBallThrower.GetComponent<HSH_FireBallThrower>().AddReward(1);
            FireBallThrower.GetComponent<HSH_FireBallThrower>().EndEpisode();

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
