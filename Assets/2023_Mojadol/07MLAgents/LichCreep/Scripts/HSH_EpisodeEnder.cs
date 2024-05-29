using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PatternAvoiderInfo
{
    public HSH_PatternAvoider PA;
    public Vector3 InitPos;
}

public class HSH_EpisodeEnder : MonoBehaviour
{
    public GameObject SpellFieldGenerator;
    Vector3 sfgInitPos;
    public List<PatternAvoiderInfo> PatternAvoiders = new List<PatternAvoiderInfo>();

    int DeathCount; //얼마나 많은 pattern avoider들이 죽었는가?
    // Start is called before the first frame update

    private void Start()
    {
        DeathCount = 0;
        sfgInitPos = SpellFieldGenerator.transform.position;
        foreach(var item in PatternAvoiders)
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
            SpellFieldGenerator.GetComponent<HSH_SpellFieldGenerator>().AddReward(1);
            SpellFieldGenerator.GetComponent<HSH_SpellFieldGenerator>().EndEpisode();
            SpellFieldGenerator.transform.position = sfgInitPos;

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
