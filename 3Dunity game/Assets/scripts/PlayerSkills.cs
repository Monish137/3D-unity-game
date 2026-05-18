using System;
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
    [SerializeField] private KeyCode defenseKey = KeyCode.Mouse1;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string attackTriggerName = "Attack";
    [SerializeField] private string defenseTriggerName = "Defense";
    [SerializeField] private string swordHandBoneName = "Hand.R";
    [SerializeField] private GameObject equippedSwordPrefab;
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
    private float attackTimer;

    public int AttackPower => hasSword ? swordAttackPower : baseAttackPower;
    public int DefensePower => baseDefense + (hasSword ? swordDefenseBonus : 0);
    public bool HasSword => hasSword;
    public event Action<bool> SwordEquipChanged;

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

        EnsureSwordVisualExists();
        UpdateSwordVisual();
    }

    private void Update()
    {
        attackTimer -= Time.deltaTime;

        if (!hasSword)
        {
            return;
        }

        if (Input.GetKeyDown(attackKey))
        {
            Attack();
        }

        if (Input.GetKeyDown(defenseKey))
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

        if (animator != null)
        {
            TrySetTrigger(attackTriggerName);
        }
        
        DamageAnimalInFront();
        Debug.Log($"PlayerSkills: Attack triggered. Power={AttackPower}");
    }

    public void Defend()
    {
        if (animator != null)
        {
            TrySetTrigger(defenseTriggerName);
        }

        Debug.Log($"PlayerSkills: Defense triggered. Defense={DefensePower}");
    }

    private void DamageAnimalInFront()
    {
        Vector3 origin = attackOrigin != null ? attackOrigin.position + Vector3.up : transform.position + Vector3.up;
        Vector3 direction = attackCamera != null ? attackCamera.transform.forward : transform.forward;

        RaycastHit[] hits = Physics.SphereCastAll(origin, attackRadius, direction, attackRange, attackMask, QueryTriggerInteraction.Ignore);
        if (hits.Length == 0)
        {
            Debug.Log($"PlayerSkills: Attack missed. Nothing in range (origin={origin}, dir={direction}, range={attackRange}, radius={attackRadius}).");
            return;
        }

        var damaged = new System.Collections.Generic.HashSet<AnimalHealth>();
        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null)
            {
                continue;
            }

            if (hitCollider.transform == transform || hitCollider.transform.IsChildOf(transform))
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

            animalHealth.TakeDamage(AttackPower, gameObject);
            Debug.Log($"PlayerSkills: Damaged '{animalHealth.gameObject.name}' for {AttackPower}.");
        }

        if (damaged.Count == 0)
        {
            Debug.Log("PlayerSkills: Attack swung but no AnimalHealth was hit.");
        }
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

        if (equippedSwordPrefab != null)
        {
            GameObject swordInstance = Instantiate(equippedSwordPrefab, equippedSwordVisual.transform);
            swordInstance.name = equippedSwordPrefab.name;
            swordInstance.transform.localPosition = Vector3.zero;
            swordInstance.transform.localRotation = Quaternion.identity;
            swordInstance.transform.localScale = Vector3.one;
            DisableCollidersInChildren(swordInstance);
        }
        else
        {
            CreateFallbackSwordVisual();
        }
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

    private void CreateFallbackSwordVisual()
    {
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

    private static void DisableCollidersInChildren(GameObject target)
    {
        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            UnityEngine.Object.Destroy(colliders[i]);
        }
    }
}
