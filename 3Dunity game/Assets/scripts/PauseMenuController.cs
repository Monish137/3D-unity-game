using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PauseMenuController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject pauseMenuRoot;
    [SerializeField] private GameObject audioPanel;
    [SerializeField] private GameObject deathMenuRoot;

    [Header("Gameplay")]
    [SerializeField] private PlayerSkills playerSkills;

    [Header("Scene Flow")]
    [SerializeField] private string mainMenuSceneName = "Mainmenu";
    [SerializeField] private MenuManager menuManager;

    [Header("Audio")]
    [SerializeField] private AudioClip openPauseSound;
    [SerializeField] private AudioClip closePauseSound;
    [SerializeField] private AudioClip buttonClickSound;

    private bool isPaused;
    private bool isDeathMenuOpen;
    private CursorLockMode previousLockMode = CursorLockMode.Locked;
    private bool previousCursorVisible;
    private Canvas pauseCanvas;
    private CanvasGroup pauseCanvasGroup;
    private GraphicRaycaster pauseRaycaster;

    private void Awake()
    {
        if (menuManager == null)
        {
            menuManager = FindFirstObjectByType<MenuManager>();
        }

        if (playerSkills == null)
        {
            playerSkills = FindFirstObjectByType<PlayerSkills>();
        }

        if (pauseMenuRoot != null)
        {
            pauseCanvas = pauseMenuRoot.GetComponent<Canvas>();
            pauseCanvasGroup = pauseMenuRoot.GetComponent<CanvasGroup>();
            pauseRaycaster = pauseMenuRoot.GetComponent<GraphicRaycaster>();
        }

        SetPauseMenuVisible(false);

        if (audioPanel != null)
        {
            audioPanel.SetActive(false);
        }

        if (deathMenuRoot != null)
        {
            deathMenuRoot.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (playerSkills == null)
        {
            playerSkills = FindFirstObjectByType<PlayerSkills>();
        }

        if (playerSkills != null)
        {
            playerSkills.Died += ShowDeathMenu;
        }
    }

    private void Update()
    {
        if (isDeathMenuOpen)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        if (isPaused || isDeathMenuOpen)
        {
            return;
        }

        isPaused = true;
        previousLockMode = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SetPauseMenuVisible(true);

        if (audioPanel != null)
        {
            audioPanel.SetActive(false);
        }

        PlayUiSound(openPauseSound);
    }

    public void ResumeGame()
    {
        if (!isPaused || isDeathMenuOpen)
        {
            return;
        }

        PlayUiSound(closePauseSound != null ? closePauseSound : buttonClickSound);
        RestoreGameplayState(true);
    }

    public void ReturnToMainMenu()
    {
        PlayUiSound(buttonClickSound);
        RestoreGameplayState(false);

        if (AudioSystem.Instance != null)
        {
            AudioSystem.Instance.StopMusic();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (menuManager != null)
        {
            menuManager.BackToMainMenu();
            return;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        PlayUiSound(buttonClickSound);
        RestoreGameplayState();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ToggleAudioPanel()
    {
        PlayUiSound(buttonClickSound);

        if (audioPanel == null)
        {
            return;
        }

        audioPanel.SetActive(!audioPanel.activeSelf);
    }

    public void ShowDeathMenu()
    {
        if (isDeathMenuOpen)
        {
            return;
        }

        isDeathMenuOpen = true;
        isPaused = false;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SetPauseMenuVisible(false);

        if (audioPanel != null)
        {
            audioPanel.SetActive(false);
        }

        if (deathMenuRoot != null)
        {
            deathMenuRoot.SetActive(true);
        }
    }

    public void HideDeathMenu()
    {
        isDeathMenuOpen = false;

        if (deathMenuRoot != null)
        {
            deathMenuRoot.SetActive(false);
        }
    }

    public void ReviveInPlace()
    {
        if (playerSkills == null)
        {
            return;
        }

        PlayUiSound(buttonClickSound);
        playerSkills.ReviveInPlace();
        HideDeathMenu();
        Cursor.lockState = previousLockMode;
        Cursor.visible = previousCursorVisible;
        Time.timeScale = 1f;
    }

    public void QuitFromDeathMenu()
    {
        PlayUiSound(buttonClickSound);
        HideDeathMenu();
        QuitGame();
    }

    public void SetMasterVolume(float value)
    {
        if (AudioSystem.Instance != null)
        {
            AudioSystem.Instance.SetMasterVolume(value);
        }
    }

    public void SetMusicVolume(float value)
    {
        if (AudioSystem.Instance != null)
        {
            AudioSystem.Instance.SetMusicVolume(value);
        }
    }

    public void SetSfxVolume(float value)
    {
        if (AudioSystem.Instance != null)
        {
            AudioSystem.Instance.SetSfxVolume(value);
        }
    }

    public void ToggleMute()
    {
        if (AudioSystem.Instance != null)
        {
            AudioSystem.Instance.ToggleMute();
        }
    }

    private void RestoreGameplayState(bool restorePreviousCursorState = true)
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (restorePreviousCursorState)
        {
            Cursor.lockState = previousLockMode;
            Cursor.visible = previousCursorVisible;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        SetPauseMenuVisible(false);

        if (audioPanel != null)
        {
            audioPanel.SetActive(false);
        }
    }

    private void SetPauseMenuVisible(bool visible)
    {
        if (pauseMenuRoot == null)
        {
            return;
        }

        pauseMenuRoot.SetActive(true);

        if (pauseCanvas != null)
        {
            pauseCanvas.enabled = visible;
        }

        if (pauseCanvasGroup != null)
        {
            pauseCanvasGroup.alpha = visible ? 1f : 0f;
            pauseCanvasGroup.interactable = visible;
            pauseCanvasGroup.blocksRaycasts = visible;
        }

        if (pauseRaycaster != null)
        {
            pauseRaycaster.enabled = visible;
        }

        Selectable[] selectables = pauseMenuRoot.GetComponentsInChildren<Selectable>(true);
        for (int i = 0; i < selectables.Length; i++)
        {
            if (selectables[i] != null)
            {
                selectables[i].interactable = visible;
            }
        }

        if (!visible)
        {
            pauseMenuRoot.SetActive(false);
        }
    }

    private void PlayUiSound(AudioClip clip)
    {
        if (AudioSystem.Instance != null)
        {
            AudioSystem.Instance.PlaySfx(clip);
        }
    }

    private void OnDisable()
    {
        if (playerSkills != null)
        {
            playerSkills.Died -= ShowDeathMenu;
        }

        if (isPaused)
        {
            RestoreGameplayState(true);
        }
    }

    private void OnDestroy()
    {
        if (playerSkills != null)
        {
            playerSkills.Died -= ShowDeathMenu;
        }

        if (isPaused)
        {
            RestoreGameplayState(true);
        }
    }
}
