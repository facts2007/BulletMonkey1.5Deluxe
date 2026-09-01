using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float acceleration = 10f;

    [Header("Jumping")]
    public float jumpHeight = 1.5f;
    public float gravity = -9.81f;
    public float fallMultiplier = 2.5f;
    public float jumpBufferTime = 0.15f;
    public float coyoteTime = 0.15f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.3f;
    public LayerMask groundMask;

    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 currentVelocity;
    private bool isGrounded;
    private bool wasGrounded;
    private bool hasJumped;
    private float jumpBufferCounter;
    private float coyoteTimeCounter;

    [Header("Enemy Stomp")]
    public int stompDamage = 25;
    public bool instantKill = false;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        HandleGroundedState();
        HandleMovement();
        HandleJump();
        ApplyGravity();
    }

    private void HandleGroundedState()
    {
        wasGrounded = isGrounded;
        isGrounded = controller.isGrounded;

        if (isGrounded && !wasGrounded)
        {
            hasJumped = false;
            coyoteTimeCounter = coyoteTime;

            HandleLanded();
        }

        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;

            if (velocity.y < 0f)
            {
                velocity.y = -2f;
            }
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 inputDirection = (transform.right * horizontal + transform.forward * vertical).normalized;
        Vector3 targetVelocity = inputDirection * walkSpeed;

        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, acceleration * Time.deltaTime);

        controller.Move(currentVelocity * Time.deltaTime);
    }

    private void HandleJump()
    {
        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        if (jumpBufferCounter > 0f &&
            coyoteTimeCounter > 0f &&
            !hasJumped)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
            hasJumped = true;
        }
    }

    private void ApplyGravity()
    {
        if (velocity.y < 0f)
        {
            velocity.y += gravity * fallMultiplier * Time.deltaTime;
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }

        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleLanded()
    {
        Collider[] hits = Physics.OverlapSphere(groundCheck.position, groundDistance, groundMask);
        if (hits.Length > 0)
        {

            if (hits[0].gameObject.layer == LayerMask.NameToLayer("Enemy1"))
            {
                HandleLandedOnEnemy(hits[0].gameObject);
            }
        }
    }

    private void HandleLandedOnEnemy(GameObject enemy)
    {
        EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
        Enemy enemyScript = enemy.GetComponent<Enemy>();

        if (instantKill)
        {
            if (enemyScript != null)
            {
                enemyScript.Explode();
            }

            return;
        }

        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(stompDamage);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
    }
}