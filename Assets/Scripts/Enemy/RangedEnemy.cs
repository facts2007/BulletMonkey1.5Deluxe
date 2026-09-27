using UnityEngine;
using UnityEngine.AI;
using System.Collections;

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
        if(trickVisual==null){FireProjectile(true);trickShotActive=false;yield break;}
        visualStart=trickVisual.localPosition;visualRotation=trickVisual.localRotation;
        if(agent!=null && agent.isOnNavMesh)agent.ResetPath();
        float duration=Mathf.Max(.1f,noscopeSeconds);
        bool fired=false;
        for(float t=0;t<duration;t+=Time.deltaTime)
        {
            if(enemyHealth.IsDead)break;
            float f=t/duration;
            trickVisual.localPosition=visualStart+Vector3.up*Mathf.Sin(f*Mathf.PI)*noscopeJumpHeight;
            trickVisual.localRotation=visualRotation*Quaternion.Euler(0,360*f,0);
            if(!fired && f>=.65f){FireProjectile(true);fired=true;}
            yield return null;
        }
        ResetTrickVisual();
    }
    private void ResetTrickVisual()
    {
        if(trickVisual!=null && trickShotActive){trickVisual.localPosition=visualStart;trickVisual.localRotation=visualRotation;}
        trickShotActive=false;
    }
    private void OnDisable(){StopAllCoroutines();ResetTrickVisual();}
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
        Vector3 direction = (targetPoint - firePoint.position).normalized;

        Vector3 shotPosition=firePoint.position+(noscope?Vector3.up*noscopeJumpHeight*.8f:Vector3.zero);
        direction=(targetPoint-shotPosition).normalized;
        GameObject projectileObject = Instantiate(projectilePrefab, shotPosition, Quaternion.LookRotation(direction));
        Projectile projectile = projectileObject.GetComponent<Projectile>();
        if (projectile == null)
        {
            projectile = projectileObject.AddComponent<Projectile>();
        }

        projectile.damage = noscope ? Mathf.RoundToInt(projectileDamage*noscopeDamageMultiplier) : projectileDamage;
        projectile.direction = direction;
        projectile.isNoscope = noscope;
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
