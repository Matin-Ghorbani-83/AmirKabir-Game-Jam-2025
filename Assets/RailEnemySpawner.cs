using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleFactory;

public class RailEnemySpawner : MonoBehaviour
{
    [Header("Spawn Points")]
    [SerializeField] GameObject[] SpawnPoints;

    [Header("Enemy Type Select")]
    [SerializeField] EnemyMovementType enemyMovementType;

    [Header("Spawn Settings")]
    public float maxSpawnTime = 5f;
    public float minSpawnTime = 1f;
    public float exactSpawnTime = 1f;

    [Header("Spawn Condition")]
    [SerializeField] bool spawnInExactTime;
    [SerializeField] bool spawnWithRandomMovmentType;

    [Header("Wait Until No Enemy Exists (then wait spawn time)")]
    [SerializeField] bool waitUntilExistingEnemyDestroyed = true;
    [SerializeField] string enemyTag = "Enemy";

    void Start()
    {
        if (SpawnPoints == null || SpawnPoints.Length == 0)
        {
            Debug.LogWarning($"[{name}] No spawn points assigned!");
            return;
        }

        StartCoroutine(SpawnEnemy());
    }

    IEnumerator SpawnEnemy()
    {
        while (true)
        {
            // اگر تنظیم شده که تا نابودی دشمن صبر کنه، فقط وقتی دشمن وجود داشته باشه منتظر نابودیش می‌شیم
            if (waitUntilExistingEnemyDestroyed)
            {
                if (GameObject.FindWithTag(enemyTag) != null)
                {
                    // صبر کن تا دیگه هیچ آبجکتی با اون تگ وجود نداشته باشه (بعد از Destroy مرجع null می‌شود)
                    yield return new WaitUntil(() => GameObject.FindWithTag(enemyTag) == null);
                }
            }

            // بعد از اینکه یا دشمن نبوده یا نابود شده، الآن تایمِ اسپاونِ بعدی اعمال می‌شود
            float wait = spawnInExactTime ? exactSpawnTime : Random.Range(minSpawnTime, maxSpawnTime);
            yield return new WaitForSeconds(wait);

            // safety
            if (SpawnPoints == null || SpawnPoints.Length == 0) yield break;

            int randSpawnPoint = Random.Range(0, SpawnPoints.Length);

            // فقط 3 یا 4 وقتی رندم مومنت تایپ فعال است
            EnemyMovementType movType;
            if (spawnWithRandomMovmentType)
            {
                movType = (EnemyMovementType)Random.Range(3, 5); // نتیجه: 3 یا 4
            }
            else
            {
                movType = enemyMovementType;
            }

            EnemyFactory.CreateEnemy(EnemyType.Shooter, SpawnPointType.Top, movType, SpawnPoints[randSpawnPoint].transform.position);
        }
    }
}
