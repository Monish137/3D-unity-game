using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MissionInteractable : MonoBehaviour
{
    [SerializeField] private MissionManager missionManager;
    [SerializeField] private string missionId;
    [SerializeField] private int amount = 1;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private bool requireInteractionKey = true;
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private bool hideAfterInteract;

    private bool playerInRange;
    private bool hasTriggered;

    private void Reset()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void Awake()
    {
        if (missionManager == null)
        {
            missionManager = MissionManager.Instance != null
                ? MissionManager.Instance
                : FindFirstObjectByType<MissionManager>();
        }
    }

    private void Update()
    {
        if (!playerInRange || !requireInteractionKey || hasTriggered && triggerOnce)
        {
            return;
        }

        if (Input.GetKeyDown(interactionKey))
        {
            CompleteInteraction();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        playerInRange = true;

        if (!requireInteractionKey)
        {
            CompleteInteraction();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }

    private void CompleteInteraction()
    {
        if (hasTriggered && triggerOnce)
        {
            return;
        }

        if (missionManager == null || !missionManager.IsCurrentMission(missionId))
        {
            return;
        }

        missionManager.AddProgress(missionId, amount);
        hasTriggered = true;

        if (hideAfterInteract)
        {
            gameObject.SetActive(false);
        }
    }
}
