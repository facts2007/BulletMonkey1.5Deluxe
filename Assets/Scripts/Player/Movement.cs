using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float acceleration = 10f;
    public SpeedBoostAbility speedBoost;

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
    private Vector3 pushVelocity;
    public void ApplyPush(Vector3 direction,float speed){direction.y=0;pushVelocity=direction.normalized*speed;}
    private Vector3 currentVelocity;
    private bool isGrounded;
    public bool IsGrounded => isGrounded;
    public float MotionDelta => Time.deltaTime*(speedBoost!=null?speedBoost.CurrentMultiplier:1f);
    private bool wasGrounded;
    private bool hasJumped;
    private float jumpBufferCounter;
    private float coyoteTimeCounter;
    private bool stompAvailable = true;
    public void ResetAfterRecovery(){pushVelocity=Vector3.zero;velocity=Vector3.zero;currentVelocity=Vector3.zero;}

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (Time.timeScale <= 0f) return;
        HandleGroundedState();
        HandleMovement();
        HandleJump();
        ApplyGravity();
        if(pushVelocity.sqrMagnitude>.01f){controller.Move(pushVelocity*Time.deltaTime);pushVelocity=Vector3.MoveTowards(pushVelocity,Vector3.zero,24*Time.deltaTime);}
    }

    private void HandleGroundedState()
    {
        wasGrounded = isGrounded;
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isGrounded && !Physics.CheckSphere(groundCheck.position, groundDistance, enemyMask))
            stompAvailable = true;

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
            coyoteTimeCounter -= MotionDelta;
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
        float speedMultiplier = speedBoost != null ? speedBoost.CurrentMultiplier : 1f;
        Vector3 targetVelocity = inputDirection * walkSpeed;

        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, acceleration * MotionDelta);

        controller.Move(currentVelocity * MotionDelta);
    }

    private void HandleJump()
    {
        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= MotionDelta;
        }

        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f && !hasJumped)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
            hasJumped = true;
            stompAvailable = true;
        }
    }

    private void ApplyGravity()
    {
        if (velocity.y < 0f)
        {
            velocity.y += gravity * fallMultiplier * MotionDelta;
        }
        else
        {
            velocity.y += gravity * MotionDelta;
        }

        controller.Move(velocity * MotionDelta);
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
        Enemy enemyScript = enemy.GetComponentInParent<Enemy>();
        if (enemyScript == null || !enemyScript.canBeStomped || !stompAvailable) return;
        stompAvailable = false;
        enemyScript.Stomp(stompDamage);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // A descending controller can hit the top before the small ground sphere overlaps it.
        if (hit.normal.y > 0.5f && velocity.y < -3f)
            HandleLandedOnEnemy(hit.gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
    }
}

