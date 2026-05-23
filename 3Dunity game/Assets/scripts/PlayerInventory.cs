using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerInventory : MonoBehaviour
{
    private class ExternalDragPayload
    {
        public string itemId;
        public string displayName;
        public InventoryItemType itemType;
        public int amount;
        public System.Action<int> onTransferred;
        public Color color;
    }

    public enum InventoryItemType
    {
        Weapon,
        Food,
        Material
    }

    [System.Serializable]
    public class InventoryEntry
    {
        public string itemId;
        public string displayName;
        public InventoryItemType itemType;
        public int amount;
    }

    [Header("Controls")]
    [SerializeField] private KeyCode toggleInventoryKey = KeyCode.B;
    [SerializeField] private KeyCode equipSwordKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode useFoodKey = KeyCode.Alpha2;

    [Header("Slots")]
    [SerializeField] private int weaponSlotCount = 6;
    [SerializeField] private int foodSlotCount = 10;
    [SerializeField] private int materialSlotCount = 10;

    [Header("Food")]
    [SerializeField] private string defaultFoodItemId = "meat";
    [SerializeField] private int foodHealAmount = 15;

    private readonly List<InventoryEntry> weaponSlots = new List<InventoryEntry>();
    private readonly List<InventoryEntry> foodSlots = new List<InventoryEntry>();
    private readonly List<InventoryEntry> materialSlots = new List<InventoryEntry>();
    private readonly List<InventorySlotUI> slotUis = new List<InventorySlotUI>();

    private PlayerSkills playerSkills;
    private SUPERCharacter.SUPERCharacterAIO superCharacter;

    private GameObject inventoryRoot;
    private Text inventoryTitleText;
    private Text inventoryHintText;
    private Text equippedWeaponText;
    private Image equippedSlotIcon;
    private Text equippedSlotNameText;
    private Text toastText;
    private InventorySlotUI dragSourceSlot;
    private ExternalDragPayload externalDragPayload;
    private Image dragGhost;
    private Text dragGhostText;
    private Canvas inventoryCanvas;
    private float toastTimer;
    private CursorLockMode previousCursorLockMode = CursorLockMode.Locked;
    private bool previousCursorVisible;

    private static Sprite sharedSlotSprite;

    private void Awake()
    {
        playerSkills = GetComponent<PlayerSkills>();
        superCharacter = GetComponent<SUPERCharacter.SUPERCharacterAIO>();

        EnsureSlotCapacity(weaponSlots, weaponSlotCount);
        EnsureSlotCapacity(foodSlots, foodSlotCount);
        EnsureSlotCapacity(materialSlots, materialSlotCount);

        CreateInventoryUi();
        SetInventoryVisible(false);
        HookPlayerSkills();
        RefreshUi();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleInventoryKey))
        {
            SetInventoryVisible(!inventoryRoot.activeSelf);
        }

        if (Input.GetKeyDown(equipSwordKey))
        {
            EquipSwordFromInventory();
        }

        if (Input.GetKeyDown(useFoodKey))
        {
            ConsumeFood(defaultFoodItemId);
        }

        if (toastTimer > 0f)
        {
            toastTimer -= Time.deltaTime;
            if (toastTimer <= 0f && toastText != null)
            {
                toastText.gameObject.SetActive(false);
            }
        }
    }

    public void AddItem(string itemId, string displayName, InventoryItemType itemType, int amount = 1)
    {
        TryAddItem(itemId, displayName, itemType, amount);
    }

    public bool TryAddItem(string itemId, string displayName, InventoryItemType itemType, int amount = 1)
    {
        if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
        {
            return false;
        }

        List<InventoryEntry> slots = GetSlots(itemType);
        InventoryEntry existing = FindItemEntry(slots, itemId);
        if (existing != null)
        {
            existing.amount += amount;
            RefreshUi();
            Debug.Log($"Inventory: Added {displayName} x{amount}.");
            return true;
        }

        int emptyIndex = FindFirstEmptySlot(slots);
        if (emptyIndex < 0)
        {
            Debug.LogWarning($"Inventory: No empty {itemType} slot available for {displayName}.");
            return false;
        }

        slots[emptyIndex] = new InventoryEntry
        {
            itemId = itemId,
            displayName = string.IsNullOrWhiteSpace(displayName) ? itemId : displayName,
            itemType = itemType,
            amount = amount
        };

        RefreshUi();
        Debug.Log($"Inventory: Added {displayName} x{amount}.");
        return true;
    }

    public bool HasItem(string itemId, int amount = 1)
    {
        InventoryEntry entry = FindItemEntryAcrossSections(itemId);
        return entry != null && entry.amount >= amount;
    }

    public int GetItemCount(string itemId)
    {
        InventoryEntry entry = FindItemEntryAcrossSections(itemId);
        return entry != null ? entry.amount : 0;
    }

    public bool RemoveItem(string itemId, int amount = 1)
    {
        if (amount <= 0)
        {
            return false;
        }

        foreach (InventoryItemType type in System.Enum.GetValues(typeof(InventoryItemType)))
        {
            List<InventoryEntry> slots = GetSlots(type);
            for (int i = 0; i < slots.Count; i++)
            {
                InventoryEntry entry = slots[i];
                if (entry == null || entry.itemId != itemId)
                {
                    continue;
                }

                if (entry.amount < amount)
                {
                    return false;
                }

                entry.amount -= amount;
                if (entry.amount <= 0)
                {
                    slots[i] = null;
                }

                RefreshUi();
                return true;
            }
        }

        return false;
    }

    public void EquipSwordFromInventory()
    {
        if (!HasItem("sword"))
        {
            Debug.Log("Inventory: No sword in bag.");
            return;
        }

        if (playerSkills == null)
        {
            playerSkills = GetComponent<PlayerSkills>();
        }

        if (playerSkills != null)
        {
            playerSkills.SetSwordEquipped(true);
            ShowToast("Sword Equipped");
            Debug.Log("Inventory: Sword equipped from bag.");
        }
    }

    public void ConsumeFood(string itemId)
    {
        if (!RemoveItem(itemId, 1))
        {
            Debug.Log("Inventory: No food available.");
            return;
        }

        TryHealPlayer(foodHealAmount);
        ShowToast("Ate Meat +15 HP");
        Debug.Log($"Inventory: Consumed {itemId}.");
    }

    public InventoryEntry GetSlotEntry(InventoryItemType itemType, int slotIndex)
    {
        List<InventoryEntry> slots = GetSlots(itemType);
        return slotIndex >= 0 && slotIndex < slots.Count ? slots[slotIndex] : null;
    }

    public void BeginDrag(InventorySlotUI slotUi, PointerEventData eventData)
    {
        InventoryEntry entry = GetSlotEntry(slotUi.ItemType, slotUi.SlotIndex);
        if (entry == null)
        {
            return;
        }

        dragSourceSlot = slotUi;
        if (dragGhost != null)
        {
            dragGhost.color = GetItemColor(entry.itemType);
            dragGhost.gameObject.SetActive(true);
        }

        if (dragGhostText != null)
        {
            dragGhostText.text = GetItemShortLabel(entry);
            dragGhostText.gameObject.SetActive(true);
        }

        UpdateDrag(eventData);
    }

    public void BeginExternalDrag(
        string itemId,
        string displayName,
        InventoryItemType itemType,
        int amount,
        Color color,
        System.Action<int> onTransferred,
        PointerEventData eventData)
    {
        if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
        {
            return;
        }

        externalDragPayload = new ExternalDragPayload
        {
            itemId = itemId,
            displayName = displayName,
            itemType = itemType,
            amount = amount,
            onTransferred = onTransferred,
            color = color
        };

        dragSourceSlot = null;

        if (dragGhost != null)
        {
            dragGhost.color = color;
            dragGhost.gameObject.SetActive(true);
        }

        if (dragGhostText != null)
        {
            dragGhostText.text = string.IsNullOrWhiteSpace(displayName)
                ? itemId
                : displayName.Substring(0, Mathf.Min(3, displayName.Length)).ToUpperInvariant();
            dragGhostText.gameObject.SetActive(true);
        }

        UpdateDrag(eventData);
    }

    public void UpdateDrag(PointerEventData eventData)
    {
        if (dragGhost == null)
        {
            return;
        }

        if (dragSourceSlot == null && externalDragPayload == null)
        {
            return;
        }

        dragGhost.rectTransform.position = eventData.position;
        if (dragGhostText != null)
        {
            dragGhostText.rectTransform.position = eventData.position;
        }
    }

    public void EndDrag()
    {
        EndDrag(false);
    }

    public void EndDrag(PointerEventData eventData)
    {
        if (!IsPointerOverInventorySlot(eventData))
        {
            TryDropDraggedItemToWorldReceiver();
        }

        EndDrag(false);
    }

    public void EndDrag(bool clearExternalPayload)
    {
        dragSourceSlot = null;
        if (dragGhost != null)
        {
            dragGhost.gameObject.SetActive(false);
        }

        if (dragGhostText != null)
        {
            dragGhostText.gameObject.SetActive(false);
        }

        if (clearExternalPayload)
        {
            externalDragPayload = null;
        }
    }

    public void HandleDrop(InventorySlotUI targetSlot)
    {
        if (dragSourceSlot == null || targetSlot == null)
        {
            if (externalDragPayload != null && targetSlot != null)
            {
                HandleExternalDrop(targetSlot);
            }

            return;
        }

        if (dragSourceSlot == targetSlot)
        {
            return;
        }

        InventoryEntry sourceEntry = GetSlotEntry(dragSourceSlot.ItemType, dragSourceSlot.SlotIndex);
        if (sourceEntry == null)
        {
            return;
        }

        if (targetSlot.ItemType != sourceEntry.itemType)
        {
            return;
        }

        List<InventoryEntry> targetSlots = GetSlots(targetSlot.ItemType);
        List<InventoryEntry> sourceSlots = GetSlots(dragSourceSlot.ItemType);
        InventoryEntry targetEntry = targetSlots[targetSlot.SlotIndex];

        sourceSlots[dragSourceSlot.SlotIndex] = targetEntry;
        targetSlots[targetSlot.SlotIndex] = sourceEntry;

        RefreshUi();
    }

    public void HandleExternalDrop(InventorySlotUI targetSlot)
    {
        if (externalDragPayload == null || targetSlot == null)
        {
            return;
        }

        if (targetSlot.ItemType != externalDragPayload.itemType)
        {
            return;
        }

        if (!TryAddItem(
                externalDragPayload.itemId,
                externalDragPayload.displayName,
                externalDragPayload.itemType,
                externalDragPayload.amount))
        {
            return;
        }

        externalDragPayload.onTransferred?.Invoke(externalDragPayload.amount);
        EndDrag(true);
    }

    public void HandleSlotClick(InventorySlotUI slotUi)
    {
        InventoryEntry entry = GetSlotEntry(slotUi.ItemType, slotUi.SlotIndex);
        if (entry == null)
        {
            return;
        }

        if (entry.itemType == InventoryItemType.Weapon && entry.itemId == "sword")
        {
            EquipSwordFromInventory();
        }
        else if (entry.itemType == InventoryItemType.Food)
        {
            ConsumeFood(entry.itemId);
        }
    }

    private void TryDropDraggedItemToWorldReceiver()
    {
        if (dragSourceSlot == null)
        {
            return;
        }

        InventoryEntry entry = GetSlotEntry(dragSourceSlot.ItemType, dragSourceSlot.SlotIndex);
        if (entry == null)
        {
            return;
        }

        VillagerWeaponReceiver[] receivers = FindObjectsByType<VillagerWeaponReceiver>(FindObjectsSortMode.None);
        for (int i = 0; i < receivers.Length; i++)
        {
            VillagerWeaponReceiver receiver = receivers[i];
            if (receiver == null || !receiver.isActiveAndEnabled)
            {
                continue;
            }

            if (receiver.TryReceiveDraggedItem(this, entry.itemId, 1))
            {
                ShowToast("Weapon Given");
                return;
            }
        }
    }

    private bool IsPointerOverInventorySlot(PointerEventData eventData)
    {
        if (eventData == null || eventData.pointerEnter == null)
        {
            return false;
        }

        return eventData.pointerEnter.GetComponentInParent<InventorySlotUI>() != null;
    }

    private void RefreshUi()
    {
        if (inventoryTitleText != null)
        {
            inventoryTitleText.text = "Traveler Bag";
        }

        if (equippedWeaponText != null)
        {
            equippedWeaponText.text = playerSkills != null && playerSkills.HasSword ? "Equipped: Sword" : "Equipped: None";
        }

        if (equippedSlotIcon != null)
        {
            bool swordEquipped = playerSkills != null && playerSkills.HasSword;
            equippedSlotIcon.color = swordEquipped
                ? GetItemColor(InventoryItemType.Weapon)
                : new Color(0.12f, 0.14f, 0.17f, 0.55f);
        }

        if (equippedSlotNameText != null)
        {
            equippedSlotNameText.text = playerSkills != null && playerSkills.HasSword ? "Sword" : "Empty";
        }

        if (inventoryHintText != null)
        {
            inventoryHintText.text = "B Open/Close   Drag to move   Drag villager weapons near villagers to give";
        }

        for (int i = 0; i < slotUis.Count; i++)
        {
            InventorySlotUI slotUi = slotUis[i];
            slotUi.Refresh(GetSlotEntry(slotUi.ItemType, slotUi.SlotIndex), GetItemColor(slotUi.ItemType));
        }
    }

    private void TryHealPlayer(int amount)
    {
        if (superCharacter == null)
        {
            superCharacter = GetComponent<SUPERCharacter.SUPERCharacterAIO>();
        }

        if (superCharacter == null)
        {
            return;
        }

        superCharacter.ImmediateStateChange(amount, SUPERCharacter.StatSelector.Health);
    }

    private void CreateInventoryUi()
    {
        EnsureEventSystemExists();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasObject = new GameObject("InventoryCanvas");
        inventoryCanvas = canvasObject.AddComponent<Canvas>();
        inventoryCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        inventoryCanvas.sortingOrder = 40;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        inventoryRoot = new GameObject("InventoryRoot");
        inventoryRoot.transform.SetParent(canvasObject.transform, false);

        Image rootImage = inventoryRoot.AddComponent<Image>();
        rootImage.sprite = GetSharedSlotSprite();
        rootImage.type = Image.Type.Sliced;
        rootImage.color = new Color(0.04f, 0.06f, 0.09f, 0.82f);

        RectTransform rootRect = inventoryRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.17f, 0.09f);
        rootRect.anchorMax = new Vector2(0.83f, 0.91f);
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        inventoryTitleText = CreateText("InventoryTitle", inventoryRoot.transform, font, 34, FontStyle.Bold,
            TextAnchor.UpperLeft, new Vector2(28f, -20f), new Vector2(-28f, -70f));

        equippedWeaponText = CreateText("EquippedWeapon", inventoryRoot.transform, font, 20, FontStyle.Bold,
            TextAnchor.UpperRight, new Vector2(28f, -22f), new Vector2(-28f, -68f));

        CreateEquippedSlot(inventoryRoot.transform, font);

        inventoryHintText = CreateText("InventoryHint", inventoryRoot.transform, font, 18, FontStyle.Bold,
            TextAnchor.LowerLeft, new Vector2(28f, 12f), new Vector2(-28f, 42f));

        CreateSection("Weapons", InventoryItemType.Weapon, weaponSlotCount, new Vector2(0.04f, 0.60f), new Vector2(0.96f, 0.86f), font);
        CreateSection("Food", InventoryItemType.Food, foodSlotCount, new Vector2(0.04f, 0.33f), new Vector2(0.96f, 0.58f), font);
        CreateSection("Materials", InventoryItemType.Material, materialSlotCount, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.31f), font);

        GameObject dragGhostObject = new GameObject("DragGhost");
        dragGhostObject.transform.SetParent(canvasObject.transform, false);
        dragGhost = dragGhostObject.AddComponent<Image>();
        dragGhost.sprite = GetSharedSlotSprite();
        dragGhost.raycastTarget = false;
        dragGhost.rectTransform.sizeDelta = new Vector2(72f, 72f);
        dragGhost.gameObject.SetActive(false);

        dragGhostText = CreateText("DragGhostText", dragGhostObject.transform, font, 18, FontStyle.Bold,
            TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(0f, 0f));
        dragGhostText.rectTransform.anchorMin = Vector2.zero;
        dragGhostText.rectTransform.anchorMax = Vector2.one;
        dragGhostText.rectTransform.offsetMin = Vector2.zero;
        dragGhostText.rectTransform.offsetMax = Vector2.zero;
        dragGhostText.raycastTarget = false;
        dragGhostText.gameObject.SetActive(false);

        toastText = CreateText("InventoryToast", canvasObject.transform, font, 28, FontStyle.Bold,
            TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
        toastText.rectTransform.anchorMin = new Vector2(0.5f, 0.16f);
        toastText.rectTransform.anchorMax = new Vector2(0.5f, 0.16f);
        toastText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        toastText.rectTransform.sizeDelta = new Vector2(420f, 44f);
        toastText.color = new Color(1f, 0.92f, 0.52f, 1f);
        toastText.gameObject.SetActive(false);
    }

    private void CreateEquippedSlot(Transform parent, Font font)
    {
        GameObject slotRoot = new GameObject("EquippedSlot");
        slotRoot.transform.SetParent(parent, false);

        Image slotBackground = slotRoot.AddComponent<Image>();
        slotBackground.sprite = GetSharedSlotSprite();
        slotBackground.type = Image.Type.Sliced;
        slotBackground.color = new Color(0.16f, 0.19f, 0.24f, 0.95f);

        RectTransform slotRect = slotRoot.GetComponent<RectTransform>();
        slotRect.anchorMin = new Vector2(0.82f, 0.89f);
        slotRect.anchorMax = new Vector2(0.94f, 0.97f);
        slotRect.offsetMin = Vector2.zero;
        slotRect.offsetMax = Vector2.zero;

        GameObject iconObject = new GameObject("EquippedIcon");
        iconObject.transform.SetParent(slotRoot.transform, false);
        equippedSlotIcon = iconObject.AddComponent<Image>();
        equippedSlotIcon.sprite = GetSharedSlotSprite();
        equippedSlotIcon.rectTransform.anchorMin = new Vector2(0.14f, 0.16f);
        equippedSlotIcon.rectTransform.anchorMax = new Vector2(0.86f, 0.70f);
        equippedSlotIcon.rectTransform.offsetMin = Vector2.zero;
        equippedSlotIcon.rectTransform.offsetMax = Vector2.zero;

        equippedSlotNameText = CreateText("EquippedSlotName", slotRoot.transform, font, 12, FontStyle.Bold,
            TextAnchor.LowerCenter, new Vector2(4f, 4f), new Vector2(-4f, 6f));
    }

    private void CreateSection(
        string title,
        InventoryItemType itemType,
        int slotCount,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Font font)
    {
        GameObject section = new GameObject($"{title}Section");
        section.transform.SetParent(inventoryRoot.transform, false);

        Image sectionImage = section.AddComponent<Image>();
        sectionImage.sprite = GetSharedSlotSprite();
        sectionImage.type = Image.Type.Sliced;
        sectionImage.color = new Color(0.10f, 0.13f, 0.17f, 0.72f);

        RectTransform sectionRect = section.GetComponent<RectTransform>();
        sectionRect.anchorMin = anchorMin;
        sectionRect.anchorMax = anchorMax;
        sectionRect.offsetMin = Vector2.zero;
        sectionRect.offsetMax = Vector2.zero;

        Text sectionTitle = CreateText($"{title}Title", section.transform, font, 22, FontStyle.Bold,
            TextAnchor.UpperLeft, new Vector2(16f, -8f), new Vector2(-16f, -34f));
        sectionTitle.text = title;

        GameObject gridObject = new GameObject($"{title}Grid");
        gridObject.transform.SetParent(section.transform, false);
        RectTransform gridRect = gridObject.AddComponent<RectTransform>();
        gridRect.anchorMin = new Vector2(0.03f, 0.10f);
        gridRect.anchorMax = new Vector2(0.97f, 0.74f);
        gridRect.offsetMin = Vector2.zero;
        gridRect.offsetMax = Vector2.zero;

        GridLayoutGroup grid = gridObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = itemType == InventoryItemType.Weapon ? new Vector2(74f, 74f) : new Vector2(70f, 70f);
        grid.spacing = new Vector2(10f, 10f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = itemType == InventoryItemType.Weapon ? 6 : 5;

        for (int i = 0; i < slotCount; i++)
        {
            CreateSlot(gridObject.transform, itemType, i, font);
        }
    }

    private void CreateSlot(Transform parent, InventoryItemType itemType, int slotIndex, Font font)
    {
        GameObject slotObject = new GameObject($"{itemType}Slot_{slotIndex + 1}");
        slotObject.transform.SetParent(parent, false);

        Image slotBackground = slotObject.AddComponent<Image>();
        slotBackground.sprite = GetSharedSlotSprite();
        slotBackground.type = Image.Type.Sliced;
        slotBackground.color = new Color(0.18f, 0.22f, 0.28f, 1f);

        InventorySlotUI slotUi = slotObject.AddComponent<InventorySlotUI>();
        slotUi.Initialize(this, itemType, slotIndex, font);
        slotUis.Add(slotUi);
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

    private void HookPlayerSkills()
    {
        if (playerSkills == null)
        {
            playerSkills = GetComponent<PlayerSkills>();
        }

        if (playerSkills != null)
        {
            playerSkills.SwordEquipChanged -= HandleSwordEquipChanged;
            playerSkills.SwordEquipChanged += HandleSwordEquipChanged;
        }
    }

    private void HandleSwordEquipChanged(bool equipped)
    {
        RefreshUi();
        ShowToast(equipped ? "Sword Equipped" : "Sword Unequipped");
    }

    private void ShowToast(string message)
    {
        if (toastText == null || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        toastText.text = message;
        toastText.gameObject.SetActive(true);
        toastTimer = 1.8f;
    }

    private void EnsureEventSystemExists()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private void SetInventoryVisible(bool isVisible)
    {
        if (inventoryRoot != null)
        {
            inventoryRoot.SetActive(isVisible);
        }

        if (isVisible)
        {
            previousCursorLockMode = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = previousCursorLockMode;
            Cursor.visible = previousCursorVisible;
        }
    }

    public void SetInventoryOpen(bool isVisible)
    {
        SetInventoryVisible(isVisible);
    }

    public bool IsInventoryOpen => inventoryRoot != null && inventoryRoot.activeSelf;

    public void CancelAllDrag()
    {
        externalDragPayload = null;
        EndDrag(true);
    }

    private static void EnsureSlotCapacity(List<InventoryEntry> slots, int desiredCount)
    {
        while (slots.Count < desiredCount)
        {
            slots.Add(null);
        }
    }

    private List<InventoryEntry> GetSlots(InventoryItemType itemType)
    {
        switch (itemType)
        {
            case InventoryItemType.Weapon:
                return weaponSlots;
            case InventoryItemType.Food:
                return foodSlots;
            default:
                return materialSlots;
        }
    }

    private InventoryEntry FindItemEntry(List<InventoryEntry> slots, string itemId)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            InventoryEntry entry = slots[i];
            if (entry != null && entry.itemId == itemId)
            {
                return entry;
            }
        }

        return null;
    }

    private InventoryEntry FindItemEntryAcrossSections(string itemId)
    {
        foreach (InventoryItemType type in System.Enum.GetValues(typeof(InventoryItemType)))
        {
            InventoryEntry entry = FindItemEntry(GetSlots(type), itemId);
            if (entry != null)
            {
                return entry;
            }
        }

        return null;
    }

    private int FindFirstEmptySlot(List<InventoryEntry> slots)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null)
            {
                return i;
            }
        }

        return -1;
    }

    private Color GetItemColor(InventoryItemType itemType)
    {
        switch (itemType)
        {
            case InventoryItemType.Weapon:
                return new Color(0.75f, 0.2f, 0.2f, 1f);
            case InventoryItemType.Food:
                return new Color(0.84f, 0.58f, 0.16f, 1f);
            default:
                return new Color(0.28f, 0.62f, 0.85f, 1f);
        }
    }

    private string GetItemShortLabel(InventoryEntry entry)
    {
        if (entry == null || string.IsNullOrWhiteSpace(entry.displayName))
        {
            return string.Empty;
        }

        return entry.displayName.Length <= 3 ? entry.displayName : entry.displayName.Substring(0, 3).ToUpperInvariant();
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
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;

        return text;
    }
}

