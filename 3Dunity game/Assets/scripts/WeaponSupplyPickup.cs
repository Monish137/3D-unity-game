using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SUPERCharacter;

[RequireComponent(typeof(Collider))]
public class WeaponSupplyPickup : MonoBehaviour
{
    private class WeaponSupplyEntry
    {
        public string displayName;
        public GameObject worldObject;
        public bool available;
    }

    [SerializeField] private string playerTag = "Player";
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private string inventoryItemId = "villager_weapon";
    [SerializeField] private PlayerInventory.InventoryItemType inventoryItemType = PlayerInventory.InventoryItemType.Material;
    [SerializeField] private int totalAvailable = 4;

    private readonly List<ArmorySlotUI> slotUis = new List<ArmorySlotUI>();
    private readonly List<WeaponSupplyEntry> supplyEntries = new List<WeaponSupplyEntry>();

    private bool playerInRange;
    private PlayerInventory cachedInventory;
    private GameObject containerRoot;
    private Text hintText;
    private Text titleText;

    private static Sprite sharedSlotSprite;

    private void Awake()
    {
        RebuildSupplyEntries();
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void Update()
    {
        if (!playerInRange)
        {
            if (containerRoot != null && containerRoot.activeSelf)
            {
                SetContainerOpen(false);
            }

            return;
        }

        if (Input.GetKeyDown(interactionKey))
        {
            ToggleContainer();
        }

        RefreshUi();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayerCollider(other))
        {
            return;
        }

        playerInRange = true;
        cachedInventory = other.GetComponentInParent<PlayerInventory>();
        if (cachedInventory == null)
        {
            cachedInventory = FindPlayerInventory();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayerCollider(other))
        {
            return;
        }

        playerInRange = false;
        SetContainerOpen(false);
    }

    private void ToggleContainer()
    {
        cachedInventory = cachedInventory != null ? cachedInventory : FindPlayerInventory();
        if (cachedInventory == null)
        {
            Debug.LogWarning("WeaponSupplyPickup: PlayerInventory not found on player.");
            return;
        }

        EnsureContainerUi();
        bool shouldOpen = !containerRoot.activeSelf;
        SetContainerOpen(shouldOpen);
        cachedInventory.SetInventoryOpen(shouldOpen);
        RefreshUi();
    }

    private void SetContainerOpen(bool isVisible)
    {
        if (containerRoot != null)
        {
            containerRoot.SetActive(isVisible);
        }
    }

    private void EnsureContainerUi()
    {
        if (containerRoot != null)
        {
            return;
        }

        EnsureEventSystemExists();

        GameObject parentObject = new GameObject("ArmoryCanvas");
        Canvas canvas = parentObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 65;

        CanvasScaler scaler = parentObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        parentObject.AddComponent<GraphicRaycaster>();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        containerRoot = new GameObject("ArmoryContainer");
        containerRoot.transform.SetParent(parentObject.transform, false);

        Image rootImage = containerRoot.AddComponent<Image>();
        rootImage.sprite = GetSharedSlotSprite();
        rootImage.type = Image.Type.Sliced;
        rootImage.color = new Color(0.08f, 0.09f, 0.12f, 0.9f);

        RectTransform rootRect = containerRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.82f, 0.24f);
        rootRect.anchorMax = new Vector2(0.98f, 0.72f);
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        titleText = CreateText("ArmoryTitle", containerRoot.transform, font, 22, FontStyle.Bold,
            TextAnchor.UpperLeft, new Vector2(14f, -10f), new Vector2(-14f, -36f));
        titleText.text = "Armory";

        hintText = CreateText("ArmoryHint", containerRoot.transform, font, 15, FontStyle.Bold,
            TextAnchor.LowerLeft, new Vector2(14f, 10f), new Vector2(-14f, 44f));

        GameObject gridObject = new GameObject("ArmoryGrid");
        gridObject.transform.SetParent(containerRoot.transform, false);
        RectTransform gridRect = gridObject.AddComponent<RectTransform>();
        gridRect.anchorMin = new Vector2(0.10f, 0.18f);
        gridRect.anchorMax = new Vector2(0.90f, 0.74f);
        gridRect.offsetMin = Vector2.zero;
        gridRect.offsetMax = Vector2.zero;

