using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class InstructionSystem : MonoBehaviour
{
    [Header("Panel References")]
    public GameObject instructionsPanel;
    public Button instructionsButton;
    public Button backButton;

    [Header("Animation")]
    public float fadeDuration = 0.3f;
    public bool useScaleAnimation = true;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector3 originalScale;
    private bool isOpen = false;

    void Start()
    {
        // Get components
        if (instructionsPanel != null)
        {
            canvasGroup = instructionsPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = instructionsPanel.AddComponent<CanvasGroup>();

            rectTransform = instructionsPanel.GetComponent<RectTransform>();
            originalScale = rectTransform.localScale;

            // Start hidden
            instructionsPanel.SetActive(false);
            canvasGroup.alpha = 0;
        }

        // Connect buttons
        if (instructionsButton != null)
            instructionsButton.onClick.AddListener(ShowInstructions);

        if (backButton != null)
            backButton.onClick.AddListener(HideInstructions);
    }

    void Update()
    {
        // Close with Escape key
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
            HideInstructions();

        // Also close with B key (toggle)
        if (isOpen && Input.GetKeyDown(KeyCode.B))
            HideInstructions();
    }

    public void ShowInstructions()
    {
        if (isOpen) return;

        instructionsPanel.SetActive(true);

        if (useScaleAnimation)
            StartCoroutine(ShowWithScale());
        else
            StartCoroutine(Fade(0, 1, fadeDuration));

        isOpen = true;
    }

    public void HideInstructions()
    {
        if (!isOpen) return;

        if (useScaleAnimation)
            StartCoroutine(HideWithScale());
        else
            StartCoroutine(Fade(1, 0, fadeDuration, () => {
                instructionsPanel.SetActive(false);
            }));

        isOpen = false;
    }

    IEnumerator ShowWithScale()
    {
        rectTransform.localScale = new Vector3(0.8f, 0.8f, 1);
        canvasGroup.alpha = 0;

        float elapsed = 0;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            canvasGroup.alpha = Mathf.Lerp(0, 1, t);
            rectTransform.localScale = Vector3.Lerp(new Vector3(0.8f, 0.8f, 1), originalScale, t);
            yield return null;
        }

        canvasGroup.alpha = 1;
        rectTransform.localScale = originalScale;
    }

    IEnumerator HideWithScale()
    {
        float elapsed = 0;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            canvasGroup.alpha = Mathf.Lerp(1, 0, t);
            rectTransform.localScale = Vector3.Lerp(originalScale, new Vector3(0.8f, 0.8f, 1), t);
            yield return null;
        }

        canvasGroup.alpha = 0;
        instructionsPanel.SetActive(false);
    }

    IEnumerator Fade(float startAlpha, float endAlpha, float duration, System.Action onComplete = null)
    {
        float elapsed = 0;
        canvasGroup.alpha = startAlpha;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = endAlpha;
        onComplete?.Invoke();
    }
}