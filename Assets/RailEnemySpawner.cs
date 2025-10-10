using System.Collections;
using UnityEngine;
using SimpleFactory; // اگر تو پروژه‌ات این namespace فرق داره اسمش رو تغییر بده

[DisallowMultipleComponent]
public class RailEnemySpawner : MonoBehaviour
{
    [Header("Spawn Points (assign at least one)")]
    [SerializeField] private GameObject[] spawnPoints;

    [Header("Enemy Selection")]
    [SerializeField] private EnemyMovementType enemyMovementType = EnemyMovementType.Moving;
    [SerializeField] private EnemyType enemyType = EnemyType.Shooter;
    [SerializeField] private SpawnPointType spawnPointType = SpawnPointType.Top;

    [Header("Spawn Timing (runtime settable via WaveSO)")]
    [Tooltip("minimum random spawn interval (seconds)")]
    [SerializeField] public float minSpawnTime = 1f;
    [Tooltip("maximum random spawn interval (seconds)")]
    [SerializeField] public float maxSpawnTime = 5f;
    [Tooltip("exact spawn interval (seconds) - used when no valid range present)")]
    [SerializeField] public float exactSpawnTime = 1f;

    [Header("Spawn Behaviour")]
    [SerializeField] private bool spawnWithRandomMovementType = false;
    [SerializeField] private bool waitUntilNoExistingEnemy = true;
    [SerializeField] private string enemyTag = "ShooterEnemy";

    [Header("General")]
    [Tooltip("Master switch to enable/disable spawning")]
    [SerializeField] private bool spawningEnabled = true;
    public bool SpawningEnabled
    {
        get => spawningEnabled;
        set
        {
            spawningEnabled = value;
            if (spawningEnabled) StartSpawner();
            else StopSpawner();
        }
    }


    // internal
    private Coroutine spawnCoroutine;
    private bool isRunning => spawnCoroutine != null;

