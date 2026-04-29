using UnityEngine;

public class Minimap : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float followSpeed = 10f;
    [SerializeField] private float heightOffset = 45f;
    [SerializeField] private Vector3 worldOffset = Vector3.zero;
    [SerializeField] private Vector3 fixedRotation = new Vector3(90f, 0f, 0f);

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = new Vector3(
            target.position.x + worldOffset.x,
            target.position.y + heightOffset + worldOffset.y,
            target.position.z + worldOffset.z
        );

        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(fixedRotation);
    }
}
