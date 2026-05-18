using UnityEngine;
using UnityEngine.UI;

public class PlayerStatusUI : MonoBehaviour
{
    [SerializeField] private PlayerSkills playerSkills;
    [SerializeField] private float dialogueHudYOffset = 120f;
    [SerializeField] private float hudMoveSpeed = 10f;

    private Canvas canvas;
    private RectTransform rootRect;
    private Image healthFill;
    private Image manaFill;
    private Text healthText;
    private Text manaText;
    private Image skillCooldownFill;
    private Text skillCooldownText;
    private Text hintText;
    private float hintTimer;
    private Vector2 baseRootOffsetMin;
    private Vector2 baseRootOffsetMax;

    private void Awake()
    {
        if (FindObjectsByType<PlayerStatusUI>(FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        if (playerSkills == null)
        {
            playerSkills = FindFirstObjectByType<PlayerSkills>();
        }

        CreateUi();
    }

    private void OnEnable()
    {
        if (playerSkills == null)
        {
            playerSkills = FindFirstObjectByType<PlayerSkills>();
        }

        if (playerSkills == null)
        {
            return;
        }

        playerSkills.HealthChanged += HandleHealthChanged;
        playerSkills.ManaChanged += HandleManaChanged;
        playerSkills.HintRequested += HandleHintRequested;
        HandleHealthChanged(playerSkills.CurrentHealth, playerSkills.MaxHealth);
        HandleManaChanged(playerSkills.CurrentMana, playerSkills.MaxMana);
    }

    private void OnDisable()
    {
        if (playerSkills == null)
        {
            return;
        }

        playerSkills.HealthChanged -= HandleHealthChanged;
        playerSkills.ManaChanged -= HandleManaChanged;
        playerSkills.HintRequested -= HandleHintRequested;
    }

    private void Update()
    {
        UpdateCooldownUi();
        UpdateHudPosition();

        if (playerSkills != null)
        {
            HandleManaChanged(playerSkills.CurrentMana, playerSkills.MaxMana);
        }

        if (hintTimer > 0f)
        {
            hintTimer -= Time.deltaTime;
            if (hintTimer <= 0f && hintText != null)
            {
                hintText.gameObject.SetActive(false);
            }
        }
    }

    private void CreateUi()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasObject = new GameObject("PlayerStatusCanvas");
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 45;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject root = new GameObject("PlayerStatusRoot");
        root.transform.SetParent(canvasObject.transform, false);
        rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.02f, 0.03f);
        rootRect.anchorMax = new Vector2(0.27f, 0.16f);
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        baseRootOffsetMin = rootRect.offsetMin;
        baseRootOffsetMax = rootRect.offsetMax;

        Image background = root.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.55f);

