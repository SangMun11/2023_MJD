using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Photon.Pun;

//장판을 소환하는 패턴을 담당하는 Agent
public class HSH_SpellFieldGenerator : Agent
{
    public PatternInfo pinfo;  //쿨타임, 데미지 관련
    public bool isRun;  //Coroutine이 연속으로 호출되는 것을 방지하기 위한 변수
    public float spd;
    public LSM_CreepCtrl creepCtrl;

    Rigidbody rb;
    public GameObject AlertField, SpellField;  //본 패턴 사용 시 스폰할 장판들, 적 캐릭터
    const float COOLTIME = 5.5f;    //쿨타임 초기화 시 사용

    private void Awake()
    {
        
    }
    public override void Initialize()   //필드 초기화
    {
        
        pinfo = new PatternInfo();
        pinfo.cooltime = COOLTIME;
        pinfo.isCool = false;
        pinfo.dmg = 20f;
        isRun = false;
        spd = 17;

        rb = GetComponent<Rigidbody>();
    }

    public void FixedUpdate()
    {
        PatCoolCtrl();  //쿨타임 제어 함수
    }

    public override void CollectObservations(VectorSensor sensor)
    {
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        AddReward(-0.0005f);
        if (!PhotonNetwork.IsMasterClient) return;

        if (pinfo.isCool)   //패턴이 쿨타임일 시
        {
            SpawnAimField(actions.DiscreteActions);
        }

        else if(!pinfo.isCool && !isRun)    //패턴 사용 가능 상태일 시
        {
            isRun = true;
            StartCoroutine(SpawnSpellField());
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        //x축 이동
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

        //y축 이동
        if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[1] = 2;
        }

        else if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[1] = 0;
        }

        else
        {
            discreteActionsOut[1] = 1;
        }
    }

    public override void OnEpisodeBegin()
    {
        pinfo.cooltime = COOLTIME;
        pinfo.isCool = false;
        isRun = false;
    }

    public void SpawnAimField(ActionSegment<int> act)   //조준 장판을 제어하는 함수(AI가 장판의 이동을 정의)
    {
        Vector3 moveX = Vector3.zero;
        Vector3 moveZ = Vector3.zero;

        switch (act[0]) //x축 이동
        {
            case 0:
                moveX = transform.forward * 1f;
                break;
            case 1:
                break;
            case 2:
                moveX = transform.forward * -1f;
                break;
        }

        switch (act[1]) //z축 이동
        {
            case 0:
                moveZ = transform.right * 1f;
                break;
            case 1:
                break;
            case 2:
                moveZ = transform.right * -1f;
                break;
        }

        rb.velocity = (moveX + moveZ).normalized * spd * 50 * Time.fixedDeltaTime;    //x, z축의 이동을 합성해 제어
    }

    IEnumerator SpawnSpellField()
    {

        rb.velocity = new Vector3(0f, 0f, 0f) * 0f;  //조준 장판 정지
        Transform spawnPos = this.transform;    //조준 장판 위치 == 데미지 필드 소환 위치

        GameObject af = PoolManager.Instance.Get_Particles(5, spawnPos.position);
            //Instantiate(AlertField, spawnPos);  //경고!

        yield return new WaitForSeconds(0.5f);

        //Destroy(af);    //경고 장판 제거
        af.GetComponent<ParticleAutoDisable>().ParticleDisable();

        GameObject sf = PoolManager.Instance.Get_Particles(6, spawnPos.position);
        //Instantiate(SpellField, spawnPos);  //공격!
        
        
        
        sf.GetComponent<HSH_SpellField>().Setting((int)creepCtrl.stat.actorHealth.Atk + 100);

        yield return new WaitForSeconds(0.5f);

        //Destroy(sf);    //공격 장판 제거
        sf.GetComponent<ParticleAutoDisable>().ParticleDisable();

        pinfo.cooltime = COOLTIME;  //쿨타임 초기화
        pinfo.isCool = true;

        isRun = false;
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

    public void OnTriggerStay(Collider c)
    {
        if (c.CompareTag("wall"))
        {
            transform.position += -new Vector3(c.transform.position.x - transform.position.x, 0, c.transform.position.z - transform.position.z).normalized;
        }

        //if (c.CompareTag("RedTeam") || c.CompareTag("BlueTeam"))
        if (c.CompareTag("PlayerMinion"))
        {
            AddReward(0.01f);    //정확히 조준하면 spell field generator에게 보상
        }
    }
}
