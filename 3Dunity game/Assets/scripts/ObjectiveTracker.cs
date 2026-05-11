using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class ObjectiveTracker : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Vector2 objectiveTextAnchor = new Vector2(0.5f, 0.94f);
    [SerializeField] private Vector2 markerClampPadding = new Vector2(90f, 80f);
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.3f, 0f);
    [SerializeField] private Color markerColor = new Color(1f, 0.84f, 0.2f, 1f);

    private MissionManager missionManager;
    private Camera gameplayCamera;
    private string cachedMissionId;
    private Transform currentTarget;

    private Canvas canvas;
    private Text objectiveText;
    private RectTransform markerRoot;
    private Text markerIconText;
    private Text markerLabelText;

    private static readonly BindingFlags PrivateInstanceFlags =
        BindingFlags.NonPublic | BindingFlags.Instance;

    private void Awake()
    {
        missionManager = MissionManager.Instance != null
            ? MissionManager.Instance
            : FindFirstObjectByType<MissionManager>();

        CreateUi();
    }

    private void LateUpdate()
    {
        if (missionManager == null)
        {
            missionManager = MissionManager.Instance != null
                ? MissionManager.Instance
                : FindFirstObjectByType<MissionManager>();
        }

        MissionManager.Mission currentMission = missionManager != null ? missionManager.CurrentMission : null;
        string missionId = currentMission != null ? currentMission.id : string.Empty;

        if (cachedMissionId != missionId || currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
        {
            cachedMissionId = missionId;
            currentTarget = FindTargetForMission(missionId);
        }

        UpdateUi(currentMission);
    }

    private void CreateUi()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasObject = new GameObject("ObjectiveTrackerCanvas");
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasObject.AddComponent<GraphicRaycaster>();

        objectiveText = CreateText("ObjectiveText", canvasObject.transform, font, 26, FontStyle.Bold);
        RectTransform objectiveRect = objectiveText.rectTransform;
        objectiveRect.anchorMin = objectiveTextAnchor;
        objectiveRect.anchorMax = objectiveTextAnchor;
        objectiveRect.pivot = new Vector2(0.5f, 0.5f);
        objectiveRect.anchoredPosition = Vector2.zero;
        objectiveRect.sizeDelta = new Vector2(900f, 60f);
        objectiveText.alignment = TextAnchor.MiddleCenter;

        GameObject markerObject = new GameObject("ObjectiveMarker");
        markerObject.transform.SetParent(canvasObject.transform, false);
        markerRoot = markerObject.AddComponent<RectTransform>();
        markerRoot.sizeDelta = new Vector2(220f, 70f);

        markerIconText = CreateText("MarkerIcon", markerRoot, font, 34, FontStyle.Bold);
        markerIconText.alignment = TextAnchor.MiddleCenter;
        markerIconText.color = markerColor;
        markerIconText.text = "◆";
        markerIconText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        markerIconText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        markerIconText.rectTransform.pivot = new Vector2(0.5f, 1f);
        markerIconText.rectTransform.anchoredPosition = new Vector2(0f, 0f);
        markerIconText.rectTransform.sizeDelta = new Vector2(80f, 40f);

        markerLabelText = CreateText("MarkerLabel", markerRoot, font, 20, FontStyle.Bold);
        markerLabelText.alignment = TextAnchor.MiddleCenter;
        markerLabelText.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        markerLabelText.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        markerLabelText.rectTransform.pivot = new Vector2(0.5f, 0f);
        markerLabelText.rectTransform.anchoredPosition = new Vector2(0f, 0f);
        markerLabelText.rectTransform.sizeDelta = new Vector2(220f, 32f);

        markerRoot.gameObject.SetActive(false);
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

    private void UpdateUi(MissionManager.Mission currentMission)
    {
        if (currentMission == null)
        {
            objectiveText.text = "当前没有任务";
            markerRoot.gameObject.SetActive(false);
            return;
        }

        gameplayCamera = GetGameplayCamera();

        if (currentTarget == null || gameplayCamera == null)
        {
            objectiveText.text = $"当前任务：{currentMission.title}";
            markerRoot.gameObject.SetActive(false);
            return;
        }

        Vector3 targetWorldPosition = currentTarget.position + worldOffset;
        Vector3 screenPoint = gameplayCamera.WorldToScreenPoint(targetWorldPosition);
        Vector3 rawDirection = currentTarget.position - gameplayCamera.transform.position;
        float distance = Vector3.Distance(gameplayCamera.transform.position, currentTarget.position);

        objectiveText.text = $"当前任务：{currentMission.title}  ({distance:0}m)";

        bool isBehindCamera = Vector3.Dot(gameplayCamera.transform.forward, rawDirection) < 0f;
        if (isBehindCamera)
        {
            screenPoint *= -1f;
        }

        float clampedX = Mathf.Clamp(screenPoint.x, markerClampPadding.x, Screen.width - markerClampPadding.x);
        float clampedY = Mathf.Clamp(screenPoint.y, markerClampPadding.y, Screen.height - markerClampPadding.y);
        markerRoot.position = new Vector3(clampedX, clampedY, 0f);
        markerLabelText.text = currentMission.title;
        markerRoot.gameObject.SetActive(true);
    }

    private Camera GetGameplayCamera()
    {
        if (gameplayCamera != null && gameplayCamera.isActiveAndEnabled && gameplayCamera.targetTexture == null)
        {
            return gameplayCamera;
        }

        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera camera = cameras[i];
            if (!camera.isActiveAndEnabled || camera.targetTexture != null)
            {
                continue;
            }

            string lowerName = camera.name.ToLowerInvariant();
            if (lowerName.Contains("mini"))
            {
                continue;
            }

            gameplayCamera = camera;
            break;
        }

        return gameplayCamera;
    }

    private Transform FindTargetForMission(string missionId)
    {
        if (string.IsNullOrWhiteSpace(missionId))
        {
            return null;
        }

        Transform target = FindTargetFromObjects(FindObjectsByType<NpcDialogue>(FindObjectsSortMode.None), "missionId", missionId);
        if (target != null) return target;

        target = FindTargetFromObjects(FindObjectsByType<MissionInteractable>(FindObjectsSortMode.None), "missionId", missionId);
        if (target != null) return target;

        target = FindTargetFromObjects(FindObjectsByType<MissionCollectible>(FindObjectsSortMode.None), "missionId", missionId);
        if (target != null) return target;

        target = FindTargetFromObjects(FindObjectsByType<MissionTrigger>(FindObjectsSortMode.None), "missionId", missionId);
        return target;
    }

    private Transform FindTargetFromObjects<T>(T[] objects, string fieldName, string missionId) where T : MonoBehaviour
    {
        for (int i = 0; i < objects.Length; i++)
        {
            T obj = objects[i];
            if (obj == null || !obj.isActiveAndEnabled || !obj.gameObject.activeInHierarchy)
            {
                continue;
            }

            FieldInfo field = obj.GetType().GetField(fieldName, PrivateInstanceFlags);
            if (field == null)
            {
                continue;
            }

            string value = field.GetValue(obj) as string;
            if (value == missionId)
            {
                return obj.transform;
            }
        }

        return null;
    }
}
