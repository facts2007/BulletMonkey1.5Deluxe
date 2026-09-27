using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class WaveArea : MonoBehaviour
{
    [Serializable]
    public class IslandWave
    {
        [Min(1)] public int enemyCount = 5;
        public bool bossWave;
    }

    [Header("Island waves — add/remove entries to change wave count")]
    public string islandName = "Island";
    public IslandWave[] waves = { new IslandWave() };
    [Tooltip("Only the first island starts unlocked. Clearing it unlocks Next Area.")]
    public bool startsUnlocked;
    [Header("Countdown UI")]
    public TextMeshProUGUI countdownText;
    private static TextMeshProUGUI sharedCountdownText;
    // Retained for compatibility with existing scenes and classmates' scripts.
    [HideInInspector] public int waveNumber = 1;
    [HideInInspector] public int baseEnemyCount = 5;
    [HideInInspector] public float scalingMultiplier = 1.5f;
    [HideInInspector] public bool isBossWave;
    [Header("Start behavior")]
    public bool autoStart;
    [Min(0)] public int countdownSeconds = 3;
    public Collider entryTrigger;
    [Header("Spawning — random mix from these prefabs")]
    public Transform[] spawnPoints;
    public GameObject[] enemyPrefabs;
    [Min(0)] public float spawnInterval = 0.5f;
    [Header("Boss and breaks")]
    public GameObject bossPrefab;
    [Min(0)] public float intermissionSeconds = 12;
    public bool IsCombatActive {get;private set;}
    public static bool AnyWaveActive => FindObjectsByType<WaveArea>(FindObjectsSortMode.None).Any(a=>a.IsCombatActive);
    [Header("Exit mist — disabled after every wave is cleared")]
    public GameObject pathBlocker;
    public WaveArea nextArea;
    [Header("Runtime status")]
    public bool waveStarted;
    public bool waveComplete;
    [SerializeField] private int currentWave;
    [SerializeField] private int enemiesRemaining;
    public int CurrentWave => currentWave;
    public int EnemiesRemaining => enemiesRemaining;
    public bool IsUnlocked { get; private set; }
    private readonly List<EnemyHealth> living = new List<EnemyHealth>();
    private TextMeshProUGUI Text => countdownText != null ? countdownText : sharedCountdownText;

    private void Awake()
    {
        if (countdownText != null) sharedCountdownText = countdownText;
        if (entryTrigger == null) entryTrigger = GetComponent<Collider>();
    }

    private void Start()
    {
        if (pathBlocker != null) pathBlocker.SetActive(!waveComplete);
        if (startsUnlocked || autoStart) Unlock();
        else if (!IsUnlocked && entryTrigger != null) entryTrigger.enabled = false;
        if (autoStart) StartCoroutine(WaitForPlayerThenStart());
    }

    public void Unlock()
    {
        IsUnlocked = true;
        if (entryTrigger != null) entryTrigger.enabled = true;
    }

    private IEnumerator WaitForPlayerThenStart()
    {
        while (FindFirstObjectByType<PlayerHealth>() == null) yield return null;
        StartWave();
    }

    private void OnTriggerEnter(Collider other) { TryEnter(other); }
    private void OnTriggerStay(Collider other) { TryEnter(other); }
    public void TryEnter(Collider other)
    {
        if (other != null && other.GetComponentInParent<PlayerHealth>() != null) StartWave();
    }

    public void StartWave()
    {
        if (!isActiveAndEnabled || !IsUnlocked || waveStarted || waveComplete) return;
        if (waves == null || waves.Length == 0 || waves.Any(w => w == null || w.enemyCount < 1) ||
            spawnPoints == null || !spawnPoints.Any(p => p != null) ||
            enemyPrefabs == null || !enemyPrefabs.Any(p => p != null && p.GetComponent<EnemyHealth>() != null))
        {
            Debug.LogError(islandName + ": assign waves, spawn points and enemy prefabs with EnemyHealth before starting.", this);
            return;
        }
        waveStarted = true;
        IsCombatActive = true;
        if (entryTrigger != null) entryTrigger.enabled = false;
        StartCoroutine(RunWaves());
    }

    private IEnumerator RunWaves()
    {
        Transform[] points = spawnPoints.Where(p => p != null).ToArray();
        GameObject[] prefabs = enemyPrefabs.Where(p => p != null && p.GetComponent<EnemyHealth>() != null).ToArray();
        for (int index = 0; index < waves.Length; index++)
        {
            IsCombatActive=true;
            foreach(var shop in FindObjectsByType<KabuWaveShop>(FindObjectsSortMode.None))shop.Ignite();
            currentWave = index + 1;
            for (int seconds = countdownSeconds; seconds > 0; seconds--)
            {
                SetText(islandName + " · Wave " + currentWave + "/" + waves.Length + " in " + seconds);
                yield return new WaitForSeconds(1f);
            }
            living.Clear();
            int count = waves[index].bossWave ? 1 : waves[index].enemyCount;
            for (int i = 0; i < count; i++)
            {
                Transform point = points[UnityEngine.Random.Range(0, points.Length)];
                GameObject source=waves[index].bossWave && bossPrefab!=null ? bossPrefab : prefabs[UnityEngine.Random.Range(0,prefabs.Length)];
                GameObject spawned = Instantiate(source, point.position, point.rotation);
                spawned.SetActive(true);
                living.Add(spawned.GetComponent<EnemyHealth>());
                UpdateCount(count - i - 1);
                if (spawnInterval > 0) yield return new WaitForSeconds(spawnInterval);
            }
            // Completion is checked only after spawning; unrelated kills never reduce this count.
            do
            {
                UpdateCount(0);
                yield return null;
            } while (enemiesRemaining > 0);
            IsCombatActive=false;
            var bucket=FindFirstObjectByType<PlayerBucketInventory>();if(bucket!=null)bucket.AddBucket();
            if(index<waves.Length-1)
            {
                SetText(islandName+" · Wave cleared! +1 bucket · Shop break");
                yield return new WaitForSeconds(intermissionSeconds);
            }
        }
        waveComplete = true;
        if (pathBlocker != null) pathBlocker.SetActive(false);
        SetText(islandName + " cleared — path open");
        if (nextArea != null) nextArea.Unlock();
    }

    private void UpdateCount(int pending)
    {
        living.RemoveAll(h => h == null || h.IsDead || !h.gameObject.activeInHierarchy);
        enemiesRemaining = living.Count + pending;
        SetText(islandName + " · Wave " + currentWave + "/" + waves.Length + " · " + enemiesRemaining + " enemies");
    }
    private void SetText(string value) { if (Text != null) Text.text = value; }
    private void OnDrawGizmosSelected()
    {
        if (spawnPoints == null) return;
        Gizmos.color = Color.cyan;
        foreach (Transform point in spawnPoints) if (point != null) Gizmos.DrawWireSphere(point.position, .5f);
    }
}
