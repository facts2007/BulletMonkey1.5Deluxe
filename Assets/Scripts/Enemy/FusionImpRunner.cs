using UnityEngine;
using UnityEngine.AI;

public class FusionImpRunner : MonoBehaviour
{
    public bool Arrived {get;private set;}
    private NavMeshAgent agent;
    private Vector3 destination,originalScale;
    private float deadline,shrink;
    public void Begin(Vector3 target,float speed)
    {
        destination=target;originalScale=transform.localScale;deadline=Time.time+25;
        agent=gameObject.AddComponent<NavMeshAgent>();agent.speed=speed;agent.acceleration=30;agent.angularSpeed=720;agent.radius=.2f;agent.height=1;agent.stoppingDistance=.7f;agent.obstacleAvoidanceType=ObstacleAvoidanceType.NoObstacleAvoidance;
        if(agent.isOnNavMesh)agent.SetDestination(target);
        var animator=GetComponentInChildren<Animator>();if(animator!=null){animator.applyRootMotion=false;animator.Play("Walk");animator.speed=2;}
    }
    private void Update()
    {
        if(Arrived)return;
        bool reached=(transform.position-destination).sqrMagnitude<2.25f || Time.time>deadline;
        if(!reached && shrink<=0)return;
        if(agent.enabled)agent.enabled=false;
        shrink+=Time.deltaTime*3;transform.localScale=originalScale*Mathf.Max(0,1-shrink);
        if(shrink>=1){Arrived=true;SpawnFog.Poof(transform.position,.7f);gameObject.SetActive(false);}
    }
}
