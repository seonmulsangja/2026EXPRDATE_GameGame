using System;
using System.Collections.Generic;
using UnityEngine;

public enum SoundType
{
    Attack,
    Hit,
    Die,
    Move,
    Button,
    Eat,
        
}

[Serializable]
public class SoundData
{
    public SoundType soundName;
    public AudioClip clip;
}

public class SoundManager_JCY : MonoBehaviour
{
    [Header("오디오 클립")]
    [SerializeField] private List<SoundData> _soundsClips;
    [SerializeField] private AudioClip _bgmClip;

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
        PlayBGM();
    }

    public void PlaySound(SoundType soundType)
    {
        SoundData sound = _soundsClips.Find(x => x.soundName == soundType);

        if (sound != null && sound.clip != null)
        {
            _soundSources.PlayOneShot(sound.clip);
        }
    }

    public void PlayBGM()
    {
        if (_bgmClip != null)
        {
            _bgmSources.clip = _bgmClip;
            _bgmSources.loop = true;
            _bgmSources.Play();
        }
    }

    public void StopBGM()
    {
        _bgmSources.Stop();
    }
}