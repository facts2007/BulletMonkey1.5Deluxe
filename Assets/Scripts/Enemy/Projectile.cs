using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class HomingBulletSettings
{
    [Min(1)] public float interval = 8;
    [Min(.1f)] public float speed = 1.5f;
    [Min(0)] public float turnDegreesPerSecond = 90;
    [Min(1)] public int health = 30;
    [Min(1)] public int damage = 25;
    [Min(1)] public float sizeMultiplier = 16;
    [Min(.1f)] public float timeoutSeconds = 15;
    [Min(1)] public int ammoReward = 30;
    [Tooltip("Empty uses the existing enemy bullet visual.")]
    public GameObject visualPrefab;
    public GameObject impPrefab;
    public GameObject ammoPrefab;
    [Tooltip("Empty uses the miniboss's explosion effect.")]
    public GameObject explosionVfx;
    public AudioClip explosionSound;
}

public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 10;
    public Vector3 direction;
    public float lifeTime = 5f;
    public bool isNoscope;
    private bool hit;
    public bool IsHomingMissile { get; private set; }
    public int MissileHealth { get; private set; }
    private HomingBulletSettings settings;
    private EnemyHealth owner;
    private PlayerHealth target;
    private WaveArea wave;
    private GameObject ammoPrefab, explosionVfx, hint;
    private float age, collisionRadius, homingAimHeight = 1.1f;
    private Transform spinningVisual;
    private Quaternion visualStartRotation;
    private float spinDegreesPerSecond;

    private void Start()
    {
        if (!IsHomingMissile) Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (IsHomingMissile) { UpdateHomingMissile(); return; }
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(hit)return;
        if (IsHomingMissile) { HitMissileCollider(other); return; }
        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
        if (playerHealth != null)
        {
            hit=true;
            int previous=playerHealth.currentHealth;
            playerHealth.TakeDamage(damage);
            if(isNoscope && playerHealth!=null && playerHealth.currentHealth<previous && GameAudio.Instance!=null)
                GameAudio.Instance.PlayEffect(GameAudio.Instance.enemyNoscopeHit);
            Destroy(gameObject);
            return;
        }

        if (other.GetComponent<EnemyHealth>() != null || other.isTrigger)
        {
            return;
        }

        Destroy(gameObject);
    }

    public void ConfigureHoming(RangedEnemy source)
    {
        var drops = source.GetComponent<Enemy>();
        ConfigureHoming(source.GetComponent<EnemyHealth>(),
            source.player != null ? source.player.GetComponentInParent<PlayerHealth>() : null,
            source.homingBullet, source.WaveOwner,
            drops != null ? drops.ammoDropPrefab : null, drops != null ? drops.explosionEffect : null);
    }

    public void ConfigureHoming(EnemyHealth sourceOwner, PlayerHealth targetHealth, HomingBulletSettings config,
        WaveArea ownerWave = null, GameObject defaultAmmo = null, GameObject defaultExplosion = null)
    {
        IsHomingMissile = true;
        settings = config;
        owner = sourceOwner;
        target = targetHealth;
        wave = ownerWave;
        ammoPrefab = settings.ammoPrefab != null ? settings.ammoPrefab : defaultAmmo;
        explosionVfx = settings.explosionVfx != null ? settings.explosionVfx : defaultExplosion;
        MissileHealth = Mathf.Max(1, settings.health);
        speed = Mathf.Max(.1f, settings.speed); damage = settings.damage;
        lifeTime = Mathf.Max(.1f, settings.timeoutSeconds);
        // The reused visual has fast Bullet scripts (including one on its light).
        // Disable them before activation so their two-second destruction timers never start.
        foreach (var bullet in GetComponentsInChildren<Bullet>(true)) bullet.enabled = false;
        var rigidbody = GetComponent<Rigidbody>();
        if (rigidbody == null) rigidbody = gameObject.AddComponent<Rigidbody>();
        rigidbody.isKinematic = true; rigidbody.useGravity = false;
        var trigger = GetComponent<BoxCollider>();
        if (trigger == null) trigger = gameObject.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        var scale = transform.lossyScale;
        collisionRadius = Mathf.Max(.1f, Mathf.Min(trigger.size.x * Mathf.Abs(scale.x), trigger.size.y * Mathf.Abs(scale.y)) * .5f);
        homingAimHeight = Mathf.Max(1.1f, trigger.size.y * Mathf.Abs(scale.y) * .5f + .5f);
        // Gun raycasts ignore triggers: a solid child makes this projectile shootable.
        var hitbox = new GameObject("Shootable giant bullet", typeof(BoxCollider));
        hitbox.transform.SetParent(transform, false);
        var collider = hitbox.GetComponent<BoxCollider>();
        collider.center = trigger.center; collider.size = trigger.size;
        CreateHint();
    }

    private void UpdateHomingMissile()
    {
        if (hit) return;
        if (owner == null || owner.IsDead || target == null || target.currentHealth <= 0 || target.HasEscaped)
        { hit = true; Destroy(gameObject); return; }
        if (Time.timeScale <= 0f || GameSceneFlow.IsLoading) return;
        age += Time.deltaTime;
        if (age >= lifeTime) { ResolveMissile(false, true); return; }
        Vector3 aim = target.transform.position + Vector3.up * homingAimHeight - transform.position;
        if (aim.sqrMagnitude > .0001f)
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(aim), settings.turnDegreesPerSecond * Time.deltaTime);
        direction = transform.forward;
        float distance = speed * Time.deltaTime;
        RaycastHit nearest = default;
        float best = float.PositiveInfinity;
        foreach (var obstacle in Physics.SphereCastAll(transform.position, collisionRadius, direction, distance, ~0, QueryTriggerInteraction.Ignore))
        {
            if (IgnoreMissileCollider(obstacle.collider) || obstacle.distance >= best) continue;
            nearest = obstacle; best = obstacle.distance;
        }
        if (nearest.collider != null) { HitMissileCollider(nearest.collider); return; }
        transform.position += direction * distance;
    }

    private bool IgnoreMissileCollider(Collider other)
    {
        return other == null || other.transform.IsChildOf(transform) || other.isTrigger ||
            other.GetComponentInParent<EnemyHealth>() != null || other.GetComponentInParent<Projectile>() != null;
    }

    private void HitMissileCollider(Collider other)
    {
        if (hit || IgnoreMissileCollider(other)) return;
        var player = other.GetComponentInParent<PlayerHealth>();
        if (player != null) player.TakeDamage(damage);
        ResolveMissile(false, false);
    }

    public void TakeDamage(int amount)
    {
        if (!IsHomingMissile || hit || amount <= 0) return;
        MissileHealth = Mathf.Max(0, MissileHealth - amount);
        if (MissileHealth == 0) ResolveMissile(true, false);
    }

    private void ResolveMissile(bool reward, bool split)
    {
        if (hit) return;
        hit = true;
        foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
        if (explosionVfx != null) Destroy(Instantiate(explosionVfx, transform.position, Quaternion.identity), 4);
        else SpawnFog.Poof(transform.position, 1.4f);
        if (GameAudio.Instance != null)
            GameAudio.Instance.PlayEffect(settings.explosionSound != null ? settings.explosionSound : GameAudio.Instance.enemyExplode);
        if (reward && ammoPrefab != null)
        {
            var drop = LootMotion.Drop(ammoPrefab, transform.position, ammoPrefab.transform.rotation);
            var pickup = drop.GetComponent<AmmoPickup>();
            if (pickup != null) pickup.amount = settings.ammoReward;
        }
        if (split)
        {
            SpawnSupplyImp(transform.right * -.8f);
            SpawnSupplyImp(transform.right * .8f);
        }
        Destroy(gameObject);
    }

    private void SpawnSupplyImp(Vector3 offset)
    {
        if (settings.impPrefab == null) return;
        Vector3 point = transform.position + offset;
        RaycastHit ground = default;
        float best = float.PositiveInfinity;
        foreach (var hit in Physics.RaycastAll(point + Vector3.up * 5, Vector3.down, 80, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.normal.y < .5f || hit.collider.GetComponentInParent<EnemyHealth>() != null ||
                hit.collider.GetComponentInParent<PlayerHealth>() != null || hit.collider.GetComponentInParent<LootMotion>() != null || hit.distance >= best) continue;
            ground = hit; best = hit.distance;
        }
        if (ground.collider != null) point = ground.point;
        NavMeshHit landing;
        bool onNavMesh = NavMesh.SamplePosition(point, out landing, 10, NavMesh.AllAreas);
        if (onNavMesh) point = landing.position;
        else if (ground.collider == null)
        {
            if (target == null || !NavMesh.SamplePosition(target.transform.position + offset, out landing, 10, NavMesh.AllAreas)) return;
            point = landing.position; onNavMesh = true;
        }
        var staging = new GameObject("Giant bullet imp staging"); staging.SetActive(false);
        var imp = Instantiate(settings.impPrefab, point, Quaternion.identity, staging.transform);
        var nav = imp.GetComponent<NavMeshAgent>();
        if (nav != null) nav.enabled = onNavMesh;
        var body = imp.GetComponent<BoxCollider>();
        float clearance = body != null ? (body.size.y * .5f - body.center.y) * Mathf.Abs(imp.transform.lossyScale.y) + .04f : .5f;
        imp.transform.position = point + Vector3.up * Mathf.Max(clearance, nav != null ? nav.baseOffset : 0);
        var enemy = imp.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.ammoDropPrefab = ammoPrefab; enemy.ammoDropChance = 1;
            enemy.minAmmoAmount = enemy.maxAmmoAmount = settings.ammoReward;
            enemy.canBeStomped = true; enemy.stompInstantKills = true;
        }
        var melee = imp.GetComponent<MeleeEnemy>(); if (melee != null) melee.chaseRange = 200;
        imp.name = "Giant bullet supply imp (" + settings.ammoReward + " ammo)";
        imp.transform.SetParent(null, true); imp.SetActive(true); Destroy(staging);
        if (wave != null) wave.RegisterReinforcement(imp);
        SpawnFog.Poof(imp.transform.position, .8f);
    }

    private void CreateHint()
    {
        hint = new GameObject("Giant bullet — Shoot it!!!", typeof(RectTransform), typeof(Canvas));
        var canvas = hint.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 20;
        hint.transform.localScale = Vector3.one * .013f;
        var rect = hint.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(280, 48); rect.pivot = new Vector2(0, .5f);
        var label = new GameObject("<-Shoot it!!!", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(hint.transform, false);
        var text = label.GetComponent<TextMeshProUGUI>();
        if (Gun.Instance != null && Gun.Instance.ammoText != null) text.font = Gun.Instance.ammoText.font;
        text.text = "<-Shoot it!!!"; text.richText = false; text.fontSize = 28;
        text.alignment = TextAlignmentOptions.MidlineLeft; text.textWrappingMode = TextWrappingModes.NoWrap;
        text.color = new Color(1, .9f, .35f); text.outlineColor = Color.black; text.outlineWidth = .2f; text.raycastTarget = false;
        text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
        UpdateHint();
    }

    public void SetSpinningVisual(Transform visual, float degreesPerSecond)
    {
        spinningVisual = visual;
        visualStartRotation = visual.localRotation;
        spinDegreesPerSecond = degreesPerSecond;
    }

    private void LateUpdate()
    {
        if (spinningVisual != null)
            spinningVisual.localRotation = visualStartRotation * Quaternion.Euler(0, age * spinDegreesPerSecond, 0);
        if (hint != null) UpdateHint();
    }
    private void UpdateHint()
    {
        var camera = Camera.main;
        if (camera == null) return;
        hint.transform.SetPositionAndRotation(transform.position + camera.transform.right * (collisionRadius + .2f), camera.transform.rotation);
        hint.GetComponent<Canvas>().worldCamera = camera;
    }
    private void OnDestroy() { if (hint != null) Destroy(hint); }
}
