using System;
using UnityEngine;


public class SoundManager_JCY : MonoBehaviour
{
    [Header("오디오 클립")]
    [SerializeField] private AudioClip[] _soundsClips;
    [SerializeField] private AudioClip[] _bgmsClips;
    
    [Header("오디오 소스")]
    [SerializeField] private AudioSource _soundSources;
    [SerializeField] private AudioSource _bgmSources;
    public static SoundManager_JCY Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        PlayBGM(0);
    }

    public void PlaySound(int index)
    {
        _soundSources.PlayOneShot(_soundsClips[index]);
    }

    public void PlayBGM(int index)
    {
        _bgmSources.PlayOneShot(_bgmsClips[index]);
    }
}
