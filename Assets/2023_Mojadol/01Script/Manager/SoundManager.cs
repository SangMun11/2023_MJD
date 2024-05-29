using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    private AudioSource audioSource;
    [SerializeField] private GameObject[] musics;
    public AudioMixer mixer;
    private int curnum = -1;
    public GameObject[] effects;
    private bool isMute;

    private bool once;

    private static SoundManager instance;
    private void Awake()
    {
        if (instance == null) { instance= this; }
    }

    public static SoundManager Instance { get { return instance; } }

    private void Start()
    {
        
    }

    void Update()
    {
        if (!once && GameManager.Instance.onceStart)
        { once = true; PlayMusic(0); BGMMute(false); }
    }

    // true¸é mute, false¸é unmute
    public void BGMMute(bool m) 
    {
        if (m) { mixer.SetFloat("BGM_Sound", -80f); }
        else { mixer.SetFloat("BGM_Sound", -15f); }
    }

    public void PlayMusic(int num)
    {
        if (curnum != num)
        {
            if (curnum != -1 && audioSource.isPlaying)
                StopMusic();
            curnum = num;
            audioSource = musics[num].GetComponent<AudioSource>();
            audioSource.Play();
        }

    }

    public void StopMusic()
    {
        if (curnum == -1)
            return;
        audioSource.Stop();
        foreach (GameObject mu in musics)
            mu.GetComponent<AudioSource>().Stop();
    }
}