        CreateBar(root.transform, font, "HP", new Vector2(18f, 62f), new Color(0.82f, 0.12f, 0.12f), out healthFill, out healthText);
        CreateBar(root.transform, font, "MP", new Vector2(18f, 28f), new Color(0.2f, 0.48f, 0.95f), out manaFill, out manaText);
        CreateSkillCooldownUi(root.transform, font);
        CreateHintUi(root.transform, font);
    }

    private void CreateBar(Transform parent, Font font, string label, Vector2 anchoredPosition, Color fillColor, out Image fillImage, out Text valueText)
    {
        GameObject labelObject = new GameObject(label + "Label");
        labelObject.transform.SetParent(parent, false);
        Text labelText = labelObject.AddComponent<Text>();
        labelText.font = font;
        labelText.fontSize = 20;
        labelText.fontStyle = FontStyle.Bold;
        labelText.color = Color.white;
        labelText.alignment = TextAnchor.MiddleLeft;
        labelText.text = label;

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(0f, 0f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.sizeDelta = new Vector2(40f, 28f);
        labelRect.anchoredPosition = anchoredPosition + new Vector2(0f, 8f);

        GameObject barBackObject = new GameObject(label + "Back");
        barBackObject.transform.SetParent(parent, false);
        Image backImage = barBackObject.AddComponent<Image>();
        backImage.color = new Color(0.12f, 0.12f, 0.12f, 0.9f);

        RectTransform backRect = barBackObject.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0f, 0f);
        backRect.anchorMax = new Vector2(0f, 0f);
        backRect.pivot = new Vector2(0f, 0.5f);
        backRect.sizeDelta = new Vector2(220f, 20f);
        backRect.anchoredPosition = anchoredPosition + new Vector2(42f, 0f);

        GameObject fillObject = new GameObject(label + "Fill");
        fillObject.transform.SetParent(barBackObject.transform, false);
        fillImage = fillObject.AddComponent<Image>();
        fillImage.sprite = backImage.sprite;
        fillImage.color = fillColor;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = 0;
        fillImage.fillAmount = 1f;

        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2f, 2f);
        fillRect.offsetMax = new Vector2(-2f, -2f);

        GameObject valueObject = new GameObject(label + "Value");
        valueObject.transform.SetParent(parent, false);
        valueText = valueObject.AddComponent<Text>();
        valueText.font = font;
        valueText.fontSize = 16;
        valueText.color = Color.white;
        valueText.alignment = TextAnchor.MiddleRight;

        RectTransform valueRect = valueObject.GetComponent<RectTransform>();
        valueRect.anchorMin = new Vector2(0f, 0f);
        valueRect.anchorMax = new Vector2(0f, 0f);
        valueRect.pivot = new Vector2(0f, 0.5f);
        valueRect.sizeDelta = new Vector2(90f, 24f);
        valueRect.anchoredPosition = anchoredPosition + new Vector2(272f, 0f);
    }

    private void CreateSkillCooldownUi(Transform parent, Font font)
    {
        GameObject iconBackObject = new GameObject("SkillCooldownBack");
        iconBackObject.transform.SetParent(parent, false);
        Image iconBack = iconBackObject.AddComponent<Image>();
        iconBack.color = new Color(0.08f, 0.08f, 0.12f, 0.92f);

        RectTransform backRect = iconBackObject.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0f, 0f);
        backRect.anchorMax = new Vector2(0f, 0f);
        backRect.pivot = new Vector2(0f, 0.5f);
        backRect.sizeDelta = new Vector2(42f, 42f);
        backRect.anchoredPosition = new Vector2(18f, 94f);

        GameObject fillObject = new GameObject("SkillCooldownFill");
        fillObject.transform.SetParent(iconBackObject.transform, false);
        skillCooldownFill = fillObject.AddComponent<Image>();
        skillCooldownFill.color = new Color(0.58f, 0.85f, 1f, 0.75f);
        skillCooldownFill.type = Image.Type.Filled;
        skillCooldownFill.fillMethod = Image.FillMethod.Radial360;
        skillCooldownFill.fillOrigin = 2;
        skillCooldownFill.fillClockwise = false;
        skillCooldownFill.fillAmount = 0f;

        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2f, 2f);
        fillRect.offsetMax = new Vector2(-2f, -2f);

        GameObject textObject = new GameObject("SkillCooldownText");
        textObject.transform.SetParent(iconBackObject.transform, false);
        skillCooldownText = textObject.AddComponent<Text>();
        skillCooldownText.font = font;
        skillCooldownText.fontSize = 14;
        skillCooldownText.fontStyle = FontStyle.Bold;
        skillCooldownText.alignment = TextAnchor.MiddleCenter;
        skillCooldownText.color = Color.white;
        skillCooldownText.text = "RMB";

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    private void CreateHintUi(Transform parent, Font font)
    {
        GameObject hintObject = new GameObject("SkillHintText");
        hintObject.transform.SetParent(parent, false);
        hintText = hintObject.AddComponent<Text>();
        hintText.font = font;
        hintText.fontSize = 16;
        hintText.fontStyle = FontStyle.Bold;
        hintText.alignment = TextAnchor.MiddleLeft;
        hintText.color = new Color(0.95f, 0.85f, 0.35f);
        hintText.gameObject.SetActive(false);

        RectTransform hintRect = hintObject.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0f, 0f);
        hintRect.anchorMax = new Vector2(0f, 0f);
        hintRect.pivot = new Vector2(0f, 0.5f);
        hintRect.sizeDelta = new Vector2(220f, 28f);
        hintRect.anchoredPosition = new Vector2(18f, 122f);
    }

    private void UpdateCooldownUi()
    {
        if (playerSkills == null || skillCooldownFill == null || skillCooldownText == null)
        {
            return;
        }

        if (!playerSkills.PhantomSkillUnlocked)
        {
            skillCooldownFill.fillAmount = 1f;
            skillCooldownText.text = "LOCK";
            return;
        }

        float remaining = playerSkills.PhantomSkillCooldownRemaining;
        if (remaining > 0f)
        {
            skillCooldownFill.fillAmount = playerSkills.PhantomSkillCooldownNormalized;
            skillCooldownText.text = remaining.ToString("0.0");
        }
        else
        {
            skillCooldownFill.fillAmount = 0f;
            skillCooldownText.text = "RMB";
        }
    }

    private void HandleHealthChanged(int current, int max)
    {
        if (healthFill != null)
        {
            healthFill.fillAmount = max <= 0 ? 0f : (float)current / max;
        }

        if (healthText != null)
        {
            healthText.text = $"{current}/{max}";
        }
    }

    private void HandleManaChanged(int current, int max)
    {
        if (manaFill != null)
        {
            manaFill.fillAmount = max <= 0 ? 0f : (float)current / max;
        }

        if (manaText != null)
        {
            manaText.text = $"{current}/{max}";
        }
    }

    private void HandleHintRequested(string message)
    {
        if (hintText == null)
        {
            return;
        }

        hintText.text = message;
        hintText.gameObject.SetActive(true);
        hintTimer = 1.2f;
    }

    private void UpdateHudPosition()
    {
        if (rootRect == null)
        {
            return;
        }

        bool dialogueOpen = DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueOpen;
        float targetYOffset = dialogueOpen ? dialogueHudYOffset : 0f;

        Vector2 targetMin = baseRootOffsetMin + new Vector2(0f, targetYOffset);
        Vector2 targetMax = baseRootOffsetMax + new Vector2(0f, targetYOffset);

        rootRect.offsetMin = Vector2.Lerp(rootRect.offsetMin, targetMin, Time.deltaTime * hudMoveSpeed);
        rootRect.offsetMax = Vector2.Lerp(rootRect.offsetMax, targetMax, Time.deltaTime * hudMoveSpeed);
    }
}
