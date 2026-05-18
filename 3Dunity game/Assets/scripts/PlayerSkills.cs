using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSkills : MonoBehaviour
{
    [Header("Sword Skill")]
    [SerializeField] private bool hasSword;
    [SerializeField] private int baseAttackPower = 5;
    [SerializeField] private int swordAttackPower = 15;
    [SerializeField] private int baseDefense;
    [SerializeField] private int swordDefenseBonus = 5;

    [Header("Input")]
    [SerializeField] private KeyCode attackKey = KeyCode.Mouse0;
    [SerializeField] private KeyCode defenseKey = KeyCode.None;
    [SerializeField] private KeyCode skillKey = KeyCode.Mouse1;

    [Header("Stats")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int startingHealth = 100;
    [SerializeField] private int maxMana = 60;
    [SerializeField] private int startingMana = 60;
    [SerializeField] private float manaRegenPerSecond = 1.5f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string attackTriggerName = "Attack";
    [SerializeField] private string defenseTriggerName = "Defense";
    [SerializeField] private string swordHandBoneName = "Hand.R";
    [SerializeField] private string[] swordHandBoneFallbackNames =
    {
        "Hand.R",
        "hand.R",
        "Hand_R",
        "RightHand",
        "Right Hand",
        "R_Hand",
        "Hand_Right",
        "mixamorig:RightHand",
        "mixamorig1:RightHand",
        "Bip001 R Hand",
        "Bip01 R Hand",
        "B_R_Hand",
        "DEF-hand.R"
    };
    [SerializeField] private Vector3 swordLocalPosition = new Vector3(-0.02f, 0.02f, 0.02f);
    [SerializeField] private Vector3 swordLocalRotation = new Vector3(0f, 0f, 90f);
    [SerializeField] private Vector3 swordLocalScale = new Vector3(0.18f, 0.65f, 0.08f);

    [Header("Fallback (when no hand bone found)")]
    [SerializeField] private Vector3 swordFallbackLocalPosition = new Vector3(0.4f, 1.1f, 0.3f);
    [SerializeField] private Vector3 swordFallbackLocalRotation = new Vector3(0f, 0f, 60f);
    [SerializeField] private Vector3 swordFallbackLocalScale = new Vector3(0.18f, 0.65f, 0.08f);

    [Header("Attack")]
    [SerializeField] private Camera attackCamera;
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private float attackRadius = 1.25f;
    [SerializeField] private LayerMask attackMask = ~0;
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private float attackDamageDelay = 0.2f;

    [Header("Skill")]
    [SerializeField] private bool phantomSkillUnlocked;
    [SerializeField] private GameObject phantomVisualPrefab;
    [SerializeField] private GameObject phantomSwordPrefab;
    [SerializeField] private string phantomVisualLayerName = "Ignore Raycast";
    [SerializeField] private int phantomSkillManaCost = 20;
    [SerializeField] private float phantomSkillCooldown = 2f;
    [SerializeField] private float phantomSkillDamageMultiplier = 0.8f;
    [SerializeField] private float phantomSkillRangeMultiplier = 1f;
    [SerializeField] private float phantomSkillRadiusMultiplier = 1f;
    [SerializeField] private float phantomSkillDamageDelay = 0.15f;
    [SerializeField] private float phantomLifetime = 0.6f;
    [SerializeField] private Vector3 phantomSpawnOffset = new Vector3(0f, 0f, 1.5f);
    [SerializeField] private string lowManaHintText = "魔法不足";
    [SerializeField] private float lowManaHintCooldown = 1.2f;

    private float attackTimer;
    private float pendingAttackDamageTimer = -1f;
    private int currentHealth;
    private int currentMana;
    private float manaRegenBuffer;
    private float phantomSkillTimer;
    private float lowManaHintTimer;


    public int AttackPower => hasSword ? swordAttackPower : baseAttackPower;
    public int DefensePower => baseDefense + (hasSword ? swordDefenseBonus : 0);
    public bool HasSword => hasSword;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public int CurrentMana => currentMana;
    public int MaxMana => maxMana;
    public bool PhantomSkillUnlocked => phantomSkillUnlocked;
    public float PhantomSkillCooldown => phantomSkillCooldown;
    public float PhantomSkillCooldownRemaining => Mathf.Max(0f, phantomSkillTimer);
    public float PhantomSkillCooldownNormalized => phantomSkillCooldown > 0f ? Mathf.Clamp01(phantomSkillTimer / phantomSkillCooldown) : 0f;
    public event Action<bool> SwordEquipChanged;
    public event Action<int, int> HealthChanged;
    public event Action<int, int> ManaChanged;
    public event Action<bool> PhantomSkillUnlockedChanged;
    public event Action<string> HintRequested;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (attackOrigin == null)
        {
            attackOrigin = transform;
        }

        if (attackCamera == null)
        {
            attackCamera = Camera.main;
        }

        currentHealth = Mathf.Clamp(startingHealth, 0, Mathf.Max(1, maxHealth));
        currentMana = Mathf.Clamp(startingMana, 0, Mathf.Max(0, maxMana));

        EnsureSwordVisualExists();
        UpdateSwordVisual();
        NotifyHealthChanged();
        NotifyManaChanged();

        MissionManager missionManager = MissionManager.Instance != null ? MissionManager.Instance : FindFirstObjectByType<MissionManager>();
        if (missionManager != null && missionManager.IsMissionCompleted("talk_to_merlin"))
        {
            UnlockPhantomSkill();
        }
    }

    private void Update()
    {
        attackTimer -= Time.deltaTime;
        phantomSkillTimer -= Time.deltaTime;
        lowManaHintTimer -= Time.deltaTime;

        if (currentMana < maxMana && manaRegenPerSecond > 0f)
        {
            manaRegenBuffer += manaRegenPerSecond * Time.deltaTime;
            if (manaRegenBuffer >= 1f)
            {
                int manaToRestore = Mathf.Min(maxMana - currentMana, Mathf.FloorToInt(manaRegenBuffer));
                if (manaToRestore > 0)
                {
                    currentMana += manaToRestore;
                    manaRegenBuffer -= manaToRestore;
                    NotifyManaChanged();
                }
            }
        }
        else
        {
            manaRegenBuffer = 0f;
        }

        if (pendingAttackDamageTimer >= 0f)
        {
            pendingAttackDamageTimer -= Time.deltaTime;
            if (pendingAttackDamageTimer <= 0f)
            {
                pendingAttackDamageTimer = -1f;
                DamageAnimalInFront();
            }
        }

        if (!hasSword)
        {
            return;
        }

        if (Input.GetKeyDown(attackKey))
        {
            Attack();
        }

        if (skillKey != KeyCode.None && Input.GetKeyDown(skillKey))
        {
            TryUseDefaultSkill();
        }

        if (defenseKey != KeyCode.None && Input.GetKeyDown(defenseKey))
        {
            Defend();
        }
    }

    public void UnlockSword()
    {
        SetSwordEquipped(true);
    }

    public void SetSwordEquipped(bool equipped)
    {
        if (hasSword == equipped)
        {
            EnsureSwordVisualExists();
            UpdateSwordVisual();
            return;
        }

        hasSword = equipped;
        EnsureSwordVisualExists();
        UpdateSwordVisual();
        SwordEquipChanged?.Invoke(hasSword);
        Debug.Log(equipped
            ? "PlayerSkills: Sword equipped. Attack and defense are now available."
            : "PlayerSkills: Sword unequipped.");
    }

    public void Attack()
    {
        if (attackTimer > 0f)
        {
            return;
        }

        attackTimer = attackCooldown;
        pendingAttackDamageTimer = Mathf.Max(0f, attackDamageDelay);

        if (animator != null)
        {
            TrySetTrigger(attackTriggerName);
        }

        Debug.Log($"PlayerSkills: Attack triggered. Power={AttackPower}, damageDelay={pendingAttackDamageTimer:0.00}s");
    }

    public void Defend()
    {
        if (animator != null)
        {
            TrySetTrigger(defenseTriggerName);
        }

        Debug.Log($"PlayerSkills: Defense triggered. Defense={DefensePower}");
    }

    public bool TryUseDefaultSkill()
    {
        if (!phantomSkillUnlocked || phantomSkillTimer > 0f)
        {
            return false;
        }

        if (!ConsumeMana(phantomSkillManaCost))
        {
            RequestHint(lowManaHintText);
            return false;
        }

        Vector3 spawnPosition = transform.position + transform.TransformDirection(phantomSpawnOffset);
        GameObject phantomObject = new GameObject("PhantomSlash");
        phantomObject.transform.position = spawnPosition;
        phantomObject.transform.rotation = Quaternion.LookRotation(transform.forward, Vector3.up);

        PhantomSkill phantomSkill = phantomObject.AddComponent<PhantomSkill>();
        phantomSkill.Initialize(
            this,
            Mathf.Max(1, Mathf.RoundToInt(AttackPower * phantomSkillDamageMultiplier)),
            attackRange * phantomSkillRangeMultiplier,
            attackRadius * phantomSkillRadiusMultiplier,
            phantomSkillDamageDelay,
            phantomLifetime,
            phantomVisualPrefab,
            phantomSwordPrefab,
            phantomVisualLayerName,
            swordHandBoneName,
            swordHandBoneFallbackNames,
            swordLocalPosition,
            swordLocalRotation,
            swordLocalScale);

        phantomSkillTimer = phantomSkillCooldown;
        Debug.Log("PlayerSkills: Phantom skill cast.");
        return true;
    }

    public void UnlockPhantomSkill()
    {
        if (phantomSkillUnlocked)
        {
            return;
        }

        phantomSkillUnlocked = true;
        PhantomSkillUnlockedChanged?.Invoke(true);
        Debug.Log("PlayerSkills: Phantom skill unlocked.");
    }

    public void TakeDamage(int amount)
    {
        currentHealth = Mathf.Clamp(currentHealth - Mathf.Max(0, amount), 0, Mathf.Max(1, maxHealth));
        NotifyHealthChanged();
    }

    public void RestoreHealth(int amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + Mathf.Max(0, amount), 0, Mathf.Max(1, maxHealth));
        NotifyHealthChanged();
    }

    public bool ConsumeMana(int amount)
    {
        int manaCost = Mathf.Max(0, amount);
        if (currentMana < manaCost)
        {
            return false;
        }

        currentMana -= manaCost;
        NotifyManaChanged();
        return true;
    }

    public void RestoreMana(int amount)
    {
        currentMana = Mathf.Clamp(currentMana + Mathf.Max(0, amount), 0, Mathf.Max(0, maxMana));
        NotifyManaChanged();
    }

    public void DealSlashDamage(Vector3 origin, Vector3 direction, float range, float radius, int damage, GameObject attacker, Transform ignoreRoot = null)
    {
        RaycastHit[] hits = Physics.SphereCastAll(origin, radius, direction.normalized, range, attackMask, QueryTriggerInteraction.Ignore);
        if (hits.Length == 0)
        {
            Debug.Log($"PlayerSkills: Attack missed. Nothing in range (origin={origin}, dir={direction}, range={range}, radius={radius}).");
            return;
        }

        var damaged = new HashSet<AnimalHealth>();
        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null)
            {
                continue;
            }

            if (ignoreRoot != null && (hitCollider.transform == ignoreRoot || hitCollider.transform.IsChildOf(ignoreRoot)))
            {
                continue;
            }

            AnimalHealth animalHealth = hitCollider.GetComponentInParent<AnimalHealth>();
            if (animalHealth == null)
            {
                Debug.Log($"PlayerSkills: Hit '{hitCollider.name}' but no AnimalHealth on it or its parents.");
                continue;
            }

            if (!damaged.Add(animalHealth))
            {
                continue;
            }

            animalHealth.TakeDamage(Mathf.Max(1, damage), attacker);
            Debug.Log($"PlayerSkills: Damaged '{animalHealth.gameObject.name}' for {damage}.");
        }

        if (damaged.Count == 0)
        {
            Debug.Log("PlayerSkills: Attack swung but no AnimalHealth was hit.");
        }
    }

    private void NotifyHealthChanged()
    {
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void NotifyManaChanged()
    {
        ManaChanged?.Invoke(currentMana, maxMana);
    }

    private void RequestHint(string message)
    {
        if (string.IsNullOrWhiteSpace(message) || lowManaHintTimer > 0f)
        {
            return;
        }

        lowManaHintTimer = Mathf.Max(0.1f, lowManaHintCooldown);
        HintRequested?.Invoke(message);
    }

    private void DamageAnimalInFront()
    {
        Vector3 origin = attackOrigin != null ? attackOrigin.position + Vector3.up : transform.position + Vector3.up;
        Vector3 direction = attackCamera != null ? attackCamera.transform.forward : transform.forward;
        DealSlashDamage(origin, direction, attackRange, attackRadius, AttackPower, gameObject, transform);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = attackOrigin != null ? attackOrigin.position + Vector3.up : transform.position + Vector3.up;
        Vector3 direction = attackCamera != null ? attackCamera.transform.forward : transform.forward;

        Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(origin, attackRadius);
        Gizmos.DrawWireSphere(origin + direction * attackRange, attackRadius);
        Gizmos.DrawLine(origin, origin + direction * attackRange);
    }

    private void TrySetTrigger(string triggerName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(triggerName))
        {
            return;
        }

        for (int i = 0; i < animator.parameterCount; i++)
        {
            AnimatorControllerParameter parameter = animator.parameters[i];
            if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == triggerName)
            {
                animator.SetTrigger(triggerName);
                return;
            }
        }
    }

    private GameObject equippedSwordVisual;

    private void EnsureSwordVisualExists()
    {
        if (equippedSwordVisual != null)
        {
            return;
        }

        Transform handBone = ResolveHandBone();
        bool attachedToBone = handBone != null;
        Transform parent = attachedToBone ? handBone : transform;

        equippedSwordVisual = new GameObject("EquippedSwordVisual");
        equippedSwordVisual.transform.SetParent(parent, false);

        if (attachedToBone)
        {
            equippedSwordVisual.transform.localPosition = swordLocalPosition;
            equippedSwordVisual.transform.localRotation = Quaternion.Euler(swordLocalRotation);
            equippedSwordVisual.transform.localScale = swordLocalScale;
        }
        else
        {
            equippedSwordVisual.transform.localPosition = swordFallbackLocalPosition;
            equippedSwordVisual.transform.localRotation = Quaternion.Euler(swordFallbackLocalRotation);
            equippedSwordVisual.transform.localScale = swordFallbackLocalScale;
            Debug.LogWarning($"PlayerSkills: Hand bone '{swordHandBoneName}' not found. Sword visual attached to player root as fallback. Configure 'swordHandBoneName' or 'swordHandBoneFallbackNames' to match your rig.");
        }

        GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blade.name = "Blade";
        blade.transform.SetParent(equippedSwordVisual.transform, false);
        blade.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        blade.transform.localScale = new Vector3(0.25f, 1.2f, 0.18f);
        SetVisualMaterial(blade, new Color(0.82f, 0.84f, 0.88f));

        GameObject guard = GameObject.CreatePrimitive(PrimitiveType.Cube);
        guard.name = "Guard";
        guard.transform.SetParent(equippedSwordVisual.transform, false);
        guard.transform.localPosition = new Vector3(0f, 0.04f, 0f);
        guard.transform.localScale = new Vector3(0.58f, 0.12f, 0.18f);
        SetVisualMaterial(guard, new Color(0.74f, 0.61f, 0.18f));

        GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        handle.name = "Handle";
        handle.transform.SetParent(equippedSwordVisual.transform, false);
        handle.transform.localPosition = new Vector3(0f, -0.18f, 0f);
        handle.transform.localScale = new Vector3(0.14f, 0.34f, 0.14f);
        SetVisualMaterial(handle, new Color(0.28f, 0.18f, 0.1f));

        DisableCollider(blade);
        DisableCollider(guard);
        DisableCollider(handle);
    }

    private Transform ResolveHandBone()
    {
        Transform bone = FindChildRecursive(transform, swordHandBoneName);
        if (bone != null)
        {
            return bone;
        }

        if (swordHandBoneFallbackNames != null)
        {
            for (int i = 0; i < swordHandBoneFallbackNames.Length; i++)
            {
                string candidate = swordHandBoneFallbackNames[i];
                if (string.IsNullOrWhiteSpace(candidate))
                {
                    continue;
                }

                bone = FindChildRecursive(transform, candidate);
                if (bone != null)
                {
                    return bone;
                }
            }
        }

        return FindHandBoneByKeyword(transform);
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

    private void UpdateSwordVisual()
    {
        if (equippedSwordVisual != null)
        {
            equippedSwordVisual.SetActive(hasSword);
        }
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
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

    private static void SetVisualMaterial(GameObject target, Color color)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        renderer.material = new Material(Shader.Find("Standard"));
        renderer.material.color = color;
    }

    private static void DisableCollider(GameObject target)
    {
        Collider collider = target.GetComponent<Collider>();
        if (collider != null)
        {
            UnityEngine.Object.Destroy(collider);
        }
    }
}
