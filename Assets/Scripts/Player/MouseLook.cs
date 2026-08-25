using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [Header("References")]
    public Transform cameraPivot;

    [Header("Sensitivity")]
    public float mouseSensitivity = 200f;

    [Header("Pitch Limits")]
    public float minPitch = -30f;
    public float maxPitch = 70f;

    private float pitch;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        transform.Rotate(Vector3.up * mouseX);

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}
