using System.Collections;
using UnityEngine;

public enum IndicatorShape
{
    Box,
    Circle
}

public enum AttackTarget
{
    PlayerPosition,
    InFrontOfBoss
}

[System.Serializable]
public class BossAttack
{
    public string attackName = "Attack";
    public IndicatorShape shape = IndicatorShape.Circle;

    [Tooltip("Circle only. Boxes always stretch from the boss to the player")]
    public AttackTarget target = AttackTarget.PlayerPosition;

    [Header("Fairness / Timing")]
    public float minTimer = 1.25f;
    public float maxTimer = 2.75f;

    [Header("Circle Size")]
    public float circleRadiusMin = 2.5f;
    public float circleRadiusMax = 4f;

    [Header("Box Size")]
    public float boxWidthMin = 2f;
    public float boxWidthMax = 4f;
    public float extraLengthBehindPlayer = 3f;

    [Tooltip("Circle only, when Target is InFrontOfBoss.")]
    public float distanceFromBoss = 6f;

    public int damage = 20;

    [Tooltip("Optional. Spawned when the timer runs out.")]
    public GameObject timerEndEffect;
    public float effectLifetime = 3f;
}

public class BossController : MonoBehaviour
{
    [Header("State")]
    public bool isActive = true;

    [Header("Behavior")]
    public float timeBetweenAttacks = 3f;
    public float turnSpeed = 5f;
    public bool randomOrder = true;

    [Header("Attacks")]
    public BossAttack[] attacks = new BossAttack[]
    {
        new BossAttack
        {
            attackName = "Circle Slam",
            shape = IndicatorShape.Circle,
            target = AttackTarget.PlayerPosition,
            minTimer = 1.25f,
            maxTimer = 2.75f,
            circleRadiusMin = 2.5f,
            circleRadiusMax = 4f,
            damage = 20
        },
        new BossAttack
        {
            attackName = "Beam",
            shape = IndicatorShape.Box,
            minTimer = 1.5f,
            maxTimer = 2.75f,
            boxWidthMin = 2f,
            boxWidthMax = 4f,
            extraLengthBehindPlayer = 3f,
            damage = 30
        }
    };

    [Header("Indicator Look")]
    public Color indicatorColor = new Color(1f, 0f, 0f, 0.4f);
    public Material indicatorMaterial;

    [Header("Ground Detection")]
    public float groundRayStartHeight = 5f;
    public float indicatorHeightOffset = 0.05f;

    private struct AttackArea
    {
        public Vector3 center;
        public Quaternion rotation;
        public float radius;
        public Vector2 boxSize;
        public float timer;
    }

    private const float IndicatorThickness = 0.02f;
    private const float GroundRayLength = 200f;

    private Transform player;
    private PlayerHealth playerHealth;
    private Material runtimeMaterial;
    private GameObject currentIndicator;
    private float attackTimer;
    private int nextAttackIndex;
    private bool isAttacking;

    private void Start()
    {
        StartCoroutine(CinematicAttackLoop());
    }

    private void Update()
    {
        if (!isActive) return;

        if (player == null)
        {
            FindPlayer();
            return;
        }

        if (!player.gameObject.activeInHierarchy) return;

        FaceTarget();

        if (isAttacking || attacks.Length == 0) return;

        attackTimer += Time.deltaTime;
        if (attackTimer >= timeBetweenAttacks)
        {
            attackTimer = 0f;
            StartCoroutine(PerformAttack(PickAttack()));
        }
    }

