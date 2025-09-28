using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement; // Required for SceneManager
using System.Collections;

public class TransitionManager : MonoBehaviour
{
    public static TransitionManager Instance { get; private set; }

    [Header("Transition Settings")]
    [SerializeField] private CanvasGroup transitionCanvasGroup;
    [SerializeField] private float fadeDuration = 0.5f;

    public bool isTransitioning = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (transitionCanvasGroup != null) transitionCanvasGroup.alpha = 0;
    }

    /// <summary>
    /// Use this for same-scene actions, like restarting a level.
    /// </summary>
    public void PlayTransition(UnityAction actionAtMidpoint)
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionCoroutine(actionAtMidpoint));
    }

    /// <summary>
    /// Use this specifically for changing scenes.
    /// </summary>
    public void TransitionToScene(string sceneName)
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionSceneCoroutine(sceneName));
    }

    private IEnumerator TransitionCoroutine(UnityAction actionAtMidpoint)
    {
        isTransitioning = true;
        transitionCanvasGroup.blocksRaycasts = true;

        yield return StartCoroutine(Fade(1f)); // Fade In
        actionAtMidpoint?.Invoke(); // Perform Action
        yield return StartCoroutine(Fade(0f)); // Fade Out

        transitionCanvasGroup.blocksRaycasts = false;
        isTransitioning = false;
    }
    
    private IEnumerator TransitionSceneCoroutine(string sceneName)
    {
        isTransitioning = true;
        transitionCanvasGroup.blocksRaycasts = true;

        yield return StartCoroutine(Fade(1f));

        yield return SceneManager.LoadSceneAsync(sceneName);

        yield return StartCoroutine(Fade(0f));

        transitionCanvasGroup.blocksRaycasts = false;
        isTransitioning = false;
    }


    private IEnumerator Fade(float targetAlpha)
    {
        // ... (This method is unchanged and correct) ...
        float startAlpha = transitionCanvasGroup.alpha;
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            transitionCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / fadeDuration);
            yield return null;
        }
        transitionCanvasGroup.alpha = targetAlpha;
    }
}