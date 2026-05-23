using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }

    public enum MissionType
    {
        ReachTrigger,
        CollectItems,
        TalkToNpc,
        DeliverItems
    }

    [System.Serializable]
    public class Mission
    {
        public string id;
        public string title;
        [TextArea] public string description;
        public MissionType missionType;
        public int requiredAmount = 1;
        public UnityEvent onMissionStarted;
        public UnityEvent onMissionCompleted;

        [HideInInspector] public int currentAmount;
        [HideInInspector] public bool isCompleted;
    }

    [Header("Mission Data")]
    [SerializeField] private List<Mission> missions = new List<Mission>();
    [SerializeField] private int startingMissionIndex;

    [Header("Optional UI")]
    [SerializeField] private Text missionTitleText;
    [SerializeField] private Text missionDescriptionText;
    [SerializeField] private Text missionProgressText;

    [Header("Debug")]
    [SerializeField] private KeyCode skipMissionKey = KeyCode.P;

    public Mission CurrentMission =>
        currentMissionIndex >= 0 && currentMissionIndex < missions.Count ? missions[currentMissionIndex] : null;

    public int CurrentMissionIndex => currentMissionIndex;

    public Mission GetMissionByIndex(int index)
    {
        if (index < 0 || index >= missions.Count)
        {
            return null;
        }

        return missions[index];
    }

    private int currentMissionIndex = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple MissionManager instances found. The latest one will be used.");
        }

        Instance = this;

        if (missionTitleText == null || missionDescriptionText == null || missionProgressText == null)
        {
            CreateFallbackUi();
        }

        EnsureObjectiveTrackerExists();
        EnsureChapterOneAutoSetupExists();
    }

    private void Start()
    {
        if (missions.Count == 0)
        {
            UpdateUI();
            return;
        }

        StartMission(startingMissionIndex);
    }

    private void Update()
    {
        if (Input.GetKeyDown(skipMissionKey))
        {
            SkipCurrentMission();
        }
    }

    public void StartMission(int missionIndex)
    {
        if (missionIndex < 0 || missionIndex >= missions.Count)
        {
            currentMissionIndex = -1;
            UpdateUI();
            return;
        }

        currentMissionIndex = missionIndex;
        Mission mission = missions[currentMissionIndex];
        mission.currentAmount = 0;
        mission.isCompleted = false;
        mission.onMissionStarted?.Invoke();
        UpdateUI();
    }

    public void AdvanceToNextMission()
    {
        StartMission(currentMissionIndex + 1);
    }

    public void SkipCurrentMission()
    {
        Mission mission = CurrentMission;
        if (mission == null)
        {
            return;
        }

        Debug.Log($"MissionManager: Skipping mission '{mission.id}' via debug key.");
        CompleteCurrentMission();
    }

    public void CompleteCurrentMission()
    {
        Mission mission = CurrentMission;
        if (mission == null || mission.isCompleted)
        {
            return;
        }

        mission.isCompleted = true;
        mission.currentAmount = Mathf.Max(mission.currentAmount, mission.requiredAmount);
        mission.onMissionCompleted?.Invoke();
        HandleMissionRewards(mission);
        UpdateUI();
        AdvanceToNextMission();
    }

    public bool IsMissionCompleted(string missionId)
    {
        if (string.IsNullOrWhiteSpace(missionId))
        {
            return false;
        }

        int missionIndex = GetMissionIndex(missionId);
        return missionIndex >= 0 && missions[missionIndex].isCompleted;
    }

    private void HandleMissionRewards(Mission mission)
    {
        if (mission == null)
        {
            return;
        }

        if (mission.id == "talk_to_merlin")
        {
            PlayerSkills playerSkills = FindFirstObjectByType<PlayerSkills>();
            if (playerSkills != null)
            {
                playerSkills.UnlockPhantomSkill();
            }
        }
    }

    public void AddProgress(string missionId, int amount = 1)
    {
        Mission mission = CurrentMission;
        if (mission == null || mission.isCompleted || mission.id != missionId)
        {
            return;
        }

        mission.currentAmount = Mathf.Clamp(mission.currentAmount + amount, 0, Mathf.Max(1, mission.requiredAmount));
        UpdateUI();

        if (mission.currentAmount >= mission.requiredAmount)
        {
            CompleteCurrentMission();
        }
    }

    public bool IsCurrentMission(string missionId)
    {
        return CurrentMission != null && CurrentMission.id == missionId;
    }

    public int GetMissionIndex(string missionId)
    {
        for (int i = 0; i < missions.Count; i++)
        {
            if (missions[i].id == missionId)
            {
                return i;
            }
        }

        return -1;
    }

    [ContextMenu("Load Chapter One Missions")]
    public void LoadChapterOneMissions()
    {
        missions.Clear();

        missions.Add(CreateMission(
            "go_to_ector",
            "去见艾克特",
            "村民说艾克特正在找你，前往他的住所与他见面。",
            MissionType.ReachTrigger,
            1));

        missions.Add(CreateMission(
            "pick_up_sword",
            "领取新剑",
            "前往铁匠铺附近，拿起为你打造的新剑。",
            MissionType.CollectItems,
            1));

        missions.Add(CreateMission(
            "hunt_for_food",
            "收集猎物",
            "进入森林，收集足够的猎物带回村庄。",
            MissionType.CollectItems,
            3));

        missions.Add(CreateMission(
            "return_to_ector",
            "回去复命",
            "带着猎物返回村庄，向艾克特报告狩猎结果。",
            MissionType.ReachTrigger,
            1));

        missions.Add(CreateMission(
            "read_notice_board",
            "查看告示",
            "前往村庄中央的布告栏，查看新张贴的告示。",
            MissionType.ReachTrigger,
            1));

        missions.Add(CreateMission(
            "talk_to_merlin",
            "询问梅林",
            "前往布告栏附近，找到梅林并了解发生了什么。",
            MissionType.TalkToNpc,
            1));

        missions.Add(CreateMission(
            "warn_ector",
            "通知艾克特",
            "把强盗即将来袭的消息告诉艾克特。",
            MissionType.TalkToNpc,
            1));

        missions.Add(CreateMission(
            "arm_villagers",
            "收集并交付武器",
            "前往 Armed weapons 收集 4 份武器补给，再回去交给村民。",
            MissionType.DeliverItems,
            4));

        missions.Add(CreateMission(
            "defend_village",
            "保卫村庄",
            "前往村口迎战入侵者，保护村民和家园。",
            MissionType.ReachTrigger,
            1));

        missions.Add(CreateMission(
            "talk_after_battle",
            "战后交谈",
            "战斗结束后，与梅林和艾克特交谈，了解真相。",
            MissionType.TalkToNpc,
            1));

        startingMissionIndex = 0;
        currentMissionIndex = -1;
        UpdateUI();
    }

    private Mission CreateMission(
        string id,
        string title,
        string description,
        MissionType missionType,
        int requiredAmount)
    {
        return new Mission
        {
            id = id,
            title = title,
            description = description,
            missionType = missionType,
            requiredAmount = requiredAmount
        };
    }

    private void UpdateUI()
    {
        if (CurrentMission == null)
        {
            if (missionTitleText != null) missionTitleText.text = "All Missions Complete";
            if (missionDescriptionText != null) missionDescriptionText.text = "You have finished the current mission chain.";
            if (missionProgressText != null) missionProgressText.text = string.Empty;
            return;
        }

        if (missionTitleText != null) missionTitleText.text = CurrentMission.title;
        if (missionDescriptionText != null) missionDescriptionText.text = CurrentMission.description;
        if (missionProgressText != null)
        {
            missionProgressText.text = $"{CurrentMission.currentAmount}/{Mathf.Max(1, CurrentMission.requiredAmount)}";
        }
    }

    private void CreateFallbackUi()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasObject = new GameObject("MissionCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject panelObject = new GameObject("MissionPanel");
        panelObject.transform.SetParent(canvasObject.transform, false);

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.58f);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.02f, 0.78f);
        panelRect.anchorMax = new Vector2(0.32f, 0.97f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        missionTitleText = CreateText("MissionTitle", panelObject.transform, font, 28, FontStyle.Bold,
            TextAnchor.UpperLeft, new Vector2(18f, -12f), new Vector2(-18f, -52f));
        missionDescriptionText = CreateText("MissionDescription", panelObject.transform, font, 22, FontStyle.Normal,
            TextAnchor.UpperLeft, new Vector2(18f, -60f), new Vector2(-18f, -100f));
        missionProgressText = CreateText("MissionProgress", panelObject.transform, font, 22, FontStyle.Bold,
            TextAnchor.LowerRight, new Vector2(18f, 12f), new Vector2(-18f, 46f));
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

    private void EnsureObjectiveTrackerExists()
    {
        if (FindFirstObjectByType<ObjectiveTracker>() != null)
        {
            return;
        }

        GameObject trackerObject = new GameObject("ObjectiveTracker");
        trackerObject.AddComponent<ObjectiveTracker>();
    }

    private void EnsureChapterOneAutoSetupExists()
    {
        if (FindFirstObjectByType<ChapterOneAutoSetup>() != null)
        {
            return;
        }

        GameObject setupObject = new GameObject("ChapterOneAutoSetup");
        setupObject.AddComponent<ChapterOneAutoSetup>();
    }
}
