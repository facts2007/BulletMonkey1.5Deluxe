using System.Collections;
using UnityEngine;
using TMPro;

public class Gun : MonoBehaviour
{
    public static Gun Instance;

    [Header("Shooting")]
    public float fireRate = 10f;
    public float range = 100f;
    public int damage = 10;

    [Header("Ammo")]
    public int maxAmmo = 30;
    public int currentAmmo;

    [Header("Effects")]
    public GameObject bulletPrefab;
    public GameObject impactEffect;
    public Transform muzzlePoint;

    [Header("Aiming")]
    public Camera aimCamera;

    [Header("UI")]
    public TextMeshProUGUI ammoText;

    [Header("Animation and super shooting")]
    public CharacterAnimationDriver characterAnimation;
    public SuperShootAbility superAbility;
    public ShootCameraShake cameraShake;

    private float fireCooldown;
    private bool wasSuperShooting;

    private void Awake()
    {
        Instance = this;
        currentAmmo = maxAmmo;
        UpdateAmmoText();

        if (aimCamera == null)
        {
            aimCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (Time.timeScale <= 0f) return;
        fireCooldown -= Time.deltaTime;

        bool superShooting = superAbility != null && superAbility.IsSuperShooting;
        if (superShooting)
        {
            if (!wasSuperShooting) fireCooldown = 0f;
            wasSuperShooting = true;
            // Accumulate intervals so the stream stays fast even below 40 FPS.
            int shotsThisFrame = 0;
            while (fireCooldown <= 0f && shotsThisFrame++ < 8)
            {
                Shoot(true);
                fireCooldown += 1f / Mathf.Max(1f, superAbility.shotsPerSecond);
            }
            return;
        }
        wasSuperShooting = false;
        if (superAbility != null && superAbility.IsCharging) return;

        if (Input.GetButton("Fire1") && fireCooldown <= 0f && currentAmmo > 0)
        {
            Shoot();
            fireCooldown = 1f / Mathf.Max(0.1f, fireRate);
        }
    }

    private void Shoot(bool superShot = false)
    {
        if (!superShot)
        {
            currentAmmo--;
            UpdateAmmoText();
        }

        Transform spawnPoint = muzzlePoint != null ? muzzlePoint : transform;
        Ray aimRay = aimCamera != null
            ? aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
            : new Ray(spawnPoint.position, spawnPoint.forward);
        RaycastHit hit = default;
        bool hasHit = false;
        float nearest = range;
        foreach (RaycastHit candidate in Physics.RaycastAll(aimRay, range, ~0, QueryTriggerInteraction.Ignore))
        {
            if (candidate.collider.transform.IsChildOf(transform.root) || candidate.distance >= nearest) continue;
            nearest = candidate.distance;
            hit = candidate;
            hasHit = true;
        }
        Vector3 target = hasHit ? hit.point : aimRay.GetPoint(range);
        Vector3 direction = target - spawnPoint.position;
        Quaternion spawnRotation = Quaternion.LookRotation(direction.sqrMagnitude > 0.001f ? direction : aimRay.direction);

        if (characterAnimation != null) characterAnimation.NotifyShot();
        if (cameraShake != null) cameraShake.Kick(superShot);
        if (GameAudio.Instance != null) GameAudio.Instance.PlayShot(superShot);

        float bulletSpeed = 0f;

        if (bulletPrefab != null)
        {
            GameObject bulletObject = Instantiate(bulletPrefab, spawnPoint.position, spawnRotation);
            Bullet bullet = bulletObject.GetComponent<Bullet>();
            if (bullet != null)
            {
                bulletSpeed = bullet.speed;
            }
        }

        if (hasHit)
        {
            EnemyHealth enemyHealth = hit.collider.GetComponentInParent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
            }

            if (impactEffect != null)
            {
                float delay = bulletSpeed > 0f ? hit.distance / bulletSpeed : 0f;
                StartCoroutine(SpawnImpactAfterDelay(hit.point, hit.normal, delay));
            }
        }
    }

    private IEnumerator SpawnImpactAfterDelay(Vector3 point, Vector3 normal, float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        Instantiate(impactEffect, point, Quaternion.LookRotation(normal));
    }

    private void UpdateAmmoText()
    {
        if (ammoText != null) ammoText.text = currentAmmo + "/" + maxAmmo;
    }

    public void Reload()
    {
        currentAmmo = maxAmmo;
        UpdateAmmoText();
    }

    public void AddAmmo(int amount)
    {
        currentAmmo = Mathf.Min(currentAmmo + amount, maxAmmo);
        UpdateAmmoText();
    }

    public void UpgradeDamage(int amount)
    {
        damage += amount;
    }

    public void UpgradeFireRate(float amount)
    {
        fireRate += amount;
    }

    public void UpgradeMaxAmmo(int amount)
    {
        maxAmmo += amount;
        currentAmmo += amount;
        UpdateAmmoText();
    }
}
