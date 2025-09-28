using UnityEngine;
using UnityEngine.Audio;

public class AudioSettingsManager : MonoBehaviour
{
    public static AudioSettingsManager Instance { get; private set; }

    public AudioMixer mainMixer;

    public const string MASTER_KEY = "MasterVolume";
    public const string MUSIC_KEY = "MusicVolume";
    public const string SFX_KEY = "SFXVolume";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        LoadVolumeSettings();
    }

    private void LoadVolumeSettings()
    {
        float masterVolume = PlayerPrefs.GetFloat(MASTER_KEY, 0f);
        float musicVolume = PlayerPrefs.GetFloat(MUSIC_KEY, 0f);
        float sfxVolume = PlayerPrefs.GetFloat(SFX_KEY, 0f);
        
        mainMixer.SetFloat(MASTER_KEY, masterVolume);
        mainMixer.SetFloat(MUSIC_KEY, musicVolume);
        mainMixer.SetFloat(SFX_KEY, sfxVolume);
    }

    public void SetMasterVolume(float sliderValue)
    {
        float volumeInDb = ConvertToDecibels(sliderValue);
        mainMixer.SetFloat(MASTER_KEY, volumeInDb);
        PlayerPrefs.SetFloat(MASTER_KEY, volumeInDb);
    }

    public void SetMusicVolume(float sliderValue)
    {
        float volumeInDb = ConvertToDecibels(sliderValue);
        mainMixer.SetFloat(MUSIC_KEY, volumeInDb);
        PlayerPrefs.SetFloat(MUSIC_KEY, volumeInDb);
    }

    public void SetSFXVolume(float sliderValue)
    {
        float volumeInDb = ConvertToDecibels(sliderValue);
        mainMixer.SetFloat(SFX_KEY, volumeInDb);
        PlayerPrefs.SetFloat(SFX_KEY, volumeInDb);
    }

    private float ConvertToDecibels(float sliderValue)
    {
        return Mathf.Log10(Mathf.Max(sliderValue, 0.0001f)) * 20;
    }
}