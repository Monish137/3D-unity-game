using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("Optional Existing UI")]
    [SerializeField] private GameObject dialogueRoot;
    [SerializeField] private Text speakerNameText;
    [SerializeField] private Text dialogueContentText;
    [SerializeField] private Text continueHintText;

    [Header("Controls")]
    [SerializeField] private KeyCode advanceKey = KeyCode.E;
    [SerializeField] private string continueHint = "按 E 继续";

    public bool IsDialogueOpen => activeDialogue != null;

    private NpcDialogue activeDialogue;
    private NpcDialogue.DialogueLine[] activeLines;
    private int currentLineIndex;

    public static DialogueManager EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        DialogueManager existingManager = FindFirstObjectByType<DialogueManager>();
        if (existingManager != null)
        {
            return existingManager;
        }

        GameObject managerObject = new GameObject("DialogueManager");
        return managerObject.AddComponent<DialogueManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dialogueRoot == null || speakerNameText == null || dialogueContentText == null)
        {
            CreateFallbackUi();
        }

        SetDialogueVisible(false);
    }

    private void Update()
    {
        if (!IsDialogueOpen)
        {
            return;
        }

        if (Input.GetKeyDown(advanceKey))
        {
            AdvanceDialogue();
        }
    }

    public void StartDialogue(NpcDialogue dialogue, NpcDialogue.DialogueLine[] lines)
    {
        if (dialogue == null || lines == null || lines.Length == 0)
        {
            return;
        }

        activeDialogue = dialogue;
        activeLines = lines;
        currentLineIndex = 0;
        SetDialogueVisible(true);
        RefreshCurrentLine();
    }

    public void AdvanceDialogue()
    {
        if (!IsDialogueOpen)
        {
            return;
        }

        currentLineIndex++;

        if (activeLines == null || currentLineIndex >= activeLines.Length)
        {
            EndDialogue();
            return;
        }

        RefreshCurrentLine();
    }

    public void EndDialogue()
    {
        NpcDialogue finishedDialogue = activeDialogue;

        activeDialogue = null;
        activeLines = null;
        currentLineIndex = 0;
        SetDialogueVisible(false);

        if (finishedDialogue != null)
        {
            finishedDialogue.NotifyDialogueFinished();
        }
    }

    private void RefreshCurrentLine()
    {
        if (activeLines == null || currentLineIndex < 0 || currentLineIndex >= activeLines.Length)
        {
            return;
        }

        NpcDialogue.DialogueLine line = activeLines[currentLineIndex];
        speakerNameText.text = string.IsNullOrWhiteSpace(line.speakerName) ? "对话" : line.speakerName;
        dialogueContentText.text = line.content;

        if (continueHintText != null)
        {
            continueHintText.text = continueHint;
        }
    }

    private void SetDialogueVisible(bool isVisible)
    {
        if (dialogueRoot != null)
        {
            dialogueRoot.SetActive(isVisible);
        }
    }

    private void CreateFallbackUi()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasObject = new GameObject("DialogueCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject panelObject = new GameObject("DialoguePanel");
        panelObject.transform.SetParent(canvasObject.transform, false);

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.82f);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.08f, 0.05f);
        panelRect.anchorMax = new Vector2(0.92f, 0.28f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        speakerNameText = CreateText("SpeakerName", panelObject.transform, font, 28, FontStyle.Bold,
            TextAnchor.UpperLeft, new Vector2(20f, -15f), new Vector2(-20f, -55f));
        dialogueContentText = CreateText("DialogueContent", panelObject.transform, font, 24, FontStyle.Normal,
            TextAnchor.UpperLeft, new Vector2(20f, -60f), new Vector2(-20f, -45f));
        continueHintText = CreateText("ContinueHint", panelObject.transform, font, 20, FontStyle.Italic,
            TextAnchor.LowerRight, new Vector2(20f, 15f), new Vector2(-20f, 45f));

        dialogueRoot = panelObject;
    }

    private Text CreateText(
        string objectName,
        Transform parent,
        Font font,
        int fontSize,
        FontStyle fontStyle,
        TextAnchor alignment,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(offsetMin.x, offsetMin.y);
        rectTransform.offsetMax = new Vector2(offsetMax.x, offsetMax.y);

        return text;
    }
}
