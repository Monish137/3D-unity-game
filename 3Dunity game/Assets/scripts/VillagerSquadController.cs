using System.Collections.Generic;
using UnityEngine;
using SUPERCharacter;

[DisallowMultipleComponent]
public class VillagerSquadController : MonoBehaviour
{
    [SerializeField] private MissionManager missionManager;
    [SerializeField] private Transform player;
    [SerializeField] private string followUnlockMissionId = "arm_villagers";
    [SerializeField] private float moveSpeed = 2.1f;
    [SerializeField] private float turnSpeed = 360f;
    [SerializeField] private float followDistance = 3f;
    [SerializeField] private float rowSpacing = 1.7f;
    [SerializeField] private float groundProbeHeight = 6f;
    [SerializeField] private float groundSnapOffset = 0.05f;
    [SerializeField] private LayerMask groundLayers = ~0;

    private readonly List<Transform> villagerUnits = new List<Transform>();
    private readonly List<VillagerCombat> villagerCombatUnits = new List<VillagerCombat>();
    private bool followActive;
    private bool battleActive;

    public bool FollowActive => followActive;
    public bool BattleActive => battleActive;

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
            PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>();
            if (inventory != null)
            {
                player = inventory.transform;
            }
            else
            {
                SUPERCharacterAIO character = FindFirstObjectByType<SUPERCharacterAIO>();
                if (character != null)
                {
                    player = character.transform;
                }
            }
        }

        CacheVillagers();
    }

    private void Update()
    {
        if (!battleActive && !followActive && missionManager != null && missionManager.IsMissionCompleted(followUnlockMissionId))
        {
            StartFollowing();
        }

        if (!followActive || battleActive || player == null)
        {
            return;
        }

        UpdateFollowFormation();
    }

    public void StartFollowing()
    {
        followActive = true;
        battleActive = false;
        for (int i = 0; i < villagerCombatUnits.Count; i++)
        {
            if (villagerCombatUnits[i] != null)
            {
                villagerCombatUnits[i].SetCombatEnabled(false, Vector3.zero);
            }
        }
    }

    public void EnterBattleMode(Vector3 battleCenter)
    {
        battleActive = true;
        followActive = false;

        for (int i = 0; i < villagerCombatUnits.Count; i++)
        {
            if (villagerCombatUnits[i] != null)
            {
                villagerCombatUnits[i].SetCombatEnabled(true, battleCenter);
            }
        }
    }

    public void ExitBattleMode()
    {
        battleActive = false;
        for (int i = 0; i < villagerCombatUnits.Count; i++)
        {
            if (villagerCombatUnits[i] != null)
            {
                villagerCombatUnits[i].SetCombatEnabled(false, Vector3.zero);
            }
        }
    }

    private void CacheVillagers()
    {
        villagerUnits.Clear();
        villagerCombatUnits.Clear();

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == null || child.GetComponentInChildren<Animator>() == null)
            {
                continue;
            }

            villagerUnits.Add(child);

            VillagerCombat combat = child.GetComponent<VillagerCombat>();
            if (combat == null)
            {
                combat = child.gameObject.AddComponent<VillagerCombat>();
            }

            villagerCombatUnits.Add(combat);
        }
    }

    private void UpdateFollowFormation()
    {
        for (int i = 0; i < villagerUnits.Count; i++)
        {
            Transform villager = villagerUnits[i];
            if (villager == null)
            {
                continue;
            }

            Vector3 targetPosition = GetFormationPosition(i);
            MoveVillager(villager, targetPosition);
        }
    }

    private Vector3 GetFormationPosition(int index)
    {
        int row = index / 2;
        int column = index % 2 == 0 ? -1 : 1;

        Vector3 behind = -player.forward * (followDistance + row * rowSpacing);
        Vector3 side = player.right * column * rowSpacing * 0.8f;
        Vector3 desired = player.position + behind + side;
        desired.y = transform.position.y;
        return desired;
    }

    private void MoveVillager(Transform villager, Vector3 targetPosition)
    {
        Vector3 current = Flatten(villager.position);
        Vector3 target = Flatten(targetPosition);
        Vector3 toTarget = target - current;
        float distance = toTarget.magnitude;

        if (distance <= 0.3f)
        {
            villager.position = GetGroundedPosition(villager.position);
            return;
        }

        Vector3 direction = toTarget.normalized;
        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        villager.rotation = Quaternion.RotateTowards(villager.rotation, targetRotation, turnSpeed * Time.deltaTime);

        float moveAmount = Mathf.Min(moveSpeed * Time.deltaTime, distance);
        Vector3 nextPosition = villager.position + direction * moveAmount;
        villager.position = GetGroundedPosition(nextPosition);
    }

    private Vector3 GetGroundedPosition(Vector3 desiredPosition)
    {
        Vector3 rayOrigin = desiredPosition + Vector3.up * groundProbeHeight;
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundProbeHeight * 3f, groundLayers, QueryTriggerInteraction.Ignore))
        {
            desiredPosition.y = hit.point.y + groundSnapOffset;
        }

        return desiredPosition;
    }

    private static Vector3 Flatten(Vector3 value)
    {
        value.y = 0f;
        return value;
    }
}
