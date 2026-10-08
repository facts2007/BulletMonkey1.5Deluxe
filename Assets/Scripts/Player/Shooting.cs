using System.Collections;
using UnityEngine;
using UnityEngine.VFX;
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
    [Tooltip("Played at the muzzle for every normal and super shot.")]
    public GameObject muzzleFlarePrefab;

    [Header("Aiming")]
    public Camera aimCamera;

    [Header("UI")]
    public TextMeshProUGUI ammoText;
    [Header("Ammo counter pop")]
    [Min(.05f)] public float ammoPopSeconds = .25f;
    [Range(1f, 1.5f)] public float ammoFirePopScale = 1.1f;
    [Range(1f, 1.5f)] public float ammoPickupPopScale = 1.2f;

    [Header("Animation and super shooting")]
    public CharacterAnimationDriver characterAnimation;
    public SuperShootAbility superAbility;
    public ShootCameraShake cameraShake;
    [Tooltip("Random cone half-angle for each super shot. Regular shots stay accurate.")]
    [Range(0f, 12f)] public float superSpreadAngle = 3.5f;

    private float fireCooldown;
    private bool wasSuperShooting;
    private GameObject muzzleFlareInstance;
    private VisualEffect[] muzzleVisualEffects;
    private ParticleSystem[] muzzleParticles;
    private Quaternion muzzleFlareRotation;
    private MouseLook playerLook;
    private Coroutine ammoPop;
    private Vector3 ammoBaseScale = Vector3.one;

    private void Awake()
    {
        Instance = this;
        playerLook = GetComponentInParent<MouseLook>();
        if (ammoText != null) ammoBaseScale = ammoText.transform.localScale;
        currentAmmo = maxAmmo;
        UpdateAmmoText();

        if (aimCamera == null)
        {
            aimCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (Time.timeScale <= 0f || GameSceneFlow.IsLoading) return;
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
        if (playerLook != null) playerLook.FaceAim();
        if (!superShot)
        {
            currentAmmo--;
            UpdateAmmoText();
        }
        PopAmmoCounter(ammoFirePopScale);

        Transform spawnPoint = muzzlePoint != null ? muzzlePoint : transform;
        Ray aimRay = aimCamera != null
            ? aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
            : new Ray(spawnPoint.position, spawnPoint.forward);
        if (superShot && superSpreadAngle > 0f)
        {
            Vector2 spread = Random.insideUnitCircle * Mathf.Tan(superSpreadAngle * Mathf.Deg2Rad);
            aimRay.direction = (Quaternion.LookRotation(aimRay.direction) * new Vector3(spread.x, spread.y, 1f)).normalized;
        }
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
        PlayMuzzleFlare(spawnPoint, spawnRotation);

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
            var missile = hit.collider.GetComponentInParent<Projectile>();
            EnemyHealth enemyHealth = hit.collider.GetComponentInParent<EnemyHealth>();
            if (missile != null && missile.IsHomingMissile) missile.TakeDamage(damage);
            else if (enemyHealth != null)
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

    private void PlayMuzzleFlare(Transform spawnPoint, Quaternion shotRotation)
    {
        if (muzzleFlarePrefab == null || spawnPoint == null) return;

        bool created = false;
        if (muzzleFlareInstance == null)
        {
            muzzleFlareInstance = Instantiate(muzzleFlarePrefab, spawnPoint);
            muzzleFlareInstance.name = "Muzzle flare";
            // Imported effects have demo-scene offsets; the actual shot origin wins.
            muzzleFlareInstance.transform.localPosition = Vector3.zero;
            muzzleVisualEffects = muzzleFlareInstance.GetComponentsInChildren<VisualEffect>(true);
            muzzleParticles = muzzleFlareInstance.GetComponentsInChildren<ParticleSystem>(true);
            created = true;
        }

        muzzleFlareRotation = shotRotation * muzzleFlarePrefab.transform.localRotation;
        muzzleFlareInstance.transform.SetPositionAndRotation(spawnPoint.position, muzzleFlareRotation);
        if (created) return; // Play-on-awake handles the first shot.

        foreach (var effect in muzzleVisualEffects) if (effect != null) effect.Play();
        foreach (var particles in muzzleParticles)
        {
            if (particles == null) continue;
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.Play(true);
        }
    }

    private void LateUpdate()
    {
        // Gun animation can rotate the muzzle after Update; keep the flash along its shot.
        if (muzzleFlareInstance != null) muzzleFlareInstance.transform.rotation = muzzleFlareRotation;
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

    private void PopAmmoCounter(float scale)
    {
        if (ammoText == null || !isActiveAndEnabled) return;
        if (ammoPop != null) StopCoroutine(ammoPop);
        ammoPop = StartCoroutine(AnimateAmmoCounter(scale));
    }

    private IEnumerator AnimateAmmoCounter(float scale)
    {
        var counter = ammoText.rectTransform;
        Vector3 from = counter.localScale, peak = ammoBaseScale * scale;
        float growSeconds = Mathf.Max(.025f, ammoPopSeconds * .55f);
        float settleSeconds = Mathf.Max(.025f, ammoPopSeconds * .45f);
        for (float elapsed = 0; elapsed < growSeconds; elapsed += Time.deltaTime)
        {
            float t = Mathf.Clamp01(elapsed / growSeconds) - 1f;
            float outBack = 1f + 2.70158f * t * t * t + 1.70158f * t * t;
            counter.localScale = Vector3.LerpUnclamped(from, peak, outBack);
            yield return null;
        }
        from = counter.localScale;
        for (float elapsed = 0; elapsed < settleSeconds; elapsed += Time.deltaTime)
        {
            counter.localScale = Vector3.Lerp(from, ammoBaseScale, Mathf.SmoothStep(0, 1, elapsed / settleSeconds));
            yield return null;
        }
        counter.localScale = ammoBaseScale;
        ammoPop = null;
    }

    private void OnDisable()
    {
        if (ammoPop != null) StopCoroutine(ammoPop);
        ammoPop = null;
        if (ammoText != null) ammoText.transform.localScale = ammoBaseScale;
    }

    public void Reload()
    {
        bool changed = currentAmmo < maxAmmo;
        currentAmmo = maxAmmo;
        UpdateAmmoText();
        if (changed) PopAmmoCounter(ammoPickupPopScale);
    }

    public void AddAmmo(int amount)
    {
        int previous = currentAmmo;
        currentAmmo = Mathf.Min(currentAmmo + amount, maxAmmo);
        UpdateAmmoText();
        if (currentAmmo > previous) PopAmmoCounter(ammoPickupPopScale);
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
        if (amount > 0) PopAmmoCounter(ammoPickupPopScale);
    }
}
