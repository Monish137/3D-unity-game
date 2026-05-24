using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class AudioSystem : MonoBehaviour
{
    public static AudioSystem Instance { get; private set; }

    private const string MasterVolumeKey = "AudioSystem.MasterVolume";
    private const string MusicVolumeKey = "AudioSystem.MusicVolume";
    private const string SfxVolumeKey = "AudioSystem.SfxVolume";
    private const string MutedKey = "AudioSystem.Muted";

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Scene Music")]
    [SerializeField] private string mainMenuSceneName = "Mainmenu";
    [SerializeField] private string gameplaySceneName = "SampleScene";
    [SerializeField] private AudioClip mainMenuMusic;
    [SerializeField] private AudioClip gameplayMusic;

    [Header("Startup")]
    [SerializeField] private AudioClip defaultMusic;
    [SerializeField] private bool playDefaultMusicOnStart = true;

    [Header("Volume")]
    [SerializeField] [Range(0f, 1f)] private float defaultMasterVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float defaultMusicVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float defaultSfxVolume = 1f;

    private float masterVolume = 1f;
    private float musicVolume = 1f;
    private float sfxVolume = 1f;
    private bool muted;

    public float MasterVolume => masterVolume;
    public float MusicVolume => musicVolume;
    public float SfxVolume => sfxVolume;
    public bool IsMuted => muted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureAudioSources();
        LoadSettings();
        ApplyVolumes();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start()
    {
        if (!TryPlaySceneMusic(SceneManager.GetActiveScene().name) && playDefaultMusicOnStart && defaultMusic != null && musicSource != null && musicSource.clip == null)
        {
            PlayMusic(defaultMusic, true);
        }
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (musicSource == null || clip == null)
        {
            return;
        }

        if (musicSource.clip == clip && musicSource.isPlaying)
        {
            return;
        }

        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource == null)
        {
            return;
        }

        musicSource.Stop();
        musicSource.clip = null;
    }

    public void PlaySfx(AudioClip clip, float volumeScale = 1f)
    {
        if (sfxSource == null || clip == null)
        {
            return;
        }

        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale) * GetEffectiveVolume(sfxVolume));
    }

    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        SaveSettings();
        ApplyVolumes();
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        SaveSettings();
        ApplyVolumes();
    }

    public void SetSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        SaveSettings();
        ApplyVolumes();
    }

    public void ToggleMute()
    {
        muted = !muted;
        SaveSettings();
        ApplyVolumes();
    }

    public void SetMuted(bool value)
    {
        muted = value;
        SaveSettings();
        ApplyVolumes();
    }

    private void EnsureAudioSources()
    {
        if (musicSource == null)
        {
            GameObject musicObject = new GameObject("MusicSource");
            musicObject.transform.SetParent(transform, false);
            musicSource = musicObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
        }

        if (sfxSource == null)
        {
            GameObject sfxObject = new GameObject("SfxSource");
            sfxObject.transform.SetParent(transform, false);
            sfxSource = sfxObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
        }
    }

    private void LoadSettings()
    {
        masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, defaultMasterVolume);
        musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, defaultMusicVolume);
        sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, defaultSfxVolume);
        muted = PlayerPrefs.GetInt(MutedKey, 0) == 1;
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
        PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
        PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
        PlayerPrefs.SetInt(MutedKey, muted ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplyVolumes()
    {
        if (musicSource != null)
        {
            musicSource.volume = GetEffectiveVolume(musicVolume);
            musicSource.mute = muted;
        }

        if (sfxSource != null)
        {
            sfxSource.volume = GetEffectiveVolume(sfxVolume);
            sfxSource.mute = muted;
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        TryPlaySceneMusic(scene.name);
    }

    private bool TryPlaySceneMusic(string sceneName)
    {
        AudioClip sceneMusic = GetSceneMusic(sceneName);
        if (sceneMusic == null)
        {
            return false;
        }

        PlayMusic(sceneMusic, true);
        return true;
    }

    private AudioClip GetSceneMusic(string sceneName)
    {
        if (!string.IsNullOrWhiteSpace(mainMenuSceneName) && sceneName == mainMenuSceneName)
        {
            return mainMenuMusic;
        }

        if (!string.IsNullOrWhiteSpace(gameplaySceneName) && sceneName == gameplaySceneName)
        {
            return gameplayMusic;
        }

        return null;
    }

    private float GetEffectiveVolume(float channelVolume)
    {
        return muted ? 0f : Mathf.Clamp01(masterVolume * channelVolume);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