        GridLayoutGroup grid = gridObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(82f, 82f);
        grid.spacing = new Vector2(12f, 12f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;

        for (int i = 0; i < supplyEntries.Count; i++)
        {
            GameObject slotObject = new GameObject($"ArmorySlot_{i + 1}");
            slotObject.transform.SetParent(gridObject.transform, false);

            Image slotBackground = slotObject.AddComponent<Image>();
            slotBackground.sprite = GetSharedSlotSprite();
            slotBackground.type = Image.Type.Sliced;
            slotBackground.color = new Color(0.18f, 0.22f, 0.28f, 1f);

            ArmorySlotUI slotUi = slotObject.AddComponent<ArmorySlotUI>();
            slotUi.Initialize(this, i, font);
            slotUis.Add(slotUi);
        }

        containerRoot.SetActive(false);
    }

    public void BeginSupplyDrag(int slotIndex, PointerEventData eventData)
    {
        if (slotIndex < 0 || slotIndex >= supplyEntries.Count || cachedInventory == null)
        {
            return;
        }

        WeaponSupplyEntry entry = supplyEntries[slotIndex];
        if (entry == null || !entry.available)
        {
            return;
        }

        cachedInventory.BeginExternalDrag(
            inventoryItemId,
            entry.displayName,
            inventoryItemType,
            1,
            new Color(0.28f, 0.62f, 0.85f, 1f),
            amount => OnSupplyTransferred(slotIndex, amount),
            eventData);
    }

    public void UpdateSupplyDrag(PointerEventData eventData)
    {
        if (cachedInventory != null)
        {
            cachedInventory.UpdateDrag(eventData);
        }
    }

    public void EndSupplyDrag()
    {
        if (cachedInventory != null)
        {
            cachedInventory.EndDrag(true);
        }
    }

    private void OnSupplyTransferred(int slotIndex, int amount)
    {
        if (slotIndex < 0 || slotIndex >= supplyEntries.Count)
        {
            return;
        }

        WeaponSupplyEntry entry = supplyEntries[slotIndex];
        if (entry == null || !entry.available)
        {
            return;
        }

        entry.available = false;
        if (entry.worldObject != null)
        {
            entry.worldObject.SetActive(false);
        }

        RefreshUi();
        Debug.Log($"WeaponSupplyPickup: Moved '{entry.displayName}' into bag.");
    }

    private void RefreshUi()
    {
        if (containerRoot == null)
        {
            return;
        }

        int remainingSupply = GetRemainingSupplyCount();

        if (titleText != null)
        {
            titleText.text = $"Armory ({remainingSupply}/{supplyEntries.Count})";
        }

        if (hintText != null)
        {
            hintText.text = remainingSupply > 0
                ? "Drag swords to Materials slots"
                : "Armory is empty";
        }

        for (int i = 0; i < slotUis.Count; i++)
        {
            WeaponSupplyEntry entry = i < supplyEntries.Count ? supplyEntries[i] : null;
            slotUis[i].Refresh(entry != null && entry.available, entry != null ? entry.displayName : "EMPTY");
        }
    }

    private void RebuildSupplyEntries()
    {
        supplyEntries.Clear();

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == null)
            {
                continue;
            }

            Renderer renderer = child.GetComponentInChildren<Renderer>(true);
            if (renderer == null)
            {
                continue;
            }