public class InventorySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
{
    public PlayerInventory.InventoryItemType ItemType { get; private set; }
    public int SlotIndex { get; private set; }

    private PlayerInventory inventory;
    private Image backgroundImage;
    private Image iconImage;
    private Text amountText;
    private Text nameText;

    public void Initialize(PlayerInventory owner, PlayerInventory.InventoryItemType itemType, int slotIndex, Font font)
    {
        inventory = owner;
        ItemType = itemType;
        SlotIndex = slotIndex;
        backgroundImage = GetComponent<Image>();

        GameObject iconObject = new GameObject("Icon");
        iconObject.transform.SetParent(transform, false);
        iconImage = iconObject.AddComponent<Image>();
        iconImage.sprite = GetSharedSlotSprite();
        iconImage.raycastTarget = false;
        iconImage.rectTransform.anchorMin = new Vector2(0.16f, 0.20f);
        iconImage.rectTransform.anchorMax = new Vector2(0.84f, 0.76f);
        iconImage.rectTransform.offsetMin = Vector2.zero;
        iconImage.rectTransform.offsetMax = Vector2.zero;

        nameText = CreateText("Name", font, 11, FontStyle.Bold, TextAnchor.UpperCenter);
        nameText.raycastTarget = false;
        nameText.rectTransform.anchorMin = new Vector2(0.06f, 0.58f);
        nameText.rectTransform.anchorMax = new Vector2(0.94f, 0.90f);
        nameText.rectTransform.offsetMin = Vector2.zero;
        nameText.rectTransform.offsetMax = Vector2.zero;

        amountText = CreateText("Amount", font, 13, FontStyle.Bold, TextAnchor.LowerRight);
        amountText.raycastTarget = false;
        amountText.rectTransform.anchorMin = new Vector2(0.08f, 0.04f);
        amountText.rectTransform.anchorMax = new Vector2(0.92f, 0.24f);
        amountText.rectTransform.offsetMin = Vector2.zero;
        amountText.rectTransform.offsetMax = Vector2.zero;
    }

    public void Refresh(PlayerInventory.InventoryEntry entry, Color typeColor)
    {
        bool hasItem = entry != null;
        backgroundImage.color = hasItem ? new Color(0.24f, 0.28f, 0.35f, 0.94f) : new Color(0.18f, 0.22f, 0.28f, 0.88f);
        iconImage.color = hasItem ? typeColor : new Color(0.12f, 0.14f, 0.17f, 0.55f);
        nameText.text = hasItem ? entry.displayName : "Empty";
        amountText.text = hasItem ? $"x{entry.amount}" : string.Empty;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        inventory.BeginDrag(this, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        inventory.UpdateDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        inventory.EndDrag(eventData);
    }

    public void OnDrop(PointerEventData eventData)
    {
        inventory.HandleDrop(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        inventory.HandleSlotClick(this);
    }

    private Text CreateText(string objectName, Font font, int fontSize, FontStyle fontStyle, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(transform, false);
        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static Sprite GetSharedSlotSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
    }
}
