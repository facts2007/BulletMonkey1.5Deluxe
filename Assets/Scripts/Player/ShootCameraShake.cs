using UnityEngine;

[DefaultExecutionOrder(1000)]
public class ShootCameraShake : MonoBehaviour
{
    [Min(0f)] public float normalStrength = 0.015f;
    [Min(0f)] public float superStrength = 0.22f;
    [Min(0.01f)] public float shakeSeconds = 0.08f;
    [Header("Super recoil — affects the aiming camera")]
    [Min(0f)] public float superRecoilPerShot = 1f;
    [Min(0f)] public float superHorizontalRecoil = 1.05f;
    [Min(0f)] public float recoilLimit = 18f;
    [Min(0f)] public float recoilRecovery = 5.5f;
    [Min(0f)] public float superAngularShake = 1.35f;
    [Header("Cocey banan — light continuous shake")]
    [Min(0f)] public float coceyPositionShake = 0.018f;
    [Min(0f)] public float coceyAngularShake = 0.25f;
    [Min(0.1f)] public float coceyShakeFrequency = 12f;
    [Header("Super banana charge zoom")]
    [Range(0f, 20f)] public float chargeZoomDegrees = 5f;
    [Min(0.1f)] public float zoomResponse = 8f;
    private Vector3 offset;
    private float remaining;
    private float strength;
    private Vector2 recoil;
    private Quaternion appliedRotation = Quaternion.identity;
    private bool lastShotWasSuper;
    private SuperShootAbility superAbility;
    private SpeedBoostAbility speedBoost;
    private Camera viewCamera;
    private float restingFieldOfView;
    private float boostShakeBlend;

    private void Awake()
    {
        superAbility = GetComponentInParent<SuperShootAbility>();
        speedBoost = GetComponentInParent<SpeedBoostAbility>();
        viewCamera = GetComponent<Camera>();
        if (viewCamera != null) restingFieldOfView = viewCamera.fieldOfView;
    }

    public void Kick(bool superShot)
    {
        remaining = shakeSeconds;
        strength = superShot ? superStrength : normalStrength;
        lastShotWasSuper = superShot;
        if (superShot)
        {
            recoil.x = Mathf.Max(-recoilLimit, recoil.x - superRecoilPerShot * Random.Range(0.55f, 1.45f));
            recoil.y = Mathf.Clamp(recoil.y + Random.Range(-superHorizontalRecoil, superHorizontalRecoil), -recoilLimit * 0.4f, recoilLimit * 0.4f);
        }
    }

    private void LateUpdate()
    {
        transform.localPosition -= offset;
        transform.localRotation *= Quaternion.Inverse(appliedRotation);
        appliedRotation = Quaternion.identity;
        offset = Vector3.zero;
        if (Time.timeScale > 0f)
        {
            recoil = Vector2.MoveTowards(recoil, Vector2.zero, recoilRecovery * Time.deltaTime);
            Vector3 angular = new Vector3(recoil.x, recoil.y, 0f);
            boostShakeBlend = Mathf.MoveTowards(boostShakeBlend, speedBoost != null && speedBoost.IsActive ? 1f : 0f, Time.deltaTime * 5f);
            float phase = Time.time * coceyShakeFrequency;
            Vector3 boostNoise = new Vector3(Mathf.PerlinNoise(phase, 17f) - 0.5f,
                Mathf.PerlinNoise(phase, 39f) - 0.5f, Mathf.PerlinNoise(phase, 73f) - 0.5f) * 2f;
            angular += boostNoise * coceyAngularShake * boostShakeBlend;
            offset += boostNoise * coceyPositionShake * boostShakeBlend;
            if (remaining > 0f && lastShotWasSuper)
                angular += Random.insideUnitSphere * superAngularShake;
            appliedRotation = Quaternion.Euler(angular);
        }
        if (remaining > 0f && Time.timeScale > 0f)
        {
            remaining -= Time.deltaTime;
            offset += Random.insideUnitSphere * strength * Mathf.Clamp01(remaining / shakeSeconds);
        }
        transform.localPosition += offset;
        transform.localRotation *= appliedRotation;
        if (viewCamera != null)
        {
            float charge = superAbility != null && superAbility.IsCharging ? superAbility.ChargeFraction : 0f;
            float targetFov = Mathf.Clamp(restingFieldOfView - chargeZoomDegrees * charge, 1f, 179f);
            viewCamera.fieldOfView = Mathf.Lerp(viewCamera.fieldOfView, targetFov, 1f - Mathf.Exp(-zoomResponse * Time.deltaTime));
        }
    }

    private void OnDisable()
    {
        transform.localPosition -= offset;
        transform.localRotation *= Quaternion.Inverse(appliedRotation);
        appliedRotation = Quaternion.identity;
        recoil = Vector2.zero;
        offset = Vector3.zero;
        remaining = 0f;
        boostShakeBlend = 0f;
        if (viewCamera != null) viewCamera.fieldOfView = restingFieldOfView;
    }
}