            supplyEntries.Add(new WeaponSupplyEntry
            {
                displayName = MakeDisplayName(child.name),
                worldObject = child.gameObject,
                available = child.gameObject.activeSelf
            });
        }

        if (supplyEntries.Count == 0)
        {
            totalAvailable = Mathf.Max(0, totalAvailable);
            for (int i = 0; i < totalAvailable; i++)
            {
                supplyEntries.Add(new WeaponSupplyEntry
                {
                    displayName = $"Weapon {i + 1}",
                    worldObject = null,
                    available = true
                });
            }
        }
        else
        {
            totalAvailable = supplyEntries.Count;
        }
    }

    private int GetRemainingSupplyCount()
    {
        int count = 0;
        for (int i = 0; i < supplyEntries.Count; i++)
        {
            if (supplyEntries[i] != null && supplyEntries[i].available)
            {
                count++;
            }
        }

        return count;
    }

    private static string MakeDisplayName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return "Villager Weapon";
        }

        return rawName.Replace('_', ' ');
    }

    private PlayerInventory FindPlayerInventory()
    {
        PlayerInventory[] inventories = FindObjectsByType<PlayerInventory>(FindObjectsSortMode.None);
        for (int i = 0; i < inventories.Length; i++)
        {
            PlayerInventory inventory = inventories[i];
            if (inventory == null)
            {
                continue;
            }

            if (inventory.CompareTag(playerTag) || inventory.GetComponent<SUPERCharacterAIO>() != null)
            {
                return inventory;
            }
        }

        return null;
    }

    private bool IsPlayerCollider(Collider other)
    {
        if (other == null)
        {
            return false;
        }

        if (other.CompareTag(playerTag))
        {
            return true;
        }

        if (other.GetComponentInParent<PlayerInventory>() != null)
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

    private static void EnsureEventSystemExists()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private static Sprite GetSharedSlotSprite()
    {
        if (sharedSlotSprite != null)
        {
            return sharedSlotSprite;
        }

        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        texture.wrapMode = TextureWrapMode.Clamp;
        sharedSlotSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        return sharedSlotSprite;
    }

    private static Text CreateText(
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
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;

        return text;
    }
}

public class ArmorySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private WeaponSupplyPickup owner;
    private int slotIndex;
    private Image backgroundImage;
    private Image iconImage;
    private Text labelText;

    public void Initialize(WeaponSupplyPickup pickup, int index, Font font)
    {
        owner = pickup;
        slotIndex = index;
        backgroundImage = GetComponent<Image>();

        GameObject iconObject = new GameObject("Icon");
        iconObject.transform.SetParent(transform, false);
        iconImage = iconObject.AddComponent<Image>();
        iconImage.sprite = GetSharedSlotSprite();
        iconImage.raycastTarget = false;
        iconImage.rectTransform.anchorMin = new Vector2(0.16f, 0.18f);
        iconImage.rectTransform.anchorMax = new Vector2(0.84f, 0.70f);
        iconImage.rectTransform.offsetMin = Vector2.zero;
        iconImage.rectTransform.offsetMax = Vector2.zero;

        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(transform, false);
        labelText = labelObject.AddComponent<Text>();
        labelText.font = font;
        labelText.fontSize = 11;
        labelText.fontStyle = FontStyle.Bold;
        labelText.alignment = TextAnchor.LowerCenter;
        labelText.color = Color.white;
        labelText.raycastTarget = false;

        RectTransform labelRect = labelText.rectTransform;
        labelRect.anchorMin = new Vector2(0.04f, 0.02f);
        labelRect.anchorMax = new Vector2(0.96f, 0.26f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
    }

    public void Refresh(bool hasSupply, string displayName)
    {
        backgroundImage.color = hasSupply
            ? new Color(0.24f, 0.28f, 0.35f, 0.94f)
            : new Color(0.18f, 0.22f, 0.28f, 0.55f);
        iconImage.color = hasSupply
            ? new Color(0.28f, 0.62f, 0.85f, 1f)
            : new Color(0.12f, 0.14f, 0.17f, 0.4f);
        labelText.text = hasSupply ? Shorten(displayName) : "EMPTY";
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        owner.BeginSupplyDrag(slotIndex, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        owner.UpdateSupplyDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        owner.EndSupplyDrag();
    }

    private static Sprite GetSharedSlotSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
    }

    private static string Shorten(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return "ITEM";
        }

        string clean = label.Replace("Sword", "SWD").Replace("Green", "GRN").Replace("White", "WHT").Replace("Blue", "BLU");
        return clean.Length <= 10 ? clean : clean.Substring(0, 10).ToUpperInvariant();
    }
}
