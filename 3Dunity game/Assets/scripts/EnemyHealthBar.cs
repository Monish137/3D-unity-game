using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private Slider slider;
    [SerializeField] private Image fillImage;
    [SerializeField] private bool lookAtCamera = true;
    [SerializeField] private bool hideWhenFull = false;
    [SerializeField] private bool createUiIfMissing = true;
    [SerializeField] private Vector2 size = new Vector2(1.8f, 0.28f);
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.65f);
    [SerializeField] private Color fillColor = new Color(0.85f, 0.16f, 0.16f, 0.95f);

    private Camera mainCamera;
    private Canvas worldCanvas;
    private Transform uiRoot;
    private bool subscribed;

    public void AutoBind(EnemyHealth health)
    {
        enemyHealth = health;
    }

    private void Awake()
    {
        if (enemyHealth == null)
        {
            enemyHealth = GetComponentInParent<EnemyHealth>();
        }

        EnsureUiExists();
    }

    private void OnEnable()
    {
        mainCamera = Camera.main;
        TrySubscribe();
        UpdateHealthBar();
    }

    private void OnDisable()
    {
        TryUnsubscribe();
    }

    private void Update()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        UpdateHealthBar();
    }

    private void LateUpdate()
    {
        if (lookAtCamera && mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                             mainCamera.transform.rotation * Vector3.up);
        }

        if (enemyHealth != null)
        {
            transform.position = enemyHealth.GetHealthBarWorldPosition();
        }
    }

    private void UpdateHealthBar()
    {
        if (enemyHealth == null)
        {
            return;
        }

        float healthPercent = (float)enemyHealth.CurrentHealth / enemyHealth.MaxHealth;

        if (slider != null)
        {
            slider.value = healthPercent;
        }

        if (fillImage != null)
        {
            fillImage.fillAmount = healthPercent;
        }

        if (enemyHealth.IsDead)
        {
            gameObject.SetActive(false);
            return;
        }

        if (hideWhenFull)
        {
            gameObject.SetActive(healthPercent < 0.999f);
        }
    }

    private void HandleHealthChanged(EnemyHealth _, int current, int max)
    {
        if (max <= 0)
        {
            return;
        }

        float normalized = Mathf.Clamp01((float)current / max);

        if (slider != null)
        {
            slider.value = normalized;
        }

        if (fillImage != null)
        {
            fillImage.fillAmount = normalized;
        }
    }

    private void EnsureUiExists()
    {
        slider = slider != null ? slider : GetComponentInChildren<Slider>(true);

        if ((slider != null || fillImage != null) || !createUiIfMissing)
        {
            return;
        }

        GameObject uiRootObject = new GameObject("WorldHealthCanvas", typeof(RectTransform));
        uiRootObject.transform.SetParent(transform, false);
        uiRootObject.transform.localPosition = Vector3.zero;
        uiRootObject.transform.localRotation = Quaternion.identity;
        uiRootObject.transform.localScale = Vector3.one;
        uiRoot = uiRootObject.transform;

        RectTransform rootRect = uiRootObject.GetComponent<RectTransform>();
        rootRect.sizeDelta = size;

        worldCanvas = uiRootObject.AddComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.WorldSpace;
        worldCanvas.overrideSorting = true;
        worldCanvas.sortingOrder = 50;
        uiRootObject.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 24f;
        uiRootObject.AddComponent<GraphicRaycaster>();
        uiRootObject.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        GameObject backgroundObject = new GameObject("Background", typeof(RectTransform));
        backgroundObject.transform.SetParent(uiRootObject.transform, false);
        Image backgroundImage = backgroundObject.AddComponent<Image>();
        backgroundImage.color = backgroundColor;

        RectTransform backgroundRect = backgroundImage.rectTransform;
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        GameObject sliderObject = new GameObject("Slider", typeof(RectTransform));
        slider = sliderObject.AddComponent<Slider>();
        slider.interactable = false;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        RectTransform sliderRect = slider.GetComponent<RectTransform>();
        sliderRect.anchorMin = Vector2.zero;
        sliderRect.anchorMax = Vector2.one;
        sliderRect.offsetMin = new Vector2(6f, 4f);
        sliderRect.offsetMax = new Vector2(-6f, -4f);

        sliderObject.transform.SetParent(uiRootObject.transform, false);

        GameObject fillAreaObject = new GameObject("Fill Area", typeof(RectTransform));
        fillAreaObject.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillAreaObject.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = Vector2.zero;
        fillAreaRect.offsetMax = Vector2.zero;

        GameObject fillObject = new GameObject("Fill", typeof(RectTransform));
        fillObject.transform.SetParent(fillAreaObject.transform, false);
        fillImage = fillObject.AddComponent<Image>();
        fillImage.color = fillColor;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = 0;
        fillImage.fillAmount = 1f;

        RectTransform fillRect = fillImage.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        slider.fillRect = fillRect;
        slider.targetGraphic = fillImage;
    }

    private void TrySubscribe()
    {
        if (enemyHealth == null || subscribed)
        {
            return;
        }

        enemyHealth.HealthChanged += HandleHealthChanged;
        enemyHealth.Died += HandleEnemyDied;
        subscribed = true;
    }

    private void TryUnsubscribe()
    {
        if (enemyHealth == null || !subscribed)
        {
            return;
        }

        enemyHealth.HealthChanged -= HandleHealthChanged;
        enemyHealth.Died -= HandleEnemyDied;
        subscribed = false;
    }

    private void HandleEnemyDied(EnemyHealth _)
    {
        gameObject.SetActive(false);
    }
}
