using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MissionTrigger : MonoBehaviour
{
    [SerializeField] private MissionManager missionManager;
    [SerializeField] private string missionId;
    [SerializeField] private int progressAmount = 1;
    [SerializeField] private bool triggerOnce = true;

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

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && triggerOnce)
        {
            return;
        }

        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (missionManager == null || !missionManager.IsCurrentMission(missionId))
        {
            return;
        }

        missionManager.AddProgress(missionId, progressAmount);
        hasTriggered = true;
    }
}
