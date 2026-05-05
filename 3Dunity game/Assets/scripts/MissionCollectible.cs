using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MissionCollectible : MonoBehaviour
{
    [SerializeField] private MissionManager missionManager;
    [SerializeField] private string missionId;
    [SerializeField] private int amount = 1;
    [SerializeField] private bool destroyOnCollect = true;

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
        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (missionManager == null || !missionManager.IsCurrentMission(missionId))
        {
            return;
        }

        missionManager.AddProgress(missionId, amount);

        if (destroyOnCollect)
        {
            Destroy(gameObject);
        }
    }
}
