using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyGroupController : MonoBehaviour
{
    [SerializeField] private EnemyHealth rootEnemyHealth;
    [SerializeField] private int fighterMaxHealth = 35;
    [SerializeField] private bool autoConfigureOnAwake = true;

    private readonly List<EnemyHealth> fighterHealths = new List<EnemyHealth>();

    public IReadOnlyList<EnemyHealth> FighterHealths => fighterHealths;

    private void Awake()
    {
        if (!autoConfigureOnAwake)
        {
            return;
        }

        ConfigureGroup();
    }

    [ContextMenu("Configure Enemy Group")]
    public void ConfigureGroup()
    {
        fighterHealths.Clear();

        if (!LooksLikeEnemyRoot(gameObject))
        {
            if (rootEnemyHealth != null)
            {
                rootEnemyHealth.SetBattleTargetEnabled(false);
                rootEnemyHealth.enabled = false;
            }

            return;
        }

        if (rootEnemyHealth == null)
        {
            rootEnemyHealth = GetComponent<EnemyHealth>();
        }

        bool rootHasVisibleEnemyModel = GetComponentInChildren<Renderer>(true) != null;
        bool rootShouldBeSingleFighter = rootHasVisibleEnemyModel && !IsEnemyContainerName(gameObject.name);

        if (rootEnemyHealth != null && !rootShouldBeSingleFighter)
        {
            rootEnemyHealth.SetBattleTargetEnabled(false);
            rootEnemyHealth.enabled = false;
        }

        if (rootShouldBeSingleFighter)
        {
            if (rootEnemyHealth == null)
            {
                rootEnemyHealth = gameObject.AddComponent<EnemyHealth>();
            }

            rootEnemyHealth.enabled = true;
            EnsureColliderExists(gameObject);
            EnsureCombatExists(gameObject);
            ConfigureEnemyHealth(rootEnemyHealth, 1);
            fighterHealths.Add(rootEnemyHealth);
            return;
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == null || child.GetComponentInChildren<Renderer>() == null)
            {
                continue;
            }

            EnemyHealth childHealth = child.GetComponent<EnemyHealth>();
            if (childHealth == null)
            {
                childHealth = child.gameObject.AddComponent<EnemyHealth>();
            }

            EnsureColliderExists(child.gameObject);
            EnsureCombatExists(child.gameObject);
            ConfigureEnemyHealth(childHealth, i + 1);
            fighterHealths.Add(childHealth);
        }

        if (fighterHealths.Count == 0 && rootHasVisibleEnemyModel)
        {
            if (rootEnemyHealth == null)
            {
                rootEnemyHealth = gameObject.AddComponent<EnemyHealth>();
            }

            rootEnemyHealth.enabled = true;
            EnsureColliderExists(gameObject);
            EnsureCombatExists(gameObject);
            ConfigureEnemyHealth(rootEnemyHealth, 1);
            fighterHealths.Add(rootEnemyHealth);
        }
    }

    private bool IsEnemyContainerName(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return false;
        }

        string lowerName = objectName.ToLowerInvariant();
        return lowerName == "enemygroup" ||
               lowerName == "enemy group" ||
               lowerName == "enemies" ||
               lowerName == "enemy_group";
    }

    private bool LooksLikeEnemyRoot(GameObject root)
    {
        if (root == null)
        {
            return false;
        }

        if (root.GetComponentInChildren<Animator>(true) != null)
        {
            return true;
        }

        int directChildEnemyIndicators = 0;
        for (int i = 0; i < root.transform.childCount; i++)
        {
            Transform child = root.transform.GetChild(i);
            if (child == null)
            {
                continue;
            }

            if (child.GetComponent<EnemyHealth>() != null ||
                child.GetComponent<EnemyCombat>() != null ||
                child.GetComponentInChildren<Animator>(true) != null)
            {
                directChildEnemyIndicators++;
            }
        }

        if (directChildEnemyIndicators > 0)
        {
            return true;
        }

        return root.GetComponent<EnemyHealth>() != null && root.transform.childCount == 0;
    }

    public EnemyHealth[] GetActiveFighters()
    {
        List<EnemyHealth> activeFighters = new List<EnemyHealth>();
        for (int i = 0; i < fighterHealths.Count; i++)
        {
            EnemyHealth fighter = fighterHealths[i];
            if (fighter != null)
            {
                activeFighters.Add(fighter);
            }
        }

        return activeFighters.ToArray();
    }

    private void ConfigureEnemyHealth(EnemyHealth enemyHealth, int fighterIndex)
    {
        if (enemyHealth == null)
        {
            return;
        }

        enemyHealth.SetBattleTargetEnabled(true);
        enemyHealth.SetMaxHealth(fighterMaxHealth);
        enemyHealth.ResetHealth();

        if (enemyHealth.gameObject.name == "Enemy")
        {
            enemyHealth.gameObject.name = $"Enemy Fighter {fighterIndex}";
        }
    }

    private void EnsureCombatExists(GameObject fighterObject)
    {
        if (fighterObject.GetComponent<EnemyCombat>() == null)
        {
            fighterObject.AddComponent<EnemyCombat>();
        }
    }

    private void EnsureColliderExists(GameObject fighterObject)
    {
        Collider collider = fighterObject.GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = false;
            return;
        }

        Bounds bounds = CalculateBounds(fighterObject);
        CapsuleCollider capsuleCollider = fighterObject.AddComponent<CapsuleCollider>();
        capsuleCollider.direction = 1;
        capsuleCollider.center = fighterObject.transform.InverseTransformPoint(bounds.center);
        capsuleCollider.height = Mathf.Max(1.6f, bounds.size.y);
        capsuleCollider.radius = Mathf.Max(0.35f, Mathf.Max(bounds.extents.x, bounds.extents.z) * 0.6f);
    }

    private Bounds CalculateBounds(GameObject target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return new Bounds(target.transform.position, new Vector3(1f, 2f, 1f));
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }
}
