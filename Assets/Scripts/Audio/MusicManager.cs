using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] mainMenuMusic;
    [SerializeField] private AudioClip[] gameplayMusic;

    private List<AudioClip> currentPlaylist = new List<AudioClip>();
    private Coroutine musicCoroutine;

    private void Awake()
    {   
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); 
    }

    public void StartPlaylist(bool isInMainMenu)
    {
        AudioClip[] selectedClips = isInMainMenu ? mainMenuMusic : gameplayMusic;

        if (selectedClips.Length == 0)
        {
            Debug.LogWarning("No music clips assigned for the current game state.");
            return;
        }
        
        if (musicCoroutine != null)
        {
            StopCoroutine(musicCoroutine);
        }

        currentPlaylist = selectedClips.OrderBy(clip => Random.value).ToList();

        musicCoroutine = StartCoroutine(PlayPlaylist());
    }
    
    private IEnumerator PlayPlaylist()
    {
        foreach (AudioClip clip in currentPlaylist)
        {
            audioSource.clip = clip;
            audioSource.Play();

            yield return new WaitForSeconds(audioSource.clip.length);
        }
        

        StartCoroutine(PlayPlaylist());

    }
    
    public void StopMusic()
    {
        if (musicCoroutine != null)
        {
            StopCoroutine(musicCoroutine);
        }
        audioSource.Stop();
    }
}