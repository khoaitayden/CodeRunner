using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // Required for OrderBy

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
        DontDestroyOnLoad(gameObject); // You should keep DontDestroyOnLoad for a music manager
    }

    /// <summary>
    /// Starts a new shuffled playlist based on the game state.
    /// </summary>
    /// <param name="isInMainMenu">True for main menu music, false for gameplay music.</param>
    public void StartPlaylist(bool isInMainMenu)
    {
        AudioClip[] selectedClips = isInMainMenu ? mainMenuMusic : gameplayMusic;

        if (selectedClips.Length == 0)
        {
            Debug.LogWarning("No music clips assigned for the current game state.");
            return;
        }
        
        // Stop any currently playing music
        if (musicCoroutine != null)
        {
            StopCoroutine(musicCoroutine);
        }

        // Shuffle the selected clips into a new playlist
        currentPlaylist = selectedClips.OrderBy(clip => Random.value).ToList();

        // Start the coroutine that will manage the playlist
        musicCoroutine = StartCoroutine(PlayPlaylist());
    }
    
    private IEnumerator PlayPlaylist()
    {
        // Loop through each song in the shuffled playlist
        foreach (AudioClip clip in currentPlaylist)
        {
            audioSource.clip = clip;
            audioSource.Play();

            // Wait until the current song has finished playing before moving to the next one
            yield return new WaitForSeconds(audioSource.clip.length);
        }
        
        // --- After the playlist finishes, decide what to do ---
        // Option A: Restart the shuffled playlist (most common)
        StartCoroutine(PlayPlaylist());

        // Option B: Stop music (less common)
        // Debug.Log("Playlist finished.");
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