using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;


//투사체를 던지는 Agent cs

public class HSH_FireBallThrower : Agent
{
    public PatternInfo pinfo;  //쿨타임, 데미지 관련
    float force;

    private float gravity = Physics.gravity.magnitude;
    public LSM_CreepCtrl creepCtrl;

    public GameObject FireBall, CoordinateGetter, AimBox;
    const float COOLTIME = 4.0f;    //쿨타임 초기화 시 사용

    public override void Initialize()   //필드 초기화
    {
        pinfo = new PatternInfo();
        pinfo.cooltime = COOLTIME;
        pinfo.isCool = true;
        pinfo.dmg = 2f;

        force = 600f;
    }

    public void FixedUpdate()
    {
        PatCoolCtrl();  //쿨타임 제어 함수
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        AddReward(-0.0005f);
        FireCtrl(actions.ContinuousActions);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = -Input.GetAxis("Vertical");
        continuousActionsOut[1] = -Input.GetAxis("Horizontal");

    }

    public override void OnEpisodeBegin()
    {
        pinfo.cooltime = COOLTIME;
        pinfo.isCool = false;
    }

    public void FireCtrl(ActionSegment<float> act)
    {      
        float axisX = Mathf.Clamp(act[0], -0.5f, 1f) * 90f;
        float axisY = Mathf.Clamp(act[1], -1f, 1f) * 180f;

        Quaternion FireBallRot = Quaternion.Euler(axisX, axisY, 0);

        if (!pinfo.isCool) {
            GameObject fb = Instantiate(FireBall, this.transform.position, FireBallRot);

            fb.GetComponent<HSH_FireBall>().dmg = 100 + Mathf.RoundToInt((float)creepCtrl.stat.actorHealth.Atk/2);
            fb.GetComponent<Rigidbody>().AddForce(fb.transform.forward * force * Time.fixedDeltaTime, ForceMode.Impulse);
            pinfo.cooltime = COOLTIME;
        }

        else
        {
            float gravity = Physics.gravity.magnitude;
            //float additionalHeight = 1.853979613739931f;
            //float time = 2 * force * Time.fixedDeltaTime * Mathf.Abs(Mathf.Sin(axisX * Mathf.Deg2Rad)) / gravity + 0.1f;
            float v = force * Time.fixedDeltaTime;
            float b = -v * Mathf.Sin(axisX * Mathf.Deg2Rad);
            float time = Mathf.Abs((-b - Mathf.Sqrt(b * b + 330.064f)) / gravity);
            float distance = v * Mathf.Cos(axisX * Mathf.Deg2Rad) * time;
            Vector3 O = this.transform.position;

            Quaternion rotY = Quaternion.Euler(0, axisY, 0); //필요한가?
            Vector3 R = O + new Vector3(0, -15.5f, distance);

            CoordinateGetter.transform.position = R;
            CoordinateGetter.GetComponent<Rigidbody>().transform.RotateAround(O, Vector3.up, axisY);
            AimBox.transform.SetPositionAndRotation(CoordinateGetter.transform.position, rotY);
        }
    }

    private void PatCoolCtrl()  //쿨타임 제어 함수
    {
        if (pinfo.cooltime > 0.5f)
        {
            pinfo.isCool = true;
            pinfo.cooltime -= Time.fixedDeltaTime;
        }

        else
        {
            pinfo.isCool = false;    //쿨타임이 돌면 패턴 사용 가능 상태
        }
    }
}
