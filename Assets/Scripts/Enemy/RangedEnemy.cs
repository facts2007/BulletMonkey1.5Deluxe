using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using UnityEngine.VFX;

[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(NavMeshAgent))]
public class RangedEnemy : MonoBehaviour
{
    [Header("Combat")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float fireRate = 2f;
    public int projectileDamage = 10;
    public float aimHeightOffset = 0f;
    [Header("Jumping 360 noscope")]
    [Range(0,1)] public float noscopeChance = .1f;
    [Min(1)] public float noscopeDamageMultiplier = 2;
    public float noscopeJumpHeight = 1.5f;
    public float noscopeSeconds = .8f;
    [Range(0,1)] public float noscopeChainChance = .33f;
    private bool trickShotActive;
    private Transform trickVisual;
    private Vector3 visualStart;
    private Quaternion visualRotation;

    [Header("Range")]
    public float detectionRange = 12f;
    public float attackRange = 8f;
    public float retreatRange = 4f;

    [Header("Target")]
    public Transform player;

    [Header("Animation")]
    public CharacterAnimationDriver characterAnimation;

    [Header("Muzzle flash")]
    [Tooltip("Uses the player's flash when empty. Plays only when a projectile is fired.")]
    public GameObject muzzleFlarePrefab;
    private GameObject muzzleFlareInstance;
    private VisualEffect[] muzzleVisualEffects;
    private ParticleSystem[] muzzleParticles;
    private Quaternion muzzleFlareRotation;

    [Header("Miniboss giant homing bullet")]
    public bool useHomingBullet;
    public HomingBulletSettings homingBullet = new HomingBulletSettings();
    public WaveArea WaveOwner { get; set; }
    private float homingTimer;

    private float fireTimer, nextPathUpdate;
    private Vector3 lastDestination;
    private EnemyHealth enemyHealth;
    private NavMeshAgent agent;
    private void Awake(){enemyHealth=GetComponent<EnemyHealth>();agent=GetComponent<NavMeshAgent>();}

    private void Start()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        
        GameObject gevondenSpeler = GameObject.Find("Speler");

        
        if (gevondenSpeler == null)
        {
            gevondenSpeler = GameObject.FindGameObjectWithTag("Player");
        }

       
        if (gevondenSpeler != null)
        {
            player = gevondenSpeler.transform;
        }
        else
        {
            Debug.LogError("Oeps! De speler kon met geen mogelijkheid worden gevonden in de scene. Controleer de naam of tag van je Player object!");
        }
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    private void Update()
    {
        if (Time.timeScale <= 0f || agent == null || !agent.isOnNavMesh) return;
        if (player == null) return;
        if (enemyHealth != null && enemyHealth.currentHealth <= 0) return;
        if (trickShotActive) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > detectionRange)
        {
            if(agent.hasPath)agent.ResetPath();
            return;
        }

        FaceTarget();

        if (useHomingBullet)
        {
            homingTimer += Time.deltaTime;
            if (homingTimer >= Mathf.Max(1, homingBullet.interval))
            {
                FireHomingBullet(); homingTimer = 0; fireTimer = 0;
                return;
            }
        }

        if (distance < retreatRange)
        {
            Retreat();
            HandleFiring();
        }
        else if (distance > attackRange)
        {
            ChasePlayer();
        }
        else
        {
            if(agent.hasPath)agent.ResetPath();
            HandleFiring();
        }
    }

    private void HandleFiring()
    {
        fireTimer += Time.deltaTime;
        if (fireTimer >= fireRate)
        {
            if (Random.value < noscopeChance) StartCoroutine(JumpingNoscope());
            else Shoot();
            fireTimer = 0f;
        }
    }

    private void ChasePlayer()
    {
        UpdateDestination(player.position);
    }

    private void UpdateDestination(Vector3 target)
    {
        if(Time.time<nextPathUpdate)return;
        nextPathUpdate=Time.time+.25f;
        if(!agent.hasPath || (lastDestination-target).sqrMagnitude>.25f){lastDestination=target;agent.SetDestination(target);}
    }
    private void FaceTarget()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    private void Retreat()
    {
        Vector3 directionAwayFromPlayer = (transform.position - player.position).normalized;
        Vector3 retreatTarget = transform.position + directionAwayFromPlayer * retreatRange;

        NavMeshHit navHit;
        if (NavMesh.SamplePosition(retreatTarget, out navHit, retreatRange, NavMesh.AllAreas))
        {
            UpdateDestination(navHit.position);
        }
    }

