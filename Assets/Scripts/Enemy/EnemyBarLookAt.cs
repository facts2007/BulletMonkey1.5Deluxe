using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera playerCamera;

    private void Start()
    {
        playerCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (playerCamera == null) return;

        transform.LookAt(playerCamera.transform);
        transform.Rotate(0f, 180f, 0f);
    }
}