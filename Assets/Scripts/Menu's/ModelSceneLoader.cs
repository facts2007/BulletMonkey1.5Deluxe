using UnityEngine;
using UnityEngine.SceneManagement;

public class ModelSceneLoader : MonoBehaviour
{
    [Header("Target Scene")]
    public string targetSceneName = "VictoryScene";

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerHealth>() != null)
        {
            GameSceneFlow.Load(targetSceneName);
        }
    }
}
