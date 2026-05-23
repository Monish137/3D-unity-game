using UnityEngine;

public class ChapterOneAutoSetup : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private MissionManager missionManager;

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private NpcDialogue merlinDialogue;
    [SerializeField] private Camera gameplayCamera;

    [Header("Scene Positions")]
    [SerializeField] private Vector3 ectorPosition = new Vector3(112f, 63f, 173f);
    [SerializeField] private Vector3 swordPosition = new Vector3(116f, 63f, 170f);
    [SerializeField] private Vector3[] huntPositions =
    {
        new Vector3(150f, 61f, 214f),
        new Vector3(156f, 61f, 224f),
        new Vector3(142f, 61f, 229f)
    };
    [SerializeField] private Vector3 villageReturnPosition = new Vector3(114f, 63f, 176f);
    [SerializeField] private Vector3 noticeBoardPosition = new Vector3(123f, 63f, 180f);
    [SerializeField] private Vector3[] villagerArmPoints =
    {
        new Vector3(121f, 63f, 172f),
        new Vector3(126f, 63f, 170f),
        new Vector3(129f, 63f, 176f),
        new Vector3(118f, 63f, 182f)
    };
    [SerializeField] private Vector3 defendPoint = new Vector3(98f, 62f, 188f);

    [Header("Debug Visuals")]
    [SerializeField] private bool showGeneratedMarkers;

    private void Awake()
    {
        if (missionManager == null)
        {
            missionManager = MissionManager.Instance != null
                ? MissionManager.Instance
                : FindFirstObjectByType<MissionManager>();
        }

        if (player == null)
        {
            Minimap minimap = FindFirstObjectByType<Minimap>();
            if (minimap != null)
            {
                player = FindPlayerFromMinimap(minimap);
            }
        }

        if (merlinDialogue == null)
        {
            merlinDialogue = FindFirstObjectByType<NpcDialogue>();
        }
    }

    private void Start()
    {
        if (missionManager == null)
        {
            Debug.LogWarning("ChapterOneAutoSetup could not find MissionManager.");
            return;
        }

        EnsurePlayerTag();
        EnsurePlayerSkills();
        ConfigureMission8Objects();
        ConfigureMerlin();
        CreateMissionTrigger("GoToEctorTrigger", "go_to_ector", ectorPosition, new Vector3(4f, 3f, 4f));
        CreateCollectible("SwordPickup", "领取新剑", "pick_up_sword", swordPosition, Color.yellow);
        ConfigureHuntAnimals();

        CreateMissionTrigger("ReturnToEctorTrigger", "return_to_ector", villageReturnPosition, new Vector3(4f, 3f, 4f));
        CreateMissionTrigger("NoticeBoardTrigger", "read_notice_board", noticeBoardPosition, new Vector3(4f, 3f, 4f));

        CreateInteractPoint("WarnEctorPoint", "通知艾克特", "warn_ector", villageReturnPosition + new Vector3(2f, 0f, -1f));

        for (int i = 0; i < villagerArmPoints.Length; i++)
        {
            CreateInteractPoint($"ArmVillager_{i + 1}", "分发武器", "arm_villagers", villagerArmPoints[i], true);
        }

        ConfigureArmVillagerReceivers();

        Vector3 battlePoint = ResolveBattlePoint();
        CreateDefendVillageZone("DefendVillageTrigger", battlePoint, new Vector3(10f, 3f, 10f));
        CreateInteractPoint("AfterBattleTalkPoint", "战后交谈", "talk_after_battle", noticeBoardPosition + new Vector3(-2f, 0f, 2f));
    }

    private void ConfigureMerlin()
    {
        if (merlinDialogue == null)
        {
            return;
        }

        SetFieldIfEmpty(merlinDialogue, "missionManager", missionManager);
        SetFieldIfEmpty(merlinDialogue, "missionId", "talk_to_merlin");
    }

    private void ConfigureMission8Objects()
    {
        GameObject villagerRoot = GameObject.Find("villager");
        if (villagerRoot != null)
        {
            if (villagerRoot.GetComponent<VillagerSquadController>() == null)
            {
                VillagerSquadController squadController = villagerRoot.AddComponent<VillagerSquadController>();
                SetPrivateField(squadController, "missionManager", missionManager);
                SetPrivateField(squadController, "player", player);
            }

            NpcDialogue villagerDialogue = villagerRoot.GetComponentInChildren<NpcDialogue>(true);
            if (villagerDialogue != null)
            {
                SetPrivateField(villagerDialogue, "completeMissionWhenDialogueEnds", false);
                SetPrivateField(villagerDialogue, "oneTimeOnly", true);

                if (villagerDialogue.GetComponent<VillagerWeaponReceiver>() == null)
                {
                    VillagerWeaponReceiver receiver = villagerDialogue.gameObject.AddComponent<VillagerWeaponReceiver>();
                    SetPrivateField(receiver, "missionManager", missionManager);
                    SetPrivateField(receiver, "missionId", "arm_villagers");
                    SetPrivateField(receiver, "requiredItemId", "villager_weapon");
                    SetPrivateField(receiver, "amountPerHandIn", 1);
                    SetPrivateField(receiver, "villagerGroupRoot", villagerRoot.transform);
                }
            }
        }

        string[] supplyNames =
        {
            "Armed weapons",
            "Armed Weapons",
            "armed weapons",
            "Armed_weapons",
            "ArmedWeapons",
            "Weapons"
        };

        for (int i = 0; i < supplyNames.Length; i++)
        {
            GameObject supplyObject = GameObject.Find(supplyNames[i]);
            if (supplyObject == null)
            {
                continue;
            }

            if (supplyObject.GetComponent<WeaponSupplyPickup>() == null)
            {
                WeaponSupplyPickup pickup = supplyObject.AddComponent<WeaponSupplyPickup>();
                SetPrivateField(pickup, "inventoryItemId", "villager_weapon");
                SetPrivateField(pickup, "inventoryDisplayName", "Villager Weapon");
                SetPrivateField(pickup, "inventoryItemType", PlayerInventory.InventoryItemType.Material);
                SetPrivateField(pickup, "amountPerPickup", 1);
                SetPrivateField(pickup, "totalAvailable", 4);
            }

            break;
        }
    }

    private void ConfigureArmVillagerReceivers()
    {
        GameObject villagerRoot = GameObject.Find("villager");

        for (int i = 0; i < villagerArmPoints.Length; i++)
        {
            GameObject armPoint = GameObject.Find($"ArmVillager_{i + 1}");
            if (armPoint == null)
            {
                continue;
            }

            MissionInteractable interactable = armPoint.GetComponent<MissionInteractable>();
            if (interactable != null)
            {
                Destroy(interactable);
            }

            VillagerWeaponReceiver receiver = armPoint.GetComponent<VillagerWeaponReceiver>();
            if (receiver == null)
            {
                receiver = armPoint.AddComponent<VillagerWeaponReceiver>();
            }

            SetPrivateField(receiver, "missionManager", missionManager);
            SetPrivateField(receiver, "missionId", "arm_villagers");
            SetPrivateField(receiver, "requiredItemId", "villager_weapon");
            SetPrivateField(receiver, "amountPerHandIn", 1);

            if (villagerRoot != null)
            {
                SetPrivateField(receiver, "villagerGroupRoot", villagerRoot.transform);
            }
        }
    }

    private void EnsurePlayerTag()
    {
        if (player != null && player.tag != "Player")
        {
            player.tag = "Player";
        }
    }

    private void EnsurePlayerSkills()
    {
        if (player == null)
        {
            return;
        }

        PlayerSkills playerSkills = player.GetComponent<PlayerSkills>();
        if (playerSkills == null)
        {
            playerSkills = player.gameObject.AddComponent<PlayerSkills>();
        }

        if (player.GetComponent<PlayerInventory>() == null)
        {
            player.gameObject.AddComponent<PlayerInventory>();
        }

        if (gameplayCamera == null)
        {
            gameplayCamera = FindGameplayCamera();
        }

        SetPrivateField(playerSkills, "attackCamera", gameplayCamera);
        SetPrivateField(playerSkills, "attackOrigin", player);
    }

    private void ConfigureHuntAnimals()
    {
        AnimalHealth[] existingAnimals = FindObjectsByType<AnimalHealth>(FindObjectsSortMode.None);
        if (existingAnimals.Length > 0)
        {
            return;
        }

        GameObject[] roots = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int configuredCount = 0;

        for (int i = 0; i < roots.Length; i++)
        {
            GameObject candidate = roots[i];
            if (candidate == null)
            {
                continue;
            }

            string lowerName = candidate.name.ToLowerInvariant();
            if (!IsHuntAnimalName(lowerName))
            {
                continue;
            }

            if (candidate.GetComponentInChildren<Renderer>() == null)
            {
                continue;
            }

            if (candidate.GetComponent<Animator>() == null && candidate.GetComponent<BearMovement>() == null)
            {
                continue;
            }

            AnimalHealth animalHealth = candidate.GetComponent<AnimalHealth>();
            if (animalHealth == null)
            {
                animalHealth = candidate.AddComponent<AnimalHealth>();
            }

            configuredCount++;
        }

        if (configuredCount == 0)
        {
            for (int i = 0; i < huntPositions.Length; i++)
            {
                CreateFallbackAnimal($"HuntAnimal_{i + 1}", huntPositions[i]);
            }
        }
    }

    private bool IsHuntAnimalName(string lowerName)
    {
        return lowerName.Contains("bear") ||
               lowerName.Contains("boar") ||
               lowerName.Contains("stag") ||
               lowerName.Contains("moose") ||
               lowerName.Contains("doe") ||
               lowerName.Contains("wolf") ||
               lowerName.Contains("fox") ||
               lowerName.Contains("hare") ||
               lowerName.Contains("calf");
    }

    private void CreateFallbackAnimal(string objectName, Vector3 position)
    {
        if (GameObject.Find(objectName) != null)
        {
            return;
        }

        GameObject animal = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        animal.name = objectName;
        animal.transform.position = position;
        animal.transform.localScale = new Vector3(1.6f, 1.2f, 1.6f);

        Renderer renderer = animal.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color(0.36f, 0.24f, 0.16f);
        }

        animal.AddComponent<AnimalHealth>();
        if (showGeneratedMarkers)
        {
            CreateWorldLabel(animal.transform, "猎物");
        }
    }

    private void CreateMissionTrigger(string objectName, string missionId, Vector3 position, Vector3 size)
    {
        if (GameObject.Find(objectName) != null)
        {
            return;
        }

        GameObject triggerObject = new GameObject(objectName);
        triggerObject.transform.position = position;

        BoxCollider collider = triggerObject.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = size;

        MissionTrigger missionTrigger = triggerObject.AddComponent<MissionTrigger>();
        SetPrivateField(missionTrigger, "missionManager", missionManager);
        SetPrivateField(missionTrigger, "missionId", missionId);
    }

    private void CreateDefendVillageZone(string objectName, Vector3 position, Vector3 size)
    {
        DisableMissionTriggersForMission("defend_village");

        GameObject zoneObject = GameObject.Find(objectName);
        if (zoneObject == null)
        {
            zoneObject = new GameObject(objectName);
        }

        zoneObject.transform.position = position;

        BoxCollider collider = zoneObject.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = zoneObject.AddComponent<BoxCollider>();
        }

        collider.isTrigger = true;
        collider.size = size;

        DefendVillageController controller = zoneObject.GetComponent<DefendVillageController>();
        if (controller == null)
        {
            controller = zoneObject.AddComponent<DefendVillageController>();
        }

        SetPrivateField(controller, "missionManager", missionManager);
        SetPrivateField(controller, "missionId", "defend_village");

        VillageHealth villageHealth = FindFirstObjectByType<VillageHealth>();
        if (villageHealth == null)
        {
            GameObject healthObject = new GameObject("VillageHealth");
            villageHealth = healthObject.AddComponent<VillageHealth>();
        }

        SetPrivateField(controller, "villageHealth", villageHealth);

        VillagerSquadController squadController = FindFirstObjectByType<VillagerSquadController>();
        if (squadController != null)
        {
            SetPrivateField(controller, "squadController", squadController);
        }

        EnemyGroupController enemyGroup = ConfigureEnemyGroup();
        if (enemyGroup != null)
        {
            controller.SetBattleEnemies(enemyGroup.GetActiveFighters());
        }
        else
        {
            EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
            controller.SetBattleEnemies(enemies);
        }
    }

    private Vector3 ResolveBattlePoint()
    {
        EnemyGroupController enemyGroup = ConfigureEnemyGroup();
        EnemyHealth[] fighters = enemyGroup != null ? enemyGroup.GetActiveFighters() : null;

        if (fighters != null && fighters.Length > 0 && fighters[0] != null)
        {
            Vector3 enemyPosition = fighters[0].transform.position;
            enemyPosition.y += 1f;
            return enemyPosition;
        }

        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] == null || !enemies[i].CanBeTargeted)
            {
                continue;
            }

            Vector3 enemyPosition = enemies[i].transform.position;
            enemyPosition.y += 1f;
            return enemyPosition;
        }

        return defendPoint;
    }

    private EnemyGroupController ConfigureEnemyGroup()
    {
        string[] candidateNames = { "EnemyGroup", "Enemies", "Enemy", "enemy" };

        for (int i = 0; i < candidateNames.Length; i++)
        {
            GameObject enemyRoot = GameObject.Find(candidateNames[i]);
            if (enemyRoot == null)
            {
                continue;
            }

            EnemyGroupController groupController = enemyRoot.GetComponent<EnemyGroupController>();
            if (groupController == null)
            {
                groupController = enemyRoot.AddComponent<EnemyGroupController>();
            }

            groupController.ConfigureGroup();
            EnemyHealth[] fighters = groupController.GetActiveFighters();
            if (fighters != null && fighters.Length > 0)
            {
                return groupController;
            }
        }

        return CreateFallbackEnemyGroup();
    }

    private EnemyGroupController CreateFallbackEnemyGroup()
    {
        GameObject existingGroup = GameObject.Find("EnemyGroup");
        if (existingGroup != null)
        {
            EnemyGroupController existingController = existingGroup.GetComponent<EnemyGroupController>();
            if (existingController == null)
            {
                existingController = existingGroup.AddComponent<EnemyGroupController>();
            }

            existingController.ConfigureGroup();
            return existingController;
        }

        GameObject enemyGroup = new GameObject("EnemyGroup");
        enemyGroup.transform.position = defendPoint;

        Vector3[] offsets =
        {
            new Vector3(-2f, 0f, 0f),
            new Vector3(1.8f, 0f, 1.4f),
            new Vector3(2.4f, 0f, -1.6f)
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.name = $"Enemy Fighter {i + 1}";
            enemy.transform.SetParent(enemyGroup.transform, false);
            enemy.transform.localPosition = offsets[i];
            enemy.transform.localScale = new Vector3(1f, 1.7f, 1f);

            Renderer renderer = enemy.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.45f, 0.08f, 0.06f);
            }

            EnemyHealth health = enemy.AddComponent<EnemyHealth>();
            health.SetMaxHealth(45);
            enemy.AddComponent<EnemyCombat>();
        }

        EnemyGroupController groupController = enemyGroup.AddComponent<EnemyGroupController>();
        groupController.ConfigureGroup();
        return groupController;
    }

    private void CreateCollectible(string objectName, string label, string missionId, Vector3 position, Color color)
    {
        if (GameObject.Find(objectName) != null)
        {
            return;
        }

        GameObject collectible = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        collectible.name = objectName;
        collectible.transform.position = position;
        collectible.transform.localScale = new Vector3(0.8f, 1.2f, 0.8f);

        Renderer renderer = collectible.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }

        Collider collider = collectible.GetComponent<Collider>();
        collider.isTrigger = true;

        MissionCollectible missionCollectible = collectible.AddComponent<MissionCollectible>();
        SetPrivateField(missionCollectible, "missionManager", missionManager);
        SetPrivateField(missionCollectible, "missionId", missionId);
        SetPrivateField(missionCollectible, "amount", 1);

        if (missionId == "pick_up_sword")
        {
            collectible.AddComponent<SwordPickup>();
        }

        SetGeneratedObjectVisible(collectible, showGeneratedMarkers);

        if (showGeneratedMarkers)
        {
            CreateWorldLabel(collectible.transform, label);
        }
    }

    private void CreateInteractPoint(string objectName, string label, string missionId, Vector3 position, bool hideAfterInteract = false)
    {
        if (GameObject.Find(objectName) != null)
        {
            return;
        }

        GameObject interactPoint = new GameObject(objectName);
        interactPoint.name = objectName;
        interactPoint.transform.position = position;

        BoxCollider collider = interactPoint.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = new Vector3(2.2f, 2.5f, 2.2f);

        if (missionId == "arm_villagers")
        {
            VillagerWeaponReceiver receiver = interactPoint.AddComponent<VillagerWeaponReceiver>();
            SetPrivateField(receiver, "missionManager", missionManager);
            SetPrivateField(receiver, "missionId", missionId);
            SetPrivateField(receiver, "requiredItemId", "villager_weapon");
            SetPrivateField(receiver, "amountPerHandIn", 1);

            GameObject villagerRoot = GameObject.Find("villager");
            if (villagerRoot != null)
            {
                SetPrivateField(receiver, "villagerGroupRoot", villagerRoot.transform);
            }
        }
        else
        {
            MissionInteractable interactable = interactPoint.AddComponent<MissionInteractable>();
            SetPrivateField(interactable, "missionManager", missionManager);
            SetPrivateField(interactable, "missionId", missionId);
            SetPrivateField(interactable, "hideAfterInteract", hideAfterInteract);
        }

        if (showGeneratedMarkers)
        {
            GameObject debugVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            debugVisual.name = $"{objectName}_DebugVisual";
            debugVisual.transform.SetParent(interactPoint.transform, false);
            debugVisual.transform.localPosition = Vector3.zero;
            debugVisual.transform.localScale = new Vector3(0.8f, 1f, 0.8f);

            Collider debugCollider = debugVisual.GetComponent<Collider>();
            if (debugCollider != null)
            {
                Destroy(debugCollider);
            }

            Renderer renderer = debugVisual.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.2f, 0.7f, 0.95f);
            }

            CreateWorldLabel(interactPoint.transform, label);
        }
    }

    private void CreateWorldLabel(Transform parent, string label)
    {
        GameObject labelRoot = new GameObject($"{label}_Label");
        labelRoot.transform.SetParent(parent, false);
        labelRoot.transform.localPosition = new Vector3(0f, 1.6f, 0f);

        TextMesh textMesh = labelRoot.AddComponent<TextMesh>();
        textMesh.text = label;
        textMesh.characterSize = 0.15f;
        textMesh.fontSize = 48;
        textMesh.color = Color.white;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
    }

    private void SetGeneratedObjectVisible(GameObject target, bool isVisible)
    {
        if (target == null)
        {
            return;
        }

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = isVisible;
            }
        }
    }

    private Transform FindPlayerFromMinimap(Minimap minimap)
    {
        System.Reflection.FieldInfo targetField = typeof(Minimap).GetField("target",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return targetField?.GetValue(minimap) as Transform;
    }

    private Camera FindGameplayCamera()
    {
        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera candidate = cameras[i];
            if (!candidate.isActiveAndEnabled || candidate.targetTexture != null)
            {
                continue;
            }

            if (candidate.name.ToLowerInvariant().Contains("mini"))
            {
                continue;
            }

            return candidate;
        }

        return Camera.main;
    }

    private void SetPrivateField(Object targetObject, string fieldName, object value)
    {
        if (targetObject == null)
        {
            return;
        }

        System.Reflection.FieldInfo field = targetObject.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (field != null)
        {
            field.SetValue(targetObject, value);
        }
    }

    private void SetFieldIfEmpty(Object targetObject, string fieldName, object value)
    {
        if (targetObject == null || value == null)
        {
            return;
        }

        System.Reflection.FieldInfo field = targetObject.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (field == null)
        {
            return;
        }

        object currentValue = field.GetValue(targetObject);

        if (field.FieldType == typeof(string))
        {
            if (string.IsNullOrWhiteSpace(currentValue as string))
            {
                field.SetValue(targetObject, value);
            }
            return;
        }

        if (currentValue == null || currentValue.Equals(null))
        {
            field.SetValue(targetObject, value);
        }
    }

    private void DisableMissionTriggersForMission(string missionId)
    {
        MissionTrigger[] missionTriggers = FindObjectsByType<MissionTrigger>(FindObjectsSortMode.None);
        for (int i = 0; i < missionTriggers.Length; i++)
        {
            MissionTrigger trigger = missionTriggers[i];
            if (trigger == null)
            {
                continue;
            }

            System.Reflection.FieldInfo field = typeof(MissionTrigger).GetField("missionId",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field == null)
            {
                continue;
            }

            string currentMissionId = field.GetValue(trigger) as string;
            if (currentMissionId == missionId)
            {
                trigger.enabled = false;
            }
        }
    }
}
