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
    [Tooltip("Minimum warning time, regardless of size.")]
    public float minTimer = 1.25f;

    [Tooltip("Maximum warning time, regardless of size.")]
    public float maxTimer = 2.75f;

    [Header("Circle Size")]
    [Tooltip("Circle only. Minimum radius.")]
    public float circleRadiusMin = 2.5f;

    [Tooltip("Circle only. Maximum radius.")]
    public float circleRadiusMax = 4f;

    [Header("Box Size")]
    [Tooltip("Box only. Minimum width of the beam.")]
    public float boxWidthMin = 2f;

    [Tooltip("Box only. Maximum width of the beam.")]
    public float boxWidthMax = 4f;

    [Tooltip("Box only. How far the beam reaches past the player.")]
    public float extraLengthBehindPlayer = 3f;

    [Tooltip("Circle only, when Target is InFrontOfBoss.")]
    public float distanceFromBoss = 6f;

    public int damage = 20;

    [Tooltip("Optional. Spawned when the timer runs out.")]
    public GameObject timerEndEffect;

    [Tooltip("Seconds before the effect is destroyed. 0 = never.")]
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

    // =========================================================
    // CINEMATIC FALLING ATTACK
    // =========================================================

    [Header("Cinematic Falling Attack")]

    [Tooltip("GameObject that falls from above and creates the cinematic impact.")]
    public GameObject cinematicEffectPrefab;

    [Tooltip("Minimum number of falling effects in one wave.")]
    public int cinematicMinAmount = 1;

    [Tooltip("Maximum number of falling effects in one wave.")]
    public int cinematicMaxAmount = 3;

    [Tooltip("Minimum delay before another wave starts.")]
    public float cinematicMinWaveDelay = 1f;

    [Tooltip("Maximum delay before another wave starts.")]
    public float cinematicMaxWaveDelay = 3f;

    [Tooltip("How high above the boss the effect starts.")]
    public float cinematicSpawnHeight = 25f;

    [Tooltip("Minimum distance from the player for a target.")]
    public float cinematicMinDistance = 4f;

    [Tooltip("Maximum distance from the player for a target.")]
    public float cinematicMaxDistance = 18f;

    [Tooltip("How long the falling effect takes to reach the ground.")]
    public float cinematicFallDuration = 0.45f;

    [Header("Cinematic Effect Size")]

    [Tooltip("Smallest possible effect scale.")]
    public float cinematicMinSize = 0.75f;

    [Tooltip("Largest possible effect scale.")]
    public float cinematicMaxSize = 1.15f;

    [Tooltip("Extra size multiplier for the largest impact.")]
    public float cinematicLargeExplosionMultiplier = 1.15f;

    [Header("Cinematic Spawn Area")]

    [Tooltip("Optional. If assigned, targets are randomly selected inside this area's X/Z size.")]
    public Transform cinematicAreaCenter;

    [Tooltip("X/Z size of the cinematic spawn area when using cinematicAreaCenter.")]
    public Vector2 cinematicAreaSize = new Vector2(40f, 40f);

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
        // Start the cinematic attack independently.
        StartCoroutine(CinematicFallingAttackLoop());
    }

    private void Update()
    {
        if (!isActive)
            return;

        if (player == null)
        {
            FindPlayer();
            return;
        }

        if (!player.gameObject.activeInHierarchy)
            return;

        FaceTarget();

        if (isAttacking || attacks.Length == 0)
            return;

        attackTimer += Time.deltaTime;

        if (attackTimer >= timeBetweenAttacks)
        {
            attackTimer = 0f;
            StartCoroutine(
                PerformAttack(
                    PickAttack()
                )
            );
        }
    }

    private void FindPlayer()
    {
        GameObject found =
            GameObject.FindGameObjectWithTag("Player");

        if (found == null)
            return;

        player = found.transform;

        playerHealth =
            found.GetComponent<PlayerHealth>();

        if (playerHealth == null)
        {
            playerHealth =
                found.GetComponentInParent<PlayerHealth>();
        }
    }

    private void FaceTarget()
    {
        Vector3 direction =
            player.position -
            transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime
            );
    }

    private BossAttack PickAttack()
    {
        if (randomOrder)
        {
            return attacks[
                Random.Range(
                    0,
                    attacks.Length
                )
            ];
        }

        BossAttack attack =
            attacks[nextAttackIndex];

        nextAttackIndex =
            (nextAttackIndex + 1) %
            attacks.Length;

        return attack;
    }

    // =========================================================
    // NORMAL ATTACK SYSTEM
    // =========================================================

    private IEnumerator PerformAttack(
        BossAttack attack
    )
    {
        isAttacking = true;

        AttackArea area =
            CalculateArea(attack);

        currentIndicator =
            CreateIndicator(
                attack,
                area
            );

        yield return new WaitForSeconds(
            area.timer
        );

        SpawnTimerEndEffect(
            attack,
            area
        );

        if (
            player != null &&
            player.gameObject.activeInHierarchy &&
            IsPlayerInside(
                attack,
                area
            )
        )
        {
            DamagePlayer(
                attack.damage
            );
        }

        if (currentIndicator != null)
        {
            Destroy(
                currentIndicator
            );

            currentIndicator = null;
        }

        isAttacking = false;
    }

    private AttackArea CalculateArea(
        BossAttack attack
    )
    {
        AttackArea area =
            new AttackArea();

        Vector3 bossPosition =
            transform.position;

        Vector3 flatBossPosition =
            new Vector3(
                bossPosition.x,
                0f,
                bossPosition.z
            );

        Vector3 toPlayer =
            player.position -
            bossPosition;

        toPlayer.y = 0f;

        float distanceToPlayer =
            toPlayer.magnitude;

        Vector3 directionToPlayer =
            distanceToPlayer > 0.001f
                ? toPlayer / distanceToPlayer
                : Vector3.forward;

        if (
            attack.shape ==
            IndicatorShape.Box
        )
        {
            float length =
                distanceToPlayer +
                attack.extraLengthBehindPlayer;

            float width =
                Random.Range(
                    Mathf.Min(
                        attack.boxWidthMin,
                        attack.boxWidthMax
                    ),
                    Mathf.Max(
                        attack.boxWidthMin,
                        attack.boxWidthMax
                    )
                );

            area.center =
                flatBossPosition +
                directionToPlayer *
                (length * 0.5f);

            area.rotation =
                Quaternion.LookRotation(
                    directionToPlayer
                );

            area.boxSize =
                new Vector2(
                    width,
                    length
                );

            float sizePercentage =
                Mathf.InverseLerp(
                    attack.boxWidthMin,
                    attack.boxWidthMax,
                    width
                );

            area.timer =
                Mathf.Lerp(
                    attack.minTimer,
                    attack.maxTimer,
                    sizePercentage
                );
        }
        else if (
            attack.target ==
            AttackTarget.InFrontOfBoss
        )
        {
            Vector3 forward =
                transform.forward;

            forward.y = 0f;
            forward.Normalize();

            area.center =
                flatBossPosition +
                forward *
                attack.distanceFromBoss;

            area.rotation =
                Quaternion.LookRotation(
                    forward
                );

            area.radius =
                GetRandomCircleRadius(
                    attack
                );

            area.timer =
                CalculateCircleTimer(
                    attack,
                    area.radius
                );
        }
        else
        {
            area.center =
                new Vector3(
                    player.position.x,
                    0f,
                    player.position.z
                );

            area.rotation =
                Quaternion.LookRotation(
                    directionToPlayer
                );

            area.radius =
                GetRandomCircleRadius(
                    attack
                );

            area.timer =
                CalculateCircleTimer(
                    attack,
                    area.radius
                );
        }

        float referenceY =
            Mathf.Max(
                bossPosition.y,
                player.position.y
            );

        area.center.y =
            GetGroundY(
                area.center.x,
                area.center.z,
                referenceY
            ) +
            indicatorHeightOffset;

        area.timer =
            Mathf.Clamp(
                area.timer,
                attack.minTimer,
                attack.maxTimer
            );

        return area;
    }

    private float GetRandomCircleRadius(
        BossAttack attack
    )
    {
        return Random.Range(
            Mathf.Min(
                attack.circleRadiusMin,
                attack.circleRadiusMax
            ),
            Mathf.Max(
                attack.circleRadiusMin,
                attack.circleRadiusMax
            )
        );
    }

    private float CalculateCircleTimer(
        BossAttack attack,
        float radius
    )
    {
        float sizePercentage =
            Mathf.InverseLerp(
                attack.circleRadiusMin,
                attack.circleRadiusMax,
                radius
            );

        return Mathf.Lerp(
            attack.minTimer,
            attack.maxTimer,
            sizePercentage
        );
    }

    // =========================================================
    // CINEMATIC FALLING ATTACK
    // =========================================================

    private IEnumerator CinematicFallingAttackLoop()
    {
        while (true)
        {
            // If the boss is inactive, don't spawn cinematic effects.
            if (!isActive)
            {
                yield return null;
                continue;
            }

            // Make sure we know where the player is.
            if (player == null)
            {
                FindPlayer();

                yield return new WaitForSeconds(0.25f);
                continue;
            }

            if (
                cinematicEffectPrefab != null &&
                player.gameObject.activeInHierarchy
            )
            {
                // Random amount of impacts in this wave.
                int amount =
                    Random.Range(
                        cinematicMinAmount,
                        cinematicMaxAmount + 1
                    );

                for (int i = 0; i < amount; i++)
                {
                    SpawnCinematicImpact();

                    // Multiple impacts are staggered very slightly.
                    if (amount > 1 && i < amount - 1)
                    {
                        float staggerDelay =
                            CalculateCinematicStagger(
                                amount
                            );

                        yield return new WaitForSeconds(
                            staggerDelay
                        );
                    }
                }
            }

            // Wait before the next group.
            float waveDelay =
                Random.Range(
                    cinematicMinWaveDelay,
                    cinematicMaxWaveDelay
                );

            yield return new WaitForSeconds(
                waveDelay
            );
        }
    }

    private float CalculateCinematicStagger(
        int amount
    )
    {
        // More impacts = faster succession.

        if (amount <= 1)
            return 0f;

        if (amount == 2)
            return Random.Range(
                0.15f,
                0.40f
            );

        if (amount == 3)
            return Random.Range(
                0.10f,
                0.30f
            );

        return Random.Range(
            0.05f,
            0.20f
        );
    }

    private void SpawnCinematicImpact()
    {
        if (
            cinematicEffectPrefab == null ||
            player == null
        )
        {
            return;
        }

        // -----------------------------------------------------
        // FIND RANDOM TARGET
        // -----------------------------------------------------

        Vector3 targetPosition =
            GetRandomCinematicTarget();

        // Find the actual ground underneath it.
        float referenceY =
            Mathf.Max(
                transform.position.y,
                player.position.y
            );

        float groundY =
            GetGroundY(
                targetPosition.x,
                targetPosition.z,
                referenceY
            );

        targetPosition.y =
            groundY +
            indicatorHeightOffset;

        // -----------------------------------------------------
        // RANDOM SIZE
        // -----------------------------------------------------

        float randomSize =
            Random.Range(
                cinematicMinSize,
                cinematicMaxSize
            );

        // -----------------------------------------------------
        // START POSITION
        // -----------------------------------------------------

        Vector3 startPosition =
            transform.position +
            Vector3.up *
            cinematicSpawnHeight;

        // Give each projectile a tiny random horizontal
        // offset from the boss so they don't all originate
        // from exactly the same point.
        startPosition +=
            new Vector3(
                Random.Range(-1.5f, 1.5f),
                0f,
                Random.Range(-1.5f, 1.5f)
            );

        // -----------------------------------------------------
        // CREATE EFFECT
        // -----------------------------------------------------

        GameObject effect =
            Instantiate(
                cinematicEffectPrefab,
                startPosition,
                Quaternion.identity
            );

        // Bigger randomized area = bigger explosion/effect.
        effect.transform.localScale =
            effect.transform.localScale *
            randomSize;

        // Start the actual falling motion.
        StartCoroutine(
            MoveCinematicEffect(
                effect,
                startPosition,
                targetPosition,
                randomSize
            )
        );
    }

    private Vector3 GetRandomCinematicTarget()
    {
        // If a specific map area has been assigned,
        // use that area.

        if (cinematicAreaCenter != null)
        {
            Vector3 center =
                cinematicAreaCenter.position;

            float halfX =
                cinematicAreaSize.x * 0.5f;

            float halfZ =
                cinematicAreaSize.y * 0.5f;

            return new Vector3(
                center.x +
                Random.Range(
                    -halfX,
                    halfX
                ),

                center.y,

                center.z +
                Random.Range(
                    -halfZ,
                    halfZ
                )
            );
        }

        // Otherwise, randomly place it around the player.
        float angle =
            Random.Range(
                0f,
                360f
            );

        float distance =
            Random.Range(
                cinematicMinDistance,
                cinematicMaxDistance
            );

        Vector3 offset =
            new Vector3(
                Mathf.Cos(
                    angle * Mathf.Deg2Rad
                ),
                0f,
                Mathf.Sin(
                    angle * Mathf.Deg2Rad
                )
            ) * distance;

        return new Vector3(
            player.position.x + offset.x,
            player.position.y,
            player.position.z + offset.z
        );
    }

    private IEnumerator MoveCinematicEffect(
        GameObject effect,
        Vector3 startPosition,
        Vector3 targetPosition,
        float size
    )
    {
        if (effect == null)
            yield break;

        float elapsed = 0f;

        while (
            elapsed <
            cinematicFallDuration
        )
        {
            if (effect == null)
                yield break;

            elapsed +=
                Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed /
                    cinematicFallDuration
                );

            // Smooth fall.
            float smoothProgress =
                progress *
                progress *
                (3f -
                 2f *
                 progress);

            effect.transform.position =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    smoothProgress
                );

            yield return null;
        }

        if (effect == null)
            yield break;

        effect.transform.position =
            targetPosition;

        // Give the larger impacts a slightly bigger
        // final scale as well.
        if (
            cinematicMaxSize >
            cinematicMinSize
        )
        {
            float normalizedSize =
                Mathf.InverseLerp(
                    cinematicMinSize,
                    cinematicMaxSize,
                    size
                );

            float explosionScale =
                Mathf.Lerp(
                    1f,
                    cinematicLargeExplosionMultiplier,
                    normalizedSize
                );

            effect.transform.localScale *=
                explosionScale;
        }

        // The effect is purely cinematic for now.
        // It does NOT damage the player.

        Destroy(
            effect,
            GetCinematicEffectLifetime(
                effect
            )
        );
    }

    private float GetCinematicEffectLifetime(
        GameObject effect
    )
    {
        // Let the prefab remain visible briefly after impact.
        // This is deliberately independent of the normal
        // attack system.
        ParticleSystem particleSystem =
            effect.GetComponentInChildren<
                ParticleSystem
            >();

        if (particleSystem != null)
        {
            float lifetime =
                particleSystem.main.duration +
                particleSystem.main.startLifetime.constantMax;

            return Mathf.Max(
                lifetime,
                0.5f
            );
        }

        // Fallback for ordinary GameObjects.
        return 2f;
    }

    // =========================================================
    // GROUND
    // =========================================================

    private float GetGroundY(
        float x,
        float z,
        float referenceY
    )
    {
        Vector3 origin =
            new Vector3(
                x,
                referenceY +
                groundRayStartHeight,
                z
            );

        RaycastHit[] hits =
            Physics.RaycastAll(
                origin,
                Vector3.down,
                groundRayStartHeight +
                GroundRayLength,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore
            );

        float closestDistance =
            float.MaxValue;

        float groundY =
            transform.position.y;

        foreach (RaycastHit hit in hits)
        {
            if (
                IsIgnoredForGround(
                    hit.collider
                )
            )
            {
                continue;
            }

            if (
                hit.distance <
                closestDistance
            )
            {
                closestDistance =
                    hit.distance;

                groundY =
                    hit.point.y;
            }
        }

        return groundY;
    }

    private bool IsIgnoredForGround(
        Collider other
    )
    {
        if (
            other.transform.IsChildOf(
                transform
            )
        )
        {
            return true;
        }

        if (
            other.GetComponentInParent<
                PlayerHealth
            >() != null
        )
        {
            return true;
        }

        if (
            other.GetComponentInParent<
                Enemy
            >() != null
        )
        {
            return true;
        }

        return false;
    }

    // =========================================================
    // INDICATOR
    // =========================================================

    private GameObject CreateIndicator(
        BossAttack attack,
        AttackArea area
    )
    {
        bool isCircle =
            attack.shape ==
            IndicatorShape.Circle;

        GameObject indicator =
            GameObject.CreatePrimitive(
                isCircle
                    ? PrimitiveType.Cylinder
                    : PrimitiveType.Cube
            );

        Destroy(
            indicator.GetComponent<Collider>()
        );

        indicator.name =
            "BossIndicator_" +
            attack.attackName;

        indicator.transform.SetPositionAndRotation(
            area.center,
            area.rotation
        );

        if (isCircle)
        {
            float diameter =
                area.radius * 2f;

            indicator.transform.localScale =
                new Vector3(
                    diameter,
                    IndicatorThickness,
                    diameter
                );
        }
        else
        {
            indicator.transform.localScale =
                new Vector3(
                    area.boxSize.x,
                    IndicatorThickness,
                    area.boxSize.y
                );
        }

        Renderer indicatorRenderer =
            indicator.GetComponent<Renderer>();

        indicatorRenderer.sharedMaterial =
            GetIndicatorMaterial();

        indicatorRenderer.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;

        indicatorRenderer.receiveShadows =
            false;

        return indicator;
    }

    private Material GetIndicatorMaterial()
    {
        if (indicatorMaterial != null)
            return indicatorMaterial;

        if (runtimeMaterial == null)
        {
            runtimeMaterial =
                new Material(
                    Shader.Find(
                        "Sprites/Default"
                    )
                );

            runtimeMaterial.color =
                indicatorColor;
        }

        return runtimeMaterial;
    }

    private void SpawnTimerEndEffect(
        BossAttack attack,
        AttackArea area
    )
    {
        if (
            attack.timerEndEffect ==
            null
        )
        {
            return;
        }

        GameObject effect =
            Instantiate(
                attack.timerEndEffect,
                area.center,
                area.rotation
            );

        if (
            attack.effectLifetime >
            0f
        )
        {
            Destroy(
                effect,
                attack.effectLifetime
            );
        }
    }

    private bool IsPlayerInside(
        BossAttack attack,
        AttackArea area
    )
    {
        Vector3 offset =
            player.position -
            area.center;

        offset.y = 0f;

        if (
            attack.shape ==
            IndicatorShape.Circle
        )
        {
            return
                offset.magnitude <=
                area.radius;
        }

        Vector3 local =
            Quaternion.Inverse(
                area.rotation
            ) * offset;

        return
            Mathf.Abs(local.x) <=
                area.boxSize.x * 0.5f
            &&
            Mathf.Abs(local.z) <=
                area.boxSize.y * 0.5f;
    }

    private void DamagePlayer(
        int damage
    )
    {
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(
                damage
            );
        }
    }

    private void OnDestroy()
    {
        if (
            currentIndicator != null
        )
        {
            Destroy(
                currentIndicator
            );
        }
    }
}