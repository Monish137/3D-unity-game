using UnityEngine;
using UnityEngine.UI;
using SUPERCharacter;

[RequireComponent(typeof(Collider))]
public class NpcDialogue : MonoBehaviour
{
    [System.Serializable]
    public class DialogueLine
    {
        public string speakerName;

        [TextArea(2, 5)]
        public string content;
    }

    [Header("Dialogue")]
    [SerializeField] private DialogueLine[] dialogueLines;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private bool oneTimeOnly;

    [Header("Prompt")]
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private Text promptText;
    [SerializeField] private string promptMessage = "按 E 对话";

    [Header("Optional Mission")]
    [SerializeField] private MissionManager missionManager;
    [SerializeField] private string missionId;
    [SerializeField] private bool completeMissionWhenDialogueEnds = true;

    private static GameObject runtimePromptRoot;
    private static Text runtimePromptText;
    private static NpcDialogue runtimePromptOwner;

    private bool playerInRange;
    private bool hasCompletedDialogue;
    private bool dialogueStarted;

    private void Reset()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void Awake()
    {
        if (missionManager == null)
        {
            missionManager = MissionManager.Instance != null
                ? MissionManager.Instance
                : FindFirstObjectByType<MissionManager>();
        }

        if (promptRoot == null || promptText == null)
        {
            EnsureRuntimePromptUi();
        }

        SetPromptVisible(false);
    }

    private void Update()
    {
        if (!playerInRange || dialogueStarted)
        {
            return;
        }

        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueOpen)
        {
            SetPromptVisible(false);
            return;
        }

        SetPromptVisible(true);

        if (Input.GetKeyDown(interactionKey))
        {
            StartDialogue();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayerCollider(other))
        {
            return;
        }

        if (oneTimeOnly && hasCompletedDialogue)
        {
            return;
        }

        playerInRange = true;
        UpdatePromptText();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayerCollider(other))
        {
            return;
        }

        playerInRange = false;
        SetPromptVisible(false);
    }

    private void StartDialogue()
    {
        DialogueManager dialogueManager = DialogueManager.EnsureInstance();

        if (dialogueManager == null || dialogueLines == null || dialogueLines.Length == 0)
        {
            Debug.LogWarning($"NpcDialogue on {name} cannot start because dialogue data or DialogueManager is missing.");
            return;
        }

        dialogueStarted = true;
        SetPromptVisible(false);
        dialogueManager.StartDialogue(this, dialogueLines);
    }

    public void NotifyDialogueFinished()
    {
        dialogueStarted = false;
        hasCompletedDialogue = true;

        if (completeMissionWhenDialogueEnds &&
            missionManager != null &&
            !string.IsNullOrWhiteSpace(missionId) &&
            missionManager.IsCurrentMission(missionId))
        {
            missionManager.AddProgress(missionId, 1);
        }

        if (oneTimeOnly)
        {
            SetPromptVisible(false);
            return;
        }

        if (playerInRange)
        {
            SetPromptVisible(true);
        }
    }

    private void UpdatePromptText()
    {
        Text activePromptText = GetPromptText();

        if (activePromptText != null)
        {
            activePromptText.text = promptMessage;
        }
    }

    private void SetPromptVisible(bool isVisible)
    {
        GameObject activePromptRoot = GetPromptRoot();

        if (activePromptRoot == null)
        {
            return;
        }

        if (promptRoot != null)
        {
            promptRoot.SetActive(isVisible);
            return;
        }

        if (isVisible)
        {
            runtimePromptOwner = this;
            UpdatePromptText();
            activePromptRoot.SetActive(true);
        }
        else if (runtimePromptOwner == this)
        {
            runtimePromptOwner = null;
            activePromptRoot.SetActive(false);
        }
    }

    private GameObject GetPromptRoot()
    {
        return promptRoot != null ? promptRoot : runtimePromptRoot;
    }

    private Text GetPromptText()
    {
        return promptText != null ? promptText : runtimePromptText;
    }

    private static void EnsureRuntimePromptUi()
    {
        if (runtimePromptRoot != null && runtimePromptText != null)
        {
            return;
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasObject = new GameObject("InteractionPromptCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasObject.AddComponent<GraphicRaycaster>();

        runtimePromptRoot = new GameObject("InteractionPromptPanel");
        runtimePromptRoot.transform.SetParent(canvasObject.transform, false);

        Image panelImage = runtimePromptRoot.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.65f);

        RectTransform panelRect = runtimePromptRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.39f, 0.13f);
        panelRect.anchorMax = new Vector2(0.61f, 0.19f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        GameObject textObject = new GameObject("InteractionPromptText");
        textObject.transform.SetParent(runtimePromptRoot.transform, false);

        runtimePromptText = textObject.AddComponent<Text>();
        runtimePromptText.font = font;
        runtimePromptText.fontSize = 24;
        runtimePromptText.fontStyle = FontStyle.Bold;
        runtimePromptText.alignment = TextAnchor.MiddleCenter;
        runtimePromptText.color = Color.white;
        runtimePromptText.horizontalOverflow = HorizontalWrapMode.Wrap;
        runtimePromptText.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(16f, 8f);
        textRect.offsetMax = new Vector2(-16f, -8f);

        runtimePromptRoot.SetActive(false);
    }

    private bool IsPlayerCollider(Collider other)
    {
        if (other == null)
        {
            return false;
        }

        if (other.CompareTag("Player"))
        {
            return true;
        }

        if (other.GetComponentInParent<SUPERCharacterAIO>() != null)
        {
            return true;
        }

        string objectName = other.transform.root.name;
        return !string.IsNullOrWhiteSpace(objectName) &&
               objectName.ToLowerInvariant().Contains("player");
    }
}
