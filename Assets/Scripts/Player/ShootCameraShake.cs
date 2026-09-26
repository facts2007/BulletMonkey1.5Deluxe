using UnityEngine;

[DefaultExecutionOrder(1000)]
public class ShootCameraShake : MonoBehaviour
{
    [Min(0f)] public float normalStrength = 0.015f;
    [Min(0f)] public float superStrength = 0.085f;
    [Min(0.01f)] public float shakeSeconds = 0.08f;
    private Vector3 offset;
    private float remaining;
    private float strength;

    public void Kick(bool superShot)
    {
        remaining = shakeSeconds;
        strength = superShot ? superStrength : normalStrength;
    }

    private void LateUpdate()
    {
        transform.localPosition -= offset;
        offset = Vector3.zero;
        if (remaining > 0f && Time.timeScale > 0f)
        {
            remaining -= Time.deltaTime;
            offset = Random.insideUnitSphere * strength * Mathf.Clamp01(remaining / shakeSeconds);
        }
        transform.localPosition += offset;
    }

    private void OnDisable()
    {
        transform.localPosition -= offset;
        offset = Vector3.zero;
        remaining = 0f;
    }
}
