using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class MenuManager : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string gameSceneName = "SampleScene";
    [SerializeField] private string mainMenuSceneName = "Mainmenu";

    [Header("Button References")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button instructionsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button backToMenuButton;

    [Header("Transition Effect")]
    [SerializeField] private CanvasGroup fadePanel;
    [SerializeField] private float transitionDuration = 1f;

    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip buttonClickSound;

    private void Start()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(StartGame);
        }

        if (instructionsButton != null)
        {
            instructionsButton.onClick.AddListener(ShowInstructions);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }

        if (backToMenuButton != null)
        {
            backToMenuButton.onClick.AddListener(BackToMainMenu);
        }

        if (fadePanel != null)
        {
            fadePanel.alpha = 0f;
            fadePanel.blocksRaycasts = false;
        }
    }

    public void StartGame()
    {
        PlayButtonSound();
        StartCoroutine(TransitionToScene(gameSceneName));
    }

    public void ShowInstructions()
    {
        PlayButtonSound();
        Debug.Log("Instructions button clicked");
    }

    public void QuitGame()
    {
        PlayButtonSound();
        StartCoroutine(QuitGameWithDelay());
    }

    public void BackToMainMenu()
    {
        PlayButtonSound();
        StartCoroutine(TransitionToScene(mainMenuSceneName));
    }

    public void LoadScene(string sceneName)
    {
        PlayButtonSound();
        StartCoroutine(TransitionToScene(sceneName));
    }

    private IEnumerator TransitionToScene(string sceneName)
    {
        if (fadePanel != null)
        {
            fadePanel.blocksRaycasts = true;
            float elapsed = 0f;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadePanel.alpha = Mathf.Lerp(0f, 1f, elapsed / transitionDuration);
                yield return null;
            }

            fadePanel.alpha = 1f;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator QuitGameWithDelay()
    {
        if (fadePanel != null)
        {
            fadePanel.blocksRaycasts = true;
            float elapsed = 0f;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadePanel.alpha = Mathf.Lerp(0f, 1f, elapsed / transitionDuration);
                yield return null;
            }
        }

        yield return null;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void PlayButtonSound()
    {
        if (AudioSystem.Instance != null)
        {
            AudioSystem.Instance.PlaySfx(buttonClickSound);
            return;
        }

        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }
}
