using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleFactory;

public enum EnemySpawnerMode
{
    side, top
}

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawner Type")]
    [SerializeField] EnemySpawnerMode enemyMode;

    [Header("Bounds")]
    [SerializeField] Transform topBound;
    [SerializeField] Transform downBound;

    [Header("Enemy Count")]
    [SerializeField] int minimumCount;
    [SerializeField] int maximumCount;

    [Header("Spawn Timing")]
    [SerializeField] float maxSpawnTime;
    [SerializeField] float minSpawnTime;

    [Header("Spawn Jitter")]
    [SerializeField] float spawnJitterPositive;
    [SerializeField] float spawnJitterNegative;

    [Header("Minimum Distance Between Enemies")]
    [SerializeField, Min(0f)] float minDistanceBetweenEnemies = 1f; // قابل تنظیم از اینسپکتور

    [Header("Enemy Settings")]
    [SerializeField] EnemyType enemyType;
    [SerializeField] SpawnPointType spawnPointType;
    [SerializeField] EnemyMovementType enemyMovementType;

    [Header("Randomization Options")]
    [SerializeField] bool randomEnemySpawning;
    [SerializeField] bool randomEnemyMovement;
    [SerializeField] bool randomEnemySpawnLocation;

    [Header("Enemy Collider Radius")]
    [SerializeField, Min(0f)] float enemyRadius = 0.5f; // نصف اندازه Collider دشمن

    void Start()
    {
        StartCoroutine(StartSpawning());
    }

    private IEnumerator StartSpawning()
    {
        while (true)
        {
            float waitTime = Random.Range(minSpawnTime, maxSpawnTime);
            yield return new WaitForSeconds(waitTime);

            int enemyCount = Random.Range(minimumCount, maximumCount + 1);
            SpawnEnemies(enemyCount);
        }
    }

    private void SpawnEnemies(int count)
    {
        List<Vector2> previousPositions = new List<Vector2>();

        for (int i = 0; i < count; i++)
        {
            Vector2 spawnPos = Vector2.zero;
            int attempts = 0;

            // تلاش برای پیدا کردن موقعیت معتبر
            do
            {
                spawnPos = GenerateSpawnPosition(previousPositions);
                attempts++;
            }
            while ((!IsPositionFree(spawnPos) || !IsFarEnough(spawnPos, previousPositions)) && attempts < 100);

            if (attempts >= 100)
            {
                Debug.LogWarning("Failed to find a valid spawn position for enemy #" + i);
                continue; // اگر پیدا نکرد، دشمن رو نساخت
            }

            previousPositions.Add(spawnPos);

            EnemyType finalEnemyType = randomEnemySpawning
                ? (EnemyType)Random.Range(0, System.Enum.GetValues(typeof(EnemyType)).Length)
                : enemyType;

            EnemyMovementType finalMovementType = randomEnemyMovement
                ? (EnemyMovementType)Random.Range(0, System.Enum.GetValues(typeof(EnemyMovementType)).Length)
                : enemyMovementType;

            EnemyFactory.CreateEnemy(finalEnemyType, spawnPointType, finalMovementType, spawnPos);
        }
    }

    private Vector2 GenerateSpawnPosition(List<Vector2> previousPositions)
    {
        float minY = Mathf.Min(topBound.position.y + spawnJitterPositive, downBound.position.y + spawnJitterNegative);
        float maxY = Mathf.Max(topBound.position.y + spawnJitterPositive, downBound.position.y + spawnJitterNegative);
        float minX = Mathf.Min(topBound.position.x + spawnJitterPositive, downBound.position.x + spawnJitterNegative);
        float maxX = Mathf.Max(topBound.position.x + spawnJitterPositive, downBound.position.x + spawnJitterNegative);

        float y = spawnPointType == SpawnPointType.Side ? Random.Range(minY, maxY) : transform.position.y;
        float x = spawnPointType != SpawnPointType.Side ? Random.Range(minX, maxX) : transform.position.x;

        return new Vector2(x, y);
    }

    // چک فاصله از سایر spawnها
    private bool IsFarEnough(Vector2 pos, List<Vector2> previousPositions)
    {
        foreach (var prev in previousPositions)
        {
            if (Vector2.Distance(pos, prev) < minDistanceBetweenEnemies)
                return false;
        }
        return true;
    }

    // چک برخورد با دشمن‌های حاضر
    private bool IsPositionFree(Vector2 pos)
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(pos, enemyRadius);
        return colliders.Length == 0;
    }
}
