using UnityEngine;

[DefaultExecutionOrder(-100)]
public class MouseLook : MonoBehaviour
{
    [Header("References")]
    public Transform cameraPivot;

    [Header("Sensitivity")]
    public float mouseSensitivity = 200f;

    [Header("Pitch Limits")]
    public float minPitch = -30f;
    public float maxPitch = 70f;

    [Header("Free look and character turning")]
    [Range(0f, 60f)] public float turnDeadZone = 18f;
    [Min(1f)] public float turnResponse = 12f;
    private float pitch;
    private float yaw;
    private Gun gun;
    private PlayerMovement movement;

    private void OnEnable()
    {
        if (cameraPivot == null) return;
        yaw = cameraPivot.eulerAngles.y;
        pitch = Mathf.DeltaAngle(0f, cameraPivot.eulerAngles.x);
        gun = GetComponentInChildren<Gun>(true);
        movement = GetComponent<PlayerMovement>();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (Time.timeScale <= 0f || GameSceneFlow.IsLoading || cameraPivot == null) return;
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        yaw = Mathf.Repeat(yaw + mouseX, 360f);

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        bool shooting = gun != null && gun.enabled &&
            ((Input.GetButton("Fire1") && gun.currentAmmo > 0) ||
            (gun.superAbility != null && gun.superAbility.IsSuperShooting));
        bool moving = movement != null && movement.enabled &&
            (Mathf.Abs(Input.GetAxisRaw("Horizontal")) + Mathf.Abs(Input.GetAxisRaw("Vertical")) > .1f);
        UpdateFacing(shooting, moving, Time.deltaTime);
    }

    private void UpdateFacing(bool shooting, bool moving, float delta)
    {
        float difference = Mathf.DeltaAngle(transform.eulerAngles.y, yaw);
        if (shooting) transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        else if (moving || Mathf.Abs(difference) > turnDeadZone)
        {
            float target = moving ? yaw : yaw - Mathf.Sign(difference) * turnDeadZone;
            float facing = Mathf.LerpAngle(transform.eulerAngles.y, target, 1f - Mathf.Exp(-turnResponse * delta));
            transform.rotation = Quaternion.Euler(0f, facing, 0f);
        }
        // Rotating the body must never drag the camera's world-space aim with it.
        cameraPivot.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    public void FaceAim()
    {
        if (enabled && cameraPivot != null) UpdateFacing(true, false, 0f);
    }
}
