using UnityEngine;

public class PlayerVisualAttachment : MonoBehaviour
{
    [Header("Visual Root")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Vector3 localPosition = Vector3.zero;
    [SerializeField] private Vector3 localRotation = Vector3.zero;
    [SerializeField] private Vector3 localScale = Vector3.one;

    [Header("Optional Old Visual")]
    [SerializeField] private GameObject oldVisualToDisable;

    private void Awake()
    {
        if (oldVisualToDisable != null)
        {
            oldVisualToDisable.SetActive(false);
        }

        if (visualRoot == null)
        {
            return;
        }

        visualRoot.SetParent(transform, false);
        visualRoot.localPosition = localPosition;
        visualRoot.localEulerAngles = localRotation;
        visualRoot.localScale = localScale;
    }
}
