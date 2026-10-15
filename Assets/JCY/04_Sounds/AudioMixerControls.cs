using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioMixerControls : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;

    [Header("Volume Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    private void Start()
    {
        // 슬라이더 범위 설정
        masterSlider.minValue = 0.0001f;
        masterSlider.maxValue = 1f;

        bgmSlider.minValue = 0.0001f;
        bgmSlider.maxValue = 1f;

        sfxSlider.minValue = 0.0001f;
        sfxSlider.maxValue = 1f;

        // 슬라이더 이벤트 연결
        masterSlider.onValueChanged.AddListener(SetMasterVolume);
        bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);

        // 초기 볼륨 적용
        SetMasterVolume(masterSlider.value);
        SetBGMVolume(bgmSlider.value);
        SetSFXVolume(sfxSlider.value);
    }

    public void SetMasterVolume(float value)
    {
        audioMixer.SetFloat("MasterVolume", Mathf.Log10(value) * 20f);
    }

    public void SetBGMVolume(float value)
    {
        audioMixer.SetFloat("BGMVolume", Mathf.Log10(value) * 20f);
    }

    public void SetSFXVolume(float value)
    {
        audioMixer.SetFloat("SFXVolume", Mathf.Log10(value) * 20f);
    }

    private void OnDestroy()
    {
        masterSlider.onValueChanged.RemoveListener(SetMasterVolume);
        bgmSlider.onValueChanged.RemoveListener(SetBGMVolume);
        sfxSlider.onValueChanged.RemoveListener(SetSFXVolume);
    }
}
