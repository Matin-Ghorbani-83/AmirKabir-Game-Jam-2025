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

    [Header("Vertical Bounds")]
    [SerializeField] Transform topBound;
    [SerializeField] Transform downBound;

    [Header("Set Enemy Count Value")]
    public int minimumCount = 1;
    public int maximumCount = 3;

    [Header("Spawn Settings")]
    public float maxSpawnTime = 5f;
    public float minSpawnTime = 1f;
    [SerializeField] float spawnJitterPosettive = 0f;
    [SerializeField] float spawnJitterNegetive = 0f;
    [SerializeField] int maxSpawnInBound = 10; // optional
    [SerializeField] float maxDistanceBetwenEnemy = 5f; // optional

    [Header("Enemy Setting")]
    [SerializeField] EnemyType enemyType;
    [SerializeField] SpawnPointType spawnPointType;
    [SerializeField] EnemyMovementType enemyMovementType;

    [Header("Make Random Enemy")]
    [SerializeField] bool randomEnemySpawning = false;
    [SerializeField] bool randomEnemyMovment = false;
    [SerializeField] bool randomEnemySpawnLocation = false;

    [Header("Spacing / Physics (2D)")]
    [SerializeField, Min(0f)] float minDistanceBetweenEnemies = 1f; // قابل تنظیم از اینسپکتور
    [SerializeField, Min(0f)] float enemyRadius = 0.5f; // نصفِ شعاعی که می‌خوای برای چک فیزیکی استفاده کنی

    void Start()
    {
        StartCoroutine(StartSpawning());
    }

    private IEnumerator StartSpawning()
    {
        while (true)
        {
            float randTime = Random.Range(minSpawnTime, maxSpawnTime);
            yield return new WaitForSeconds(randTime);

            int randEnemyCount = Random.Range(minimumCount, maximumCount + 1);
            SpawnEnemies(randEnemyCount);
        }
    }

    private void SpawnEnemies(int count)
    {
        // <-- مهم: این لیست هر بار که SpawnEnemies صدا زده میشه ریست میشه،
        // بنابراین موج بعدی میتونه دوباره از همان نقاط استفاده کنه.
        List<Vector2> previousPositions = new List<Vector2>();

        for (int i = 0; i < count; i++)
        {
            Vector2 spawnPos = Vector2.zero;
            int attempts = 0;

            // تلاش برای پیدا کردن موقعیت معتبر: هم فاصله منطقی با قبلی‌ها، هم چک فیزیکی (OverlapCircle)
            do
            {
                spawnPos = GenerateSpawnPosition();
                attempts++;
            }
            while ((!IsFarEnough(spawnPos, previousPositions) || !IsPositionFree(spawnPos)) && attempts < 100);

            if (attempts >= 100)
            {
                Debug.LogWarning($"EnemySpawner: couldn't find valid spawn pos for enemy #{i} after {attempts} attempts. Skipping this spawn.");
                continue; // اگر نتونست جای درست پیدا کنه، اون دشمن رو نمی‌سازه
            }

            previousPositions.Add(spawnPos);

            // *** تغییر طبق خواستهٔ تو: Movement random با بازه‌ی 0 تا 3 ***
            EnemyType finalEnemyType = randomEnemySpawning
                ? (EnemyType)Random.Range(0, System.Enum.GetValues(typeof(EnemyType)).Length)
                : enemyType;

            EnemyMovementType finalMovementType = randomEnemyMovment
                ? (EnemyMovementType)Random.Range(0, 3) // <-- برگشت به Random.Range(0, 3) طبق خواست تو
                : enemyMovementType;

            // اگر خواستی مکان اسپاون رو رندم یا ثابت بسازی (فیلد randomEnemySpawnLocation)
            if (!randomEnemySpawnLocation)
            {
                if (spawnPointType == SpawnPointType.Side)
                {
                    spawnPos = new Vector2(transform.position.x, spawnPos.y);
                }
                else
                {
                    spawnPos = new Vector2(spawnPos.x, transform.position.y);
                }
            }

            // Create as Vector3 (z from this transform)
            EnemyFactory.CreateEnemy(finalEnemyType, spawnPointType, finalMovementType, new Vector3(spawnPos.x, spawnPos.y, transform.position.z));
        }
    }

    private Vector2 GenerateSpawnPosition()
    {
        float minY = Mathf.Min(topBound.position.y + spawnJitterPosettive, downBound.position.y + spawnJitterNegetive);
        float maxY = Mathf.Max(topBound.position.y + spawnJitterPosettive, downBound.position.y + spawnJitterNegetive);

        float minX = Mathf.Min(topBound.position.x + spawnJitterPosettive, downBound.position.x + spawnJitterNegetive);
        float maxX = Mathf.Max(topBound.position.x + spawnJitterPosettive, downBound.position.x + spawnJitterNegetive);

        float y = (spawnPointType == SpawnPointType.Side) ? Random.Range(minY, maxY) : transform.position.y;
        float x = (spawnPointType != SpawnPointType.Side) ? Random.Range(minX, maxX) : transform.position.x;

        return new Vector2(x, y);
    }

    // فاصله با بقیه spawnها (فقط برای همین دور)
    private bool IsFarEnough(Vector2 pos, List<Vector2> previousPositions)
    {
        foreach (var prev in previousPositions)
        {
            if (Vector2.Distance(pos, prev) < minDistanceBetweenEnemies)
                return false;
        }
        return true;
    }

    // چک برخورد با collider2D های حاضر
    private bool IsPositionFree(Vector2 pos)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, enemyRadius);
        return hits.Length == 0;
    }

    // Debug gizmos
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        if (topBound != null && downBound != null)
        {
            Vector3 a = new Vector3(transform.position.x, topBound.position.y, transform.position.z);
            Vector3 b = new Vector3(transform.position.x, downBound.position.y, transform.position.z);
            Gizmos.DrawLine(a, b);
        }
        Gizmos.DrawWireSphere(transform.position, enemyRadius);
    }
}
