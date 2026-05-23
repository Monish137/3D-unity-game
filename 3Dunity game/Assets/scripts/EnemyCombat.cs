using UnityEngine;

[DisallowMultipleComponent]
public class EnemyCombat : MonoBehaviour
{
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float attackRange = 2.2f;
    [SerializeField] private float moveSpeed = 2.6f;
    [SerializeField] private float turnSpeed = 360f;
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float groundProbeHeight = 6f;
    [SerializeField] private float groundSnapOffset = 0.05f;
    [SerializeField] private LayerMask groundLayers = ~0;

    private PlayerSkills playerSkills;
    private float attackTimer;
    private bool battleActive;

    private void Awake()
    {
        if (enemyHealth == null)
        {
            enemyHealth = GetComponent<EnemyHealth>();
        }

        playerSkills = FindFirstObjectByType<PlayerSkills>();
        enabled = false;
    }

    private void Update()
    {
        if (!battleActive || enemyHealth == null || enemyHealth.IsDead || !enemyHealth.CanBeTargeted || playerSkills == null)
        {
            return;
        }

        attackTimer -= Time.deltaTime;

        Vector3 playerPosition = Flatten(playerSkills.transform.position);
        Vector3 enemyPosition = Flatten(transform.position);
        Vector3 toPlayer = playerPosition - enemyPosition;
        float distance = toPlayer.magnitude;

        if (distance > detectionRange)
        {
            return;
        }

        if (distance > attackRange)
        {
            MoveTowards(playerPosition, 0.2f);
            return;
        }

        RotateTowards(toPlayer.normalized);

        if (attackTimer <= 0f)
        {
            attackTimer = attackCooldown;
            playerSkills.TakeDamage(attackDamage);
        }
    }

    public void SetBattleActive(bool isActive)
    {
        battleActive = isActive;
        enabled = isActive;
        attackTimer = 0f;
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
