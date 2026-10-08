using System.Collections;
using UnityEngine;

public enum BossAttackEffect { Damage, SlamImps, ScatterImps, Push }

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
    public BossAttackEffect effect;
    public float pushSpeed=14;
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

    [Header("Imp attacks")]
    [Header("Custom VFX — empty slots use the existing fog")]
    public GameObject slamImpactVfx, impExplosionVfx, impLandingVfx, impTrailVfx;
    public float customVfxLifetime=5;
    public GameObject walkingImpPrefab;
    public GameObject thrownImpVisualPrefab;
    public int slamImpCount=3;
    public int slamImpAmmo=30;
    public int scatterCount=6;
    public float scatterRadius=7;
    public float impExplosionRadius=3;
    public int impExplosionDamage=20;
    [Header("Giant homing rocket imp")]
    public bool useHomingImp;
    public HomingBulletSettings homingImp = new HomingBulletSettings { sizeMultiplier = 2.5f };
    public Transform homingImpLaunchPoint;
    public float homingImpLaunchHeight = 8;
    public float homingImpSpin = 720;
    public GameObject homingThrusterVfx;
    public float homingThrusterScale = .35f;
    private float homingImpTimer;
    private readonly System.Collections.Generic.List<GameObject> transientObjects=new System.Collections.Generic.List<GameObject>();
    private EnemyHealth health;
    private bool stopped;
    private bool CanFight => isActive && !stopped && (health==null || !health.IsDead) && !BossFusionEncounter.IsCutsceneActive && !PlayerCheer.IsCutsceneActive;
    private Transform player;
    private PlayerHealth playerHealth;
    private Material runtimeMaterial;
    private GameObject currentIndicator;
    private float attackTimer;
    private int nextAttackIndex;
    private bool isAttacking;

    private void Start()
    {
        health=GetComponent<EnemyHealth>();
        StartCoroutine(CinematicAttackLoop());
    }

    private void Update()
    {
        if(health!=null && health.IsDead){Shutdown();return;}
        if (!CanFight || Time.timeScale <= 0 || GameSceneFlow.IsLoading) return;

        if (player == null)
        {
            FindPlayer();
            return;
        }

        if (!player.gameObject.activeInHierarchy || playerHealth == null || playerHealth.currentHealth <= 0 || playerHealth.HasEscaped) return;

        if (useHomingImp) homingImpTimer += Time.deltaTime;

        if(!isAttacking)FaceTarget();

        if (isAttacking) return;
        if (useHomingImp && homingImpTimer >= Mathf.Max(1, homingImp.interval))
        {
            homingImpTimer = 0;
            attackTimer = 0;
            FireHomingImp();
            return;
        }
        if (attacks.Length == 0) return;

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

    private void FireHomingImp()
    {
        var prefab = homingImp.visualPrefab != null ? homingImp.visualPrefab : thrownImpVisualPrefab;
        if (prefab == null || playerHealth == null || !CanFight) return;
        Vector3 launch = homingImpLaunchPoint != null ? homingImpLaunchPoint.position :
            transform.position + Vector3.up * homingImpLaunchHeight + transform.forward * 3;
        var staging = new GameObject("Rocket imp staging"); staging.SetActive(false);
        var carrier = new GameObject("Final boss homing rocket imp");
        carrier.transform.SetParent(staging.transform, false);
        carrier.transform.position = launch;
        var rotor = new GameObject("Spinning imp"); rotor.transform.SetParent(carrier.transform, false);
        var visual = Instantiate(prefab, rotor.transform, false);
        visual.transform.localScale *= Mathf.Max(1, homingImp.sizeMultiplier);
        foreach (var animator in visual.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
        foreach (var collider in visual.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        var renderers = visual.GetComponentsInChildren<Renderer>(true);
        var bounds = new Bounds(launch, Vector3.one);
        if (renderers.Length > 0)
        {
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            visual.transform.position += launch - bounds.center;
        }
        float width = Mathf.Max(bounds.size.x, bounds.size.z);
        var body = carrier.AddComponent<BoxCollider>();
        body.size = new Vector3(width, bounds.size.y, width);
        if (homingThrusterVfx != null)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                var jet = Instantiate(homingThrusterVfx, carrier.transform, false);
                jet.name = side < 0 ? "Left downward thruster" : "Right downward thruster";
                jet.transform.localPosition = new Vector3(side * width * .275f, -bounds.extents.y - .1f, 0);
                jet.transform.localRotation = Quaternion.Euler(180, 0, 0);
                jet.transform.localScale *= homingThrusterScale;
            }
        }
        Vector3 aim = player.position + Vector3.up * Mathf.Max(1.1f, bounds.extents.y + .5f) - launch;
        if (aim.sqrMagnitude > .001f) carrier.transform.rotation = Quaternion.LookRotation(aim);
        var drops = walkingImpPrefab != null ? walkingImpPrefab.GetComponent<Enemy>() : null;
        var missile = carrier.AddComponent<Projectile>();
        missile.ConfigureHoming(health, playerHealth, homingImp, null, drops != null ? drops.ammoDropPrefab : null, impExplosionVfx);
        missile.SetSpinningVisual(rotor.transform, homingImpSpin);
        carrier.transform.SetParent(null, true); carrier.SetActive(true); Destroy(staging);
        transientObjects.RemoveAll(item => item == null);
        transientObjects.Add(carrier);
        if (GameAudio.Instance != null) GameAudio.Instance.PlayShot(false);
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

        if(!CanFight){isAttacking=false;yield break;}
        if(attack.effect==BossAttackEffect.ScatterImps)
        {
            for(int i=0;i<scatterCount;i++)
            {
                Vector2 offset=Random.insideUnitCircle*scatterRadius;
                Vector3 target=area.center+new Vector3(offset.x,0,offset.y);target.y=GetGroundY(target.x,target.z,target.y)+indicatorHeightOffset;
                StartCoroutine(ImpImpact(target,transform.position+Vector3.up*cinematicProjectileLaunchHeight,1.5f,impExplosionRadius,impExplosionDamage,false));
            }
        }
        if(attack.effect==BossAttackEffect.SlamImps)
        {
            PlayVfx(slamImpactVfx,area.center,4);
            for(int i=0;i<slamImpCount;i++)
            {
                Vector3 offset=Quaternion.Euler(0,i*360f/Mathf.Max(1,slamImpCount),0)*Vector3.forward*3;
                Vector3 target=area.center+offset;UnityEngine.AI.NavMeshHit nav;
                if(UnityEngine.AI.NavMesh.SamplePosition(target,out nav,5,UnityEngine.AI.NavMesh.AllAreas))target=nav.position;
                target.y=GetGroundY(target.x,target.z,target.y);
                StartCoroutine(ImpImpact(target,area.center+Vector3.up,.75f,0,0,true));
            }
        }
        SpawnTimerEndEffect(attack.timerEndEffect, attack.effectLifetime, area.center, area.rotation);

        if (attack.effect!=BossAttackEffect.ScatterImps && player != null && player.gameObject.activeInHierarchy && IsPlayerInside(attack.shape, area))
        {
            DamagePlayer(attack.damage);
            if(attack.effect==BossAttackEffect.Push){var movement=player.GetComponent<PlayerMovement>();if(movement!=null)movement.ApplyPush(area.rotation*Vector3.forward,attack.pushSpeed);}
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
            if (!cinematicEnabled || !CanFight || player == null || !player.gameObject.activeInHierarchy)
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
        Vector3 target=GetRandomCinematicPosition();
        target.y=GetGroundY(target.x,target.z,Mathf.Max(transform.position.y,player.position.y))+indicatorHeightOffset;
        float timer=Random.Range(cinematicMinTimer,cinematicMaxTimer);
        float fraction=Mathf.InverseLerp(cinematicMinTimer,cinematicMaxTimer,timer);
        yield return ImpImpact(target,transform.position+Vector3.up*cinematicProjectileLaunchHeight,timer,Mathf.Lerp(cinematicRadiusMin,cinematicRadiusMax,fraction),cinematicDamage,false);
    }
    private IEnumerator ImpImpact(Vector3 target,Vector3 launch,float duration,float radius,int damage,bool walking)
    {
        var area=new AttackArea{center=target,rotation=Quaternion.identity,radius=radius};
        GameObject indicator=walking?null:CreateIndicator(IndicatorShape.Circle,area,cinematicIndicatorColor);
        if(indicator!=null)transientObjects.Add(indicator);
        var visual=thrownImpVisualPrefab!=null?thrownImpVisualPrefab:cinematicProjectilePrefab;
        GameObject projectile=null;
        if(visual!=null)
        {
            var staging=new GameObject("Imp projectile staging");staging.SetActive(false);
            projectile=Instantiate(visual,launch,Quaternion.identity,staging.transform);projectile.name=walking?"Slam supply imp airborne":"Explosive imp airborne";
            foreach(var behaviour in projectile.GetComponentsInChildren<MonoBehaviour>())behaviour.enabled=false;
            foreach(var agent in projectile.GetComponentsInChildren<UnityEngine.AI.NavMeshAgent>())agent.enabled=false;
            foreach(var collider in projectile.GetComponentsInChildren<Collider>())collider.enabled=false;
            foreach(var rb in projectile.GetComponentsInChildren<Rigidbody>()){rb.isKinematic=true;rb.useGravity=false;}
            if(impTrailVfx!=null){var trail=Instantiate(impTrailVfx,projectile.transform);trail.transform.localPosition=Vector3.zero;}
            projectile.transform.SetParent(null,true);projectile.SetActive(true);Destroy(staging);transientObjects.Add(projectile);
            yield return MoveProjectileArc(projectile,launch,target,duration,walking?3:cinematicProjectileArcHeight);
        }
        else yield return new WaitForSeconds(duration);
        if(CanFight)
        {
            PlayVfx(walking?impLandingVfx:impExplosionVfx,target,walking?1:2);
            if(walking)SpawnWalkingImp(target,slamImpAmmo);
            else
            {
                SpawnTimerEndEffect(cinematicTimerEndEffect,cinematicEffectLifetime,target,Quaternion.identity);
                if(player!=null&&IsPlayerInside(IndicatorShape.Circle,area))DamagePlayer(damage);
            }
        }
        if(projectile!=null){transientObjects.Remove(projectile);Destroy(projectile);}
        if(indicator!=null){transientObjects.Remove(indicator);Destroy(indicator);}
    }
    public GameObject SpawnWalkingImp(Vector3 position,int ammo)
    {
        if(walkingImpPrefab==null)return null;
        var staging=new GameObject("Supply imp staging");staging.SetActive(false);
        var imp=Instantiate(walkingImpPrefab,position,Quaternion.identity,staging.transform);
        var nav=imp.GetComponent<UnityEngine.AI.NavMeshAgent>();if(nav!=null)nav.enabled=false;
        var enemy=imp.GetComponent<Enemy>();if(enemy!=null){enemy.ammoDropChance=1;enemy.minAmmoAmount=enemy.maxAmmoAmount=ammo;}
        var melee=imp.GetComponent<MeleeEnemy>();if(melee!=null)melee.chaseRange=200;
        imp.name="Boss supply imp ("+ammo+" ammo)";imp.transform.SetParent(null,true);imp.SetActive(true);Destroy(staging);return imp;
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
            projectile.transform.Rotate(new Vector3(240,330,170)*Time.deltaTime,Space.Self);

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
        if (other.GetComponentInParent<Enemy>() != null || other.GetComponentInParent<EnemyHealth>()!=null) return true;

        return false;
    }

    private GameObject CreateIndicator(IndicatorShape shape, AttackArea area, Color color)
    {
        return GroundAttackIndicator.Create(shape==IndicatorShape.Circle,area.center,area.rotation,area.radius,area.boxSize,GetIndicatorMaterial(color),transform,indicatorHeightOffset);
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
        if(Mathf.Abs(offset.y)>4)return false;
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

    private void PlayVfx(GameObject prefab,Vector3 position,float fallbackSize)
    {
        if(prefab==null){SpawnFog.Poof(position,fallbackSize);return;}
        var effect=Instantiate(prefab,position,Quaternion.identity);Destroy(effect,Mathf.Max(.1f,customVfxLifetime));
    }
    private void Shutdown()
    {
        if(stopped)return;stopped=true;StopAllCoroutines();
        if(currentIndicator!=null)Destroy(currentIndicator);
        foreach(var item in transientObjects)if(item!=null)Destroy(item);transientObjects.Clear();
    }
    private void OnDisable(){Shutdown();}
    private void OnDestroy(){Shutdown();if(runtimeMaterial!=null)Destroy(runtimeMaterial);}
}
