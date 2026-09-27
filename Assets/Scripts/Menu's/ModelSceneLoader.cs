using UnityEngine;
using UnityEngine.SceneManagement;

public class ModelSceneLoader : MonoBehaviour
{
    [Header("Target Scene")]
    public string targetSceneName = "NextScene";

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            SceneManager.LoadScene(targetSceneName);
        }
    }
}