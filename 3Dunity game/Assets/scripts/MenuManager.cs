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
        // Hook up button events
        if (startButton != null)
            startButton.onClick.AddListener(StartGame);

        if (instructionsButton != null)
            instructionsButton.onClick.AddListener(ShowInstructions);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

        if (backToMenuButton != null)
            backToMenuButton.onClick.AddListener(BackToMainMenu);  // ← This is the function

        // Ensure fade panel starts transparent
        if (fadePanel != null)
        {
            fadePanel.alpha = 0;
            fadePanel.blocksRaycasts = false;
        }
    }

    // Called when Start button is clicked
    public void StartGame()
    {
        PlayButtonSound();
        StartCoroutine(TransitionToScene(gameSceneName));
    }

    // Called when Instructions button is clicked
    public void ShowInstructions()
    {
        PlayButtonSound();
        // The InstructionSystem will handle showing the panel
        Debug.Log("Instructions button clicked");
    }

    // Called when Quit button is clicked
    public void QuitGame()
    {
        PlayButtonSound();
        StartCoroutine(QuitGameWithDelay());
    }

    // Called when Back button is clicked - THIS IS THE FUNCTION YOU NEED
    public void BackToMainMenu()
    {
        PlayButtonSound();
        StartCoroutine(TransitionToScene(mainMenuSceneName));
    }

    // Alternative: If you want to use LoadScene directly
    public void LoadScene(string sceneName)
    {
        PlayButtonSound();
        StartCoroutine(TransitionToScene(sceneName));
    }

    // Transition to a scene with fade effect
    private IEnumerator TransitionToScene(string sceneName)
    {
        // Fade out
        if (fadePanel != null)
        {
            fadePanel.blocksRaycasts = true;
            float elapsed = 0;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                fadePanel.alpha = Mathf.Lerp(0, 1, elapsed / transitionDuration);
                yield return null;
            }

            fadePanel.alpha = 1;
        }

        // Load the scene
        SceneManager.LoadScene(sceneName);
    }

    // Quit game with delay
    private IEnumerator QuitGameWithDelay()
    {
        if (fadePanel != null)
        {
            fadePanel.blocksRaycasts = true;
            float elapsed = 0;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                fadePanel.alpha = Mathf.Lerp(0, 1, elapsed / transitionDuration);
                yield return null;
            }
        }

        yield return new WaitForSeconds(0.1f);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

    // Play button click sound
    private void PlayButtonSound()
    {
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }
}