    private void FindPlayer()
    {
        GameObject found = GameObject.FindGameObjectWithTag("Player");
        if (found == null) return;

        player = found.transform;
        playerHealth = found.GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            playerHealth = found.GetComponentInParent<PlayerHealth>();
        }
    }

    private void FaceTarget()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    private BossAttack PickAttack()
    {
        if (randomOrder)
        {
            return attacks[Random.Range(0, attacks.Length)];
        }

        BossAttack attack = attacks[nextAttackIndex];
        nextAttackIndex = (nextAttackIndex + 1) % attacks.Length;
        return attack;
    }

    // =========================================================
    // MAIN ATTACK SYSTEM (circle/box, unchanged from before)
    // =========================================================

    private IEnumerator PerformAttack(BossAttack attack)
    {
        isAttacking = true;

        AttackArea area = CalculateArea(attack);
        currentIndicator = CreateIndicator(attack.shape, area, indicatorColor);

        yield return new WaitForSeconds(area.timer);

        SpawnTimerEndEffect(attack.timerEndEffect, attack.effectLifetime, area.center, area.rotation);

        if (player != null && player.gameObject.activeInHierarchy && IsPlayerInside(attack.shape, area))
        {
            DamagePlayer(attack.damage);
        }

        if (currentIndicator != null)
        {
            Destroy(currentIndicator);
            currentIndicator = null;
        }

        isAttacking = false;
    }

    private AttackArea CalculateArea(BossAttack attack)
    {
        AttackArea area = new AttackArea();
        Vector3 bossPosition = transform.position;
        Vector3 flatBossPosition = new Vector3(bossPosition.x, 0f, bossPosition.z);

        Vector3 toPlayer = player.position - bossPosition;
        toPlayer.y = 0f;
        float distanceToPlayer = toPlayer.magnitude;
        Vector3 directionToPlayer = distanceToPlayer > 0.001f ? toPlayer / distanceToPlayer : Vector3.forward;

        if (attack.shape == IndicatorShape.Box)
        {
            float length = distanceToPlayer + attack.extraLengthBehindPlayer;
            float width = Random.Range(Mathf.Min(attack.boxWidthMin, attack.boxWidthMax), Mathf.Max(attack.boxWidthMin, attack.boxWidthMax));

            area.center = flatBossPosition + directionToPlayer * (length * 0.5f);
            area.rotation = Quaternion.LookRotation(directionToPlayer);
            area.boxSize = new Vector2(width, length);

            float sizePercentage = Mathf.InverseLerp(attack.boxWidthMin, attack.boxWidthMax, width);
            area.timer = Mathf.Lerp(attack.minTimer, attack.maxTimer, sizePercentage);
        }
        else if (attack.target == AttackTarget.InFrontOfBoss)
        {
            Vector3 forward = transform.forward;
            forward.y = 0f;
            forward.Normalize();

            area.center = flatBossPosition + forward * attack.distanceFromBoss;
            area.rotation = Quaternion.LookRotation(forward);
            area.radius = GetRandomCircleRadius(attack);
            area.timer = CalculateCircleTimer(attack, area.radius);
        }
        else
        {
            area.center = new Vector3(player.position.x, 0f, player.position.z);
            area.rotation = Quaternion.LookRotation(directionToPlayer);
            area.radius = GetRandomCircleRadius(attack);
            area.timer = CalculateCircleTimer(attack, area.radius);
        }

        float referenceY = Mathf.Max(bossPosition.y, player.position.y);
        area.center.y = GetGroundY(area.center.x, area.center.z, referenceY) + indicatorHeightOffset;
        area.timer = Mathf.Clamp(area.timer, attack.minTimer, attack.maxTimer);

        return area;
    }

    private float GetRandomCircleRadius(BossAttack attack)
    {
        return Random.Range(Mathf.Min(attack.circleRadiusMin, attack.circleRadiusMax), Mathf.Max(attack.circleRadiusMin, attack.circleRadiusMax));
    }

    private float CalculateCircleTimer(BossAttack attack, float radius)
    {
        float sizePercentage = Mathf.InverseLerp(attack.circleRadiusMin, attack.circleRadiusMax, radius);
        return Mathf.Lerp(attack.minTimer, attack.maxTimer, sizePercentage);
    }

    // =========================================================
    // CINEMATIC ATTACK SYSTEM (fully independent, own timer,
    // own damage check - never touches isAttacking, attackTimer,
    // or the attacks[] array above)
    // =========================================================

    [Header("Cinematic Attack (Independent System)")]
    public bool cinematicEnabled = true;

    [Header("Cinematic Timing")]
    [Tooltip("Timer is randomized in this range, then everything below scales off where it falls")]
    public float cinematicMinTimer = 1f;
    public float cinematicMaxTimer = 2.5f;
    public int cinematicMinAmountPerWave = 1;
    public int cinematicMaxAmountPerWave = 3;
    public float cinematicMinWaveDelay = 1f;
    public float cinematicMaxWaveDelay = 3f;
    public float cinematicStaggerMin = 0.1f;
    public float cinematicStaggerMax = 0.3f;

    [Header("Cinematic Damage Circle")]
    [Tooltip("Radius scales with timer length: short timer = small circle, long timer = big circle")]
    public float cinematicRadiusMin = 2f;
    public float cinematicRadiusMax = 4f;
    public int cinematicDamage = 15;
    public Color cinematicIndicatorColor = new Color(1f, 0f, 0f, 0.4f);

    [Header("Cinematic Leaping Projectile")]
    [Tooltip("Optional. Thrown from the boss and lands exactly when this impact's timer ends")]
    public GameObject cinematicProjectilePrefab;
    public float cinematicProjectileArcHeight = 8f;
    public float cinematicProjectileLaunchHeight = 2f;

    [Tooltip("Projectile's own scale multiplier. Also scales with timer length, same as the radius")]
    public float cinematicProjectileMinScale = 0.75f;
    public float cinematicProjectileMaxScale = 1.5f;

    [Header("Cinematic Targeting")]
    [Tooltip("Optional. If assigned, impacts land randomly inside this area's X/Z size instead of around the player")]
    public Transform cinematicAreaCenter;
    public Vector2 cinematicAreaSize = new Vector2(40f, 40f);
    public float cinematicMinDistanceFromPlayer = 4f;
    public float cinematicMaxDistanceFromPlayer = 18f;

    [Header("Cinematic Optional Effect")]
    [Tooltip("Optional. If empty, nothing spawns and nothing errors")]
    public GameObject cinematicTimerEndEffect;
    public float cinematicEffectLifetime = 3f;

    [Tooltip("Effect's own scale multiplier. Also scales with timer length, same as the radius")]
    public float cinematicEffectMinScale = 0.75f;
    public float cinematicEffectMaxScale = 1.5f;

    private IEnumerator CinematicAttackLoop()
    {
        while (true)
        {
            if (!cinematicEnabled || !isActive || player == null || !player.gameObject.activeInHierarchy)
            {
                yield return null;
                continue;
            }

            int amount = Random.Range(cinematicMinAmountPerWave, cinematicMaxAmountPerWave + 1);

            for (int i = 0; i < amount; i++)
            {
                StartCoroutine(PerformCinematicImpact());

                if (i < amount - 1)
                {
                    yield return new WaitForSeconds(Random.Range(cinematicStaggerMin, cinematicStaggerMax));
                }
            }

            yield return new WaitForSeconds(Random.Range(cinematicMinWaveDelay, cinematicMaxWaveDelay));
        }
    }

    private IEnumerator PerformCinematicImpact()
    {
        Vector3 target = GetRandomCinematicPosition();
        float referenceY = Mathf.Max(transform.position.y, player.position.y);
        target.y = GetGroundY(target.x, target.z, referenceY) + indicatorHeightOffset;

        float timer = Random.Range(cinematicMinTimer, cinematicMaxTimer);
        float sizePercentage = Mathf.InverseLerp(cinematicMinTimer, cinematicMaxTimer, timer);

        float radius = Mathf.Lerp(cinematicRadiusMin, cinematicRadiusMax, sizePercentage);
        float projectileScale = Mathf.Lerp(cinematicProjectileMinScale, cinematicProjectileMaxScale, sizePercentage);
        float effectScale = Mathf.Lerp(cinematicEffectMinScale, cinematicEffectMaxScale, sizePercentage);

        AttackArea area = new AttackArea();
        area.center = target;
        area.rotation = Quaternion.identity;
        area.radius = radius;

        GameObject indicator = CreateIndicator(IndicatorShape.Circle, area, cinematicIndicatorColor);

        GameObject projectile = null;
        if (cinematicProjectilePrefab != null)
        {
            Vector3 startPosition = transform.position + Vector3.up * cinematicProjectileLaunchHeight;
            projectile = Instantiate(cinematicProjectilePrefab, startPosition, Quaternion.identity);
            projectile.transform.localScale *= projectileScale;

            StartCoroutine(MoveProjectileArc(projectile, startPosition, target, timer, cinematicProjectileArcHeight));
        }

        yield return new WaitForSeconds(timer);

        SpawnTimerEndEffect(cinematicTimerEndEffect, cinematicEffectLifetime, target, Quaternion.identity, effectScale);

        if (player != null && player.gameObject.activeInHierarchy && IsPlayerInside(IndicatorShape.Circle, area))
        {
            DamagePlayer(cinematicDamage);
        }

        if (indicator != null)
        {
            Destroy(indicator);
        }

        if (projectile != null)
        {
            Destroy(projectile);
        }
    }

    private Vector3 GetRandomCinematicPosition()
    {
        if (cinematicAreaCenter != null)
        {
            Vector3 center = cinematicAreaCenter.position;
            float halfX = cinematicAreaSize.x * 0.5f;
            float halfZ = cinematicAreaSize.y * 0.5f;

            return new Vector3(center.x + Random.Range(-halfX, halfX), center.y, center.z + Random.Range(-halfZ, halfZ));
        }

        float angle = Random.Range(0f, 360f);
        float distance = Random.Range(cinematicMinDistanceFromPlayer, cinematicMaxDistanceFromPlayer);
        Vector3 offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0f, Mathf.Sin(angle * Mathf.Deg2Rad)) * distance;

        return new Vector3(player.position.x + offset.x, player.position.y, player.position.z + offset.z);
    }

    private IEnumerator MoveProjectileArc(GameObject projectile, Vector3 start, Vector3 end, float duration, float arcHeight)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (projectile == null) yield break;

            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);

            Vector3 position = Vector3.Lerp(start, end, progress);
            position.y += Mathf.Sin(progress * Mathf.PI) * arcHeight;

            projectile.transform.position = position;

            yield return null;
        }

        if (projectile != null)
        {
            projectile.transform.position = end;
        }
    }

    // =========================================================
    // SHARED HELPERS (used by both systems above)
    // =========================================================

    private float GetGroundY(float x, float z, float referenceY)
    {
        Vector3 origin = new Vector3(x, referenceY + groundRayStartHeight, z);
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, groundRayStartHeight + GroundRayLength, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        float closestDistance = float.MaxValue;
        float groundY = transform.position.y;

        foreach (RaycastHit hit in hits)
        {
            if (IsIgnoredForGround(hit.collider)) continue;

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                groundY = hit.point.y;
            }
        }

        return groundY;
    }

    private bool IsIgnoredForGround(Collider other)
    {
        if (other.transform.IsChildOf(transform)) return true;
        if (other.GetComponentInParent<PlayerHealth>() != null) return true;
        if (other.GetComponentInParent<Enemy>() != null) return true;

        return false;
    }

    private GameObject CreateIndicator(IndicatorShape shape, AttackArea area, Color color)
    {
        bool isCircle = shape == IndicatorShape.Circle;
        GameObject indicator = GameObject.CreatePrimitive(isCircle ? PrimitiveType.Cylinder : PrimitiveType.Cube);

        Destroy(indicator.GetComponent<Collider>());

        indicator.name = isCircle ? "BossIndicator_Circle" : "BossIndicator_Box";
        indicator.transform.SetPositionAndRotation(area.center, area.rotation);

        if (isCircle)
        {
            float diameter = area.radius * 2f;
            indicator.transform.localScale = new Vector3(diameter, IndicatorThickness, diameter);
        }
        else
        {
            indicator.transform.localScale = new Vector3(area.boxSize.x, IndicatorThickness, area.boxSize.y);
        }

        Renderer indicatorRenderer = indicator.GetComponent<Renderer>();
        indicatorRenderer.sharedMaterial = GetIndicatorMaterial(color);
        indicatorRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        indicatorRenderer.receiveShadows = false;

        return indicator;
    }

    private Material GetIndicatorMaterial(Color color)
    {
        if (indicatorMaterial != null) return indicatorMaterial;

        if (runtimeMaterial == null)
        {
            runtimeMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        runtimeMaterial.color = color;
        return runtimeMaterial;
    }

    private void SpawnTimerEndEffect(GameObject effectPrefab, float lifetime, Vector3 position, Quaternion rotation, float scale = 1f)
    {
        if (effectPrefab == null) return;

        GameObject effect = Instantiate(effectPrefab, position, rotation);
        effect.transform.localScale *= scale;

        if (lifetime > 0f)
        {
            Destroy(effect, lifetime);
        }
    }

    private bool IsPlayerInside(IndicatorShape shape, AttackArea area)
    {
        Vector3 offset = player.position - area.center;
        offset.y = 0f;

        if (shape == IndicatorShape.Circle)
        {
            return offset.magnitude <= area.radius;
        }

        Vector3 local = Quaternion.Inverse(area.rotation) * offset;
        return Mathf.Abs(local.x) <= area.boxSize.x * 0.5f && Mathf.Abs(local.z) <= area.boxSize.y * 0.5f;
    }

    private void DamagePlayer(int damage)
    {
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
        }
    }

    private void OnDestroy()
    {
        if (currentIndicator != null)
        {
            Destroy(currentIndicator);
        }
    }
}