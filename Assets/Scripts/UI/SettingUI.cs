using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class SettingsMenuUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Button backButton;

    private void OnEnable()
    {
        LoadSliderValues();
        
        masterSlider.onValueChanged.AddListener(AudioSettingsManager.Instance.SetMasterVolume);
        musicSlider.onValueChanged.AddListener(AudioSettingsManager.Instance.SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(AudioSettingsManager.Instance.SetSFXVolume);
        
        backButton.onClick.AddListener(() => gameObject.SetActive(false));
    }

    private void OnDisable()
    {
        masterSlider.onValueChanged.RemoveAllListeners();
        musicSlider.onValueChanged.RemoveAllListeners();
        sfxSlider.onValueChanged.RemoveAllListeners();
        backButton.onClick.RemoveAllListeners();
    }

    private void LoadSliderValues()
    {
        AudioSettingsManager.Instance.mainMixer.GetFloat(AudioSettingsManager.MASTER_KEY, out float masterDb);
        masterSlider.value = Mathf.Pow(10, masterDb / 20);

        AudioSettingsManager.Instance.mainMixer.GetFloat(AudioSettingsManager.MUSIC_KEY, out float musicDb);
        musicSlider.value = Mathf.Pow(10, musicDb / 20);

        AudioSettingsManager.Instance.mainMixer.GetFloat(AudioSettingsManager.SFX_KEY, out float sfxDb);
        sfxSlider.value = Mathf.Pow(10, sfxDb / 20);
    }
}