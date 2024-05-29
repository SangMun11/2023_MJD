using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PSH_GameStart : MonoBehaviour
{
    public GameObject cameraLook;
    public Camera cam;
    float speed = 5.0f;
    // Start is called before the first frame update
    void Start()
    {
        cam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        cam.transform.position = this.transform.position;
        cam.transform.rotation = this.transform.rotation;
        cameraLook.transform.Rotate(Vector3.up * speed * Time.deltaTime);
        this.transform.LookAt(cameraLook.transform.position);
    }

    // 게임 시작 버튼
    public void GameStartButton()
    {
        SceneManager.LoadScene("PSH_LobbyScene");
    }
}
