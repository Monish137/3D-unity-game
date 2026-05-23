using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class VillageHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private bool showUiOnStart;
    [SerializeField] private Vector2 panelAnchorMin = new Vector2(0.68f, 0.84f);
    [SerializeField] private Vector2 panelAnchorMax = new Vector2(0.98f, 0.95f);

    private int currentHealth;
    private bool isDestroyed;
    private GameObject uiRoot;
    private Image fillImage;
    private Text labelText;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDestroyed => isDestroyed;

    public event Action<VillageHealth, int, int> HealthChanged;
    public event Action<VillageHealth> Destroyed;

    private void Awake()
    {
        currentHealth = Mathf.Max(1, maxHealth);
        CreateUi();
        SetUiVisible(showUiOnStart);
        RefreshUi();
    }

    public void TakeDamage(int damage)
    {
        if (isDestroyed)
        {
            return;
        }

        currentHealth = Mathf.Clamp(currentHealth - Mathf.Max(1, damage), 0, Mathf.Max(1, maxHealth));
        HealthChanged?.Invoke(this, currentHealth, maxHealth);
        RefreshUi();

        if (currentHealth > 0)
        {
            return;
        }

        isDestroyed = true;
        Destroyed?.Invoke(this);
        Debug.LogWarning("VillageHealth: The village has fallen.");
    }

    public void ResetHealth()
    {
        isDestroyed = false;
        currentHealth = Mathf.Max(1, maxHealth);
        HealthChanged?.Invoke(this, currentHealth, maxHealth);
        RefreshUi();
    }

    public void SetUiVisible(bool isVisible)
    {
        if (uiRoot != null)
        {
            uiRoot.SetActive(isVisible);
        }
    }

    private void CreateUi()
    {
        if (uiRoot != null)
        {
            return;
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasObject = new GameObject("VillageHealthCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 45;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        uiRoot = new GameObject("VillageHealthPanel");
        uiRoot.transform.SetParent(canvasObject.transform, false);

        Image panelImage = uiRoot.AddComponent<Image>();
        panelImage.color = new Color(0.04f, 0.06f, 0.08f, 0.74f);

        RectTransform panelRect = uiRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = panelAnchorMin;
        panelRect.anchorMax = panelAnchorMax;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        labelText = CreateText("VillageHealthLabel", uiRoot.transform, font, 24, FontStyle.Bold);
        labelText.alignment = TextAnchor.UpperLeft;
        labelText.rectTransform.anchorMin = new Vector2(0.05f, 0.52f);
        labelText.rectTransform.anchorMax = new Vector2(0.95f, 0.94f);
        labelText.rectTransform.offsetMin = Vector2.zero;
        labelText.rectTransform.offsetMax = Vector2.zero;

        GameObject barBackObject = new GameObject("VillageHealthBarBack");
        barBackObject.transform.SetParent(uiRoot.transform, false);
        Image barBack = barBackObject.AddComponent<Image>();
        barBack.color = new Color(0.12f, 0.12f, 0.14f, 0.96f);
        RectTransform barBackRect = barBack.rectTransform;
        barBackRect.anchorMin = new Vector2(0.05f, 0.18f);
        barBackRect.anchorMax = new Vector2(0.95f, 0.42f);
        barBackRect.offsetMin = Vector2.zero;
        barBackRect.offsetMax = Vector2.zero;

        GameObject fillObject = new GameObject("VillageHealthFill");
        fillObject.transform.SetParent(barBackObject.transform, false);
        fillImage = fillObject.AddComponent<Image>();
        fillImage.color = new Color(0.18f, 0.78f, 0.38f, 1f);
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = 0;
        RectTransform fillRect = fillImage.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
    }

    private Text CreateText(string objectName, Transform parent, Font font, int fontSize, FontStyle fontStyle)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private void RefreshUi()
    {
        float healthPercent = maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;

        if (labelText != null)
        {
            labelText.text = $"Village Health: {currentHealth}/{maxHealth}";
        }

        if (fillImage != null)
        {
            fillImage.fillAmount = Mathf.Clamp01(healthPercent);
            fillImage.color = healthPercent > 0.5f
                ? new Color(0.18f, 0.78f, 0.38f, 1f)
                : healthPercent > 0.25f
                    ? new Color(0.95f, 0.70f, 0.22f, 1f)
                    : new Color(0.90f, 0.18f, 0.16f, 1f);
        }
    }
}
