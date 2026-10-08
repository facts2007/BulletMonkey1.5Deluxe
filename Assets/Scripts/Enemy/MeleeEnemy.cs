using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(EnemyHealth))]
public class MeleeEnemy : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public float chaseRange = 6f;
    public float stopDistance = 1.2f;

    [Header("Stomp dodge")]
    [Range(0, 1)] public float stompDodgeChance = .1f;
    [Min(.1f)] public float stompDodgeDistance = 2.5f;
    [Min(.1f)] public float stompDodgeSeconds = .4f;
    [Min(0)] public float stompDodgeHop = .3f;
    public LayerMask dodgeGroundMask = ~0;
    [Tooltip("Optional mist prefab. Leave empty for the small pooled fog puff.")]
    public GameObject stompDodgeVfx;
    public bool IsDodging { get; private set; }

    [Header("Target")]
    public Transform player;

    private EnemyHealth enemyHealth;
    private NavMeshAgent agent;
    private Collider body;
    private bool restoreAgent;
    private Transform dodgeVisual;
    private Quaternion visualRotation;

    private void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        agent = GetComponent<NavMeshAgent>();
        body = GetComponent<Collider>();
    }

    private void Start()
    {
        enemyHealth = GetComponent<EnemyHealth>();
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
            Debug.LogError("Geen speler gevonden");
        }
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    private void Update()
    {
        if (player == null || IsDodging || Time.timeScale <= 0f || GameSceneFlow.IsLoading) return;
        if (enemyHealth != null && enemyHealth.currentHealth <= 0) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= chaseRange && distance > stopDistance)
        {
            Chase();
        }
    }

    private void Chase()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;
        direction.Normalize();

        transform.position += direction * moveSpeed * Time.deltaTime;

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    public bool TryDodgeStomp()
    {
        if (!isActiveAndEnabled || IsDodging || enemyHealth == null || enemyHealth.IsDead ||
            Time.timeScale <= 0f || Random.value >= stompDodgeChance) return false;
        Vector3[] route;
        if (!FindDodgeRoute(out route)) return false;
        IsDodging = true;
        restoreAgent = agent != null && agent.enabled;
        if (restoreAgent) agent.enabled = false;
        var enemy = GetComponent<Enemy>();
        dodgeVisual = enemy != null ? enemy.visualRoot : null;
        if (dodgeVisual != null) visualRotation = dodgeVisual.localRotation;
        Vector3 direction = route[route.Length - 1] - route[0]; direction.y = 0;
        if (direction.sqrMagnitude > .01f) transform.rotation = Quaternion.LookRotation(direction);
        StartCoroutine(Dodge(route));
        return true;
    }

    private bool FindDodgeRoute(out Vector3[] route)
    {
        route = null;
        Vector3 start = transform.position;
        float clearance = body != null ? start.y - body.bounds.min.y + .04f : .55f;
        float angle = Random.Range(0f, 360f);
        bool useNav = agent != null && agent.enabled && agent.isOnNavMesh;
        NavMeshHit navStart;
        NavMesh.SamplePosition(start, out navStart, 2f, NavMesh.AllAreas);
        // Try alternate directions when the first points into a wall or off the island.
        for (int attempt = 0; attempt < 8; attempt++)
        {
            Vector3 direction = Quaternion.Euler(0, angle + attempt * 45f, 0) * Vector3.forward;
            Vector3 end = start + direction * stompDodgeDistance;
            if (useNav)
            {
                NavMeshHit landing, edge;
                if (!NavMesh.SamplePosition(end, out landing, .7f, agent.areaMask) ||
                    NavMesh.Raycast(navStart.position, landing.position, out edge, agent.areaMask)) continue;
                end.x = landing.position.x; end.z = landing.position.z;
            }
            var points = new Vector3[13]; points[0] = start;
            bool safe = true;
            for (int step = 1; step < points.Length; step++)
            {
                Vector3 point = Vector3.Lerp(start, end, step / 12f);
                Vector3 previous = points[step - 1];
                point.y = previous.y;
                if (!TryGround(point, clearance, out point)) { safe = false; break; }
                if (Mathf.Abs(point.y - previous.y) > .6f || !ClearDodgeStep(previous, point)) { safe = false; break; }
                points[step] = point;
            }
            if (safe && (points[12] - start).sqrMagnitude > 1f) { route = points; return true; }
        }
        return false;
    }

    private bool TryGround(Vector3 point, float clearance, out Vector3 grounded)
    {
        grounded = point;
        float nearest = float.PositiveInfinity;
        foreach (var hit in Physics.RaycastAll(point + Vector3.up, Vector3.down, 3f, dodgeGroundMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.normal.y < .5f || hit.collider.GetComponentInParent<Enemy>() != null ||
                hit.collider.GetComponentInParent<PlayerHealth>() != null || hit.distance >= nearest ||
                (hit.rigidbody != null && !hit.rigidbody.isKinematic)) continue;
            grounded.y = hit.point.y + clearance; nearest = hit.distance;
        }
        return !float.IsPositiveInfinity(nearest);
    }

    private bool ClearDodgeStep(Vector3 from, Vector3 to)
    {
        if (body == null) return false;
        Vector3 delta = to - from;
        if (delta.sqrMagnitude < .00001f) return true;
        Bounds bounds = body.bounds;
        float radius = Mathf.Max(.1f, Mathf.Min(bounds.extents.x, bounds.extents.z) * .9f);
        Vector3 center = from + bounds.center - transform.position;
        float half = Mathf.Max(0, bounds.extents.y - radius);
        foreach (var hit in Physics.CapsuleCastAll(center + Vector3.up * half, center - Vector3.up * half,
            radius, delta.normalized, delta.magnitude, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.GetComponentInParent<MeleeEnemy>() == this ||
                hit.collider.GetComponentInParent<PlayerHealth>() != null || hit.normal.y > .5f) continue;
            return false;
        }
        return true;
    }

    private IEnumerator Dodge(Vector3[] route)
    {
        float seconds = Mathf.Max(.1f, stompDodgeSeconds), nextMist = 0;
        float clearance = body != null ? transform.position.y - body.bounds.min.y + .04f : .55f;
        for (float t = 0; t < seconds; t += Time.deltaTime)
        {
            if (enemyHealth == null || enemyHealth.IsDead) break;
            if (Time.timeScale <= 0f || GameSceneFlow.IsLoading) { yield return null; continue; }
            float f = Mathf.Clamp01(t / seconds), along = Mathf.SmoothStep(0, 1, f) * (route.Length - 1);
            int index = Mathf.Min(Mathf.FloorToInt(along), route.Length - 2);
            Vector3 point = Vector3.Lerp(route[index], route[index + 1], along - index);
            point.y += Mathf.Sin(f * Mathf.PI) * stompDodgeHop;
            Vector3 grounded;
            if (!TryGround(point, clearance, out grounded)) break;
            point.y = Mathf.Max(point.y, grounded.y);
            if (!ClearDodgeStep(transform.position, point)) break;
            transform.position = point;
            if (dodgeVisual != null) dodgeVisual.localRotation = visualRotation * Quaternion.Euler(0, 0, -18 * Mathf.Sin(f * Mathf.PI));
            if (t >= nextMist) { EmitDodgeMist(); nextMist = t + .13f; }
            yield return null;
        }
        // Settle from the small hop without moving through a newly arrived obstacle.
        if (enemyHealth != null && !enemyHealth.IsDead)
        {
            Vector3 settled;
            if (TryGround(transform.position, clearance, out settled) && ClearDodgeStep(transform.position, settled))
                transform.position = settled;
            EmitDodgeMist();
        }
        FinishDodge();
    }

    private void EmitDodgeMist()
    {
        if (stompDodgeVfx == null) SpawnFog.Poof(transform.position, .45f);
        else Destroy(Instantiate(stompDodgeVfx, transform.position, Quaternion.identity), 3f);
    }

    private void FinishDodge()
    {
        if (dodgeVisual != null) dodgeVisual.localRotation = visualRotation;
        IsDodging = false;
        if (restoreAgent && agent != null && enemyHealth != null && !enemyHealth.IsDead)
        {
            agent.enabled = true;
            if (gameObject.activeInHierarchy && agent.isOnNavMesh) agent.Warp(transform.position);
        }
        restoreAgent = false;
    }

    private void OnDisable() { StopAllCoroutines(); FinishDodge(); }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
    }
}
