using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using UnityEngine;

public class HSH_PatternAvoider : Agent
{
    const int INITHP = 4;  //에피소드가 초기화 될 때 초기화할 hp 값
    public float hp, spd;

    Rigidbody rb;

    public GameObject sfg;  //spell field generator를 제어

    // Start is called before the first frame update
    public override void Initialize()
    {
        hp = INITHP; spd = 15 + UnityEngine.Random.Range(-3, 3);  //실제 캐릭터와 비슷하게 spd를 설정
        int i = UnityEngine.Random.Range(0, 2);

        rb = GetComponent<Rigidbody>();

        int j = UnityEngine.Random.Range(0, 2);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        MoveAgent(actions.DiscreteActions);
        AddReward(0.0005f); //오래 생존할 수록 큰 보상
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        //앞뒤 이동
        var discreteActionsOut = actionsOut.DiscreteActions;

        if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[0] = 0;
        }

        else if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[0] = 2;
        }

        else
        {
            discreteActionsOut[0] = 1;
        }

        //좌우 회전
        if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[1] = 0;
        }

        else if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[1] = 2;
        }

        else
        {
            discreteActionsOut[1] = 1;
        }
    }

    public void MoveAgent(ActionSegment<int> act)
    {
        Vector3 dirToGoX = Vector3.zero;

        var action = act[0];
        switch (action)
        {
            case 0:
                dirToGoX = transform.forward * 1f;
                break;
            case 1:
                break;
            case 2:
                dirToGoX = transform.forward * -1f;
                break;
        }

        Vector3 dirToGoZ = Vector3.zero;

        var action_2 = act[1];
        switch (action_2)
        {
            case 0:
                dirToGoZ = -transform.right * 1f;
                break;
            case 1:
                break;
            case 2:
                dirToGoZ = transform.right * 1f;
                break;
            default:
                break;
        }

        rb.velocity = (dirToGoX+dirToGoZ).normalized * spd * 50f * Time.fixedDeltaTime;
    }

    public override void OnEpisodeBegin()
    {
        hp = INITHP;
    }

    public void Damaged(float dmg)
    {
        hp -= dmg;
        AddReward(dmg * (-0.05f));
        sfg.GetComponent<HSH_SpellFieldGenerator>().AddReward(0.1f);    //데미지를 받으면 SpellField Generator에게 보상

        if(hp > 0)
        {
        }

        else
        {
            this.gameObject.SetActive(false);
        }
    }

    public void OnTriggerStay(Collider c)
    {
        if (c.CompareTag("AimField"))
        {
            AddReward(-0.001f);   //조준당하면 pattern avoider는 감점
        }
    }
}
