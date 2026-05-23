using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class VillagerCombat : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private RuntimeAnimatorController attackController;
    [SerializeField] private string attackTriggerName = "Attack";
    [SerializeField] private float attackAnimationDuration = 0.95f;
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float attackRange = 2.1f;
    [SerializeField] private float moveSpeed = 2.3f;
    [SerializeField] private float turnSpeed = 360f;
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private int attackDamage = 12;
    [SerializeField] private float attackWindup = 0.3f;
    [SerializeField] private float groundProbeHeight = 6f;
    [SerializeField] private float groundSnapOffset = 0.05f;
    [SerializeField] private LayerMask groundLayers = ~0;

    private bool combatEnabled;
    private float attackTimer;
    private float pendingDamageTimer = -1f;
    private float attackAnimationTimer;
    private EnemyHealth targetEnemy;
    private Vector3 battleCenter;
    private RuntimeAnimatorController defaultController;

    public bool CombatEnabled => combatEnabled;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (animator != null)
        {
            defaultController = animator.runtimeAnimatorController;
        }

        if (attackController == null)
        {
            attackController = LoadDefaultAttackController();
        }
    }

    private void Update()
    {
        if (!combatEnabled)
        {
            return;
        }

        attackTimer -= Time.deltaTime;
        UpdateAttackAnimationTimer();

        if (pendingDamageTimer >= 0f)
        {
            pendingDamageTimer -= Time.deltaTime;
            if (pendingDamageTimer <= 0f)
            {
                pendingDamageTimer = -1f;
                ApplyDamage();
            }
        }

        if (targetEnemy == null || targetEnemy.IsDead)
        {
            targetEnemy = FindNearestEnemy();
        }

        if (targetEnemy == null)
        {
            return;
        }

        Vector3 targetPosition = Flatten(targetEnemy.transform.position);
        Vector3 currentPosition = Flatten(transform.position);
        Vector3 toTarget = targetPosition - currentPosition;
        float distance = toTarget.magnitude;

        if (distance > attackRange)
        {
            MoveTowards(targetPosition, 0.15f);
            return;
        }

        RotateTowards(toTarget.normalized);

        if (attackTimer <= 0f)
        {
            attackTimer = attackCooldown;
            pendingDamageTimer = Mathf.Max(0f, attackWindup);
            PlayAttackAnimation();
        }
    }

    public void SetCombatEnabled(bool enabled, Vector3 center)
    {
        combatEnabled = enabled;
        battleCenter = center;
        targetEnemy = null;
        attackTimer = 0f;
        pendingDamageTimer = -1f;
        attackAnimationTimer = 0f;

        if (enabled)
        {
            transform.position = GetGroundedPosition(transform.position);
        }
        else
        {
            RestoreDefaultController();
        }
    }

    private EnemyHealth FindNearestEnemy()
    {
        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        EnemyHealth bestEnemy = null;
        float bestDistance = detectionRange;

        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyHealth enemy = enemies[i];
            if (enemy == null || enemy.IsDead || !enemy.CanBeTargeted)
            {
                continue;
            }

            float distance = Vector3.Distance(Flatten(transform.position), Flatten(enemy.transform.position));
            if (distance > bestDistance)
            {
                continue;
            }

            bestDistance = distance;
            bestEnemy = enemy;
        }

        return bestEnemy;
    }

    private void MoveTowards(Vector3 targetPosition, float stopDistance)
    {
        Vector3 current = Flatten(transform.position);
        Vector3 toTarget = targetPosition - current;
        float distance = toTarget.magnitude;

        if (distance <= stopDistance || distance <= 0.001f)
        {
            return;
        }

        Vector3 direction = toTarget.normalized;
        RotateTowards(direction);

        float moveAmount = Mathf.Min(moveSpeed * Time.deltaTime, distance - stopDistance);
        Vector3 nextPosition = transform.position + direction * Mathf.Max(0f, moveAmount);
        transform.position = GetGroundedPosition(nextPosition);
    }

    private void RotateTowards(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    private void ApplyDamage()
    {
        if (targetEnemy == null || targetEnemy.IsDead)
        {
            return;
        }

        float distance = Vector3.Distance(Flatten(transform.position), Flatten(targetEnemy.transform.position));
        if (distance > attackRange + 0.35f)
        {
            return;
        }

        targetEnemy.TakeDamage(attackDamage, gameObject);
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

    private void PlayAttackAnimation()
    {
        if (animator == null)
        {
            return;
        }

        if (attackController != null)
        {
            if (defaultController == null)
            {
                defaultController = animator.runtimeAnimatorController;
            }

            animator.runtimeAnimatorController = attackController;
            animator.Play(0, 0, 0f);
            attackAnimationTimer = Mathf.Max(0.1f, attackAnimationDuration);
            return;
        }

        TrySetTrigger(attackTriggerName);
    }

    private void UpdateAttackAnimationTimer()
    {
        if (attackAnimationTimer <= 0f)
        {
            return;
        }

        attackAnimationTimer -= Time.deltaTime;
        if (attackAnimationTimer <= 0f)
        {
            RestoreDefaultController();
        }
    }

    private void RestoreDefaultController()
    {
        if (animator != null && defaultController != null && animator.runtimeAnimatorController != defaultController)
        {
            animator.runtimeAnimatorController = defaultController;
        }
    }

    private RuntimeAnimatorController LoadDefaultAttackController()
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/DoubleL/Demo/Animator/OneHand_Up_Attack_1_InPlace.controller");
#else
        return null;
#endif
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
