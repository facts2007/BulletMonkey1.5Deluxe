using UnityEngine;
using TMPro;

public class Gun : MonoBehaviour
{
    [Header("Shooting")]
    public float fireRate = 10f;
    public float range = 100f;
    public int damage = 10;

    [Header("Ammo")]
    public int maxAmmo = 30;
    public int currentAmmo;

    [Header("Effects")]
    public GameObject bulletPrefab;
    public Transform muzzlePoint;

    [Header("Aiming")]
    public Camera aimCamera;

    [Header("UI")]
    public TextMeshProUGUI ammoText;

    private float fireCooldown;

    private void Awake()
    {
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
        Quaternion spawnRotation = Quaternion.LookRotation(aimCamera.transform.forward);

        if (bulletPrefab != null)
        {
            Instantiate(bulletPrefab, spawnPoint.position, spawnRotation);
        }

        RaycastHit hit;
        if (Physics.Raycast(aimCamera.transform.position, aimCamera.transform.forward, out hit, range))
        {
            EnemyHealth enemyHealth = hit.collider.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
            }
        }
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
}