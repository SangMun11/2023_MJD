using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PSH_LobbyCtrl : MonoBehaviour
{
    // ĳ���� �����ڵ�� PlayerPrefs.SetInt("CharacterCode", selectcode); �Դϴ�

    public Light[] highlights;
    float[] maxLightIntensity = new float[] { 78f, 20f, 18f};
    float lightingTime = 0.5f;
    float tempLight;
    bool isLighting = false;
    float coroutineTimer = 0.0f;
    bool runCoroutineTimer = false;

    public GameObject firstOrigin;

    public GameObject[] cameradestinposes;
    public GameObject[] actorposes;

    public GameObject cameraBoom; // ī�޶� ��ġ�� ��Ƽ������Ʈ
    public GameObject cameraLookBoom; // ī�޶� �ٶ� ��ġ�� ��Ƽ������Ʈ
    Camera cam;
    bool isMoving = false;

    Vector3 currentCamPos; // ���� ī�޶� ��ġ
    Vector3 currentCamLookPos;

    Vector3 camMovePos; // ������ �̵��� ī�޶� ��ġ
    Vector3 camLookPos; // ������ �̵��� ī�޶� ������ ��ġ

    // Vector3.Lerp ����
    float lerpTime = 1.0f;
    float currentLerpTime = 0.0f;

    // Hero Magician Shaman
    public int selectcode = 1; // ������ ���� �ڵ尡 �ɰ���

    // UI variables
    public bool keyEnable = false;

    // �ʱ�ȭ��
    public bool once = false; // �ʱ� ����

    private void Awake()
    {
        for (int i = 0; i < highlights.Length; i++)
        {
            // highlights[i].gameObject.SetActive(false);
            highlights[i].intensity = 0f;
        }
        cam = Camera.main;
        cameraBoom.transform.position = firstOrigin.transform.position;
        //cameraLookBoom.transform.position = firstOrigin.transform.position + Vector3.down;
        cameraLookBoom.transform.position = firstOrigin.transform.position + Vector3.forward;
        cameraBoom.transform.LookAt(cameraLookBoom.transform.position);
        cam.transform.position = cameraBoom.transform.position;
        cam.transform.rotation = cameraBoom.transform.rotation;
    }


    // Update is called once per frame
    void Update()
    {
        cam.transform.position = cameraBoom.transform.position;
        cam.transform.rotation = cameraBoom.transform.rotation;
        currentLerpTime += Time.deltaTime;

        if(once)
        {
            GameObject.Find("LobbyUIManager").GetComponent<PSH_LobbyUI>().characterCode = selectcode;
            PlayerPrefs.SetInt("CharacterCode", selectcode);
            currentLerpTime = 0.0f;
            WhichLook(selectcode);
            tempLight = maxLightIntensity[selectcode] / lightingTime;
            StartCoroutine(HighLightControl(0.1f, selectcode));
        }

        if(keyEnable)
        {
            
            if ((Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) && !isMoving)
            {
                if (selectcode == 0) selectcode = 2; else selectcode--;
                GameObject.Find("LobbyUIManager").GetComponent<PSH_LobbyUI>().characterCode = selectcode;
                PlayerPrefs.SetInt("CharacterCode", selectcode);
                currentLerpTime = 0.0f;
                WhichLook(selectcode);
                tempLight = maxLightIntensity[selectcode] / lightingTime;
                StartCoroutine(HighLightControl(lerpTime + 0.1f, selectcode));
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D) && !isMoving)
            {
                if (selectcode == 2) selectcode = 0; else selectcode++;
                GameObject.Find("LobbyUIManager").GetComponent<PSH_LobbyUI>().characterCode = selectcode;
                PlayerPrefs.SetInt("CharacterCode", selectcode);
                currentLerpTime = 0.0f;
                WhichLook(selectcode);
                tempLight = maxLightIntensity[selectcode] / lightingTime;
                StartCoroutine(HighLightControl(lerpTime + 0.1f, selectcode));
            }

            if (runCoroutineTimer)
            {
                coroutineTimer += Time.deltaTime;

            }


            if (isLighting)
            {
                if (highlights[selectcode].intensity < maxLightIntensity[selectcode])
                {
                    highlights[selectcode].intensity += tempLight * Time.deltaTime;
                }
                else if (highlights[selectcode].intensity >= maxLightIntensity[selectcode])
                {
                    isLighting = false;
                }
            }

            if (currentLerpTime >= lerpTime)
                currentLerpTime = lerpTime;

            float temp = currentLerpTime / lerpTime;
            // temp = Mathf.Sin(temp * Mathf.PI * 0.5f);
            temp = temp * temp * temp * (temp * (6f * temp - 15f) + 10f);

            cameraBoom.transform.position = Vector3.Lerp(currentCamPos, camMovePos, temp);
            cameraLookBoom.transform.position = Vector3.Lerp(currentCamLookPos, camLookPos, temp);
            cameraBoom.transform.LookAt(cameraLookBoom.transform.position);
        }

        
    }

    void WhichLook(int code)
    {
        currentCamPos = cameraBoom.transform.position;
        currentCamLookPos = cameraLookBoom.transform.position;
        camMovePos = cameradestinposes[code].transform.position;
        camLookPos = actorposes[code].transform.position;
    }

    IEnumerator HighLightControl(float time, int code)
    {
        for(int i = 0; i<highlights.Length; i++)
        {
            // highlights[i].gameObject.SetActive(false);
            highlights[i].intensity = 0f;
        }
        isMoving = true;
        runCoroutineTimer = true;

        yield return new WaitForSecondsRealtime(time);

        // highlights[code].gameObject.SetActive(true);

        if (coroutineTimer > lightingTime && isLighting == false)
        {
            isLighting = true;
            isMoving = false;
            runCoroutineTimer = false;
            coroutineTimer = 0.0f;
            once = false;
            StopAllCoroutines();
        }
    }
}
