using UnityEngine;

/// <summary>Presentation only; movement and combat remain in the existing scripts.</summary>
public class CharacterAnimationDriver : MonoBehaviour
{
    public Animator animator;
    public Transform movementRoot;
    public SuperShootAbility superAbility;
    public Gun playerGun;
    public bool walkOnly;
    public float movementThreshold = 0.08f;
    public float shotAnimationSeconds = 0.18f;
    private Vector3 previousPosition;
    private float shotUntil;
    private string currentState;

    private void OnEnable()
    {
        if (movementRoot == null) movementRoot = transform;
        previousPosition = movementRoot.position;
        currentState = null;
    }

    public void NotifyShot() { shotUntil = Time.time + shotAnimationSeconds; }

    private void LateUpdate()
    {
        Vector3 difference = movementRoot.position - previousPosition;
        previousPosition = movementRoot.position;
        if (animator == null || Time.deltaTime <= 0f) return;
        difference.y = 0f;
        bool moving = difference.magnitude / Time.deltaTime > movementThreshold;
        string state = moving ? "Walk" : "Idle";
        if (walkOnly)
        {
            state = "Walk";
            animator.speed = moving ? 1f : 0f;
        }
        else if (superAbility != null && superAbility.IsSuperShooting) state = "SuperShoot";
        else if (Time.time < shotUntil) state = "Shoot";
        else if (!moving && playerGun != null && playerGun.currentAmmo <= 0) state = "Empty";

        if (state == currentState) return;
        animator.CrossFadeInFixedTime(state, 0.06f, 0);
        currentState = state;
    }
}
