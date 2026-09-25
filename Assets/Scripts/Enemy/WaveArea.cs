using System.Collections;
using UnityEngine;
using TMPro;

public class WaveArea : MonoBehaviour
{
    [Header("Countdown UI")]
    public TextMeshProUGUI countdownText;
    private static TextMeshProUGUI sharedCountdownText;

    [Header("Wave Settings")]
    public int waveNumber = 1;
    public int baseEnemyCount = 5;
    public float scalingMultiplier = 1.5f;
    public bool isBossWave = false;

    [Header("Start Behavior")]
    public bool autoStart = false;
    public int countdownSeconds = 3;

    [Header("Spawning")]
    public Transform[] spawnPoints;
    public GameObject[] enemyPrefabs;
    public float spawnInterval = 0.5f;

    [Header("Status")]
    public bool waveStarted;
    public bool waveComplete;

    [Header("Path Blocker")]
    public GameObject pathBlocker;
    public WaveArea nextArea;

    private int enemiesRemaining;
    private int enemiesToSpawn;

    private void Awake()
    {
        if (countdownText != null)
        {
            sharedCountdownText = countdownText;
        }
    }

    private void Start()
    {
        if (autoStart)
        {
            StartCoroutine(WaitForPlayerThenStart());
        }
        else
        {
            SetTriggerEnabled(false);
        }
    }

    private void SetTriggerEnabled(bool isEnabled)
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.enabled = isEnabled;
        }
    }

    public void Unlock()
    {
        SetTriggerEnabled(true);
    }

    private IEnumerator WaitForPlayerThenStart()
    {
        while (GameObject.FindGameObjectWithTag("Player") == null)
        {
            yield return null;
        }

        StartWave();
    }

    private void OnEnable()
    {
        Enemy.OnEnemyDied += HandleEnemyDied;
    }

    private void OnDisable()
    {
        Enemy.OnEnemyDied -= HandleEnemyDied;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (waveStarted) return;
        if (other.GetComponentInParent<PlayerHealth>() == null) return;

        StartWave();
    }

    private void StartWave()
    {
        waveStarted = true;

        if (isBossWave)
        {
            StartBossWave();
            return;
        }

        StartCoroutine(CountdownThenSpawn());
    }

    private IEnumerator CountdownThenSpawn()
    {
        int secondsLeft = countdownSeconds;

        while (secondsLeft > 0)
        {
            if (sharedCountdownText != null)
            {
                sharedCountdownText.text = "Wave " + waveNumber + " starts in " + secondsLeft;
            }

            yield return new WaitForSeconds(1f);
            secondsLeft--;
        }

        if (sharedCountdownText != null)
        {
            sharedCountdownText.text = "";
        }

        int enemyCount = Mathf.RoundToInt(baseEnemyCount * Mathf.Pow(scalingMultiplier, waveNumber - 1));
        enemiesToSpawn = enemyCount;
        enemiesRemaining = enemyCount;

        StartCoroutine(SpawnWave());
    }

    private IEnumerator SpawnWave()
    {
        for (int i = 0; i < enemiesToSpawn; i++)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnEnemy()
    {
        if (spawnPoints.Length == 0 || enemyPrefabs.Length == 0) return;

        Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

        Instantiate(prefab, point.position, point.rotation);
    }

    private void HandleEnemyDied()
    {
        if (!waveStarted || waveComplete) return;

        enemiesRemaining--;
        if (enemiesRemaining <= 0)
        {
            CompleteWave();
        }
    }

    private void CompleteWave()
    {
        waveComplete = true;

        if (pathBlocker != null)
        {
            Destroy(pathBlocker);
        }

        if (nextArea != null)
        {
            nextArea.Unlock();
        }
    }

    private void StartBossWave()
    {
        Debug.Log("Boss wave triggered for wave " + waveNumber + " - not implemented yet");
    }

    private void OnDrawGizmosSelected()
    {
        if (spawnPoints == null) return;

        Gizmos.color = Color.cyan;
        foreach (Transform point in spawnPoints)
        {
            if (point != null)
            {
                Gizmos.DrawWireSphere(point.position, 0.5f);
            }
        }
    }
}