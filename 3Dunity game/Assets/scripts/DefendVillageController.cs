using System.Collections.Generic;
using UnityEngine;
using SUPERCharacter;

[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
public class DefendVillageController : MonoBehaviour
{
    [SerializeField] private MissionManager missionManager;
    [SerializeField] private string missionId = "defend_village";
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private VillagerSquadController squadController;
    [SerializeField] private float enemySearchRadius = 40f;
    [SerializeField] private EnemyHealth[] battleEnemies;
    [SerializeField] private bool activateEnemyCombatOnBattleStart = true;
    [Header("Village Health")]
    [SerializeField] private VillageHealth villageHealth;
    [SerializeField] private float villageThreatRadius = 12f;
    [SerializeField] private float villageDamageInterval = 2f;
    [SerializeField] private int villageDamagePerEnemy = 4;

    private readonly List<EnemyHealth> trackedEnemies = new List<EnemyHealth>();
    private bool battleStarted;
    private bool missionCompleted;
    private bool hadBattleTargets;
    private float villageDamageTimer;

    private void Awake()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }

        if (missionManager == null)
        {
            missionManager = MissionManager.Instance != null
                ? MissionManager.Instance
                : FindFirstObjectByType<MissionManager>();
        }

        if (squadController == null)
        {
            squadController = FindFirstObjectByType<VillagerSquadController>();
        }

        if (villageHealth == null)
        {
            villageHealth = FindFirstObjectByType<VillageHealth>();
        }
    }

    private void Update()
    {
        if (!battleStarted || missionCompleted)
        {
            return;
        }

        RefreshEnemies();
        UpdateVillageThreat();

        if (!hadBattleTargets || trackedEnemies.Count > 0)
        {
            return;
        }

        missionCompleted = true;
        if (squadController != null)
        {
            squadController.ExitBattleMode();
        }

        if (villageHealth != null)
        {
            villageHealth.SetUiVisible(false);
        }

        if (missionManager != null && missionManager.IsCurrentMission(missionId))
        {
            missionManager.AddProgress(missionId, 1);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (battleStarted || !IsPlayerCollider(other))
        {
            return;
        }

        if (missionManager == null || !missionManager.IsCurrentMission(missionId))
        {
            return;
        }

        StartBattle();
    }

    private void StartBattle()
    {
        battleStarted = true;
        RefreshEnemies();
        hadBattleTargets = trackedEnemies.Count > 0;
        villageDamageTimer = villageDamageInterval;

        if (villageHealth == null)
        {
            villageHealth = EnsureVillageHealthExists();
        }

        if (villageHealth != null)
        {
            villageHealth.ResetHealth();
            villageHealth.SetUiVisible(true);
        }

        if (squadController != null)
        {
            squadController.EnterBattleMode(transform.position);
        }

        if (activateEnemyCombatOnBattleStart)
        {
            for (int i = 0; i < trackedEnemies.Count; i++)
            {
                EnemyCombat enemyCombat = trackedEnemies[i] != null
                    ? trackedEnemies[i].GetComponent<EnemyCombat>()
                    : null;

                if (enemyCombat == null && trackedEnemies[i] != null)
                {
                    enemyCombat = trackedEnemies[i].gameObject.AddComponent<EnemyCombat>();
                }

                if (enemyCombat != null)
                {
                    enemyCombat.SetBattleActive(true);
                }
            }
        }

        Debug.Log($"DefendVillageController: Battle started with {trackedEnemies.Count} enemy target(s).");
    }

    private void UpdateVillageThreat()
    {
        if (villageHealth == null || villageHealth.IsDestroyed || trackedEnemies.Count == 0)
        {
            return;
        }

        villageDamageTimer -= Time.deltaTime;
        if (villageDamageTimer > 0f)
        {
            return;
        }

        villageDamageTimer = Mathf.Max(0.25f, villageDamageInterval);

        int enemiesThreateningVillage = 0;
        for (int i = 0; i < trackedEnemies.Count; i++)
        {
            EnemyHealth enemy = trackedEnemies[i];
            if (enemy == null || enemy.IsDead)
            {
                continue;
            }

            float distance = Vector3.Distance(Flatten(transform.position), Flatten(enemy.transform.position));
            if (distance <= villageThreatRadius)
            {
                enemiesThreateningVillage++;
            }
        }

        if (enemiesThreateningVillage > 0)
        {
            villageHealth.TakeDamage(enemiesThreateningVillage * Mathf.Max(1, villageDamagePerEnemy));
        }
    }

    private void RefreshEnemies()
    {
        trackedEnemies.RemoveAll(enemy => enemy == null || enemy.IsDead);
        if (trackedEnemies.Count > 0)
        {
            return;
        }

        if (battleEnemies != null && battleEnemies.Length > 0)
        {
            for (int i = 0; i < battleEnemies.Length; i++)
            {
                EnemyHealth enemy = battleEnemies[i];
                if (enemy != null && !enemy.IsDead && enemy.CanBeTargeted && !trackedEnemies.Contains(enemy))
                {
                    trackedEnemies.Add(enemy);
                }
            }
        }

        if (trackedEnemies.Count > 0)
        {
            return;
        }

        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyHealth enemy = enemies[i];
            if (enemy == null || enemy.IsDead || !enemy.CanBeTargeted)
            {
                continue;
            }

            float distance = Vector3.Distance(Flatten(transform.position), Flatten(enemy.transform.position));
            if (distance <= enemySearchRadius)
            {
                trackedEnemies.Add(enemy);
            }
        }
    }

    public void SetBattleEnemies(EnemyHealth[] enemies)
    {
        battleEnemies = enemies;
    }

    private VillageHealth EnsureVillageHealthExists()
    {
        VillageHealth existingHealth = FindFirstObjectByType<VillageHealth>();
        if (existingHealth != null)
        {
            return existingHealth;
        }

        GameObject healthObject = new GameObject("VillageHealth");
        return healthObject.AddComponent<VillageHealth>();
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

    private static Vector3 Flatten(Vector3 value)
    {
        value.y = 0f;
        return value;
    }
}
