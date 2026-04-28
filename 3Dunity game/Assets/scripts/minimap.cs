using UnityEngine;
using System.Collections;

public class Minimap : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float followSpeed = 10f;

    void Update()
    {
        if (target == null) return;
        transform.position = Vector3.Lerp(transform.position, target.position, followSpeed * Time.deltaTime);
    }
}