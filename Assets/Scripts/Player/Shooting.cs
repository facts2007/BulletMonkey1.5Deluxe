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

    private float fireCooldown;

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
        fireCooldown -= Time.deltaTime;

        if (Input.GetButton("Fire1") && fireCooldown <= 0f && currentAmmo > 0)
        {
            Shoot();
            fireCooldown = 1f / fireRate;
        }
    }

    private void Shoot()
    {
        currentAmmo--;
        UpdateAmmoText();

        Transform spawnPoint = muzzlePoint != null ? muzzlePoint : transform;
        Ray aimRay = aimCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
        Quaternion spawnRotation = Quaternion.LookRotation(aimRay.direction);

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

        RaycastHit hit;
        if (Physics.Raycast(aimRay, out hit, range))
        {
            EnemyHealth enemyHealth = hit.collider.GetComponent<EnemyHealth>();
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
        ammoText.text = currentAmmo + "/" + maxAmmo;
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