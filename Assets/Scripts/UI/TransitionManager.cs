using UnityEngine;
using UnityEngine.Events; // Required for UnityAction
using System.Collections;

public class TransitionManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    public static TransitionManager Instance { get; private set; }

    [Header("Transition Settings")]
    [SerializeField] private CanvasGroup transitionCanvasGroup;
    [SerializeField] private float fadeDuration = 0.5f;

    private bool isTransitioning = false;

    void Awake()
    {
        // Implement the Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Persist across scenes/levels

        // Ensure the panel is invisible on start
        transitionCanvasGroup.alpha = 0;
    }

    public void PlayTransition(UnityAction actionAtMidpoint)
    {
        if (isTransitioning)
        {
            Debug.LogWarning("Transition already in progress.");
            return;
        }
        StartCoroutine(TransitionCoroutine(actionAtMidpoint));
    }

    private IEnumerator TransitionCoroutine(UnityAction actionAtMidpoint)
    {
        isTransitioning = true;
        transitionCanvasGroup.blocksRaycasts = true; 
        // --- 1. Fade In (to black) ---
        yield return StartCoroutine(Fade(1f));

        // --- 2. Perform the Action ---
        actionAtMidpoint?.Invoke(); // Safely invoke the provided action

        // --- 3. Fade Out (from black) ---
        yield return StartCoroutine(Fade(0f));
        transitionCanvasGroup.blocksRaycasts = false;
        isTransitioning = false;
    }

    private IEnumerator Fade(float targetAlpha)
    {
        float startAlpha = transitionCanvasGroup.alpha;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / fadeDuration);
            transitionCanvasGroup.alpha = newAlpha;
            yield return null; // Wait for the next frame
        }

        // Ensure the final alpha is set correctly
        transitionCanvasGroup.alpha = targetAlpha;
    }
}