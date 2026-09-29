using UnityEngine;

public class ModelSceneLoader : MonoBehaviour
{
    [Header("Target Scene")]
    public string targetSceneName="VictoryScene";
    private void OnTriggerEnter(Collider other){Touch(other);}
    private void OnTriggerStay(Collider other){Touch(other);}
    private void Touch(Collider other){var player=other.GetComponentInParent<PlayerHealth>();if(player!=null)player.Escape(targetSceneName);}
    // Water and boat triggers may overlap in the same physics step. A valid exit wins.
    public static bool TryExitAt(PlayerHealth player)
    {
        if(player==null)return false;if(player.HasEscaped)return true;
        var body=player.GetComponent<Collider>();if(body==null)return false;
        foreach(var exit in FindObjectsByType<ModelSceneLoader>(FindObjectsSortMode.None))
            foreach(var c in exit.GetComponentsInChildren<Collider>())
                if(c.enabled && c.gameObject.activeInHierarchy && c.isTrigger && c.bounds.Intersects(body.bounds))
                    return player.Escape(exit.targetSceneName);
        return false;
    }
}
