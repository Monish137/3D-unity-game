using UnityEngine;

public class BearMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private bool autoMove = true;
    
    private Animator animator;
    private Vector3 moveDirection = Vector3.forward;
    
    private void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("BearMovement: No Animator component found on this GameObject!");
        }
    }
    
    private void Update()
    {
        if (autoMove && animator != null)
        {
            MoveForward();
        }
    }
    
    private void MoveForward()
    {
        // Move in the direction the bear is facing
        Vector3 movement = transform.forward * moveSpeed * Time.deltaTime;
        transform.position += movement;
    }
    
    // Public method to set movement speed
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }
    
    // Public method to enable/disable auto movement
    public void SetAutoMove(bool enable)
    {
        autoMove = enable;
    }
    
    // Public method to move in a specific direction
    public void MoveInDirection(Vector3 direction, float speed)
    {
        moveDirection = direction.normalized;
        transform.position += moveDirection * speed * Time.deltaTime;
    }
}
