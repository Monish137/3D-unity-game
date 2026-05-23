using UnityEngine;

[DisallowMultipleComponent]
public class VillagerEscortToWeapons : MonoBehaviour
{
    private enum EscortState
    {
        Idle,
        FollowingPlayer,
        MovingToWeapons,
        TakingWeapons,
        Finished
    }

    [Header("Targets")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform armedWeaponsPoint;
    [SerializeField] private string armedWeaponsObjectName = "Armed weapons";
    [SerializeField] private string[] armedWeaponsFallbackNames =
    {
        "Armed weapons",
        "Armed Weapons",
        "armed weapons",
        "Armed_weapons",
        "ArmedWeapons",
        "Weapons",
        "Weapon Point"
    };

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.2f;
    [SerializeField] private float turnSpeed = 360f;
    [SerializeField] private float followDistance = 2.3f;
    [SerializeField] private float playerArrivalDistance = 3.2f;
    [SerializeField] private float weaponStopDistance = 1.2f;
    [SerializeField] private float takeWeaponDuration = 1.2f;
    [SerializeField] private float groundProbeHeight = 6f;
    [SerializeField] private float groundSnapOffset = 0.05f;
    [SerializeField] private LayerMask groundLayers = ~0;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private RuntimeAnimatorController idleController;
    [SerializeField] private RuntimeAnimatorController walkController;

    private EscortState state = EscortState.Idle;
    private float stateTimer;
    private bool escortStarted;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        if (armedWeaponsPoint == null)
        {
            armedWeaponsPoint = FindWeaponsPoint();
        }

        if (animator != null && idleController == null)
        {
            idleController = animator.runtimeAnimatorController;
        }

        SnapToGround();
        ApplyAnimation(false);
    }

    private void Update()
    {
        switch (state)
        {
            case EscortState.FollowingPlayer:
                UpdateFollowPlayer();
                break;

            case EscortState.MovingToWeapons:
                UpdateMoveToWeapons();
                break;

            case EscortState.TakingWeapons:
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f)
                {
                    state = EscortState.Finished;
                    ApplyAnimation(false);
                }
                break;
        }
    }

    private void OnNpcDialogueFinished(NpcDialogue dialogue)
    {
        if (escortStarted || dialogue == null)
        {
            return;
        }

        if (!BelongsToThisVillager(dialogue.transform))
        {
            return;
        }

        StartEscort();
    }

    public void StartEscort()
    {
        if (escortStarted)
        {
            return;
        }

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        if (armedWeaponsPoint == null)
        {
            armedWeaponsPoint = FindWeaponsPoint();
        }

        escortStarted = true;
        state = EscortState.FollowingPlayer;
        ApplyAnimation(true);
    }

    private void UpdateFollowPlayer()
    {
        if (player == null)
        {
            return;
        }

        if (armedWeaponsPoint != null && Vector3.Distance(Flatten(player.position), Flatten(armedWeaponsPoint.position)) <= playerArrivalDistance)
        {
            state = EscortState.MovingToWeapons;
            ApplyAnimation(true);
            return;
        }

        Vector3 targetPosition = Flatten(player.position);
        Vector3 villagerPosition = Flatten(transform.position);
        float distance = Vector3.Distance(villagerPosition, targetPosition);

        if (distance > followDistance)
        {
            MoveTowards(targetPosition, followDistance);
            ApplyAnimation(true);
        }
        else
        {
            ApplyAnimation(false);
        }
    }

    private void UpdateMoveToWeapons()
    {
        if (armedWeaponsPoint == null)
        {
            ApplyAnimation(false);
            return;
        }

        Vector3 targetPosition = Flatten(armedWeaponsPoint.position);
        float distance = Vector3.Distance(Flatten(transform.position), targetPosition);

        if (distance > weaponStopDistance)
        {
            MoveTowards(targetPosition, weaponStopDistance);
            ApplyAnimation(true);
            return;
        }

        state = EscortState.TakingWeapons;
        stateTimer = takeWeaponDuration;
        ApplyAnimation(false);
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
        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

        float moveAmount = Mathf.Min(moveSpeed * Time.deltaTime, distance - stopDistance);
        Vector3 nextPosition = transform.position + direction * Mathf.Max(0f, moveAmount);
        transform.position = GetGroundedPosition(nextPosition);
    }

    private void ApplyAnimation(bool isWalking)
    {
        if (animator == null)
        {
            return;
        }

        RuntimeAnimatorController desiredController = isWalking && walkController != null ? walkController : idleController;
        if (desiredController != null && animator.runtimeAnimatorController != desiredController)
        {
            animator.runtimeAnimatorController = desiredController;
        }
    }

    private Transform FindWeaponsPoint()
    {
        if (!string.IsNullOrWhiteSpace(armedWeaponsObjectName))
        {
            GameObject exact = GameObject.Find(armedWeaponsObjectName);
            if (exact != null)
            {
                return exact.transform;
            }
        }

        for (int i = 0; i < armedWeaponsFallbackNames.Length; i++)
        {
            string candidate = armedWeaponsFallbackNames[i];
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            GameObject found = GameObject.Find(candidate);
            if (found != null)
            {
                return found.transform;
            }
        }

        return null;
    }

    private bool BelongsToThisVillager(Transform dialogueTransform)
    {
        return dialogueTransform == transform || dialogueTransform.IsChildOf(transform);
    }

    private static Vector3 Flatten(Vector3 value)
    {
        value.y = 0f;
        return value;
    }

    private void SnapToGround()
    {
        transform.position = GetGroundedPosition(transform.position);
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
}
