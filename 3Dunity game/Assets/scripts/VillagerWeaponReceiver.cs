using UnityEngine;
using SUPERCharacter;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class VillagerWeaponReceiver : MonoBehaviour
{
    [SerializeField] private MissionManager missionManager;
    [SerializeField] private string missionId = "arm_villagers";
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private string requiredItemId = "villager_weapon";
    [SerializeField] private int amountPerHandIn = 1;
    [SerializeField] private float dragHandInDistance = 5f;
    [Header("Villager Visuals")]
    [SerializeField] private Transform villagerGroupRoot;
    [SerializeField] private GameObject weaponVisualPrefab;
    [SerializeField] private string rightHandBoneName = "Hand_R";
    [SerializeField] private string[] rightHandFallbackNames =
    {
        "B-hand.R",
        "B-handProp.R",
        "Bip001 R Hand",
        "Bip01 R Hand",
        "Hand.R",
        "hand.R",
        "hand.r",
        "Hand_R",
        "hand_r",
        "RightHand",
        "Right Hand",
        "right_hand",
        "Right Hand",
        "R_Hand",
        "r_hand",
        "R Hand",
        "Hand_Right",
        "mixamorig:RightHand",
        "mixamorig1:RightHand",
        "mixamorig:RightHandIndex1",
        "B_R_Hand",
        "DEF-hand.R",
        "CC_Base_R_Hand",
        "R HandNub"
    };
    [SerializeField] private Vector3 weaponLocalPosition = new Vector3(0.03f, 0.02f, 0.10f);
    [SerializeField] private Vector3 weaponLocalRotation = new Vector3(10f, 90f, 90f);
    [SerializeField] private Vector3 weaponLocalScale = new Vector3(0.75f, 0.75f, 0.75f);

    private bool playerInRange;
    private readonly List<Transform> villagerTargets = new List<Transform>();
    private int nextVillagerIndex;

    private void Awake()
    {
        if (missionManager == null)
        {
            missionManager = MissionManager.Instance != null
                ? MissionManager.Instance
                : FindFirstObjectByType<MissionManager>();
        }

        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }

        if (villagerGroupRoot == null)
        {
            villagerGroupRoot = transform.parent;
        }

        if (weaponVisualPrefab == null)
        {
            weaponVisualPrefab = FindDefaultWeaponPrefab();
        }

        CacheVillagerTargets();
    }

    private void Update()
    {
        if (!playerInRange)
        {
            return;
        }

        if (Input.GetKeyDown(interactionKey))
        {
            TryGiveWeapon();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayerCollider(other))
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayerCollider(other))
        {
            playerInRange = false;
        }
    }

    private void TryGiveWeapon()
    {
        if (missionManager == null || !missionManager.IsCurrentMission(missionId))
        {
            return;
        }

        PlayerInventory inventory = FindPlayerInventory();
        if (inventory == null)
        {
            return;
        }

        if (inventory == null)
        {
            Debug.LogWarning("VillagerWeaponReceiver: PlayerInventory not found on player.");
            return;
        }

        int available = inventory.GetItemCount(requiredItemId);
        if (available <= 0)
        {
            Debug.Log("VillagerWeaponReceiver: No villager weapons in bag.");
            return;
        }

        MissionManager.Mission mission = missionManager.CurrentMission;
        int remainingNeeded = mission != null ? Mathf.Max(0, mission.requiredAmount - mission.currentAmount) : amountPerHandIn;
        int handInAmount = Mathf.Min(remainingNeeded, Mathf.Min(amountPerHandIn, available));
        if (handInAmount <= 0)
        {
            return;
        }

        if (!inventory.RemoveItem(requiredItemId, handInAmount))
        {
            return;
        }

        EnsureVillagerTargetsReady();

        for (int i = 0; i < handInAmount; i++)
        {
            EquipNextVillager();
        }

        missionManager.AddProgress(missionId, handInAmount);
        Debug.Log($"VillagerWeaponReceiver: Gave villager {handInAmount} weapon(s).");
    }

    public bool TryReceiveDraggedItem(PlayerInventory inventory, string itemId, int amount)
    {
        if (inventory == null || itemId != requiredItemId || amount <= 0)
        {
            return false;
        }

        if (!CanReceiveFromInventory(inventory))
        {
            return false;
        }

        int handInAmount = Mathf.Min(amountPerHandIn, amount);
        if (!inventory.RemoveItem(requiredItemId, handInAmount))
        {
            return false;
        }

        EnsureVillagerTargetsReady();

        for (int i = 0; i < handInAmount; i++)
        {
            EquipNextVillager();
        }

        missionManager.AddProgress(missionId, handInAmount);
        Debug.Log($"VillagerWeaponReceiver: Drag-gave villager {handInAmount} weapon(s).");
        return true;
    }

    private bool CanReceiveFromInventory(PlayerInventory inventory)
    {
        if (missionManager == null)
        {
            missionManager = MissionManager.Instance != null
                ? MissionManager.Instance
                : FindFirstObjectByType<MissionManager>();
        }

        if (missionManager == null || !missionManager.IsCurrentMission(missionId))
        {
            return false;
        }

        if (!inventory.HasItem(requiredItemId))
        {
            return false;
        }

        return Vector3.Distance(inventory.transform.position, transform.position) <= dragHandInDistance;
    }

    private void EnsureVillagerTargetsReady()
    {
        if (villagerTargets.Count == 0)
        {
            CacheVillagerTargets();
        }
    }

    private void CacheVillagerTargets()
    {
        villagerTargets.Clear();
        nextVillagerIndex = 0;

        if (villagerGroupRoot == null)
        {
            return;
        }

        Animator[] animators = villagerGroupRoot.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];
            if (animator == null || animator.transform == transform || animator.GetComponentInParent<VillagerWeaponReceiver>() == this)
            {
                continue;
            }

            Transform villagerRoot = animator.transform;
            while (villagerRoot.parent != null && villagerRoot.parent != villagerGroupRoot && villagerRoot.parent.GetComponentInParent<VillagerWeaponReceiver>() != this)
            {
                villagerRoot = villagerRoot.parent;
            }

            if (!villagerTargets.Contains(villagerRoot))
            {
                villagerTargets.Add(villagerRoot);
            }
        }
    }

    private void EquipNextVillager()
    {
        GameObject visualPrefab = ResolveWeaponVisualPrefab();
        if (villagerTargets.Count == 0)
        {
            Debug.LogWarning("VillagerWeaponReceiver: No villager targets found under the villager group.");
            return;
        }

        while (nextVillagerIndex < villagerTargets.Count)
        {
            Transform villager = villagerTargets[nextVillagerIndex++];
            if (villager == null || VillagerAlreadyArmed(villager))
            {
                continue;
            }

            Transform handBone = ResolveHandBone(villager);
            Transform parent = handBone != null ? handBone : villager;

            GameObject weapon = visualPrefab != null
                ? Instantiate(visualPrefab, parent)
                : CreateFallbackHeldWeapon(parent);
            weapon.name = "VillagerHeldWeapon";
            weapon.SetActive(true);
            weapon.transform.localPosition = weaponLocalPosition;
            weapon.transform.localRotation = Quaternion.Euler(weaponLocalRotation);
            weapon.transform.localScale = weaponLocalScale;

            Renderer[] renderers = weapon.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].enabled = true;
            }

            Collider[] colliders = weapon.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }

            return;
        }

        Debug.LogWarning("VillagerWeaponReceiver: All cached villagers are already armed or missing.");
    }

    private GameObject ResolveWeaponVisualPrefab()
    {
        if (weaponVisualPrefab == null)
        {
            weaponVisualPrefab = FindDefaultWeaponPrefab();
        }

        if (weaponVisualPrefab == null)
        {
            Debug.LogWarning("VillagerWeaponReceiver: No weapon visual prefab assigned.");
            return null;
        }

        if (weaponVisualPrefab.GetComponent<WeaponSupplyPickup>() == null &&
            weaponVisualPrefab.GetComponentInChildren<Renderer>(true) != null)
        {
            return weaponVisualPrefab;
        }

        for (int i = 0; i < weaponVisualPrefab.transform.childCount; i++)
        {
            Transform child = weaponVisualPrefab.transform.GetChild(i);
            if (child != null && child.GetComponentInChildren<Renderer>(true) != null)
            {
                return child.gameObject;
            }
        }

        Debug.LogWarning($"VillagerWeaponReceiver: '{weaponVisualPrefab.name}' has no visible sword child to use.");
        return null;
    }

    private bool VillagerAlreadyArmed(Transform villager)
    {
        return villager != null && FindChildRecursive(villager, "VillagerHeldWeapon") != null;
    }

    private Transform ResolveHandBone(Transform villager)
    {
        if (villager == null)
        {
            return null;
        }

        Transform bone = FindChildRecursive(villager, rightHandBoneName);
        if (bone != null)
        {
            return bone;
        }

        Animator animator = villager.GetComponentInChildren<Animator>();
        if (animator != null && animator.isHuman)
        {
            bone = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (bone != null)
            {
                return bone;
            }
        }

        if (rightHandFallbackNames != null)
        {
            for (int i = 0; i < rightHandFallbackNames.Length; i++)
            {
                string candidate = rightHandFallbackNames[i];
                if (string.IsNullOrWhiteSpace(candidate))
                {
                    continue;
                }

                bone = FindChildRecursive(villager, candidate);
                if (bone != null)
                {
                    return bone;
                }
            }
        }

        bone = FindHandBoneByKeyword(villager);
        if (bone == null)
        {
            Debug.LogWarning($"VillagerWeaponReceiver: Could not find right hand bone for '{villager.name}', attaching weapon to villager root.");
        }

        return bone;
    }

    private GameObject CreateFallbackHeldWeapon(Transform parent)
    {
        GameObject fallbackWeapon = new GameObject("FallbackVillagerSword");
        fallbackWeapon.transform.SetParent(parent, false);

        GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blade.name = "Blade";
        blade.transform.SetParent(fallbackWeapon.transform, false);
        blade.transform.localPosition = new Vector3(0f, 0.45f, 0f);
        blade.transform.localScale = new Vector3(0.08f, 0.85f, 0.06f);

        Renderer bladeRenderer = blade.GetComponent<Renderer>();
        if (bladeRenderer != null)
        {
            bladeRenderer.material.color = new Color(0.75f, 0.82f, 0.88f);
        }

        GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        handle.name = "Handle";
        handle.transform.SetParent(fallbackWeapon.transform, false);
        handle.transform.localPosition = new Vector3(0f, -0.08f, 0f);
        handle.transform.localScale = new Vector3(0.12f, 0.22f, 0.12f);

        Renderer handleRenderer = handle.GetComponent<Renderer>();
        if (handleRenderer != null)
        {
            handleRenderer.material.color = new Color(0.30f, 0.17f, 0.08f);
        }

        return fallbackWeapon;
    }

    private GameObject FindDefaultWeaponPrefab()
    {
        PlayerSkills playerSkills = FindFirstObjectByType<PlayerSkills>();
        if (playerSkills != null && playerSkills.EquippedSwordPrefab != null)
        {
            return playerSkills.EquippedSwordPrefab;
        }

        WeaponSupplyPickup pickup = FindFirstObjectByType<WeaponSupplyPickup>();
        if (pickup == null)
        {
            return null;
        }

        for (int i = 0; i < pickup.transform.childCount; i++)
        {
            Transform child = pickup.transform.GetChild(i);
            if (child != null && child.GetComponentInChildren<Renderer>(true) != null)
            {
                return child.gameObject;
            }
        }

        return null;
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

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        if (root.name == childName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindChildRecursive(root.GetChild(i), childName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private static Transform FindHandBoneByKeyword(Transform root)
    {
        if (root == null)
        {
            return null;
        }

        Transform best = null;
        int bestScore = 0;
        FindHandBoneByKeywordRecursive(root, ref best, ref bestScore);
        return best;
    }

    private static void FindHandBoneByKeywordRecursive(Transform node, ref Transform best, ref int bestScore)
    {
        string lower = node.name.ToLowerInvariant();
        bool hasHand = lower.Contains("hand");
        bool hasRight = lower.Contains("right") || lower.EndsWith(".r") || lower.EndsWith("_r") || lower.EndsWith(" r");

        if (hasHand)
        {
            int score = 1;
            if (hasRight)
            {
                score += 2;
            }

            if (!lower.Contains("forearm") && !lower.Contains("upper") && !lower.Contains("shoulder"))
            {
                score += 1;
            }

            if (score > bestScore)
            {
                bestScore = score;
                best = node;
            }
        }

        for (int i = 0; i < node.childCount; i++)
        {
            FindHandBoneByKeywordRecursive(node.GetChild(i), ref best, ref bestScore);
        }
    }
}
