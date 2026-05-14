using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MeatPickup : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string missionId = "hunt_for_food";
    [SerializeField] private int missionProgressAmount = 1;
    [SerializeField] private bool destroyOnPickup = true;
    [SerializeField] private string meatLabel = "Meat";
    [SerializeField] private string inventoryItemId = "meat";

    private MissionManager missionManager;

    private void Awake()
    {
        missionManager = MissionManager.Instance != null
            ? MissionManager.Instance
            : FindFirstObjectByType<MissionManager>();

        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        if (missionManager != null && missionManager.IsCurrentMission(missionId))
        {
            missionManager.AddProgress(missionId, missionProgressAmount);
        }

        PlayerInventory inventory = other.GetComponent<PlayerInventory>();
        if (inventory == null)
        {
            inventory = other.GetComponentInParent<PlayerInventory>();
        }

        if (inventory != null)
        {
            inventory.AddItem(inventoryItemId, meatLabel, PlayerInventory.InventoryItemType.Food, 1);
        }

        Debug.Log($"Picked up {meatLabel}.");

        if (destroyOnPickup)
        {
            Destroy(gameObject);
        }
    }

    public void SetMeatLabel(string value)
    {
        meatLabel = value;
    }
}
