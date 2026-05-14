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
    [SerializeField] private Vector3 swordLocalPosition = new Vector3(-0.02f, 0.02f, 0.02f);
    [SerializeField] private Vector3 swordLocalRotation = new Vector3(0f, 0f, 90f);
    [SerializeField] private Vector3 swordLocalScale = new Vector3(0.18f, 0.65f, 0.08f);

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
            UpdateSwordVisual();
            return;
        }

        hasSword = equipped;
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

        if (Physics.SphereCast(origin, attackRadius, direction, out RaycastHit hit, attackRange, attackMask, QueryTriggerInteraction.Ignore))
        {
            AnimalHealth animalHealth = hit.collider.GetComponentInParent<AnimalHealth>();
            if (animalHealth != null)
            {
                animalHealth.TakeDamage(AttackPower, gameObject);
            }
        }
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

        Transform handBone = FindChildRecursive(transform, swordHandBoneName);
        if (handBone == null)
        {
            return;
        }

        equippedSwordVisual = new GameObject("EquippedSwordVisual");
        equippedSwordVisual.transform.SetParent(handBone, false);
        equippedSwordVisual.transform.localPosition = swordLocalPosition;
        equippedSwordVisual.transform.localRotation = Quaternion.Euler(swordLocalRotation);
        equippedSwordVisual.transform.localScale = swordLocalScale;

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