    private void Shoot()
    {
        FireProjectile(false);
    }
    private IEnumerator JumpingNoscope()
    {
        trickShotActive=true;
        trickVisual=characterAnimation!=null && characterAnimation.animator!=null ? characterAnimation.animator.transform : null;
        if(trickVisual!=null){visualStart=trickVisual.localPosition;visualRotation=trickVisual.localRotation;}
        if(agent!=null && agent.isOnNavMesh)agent.ResetPath();
        double duration=System.Math.Max(.1,noscopeSeconds);
        bool chain;
        do
        {
            while(Time.timeScale<=0f)yield return null;
            if(enemyHealth.IsDead || player==null || player.GetComponent<PlayerHealth>()?.currentHealth<=0)break;
            FaceTarget();
            bool fired=false;
            for(double t=0;t<duration;t+=Time.deltaTime)
            {
                if(enemyHealth.IsDead)break;
                float f=(float)(t/duration);
                if(trickVisual!=null)
                {
                    trickVisual.localPosition=visualStart+Vector3.up*Mathf.Sin(f*Mathf.PI)*noscopeJumpHeight;
                    trickVisual.localRotation=visualRotation*Quaternion.Euler(0,360*f,0);
                }
                if(!fired && f>=.65f){FireProjectile(true);fired=true;}
                yield return null;
            }
            // Even a spin shorter than one frame must fire once, never skip its shot.
            if(!enemyHealth.IsDead && !fired)FireProjectile(true);
            if(trickVisual!=null){trickVisual.localPosition=visualStart;trickVisual.localRotation=visualRotation;}
            chain=!enemyHealth.IsDead && Random.value<noscopeChainChance;
            duration*=.5;
            // No chain-count cap; yield between spins even at extreme speeds.
            yield return null;
        }while(chain);
        ResetTrickVisual();
    }
    private void ResetTrickVisual()
    {
        if(trickVisual!=null && trickShotActive){trickVisual.localPosition=visualStart;trickVisual.localRotation=visualRotation;}
        trickShotActive=false;
    }
    private void OnDisable()
    {
        StopAllCoroutines();ResetTrickVisual();
        if (muzzleVisualEffects != null) foreach (var effect in muzzleVisualEffects) if (effect != null) effect.Stop();
        if (muzzleParticles != null) foreach (var particles in muzzleParticles)
            if (particles != null) particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
    private void FireProjectile(bool noscope)
    {
        if (projectilePrefab == null || firePoint == null || player == null) return;

        if (characterAnimation != null) characterAnimation.NotifyShot();
        if (GameAudio.Instance != null)
        {
            if(noscope)GameAudio.Instance.PlayEffect(GameAudio.Instance.enemyNoscope);
            else GameAudio.Instance.PlayShot(false);
        }

        Vector3 targetPoint = player.position + Vector3.up * aimHeightOffset;
        // The muzzle is already parented to the jumping/spinning visual.
        Vector3 shotPosition = firePoint.position;
        Vector3 direction = (targetPoint - shotPosition).normalized;
        Quaternion shotRotation = Quaternion.LookRotation(direction);
        PlayMuzzleFlare(shotRotation);
        GameObject projectileObject = Instantiate(projectilePrefab, shotPosition, shotRotation);
        Projectile projectile = projectileObject.GetComponent<Projectile>();
        if (projectile == null)
        {
            projectile = projectileObject.AddComponent<Projectile>();
        }

        projectile.damage = noscope ? Mathf.RoundToInt(projectileDamage*noscopeDamageMultiplier) : projectileDamage;
        projectile.direction = direction;
        projectile.isNoscope = noscope;
    }

    private void PlayMuzzleFlare(Quaternion shotRotation)
    {
        if (muzzleFlarePrefab == null && Gun.Instance != null) muzzleFlarePrefab = Gun.Instance.muzzleFlarePrefab;
        if (muzzleFlarePrefab == null) return;
        bool created = false;
        if (muzzleFlareInstance == null)
        {
            muzzleFlareInstance = Instantiate(muzzleFlarePrefab, firePoint);
            muzzleFlareInstance.name = "Ranged muzzle flare";
            muzzleFlareInstance.transform.localPosition = Vector3.zero;
            muzzleVisualEffects = muzzleFlareInstance.GetComponentsInChildren<VisualEffect>(true);
            muzzleParticles = muzzleFlareInstance.GetComponentsInChildren<ParticleSystem>(true);
            created = true;
        }
        muzzleFlareRotation = shotRotation * muzzleFlarePrefab.transform.localRotation;
        muzzleFlareInstance.transform.SetPositionAndRotation(firePoint.position, muzzleFlareRotation);
        if (created) return;
        foreach (var effect in muzzleVisualEffects) if (effect != null) effect.Play();
        foreach (var particles in muzzleParticles)
        {
            if (particles == null) continue;
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.Play(true);
        }
    }

    private void FireHomingBullet()
    {
        if (firePoint == null || player == null || enemyHealth.IsDead) return;
        var prefab = homingBullet.visualPrefab != null ? homingBullet.visualPrefab : projectilePrefab;
        if (prefab == null) return;
        Quaternion rotation = Quaternion.LookRotation(player.position + Vector3.up * aimHeightOffset - firePoint.position);
        // Configure inactive so inherited Bullet.Start timers cannot destroy the giant shot early.
        var staging = new GameObject("Giant bullet staging"); staging.SetActive(false);
        var obj = Instantiate(prefab, firePoint.position, rotation, staging.transform);
        obj.name = "Miniboss giant homing bullet";
        obj.transform.localScale *= Mathf.Max(1, homingBullet.sizeMultiplier);
        var projectile = obj.GetComponent<Projectile>(); if (projectile == null) projectile = obj.AddComponent<Projectile>();
        projectile.ConfigureHoming(this);
        obj.transform.SetParent(null, true); obj.SetActive(true); Destroy(staging);
        if (characterAnimation != null) characterAnimation.NotifyShot();
        PlayMuzzleFlare(rotation);
        if (GameAudio.Instance != null) GameAudio.Instance.PlayShot(false);
    }

    private void LateUpdate()
    {
        if (muzzleFlareInstance != null) muzzleFlareInstance.transform.rotation = muzzleFlareRotation;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, retreatRange);
    }
}
