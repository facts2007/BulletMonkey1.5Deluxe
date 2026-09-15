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

    [Header("Stomp Detection")]
    public LayerMask enemyMask;
    public int stompDamage = 25;

    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 currentVelocity;
    private bool isGrounded;
    private bool wasGrounded;
    private bool hasJumped;
    private float jumpBufferCounter;
    private float coyoteTimeCounter;

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
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && !wasGrounded)
        {
            HandleLanded();
        }

        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            hasJumped = false;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
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

        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f && !hasJumped)
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
            Debug.Log("Landed on " + hits[0].gameObject.name);
        }

        CheckStomp();
    }

    private void CheckStomp()
    {
        Collider[] enemyHits = Physics.OverlapSphere(groundCheck.position, groundDistance, enemyMask);
        foreach (Collider hit in enemyHits)
        {
            HandleLandedOnEnemy(hit.gameObject);
        }
    }

    private void HandleLandedOnEnemy(GameObject enemy)
    {
        Enemy enemyScript = enemy.GetComponent<Enemy>();
        if (enemyScript == null || !enemyScript.canBeStomped) return;

        if (enemyScript.stompInstantKills)
        {
            enemyScript.Explode();
            return;
        }

        EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
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