    private void Start()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning($"[{name}] No spawn points assigned. Spawner will not run until spawnPoints are set.");
            // DON'T permanently flip spawningEnabled here; just don't start.
            return;
        }

        if (!HasValidTimingConfig())
        {
            Debug.Log($"[{name}] Timing config invalid at Start (min/max/exact not set). Spawner will wait for config via ApplyWaveConfig or SetTiming.");
            // DON'T flip spawningEnabled here; waiting for ApplyWaveConfig to be called.
            return;
        }

        if (spawningEnabled)
        {
            StartSpawner();
        }
    }

    #region Public API (call from GameManager / Wave system)

    /// <summary>
    /// Apply a WaveSO (ScriptableObject) config. If wave is null this will log and ignore.
    /// WaveSO is expected to contain spawn timing fields (min/max/exact) and an enable flag.
    /// </summary>
    public void ApplyWaveConfig(WaveSO wave)
    {
        if (wave == null)
        {
            Debug.LogWarning($"[{name}] ApplyWaveConfig called with null wave -> ignoring.");
            return;
        }

        // apply values (you can pick fields you want from your WaveSO)
        spawningEnabled = wave.enableSpawning;
        minSpawnTime = wave.minSpawnTime;
        maxSpawnTime = wave.maxSpawnTime;
        exactSpawnTime = wave.exactSpawnTime;

        Debug.Log($"[{name}] ApplyWaveConfig -> enable:{spawningEnabled} min:{minSpawnTime} max:{maxSpawnTime} exact:{exactSpawnTime}");

        // restart/stop based on new config
        if (!HasValidTimingConfig() || !spawningEnabled)
        {
            StopSpawner();
            Debug.Log($"[{name}] Spawner disabled by wave config (no valid times or disabled flag).");
            return;
        }

        // valid => restart to pick up new timings immediately
        RestartSpawner();
    }

    /// <summary>Turn spawning on/off at runtime (keeps current timing values).</summary>
    public void SetSpawningEnabled(bool enabled)
    {
        spawningEnabled = enabled;
        if (!enabled) StopSpawner();
        else if (HasValidTimingConfig()) StartSpawner();
        else Debug.LogWarning($"[{name}] SetSpawningEnabled(true) called but timing config invalid (min/max/exact).");
    }

    /// <summary>Directly set timing (useful for debug/testing)</summary>
    public void SetTiming(float minSeconds, float maxSeconds, float exactSeconds)
    {
        minSpawnTime = minSeconds;
        maxSpawnTime = maxSeconds;
        exactSpawnTime = exactSeconds;
        Debug.Log($"[{name}] SetTiming -> min:{minSpawnTime} max:{maxSpawnTime} exact:{exactSpawnTime}");

        if (!HasValidTimingConfig())
        {
            Debug.LogWarning($"[{name}] Timing invalid after SetTiming. Spawner will be stopped.");
            StopSpawner();
            spawningEnabled = false;
            return;
        }

        // restart to apply
        RestartSpawner();
    }

    #endregion

    #region Spawner control

    private void StartSpawner()
    {
        if (!spawningEnabled)
        {
            Debug.Log($"[{name}] StartSpawner called but spawningEnabled is false -> not starting.");
            return;
        }
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning($"[{name}] StartSpawner aborted: no spawn points assigned.");
            return;
        }
        if (isRunning)
        {
            Debug.Log($"[{name}] Spawner already running.");
            return;
        }

        spawnCoroutine = StartCoroutine(SpawnLoop());
        Debug.Log($"[{name}] Spawner started.");
    }

    private void StopSpawner()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
            Debug.Log($"[{name}] Spawner stopped.");
        }
    }

    private void RestartSpawner()
    {
        StopSpawner();
        StartSpawner();
    }

    #endregion

    #region Core spawn loop

    private IEnumerator SpawnLoop()
    {
        while (spawningEnabled)
        {
            // optionally wait until no existing enemy with the given tag remains
            if (waitUntilNoExistingEnemy && !string.IsNullOrEmpty(enemyTag))
            {
                var found = GameObject.FindWithTag(enemyTag);
                if (found != null)
                {
                    Debug.Log($"[{name}] Found existing object with tag '{enemyTag}' ({found.name}). Waiting until it's destroyed before next spawn...");
                    yield return new WaitUntil(() => GameObject.FindWithTag(enemyTag) == null);
                    Debug.Log($"[{name}] No objects with tag '{enemyTag}' found anymore. Resuming spawns.");
                }
            }

            float wait = ComputeNextWait();
            if (wait <= 0f)
            {
                Debug.LogWarning($"[{name}] Computed wait <= 0 ({wait}). Stopping spawner to avoid instant-spawn loop.");
                yield break;
            }

            Debug.Log($"[{name}] Waiting {wait:F2}s before next spawn...");
            yield return new WaitForSeconds(wait);

            // final safety checks
            if (!spawningEnabled) yield break;
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogWarning($"[{name}] Spawn points missing at runtime. Stopping spawner.");
                yield break;
            }

            SpawnOneEnemy();
        }
    }

    private void SpawnOneEnemy()
    {
        int idx = Random.Range(0, spawnPoints.Length);
        if (idx < 0 || idx >= spawnPoints.Length)
        {
            Debug.LogWarning($"[{name}] Spawn index out of range ({idx}). Aborting spawn.");
            return;
        }

        EnemyMovementType moveType = spawnWithRandomMovementType ? GetRandomMovementType() : enemyMovementType;
        Vector3 pos = spawnPoints[idx].transform.position;

        Debug.Log($"[{name}] Spawning enemy at {pos} with movementType={moveType}");
        // call your factory (maintain same signature you had)
        EnemyFactory.CreateEnemy(enemyType, spawnPointType, moveType, pos);
    }

    private float ComputeNextWait()
    {
        bool hasRange = (minSpawnTime > 0f && maxSpawnTime > 0f && maxSpawnTime > minSpawnTime);
        bool hasExact = (exactSpawnTime > 0f);

        if (hasRange) return Random.Range(minSpawnTime, maxSpawnTime);
        if (hasExact) return exactSpawnTime;
        return -1f;
    }

    #endregion

    #region Helpers

    private EnemyMovementType GetRandomMovementType()
    {
        var values = System.Enum.GetValues(typeof(EnemyMovementType));
        if (values == null || values.Length == 0) return enemyMovementType;
        int r = Random.Range(0, values.Length);
        return (EnemyMovementType)values.GetValue(r);
    }

    private bool HasValidTimingConfig()
    {
        bool hasRange = (minSpawnTime > 0f && maxSpawnTime > 0f && maxSpawnTime > minSpawnTime);
        bool hasExact = (exactSpawnTime > 0f);
        return hasRange || hasExact;
    }

    #endregion

    #region Editor safety
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (minSpawnTime < 0f) minSpawnTime = 0f;
        if (maxSpawnTime < 0f) maxSpawnTime = 0f;
        if (exactSpawnTime < 0f) exactSpawnTime = 0f;
    }
#endif
    #endregion
}
