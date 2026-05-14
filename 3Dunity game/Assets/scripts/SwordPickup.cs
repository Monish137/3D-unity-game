using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SwordPickup : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool destroyOnPickup = true;
    [SerializeField] private bool equipImmediatelyOnPickup;

    private PlayerSkills playerSkills;
    private PlayerInventory playerInventory;

    private void Awake()
    {
        playerSkills = FindPlayerSkills();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        if (playerSkills == null)
        {
            playerSkills = other.GetComponent<PlayerSkills>();
        }

        if (playerSkills == null)
        {
            playerSkills = other.GetComponentInParent<PlayerSkills>();
        }

        if (playerInventory == null)
        {
            playerInventory = other.GetComponent<PlayerInventory>();
        }

        if (playerInventory == null)
        {
            playerInventory = other.GetComponentInParent<PlayerInventory>();
        }

        if (playerSkills == null)
        {
            Debug.LogWarning("SwordPickup: PlayerSkills component not found on the player.");
            return;
        }

        if (playerInventory != null)
        {
            playerInventory.AddItem("sword", "Sword", PlayerInventory.InventoryItemType.Weapon, 1);
        }

        if (equipImmediatelyOnPickup)
        {
            playerSkills.SetSwordEquipped(true);
        }

        if (destroyOnPickup)
        {
            Destroy(gameObject);
        }
    }

    private PlayerSkills FindPlayerSkills()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null)
        {
            return null;
        }

        PlayerSkills skills = player.GetComponent<PlayerSkills>();
        return skills != null ? skills : player.GetComponentInParent<PlayerSkills>();
    }
}
