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

    [Header("Bounds (Use two transforms as range ends)")]
    [SerializeField] Transform topBound;   // برای Top: نقش Left/Right را بازی می‌کند (Xها مهم‌اند)
    [SerializeField] Transform downBound;  // برای Side: نقش Up/Down را بازی می‌کند (Yها مهم‌اند)

    [Header("Set Enemy Count Value")]
    public int minimumCount = 1;
    public int maximumCount = 3;

    [Header("Spawn Settings")]
    public float maxSpawnTime = 5f;
    public float minSpawnTime = 1f;
    [SerializeField] float spawnJitterPosettive = 0f; // نگه‌داشتن تایپو برای سازگاری با اینسپکتور
    [SerializeField] float spawnJitterNegetive = 0f;  // نگه‌داشتن تایپو برای سازگاری با اینسپکتور
    [SerializeField] int maxSpawnInBound = 10;        // optional - فعلاً استفاده نشده
    [SerializeField] float maxDistanceBetwenEnemy = 5f; // optional - فعلاً استفاده نشده

    [Header("Enemy Setting")]
    [SerializeField] EnemyType enemyType;
    [SerializeField] SpawnPointType spawnPointType;
    [SerializeField] EnemyMovementType enemyMovementType;

    [Header("Make Random Enemy")]
    [SerializeField] bool randomEnemySpawning = false;
    [SerializeField] bool randomEnemyMovment = false;
    [SerializeField] bool randomEnemySpawnLocation = false;
    [SerializeField] bool top = false; // اگر true، رنج موومنت برای تاپ 0..3 می‌شود

    [Header("Spacing / Physics (2D)")]
    [SerializeField, Min(0f)] float minDistanceBetweenEnemies = 1f; // حداقل فاصله بین اسپاون‌های همین موج
    [SerializeField, Min(0f)] float enemyRadius = 0.5f;             // شعاع برای چک OverlapCircle
    [SerializeField] LayerMask physicsCheckMask = ~0;               // فقط این لایه‌ها مانع اسپاون باشند
    [SerializeField] bool ignoreTriggers = true;                    // تریگرها را نادیده بگیر
    [SerializeField] bool bypassPhysicsCheck = false;               // برای تست: خاموش کردن چک فیزیک

    [Header("Debug")]
    [SerializeField] bool verboseDebug = false;
    [SerializeField] Color debugPickColor = Color.yellow;
    [SerializeField] float debugPickDuration = 0.5f;

    private EnemyMovementType finalMovementType;

    // کشِ بازه‌ها برای لاگ/کلَمپ
    float _minX, _maxX, _minY, _maxY;

    private void OnValidate()
    {
        // هم‌راستا کردن enemyMode و spawnPointType
        spawnPointType = (enemyMode == EnemySpawnerMode.top) ? SpawnPointType.Top : SpawnPointType.Side;

        if (topBound == null || downBound == null)
        {
            Debug.LogWarning($"{name}: topBound/downBound are not assigned.");
            return;
        }

        // محاسبه‌ی بازه‌ها با احتساب جیتر
        _minY = Mathf.Min(topBound.position.y + spawnJitterPosettive, downBound.position.y + spawnJitterNegetive);
        _maxY = Mathf.Max(topBound.position.y + spawnJitterPosettive, downBound.position.y + spawnJitterNegetive);

        _minX = Mathf.Min(topBound.position.x + spawnJitterPosettive, downBound.position.x + spawnJitterNegetive);
        _maxX = Mathf.Max(topBound.position.x + spawnJitterPosettive, downBound.position.x + spawnJitterNegetive);

        // هشدار بازه‌ی صفر
        if (spawnPointType == SpawnPointType.Top && Mathf.Approximately(_minX, _maxX))
            Debug.LogWarning($"{name}: X-range is zero for Top spawn. Check X of bounds or jitters.");
        if (spawnPointType == SpawnPointType.Side && Mathf.Approximately(_minY, _maxY))
            Debug.LogWarning($"{name}: Y-range is zero for Side spawn. Check Y of bounds or jitters.");

        // اطمینان از درست بودن بازه (min<=max)
        if (_minX > _maxX) { var t = _minX; _minX = _maxX; _maxX = t; }
        if (_minY > _maxY) { var t = _minY; _minY = _maxY; _maxY = t; }
    }

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
        // این لیست فقط در همین موج استفاده می‌شود تا فاصله‌ی منطقی بین دشمن‌های جدید رعایت شود
        List<Vector2> previousPositions = new List<Vector2>();

        // محاسبه‌ی بازه‌ها یک‌بار برای این موج (همسو با OnValidate ولی به‌روز)
        float minY = Mathf.Min(topBound.position.y + spawnJitterPosettive, downBound.position.y + spawnJitterNegetive);
        float maxY = Mathf.Max(topBound.position.y + spawnJitterPosettive, downBound.position.y + spawnJitterNegetive);
        float minX = Mathf.Min(topBound.position.x + spawnJitterPosettive, downBound.position.x + spawnJitterNegetive);
        float maxX = Mathf.Max(topBound.position.x + spawnJitterPosettive, downBound.position.x + spawnJitterNegetive);

        // همچنین در کش داخلی نگه داریم برای لاگ/کلَمپ
        _minX = Mathf.Min(minX, maxX);
        _maxX = Mathf.Max(minX, maxX);
        _minY = Mathf.Min(minY, maxY);
        _maxY = Mathf.Max(minY, maxY);

        for (int i = 0; i < count; i++)
        {
            Vector2 spawnPos = Vector2.zero;
            int attempts = 0;

            // تلاش برای پیدا کردن موقعیت معتبر: فاصله + فیزیک آزاد
            do
            {
                spawnPos = GenerateSpawnPosition(minX, maxX, minY, maxY);
#if UNITY_EDITOR
                if (verboseDebug)
                {
                    Vector3 p = new Vector3(spawnPos.x, spawnPos.y, transform.position.z);
                    Debug.DrawLine(p + Vector3.down * 0.25f, p + Vector3.up * 0.25f, debugPickColor, debugPickDuration);
                }
#endif
                attempts++;
            }
            while ((!IsFarEnough(spawnPos, previousPositions) || !IsPositionFree(spawnPos)) && attempts < 100);

            if (attempts >= 100)
            {
                Debug.LogWarning($"[{name}] Couldn't find valid spawn pos for enemy #{i} after {attempts} attempts. Skipping.");
                continue;
            }

            // اگر مکان اسپاون رندوم نباشد، محور مقابل را روی ترنسفرم اسپاونر قفل کن
            if (!randomEnemySpawnLocation)
            {
                if (spawnPointType == SpawnPointType.Side)
                {
                    // از کنار: X ثابت، Y رندوم
                    spawnPos = new Vector2(transform.position.x, spawnPos.y);
                }
                else // Top
                {
                    // از بالا: Y ثابت، X رندوم
                    spawnPos = new Vector2(spawnPos.x, transform.position.y);
                }
            }

            // کلَمپ اطمینانی داخل بازه
            if (spawnPointType == SpawnPointType.Top)
                spawnPos.x = Mathf.Clamp(spawnPos.x, _minX, _maxX);
            else
                spawnPos.y = Mathf.Clamp(spawnPos.y, _minY, _maxY);

            previousPositions.Add(spawnPos);

            // نوع دشمن
            EnemyType finalEnemyType = randomEnemySpawning
                ? (EnemyType)Random.Range(0, System.Enum.GetValues(typeof(EnemyType)).Length)
                : enemyType;

            // نوع حرکت: اگر top=true بود، رنج 0..3، وگرنه 0..2
            if (top)
            {
                finalMovementType = randomEnemyMovment
                    ? (EnemyMovementType)Random.Range(0, 3)
                    : enemyMovementType;
            }
            else
            {
                finalMovementType = randomEnemyMovment
                    ? (EnemyMovementType)Random.Range(0, 2)
                    : enemyMovementType;
            }

            if (verboseDebug)
            {
                Debug.Log(
                    $"[{name}] spawn[{i}] pos=({spawnPos.x:F2},{spawnPos.y:F2}) | " +
                    $"Xrange:[{_minX:F2},{_maxX:F2}]  Yrange:[{_minY:F2},{_maxY:F2}] | " +
                    $"mode:{spawnPointType} randomLoc:{randomEnemySpawnLocation}"
                );
            }// ساخت دشمن
            EnemyFactory.CreateEnemy(
                finalEnemyType,
                spawnPointType,
                finalMovementType,
                new Vector3(spawnPos.x, spawnPos.y, transform.position.z)
            );
        }
    }

    /// <summary>
    /// اگر Side ⇒ بین Yها رندوم (X = X اسپاونر)
    /// اگر Top  ⇒ بین Xها رندوم (Y = Y اسپاونر)
    /// </summary>
    private Vector2 GenerateSpawnPosition(float minX, float maxX, float minY, float maxY)
    {
        if (spawnPointType == SpawnPointType.Side)
        {
            float y = Random.Range(minY, maxY);
            return new Vector2(transform.position.x, y);
        }
        else // Top
        {
            float x = Random.Range(minX, maxX);
            return new Vector2(x, transform.position.y);
        }
    }

    // بررسی فاصله با اسپاون‌های قبلی همین موج
    private bool IsFarEnough(Vector2 pos, List<Vector2> previousPositions)
    {
        foreach (var prev in previousPositions)
        {
            if (Vector2.Distance(pos, prev) < minDistanceBetweenEnemies)
                return false;
        }
        return true;
    }

    // چک برخورد با Collider2D های حاضر (با ماسک و نادیده‌گرفتن تریگرها)
    private bool IsPositionFree(Vector2 pos)
    {
        if (bypassPhysicsCheck) return true;

        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, enemyRadius, physicsCheckMask);
        if (hits == null || hits.Length == 0) return true;

        if (!ignoreTriggers) return false;

        // اگر تریگرها را نادیده می‌گیریم، فقط وقتی برخورد مؤثر حساب می‌شود که حداقل یک کالایدرِ غیرتریگر پیدا شود
        foreach (var h in hits)
        {
            if (h != null && !h.isTrigger)
                return false;
        }
        return true;
    }

    // Debug gizmos
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        if (topBound != null && downBound != null)
        {
            if (spawnPointType == SpawnPointType.Side)
            {
                // از کنار: خط عمودی بین Yهای دو باند روی X اسپاونر
                Vector3 a = new Vector3(transform.position.x, topBound.position.y, transform.position.z);
                Vector3 b = new Vector3(transform.position.x, downBound.position.y, transform.position.z);
                Gizmos.DrawLine(a, b);
            }
            else // Top
            {
                // از بالا: خط افقی بین Xهای دو باند روی Y اسپاونر
                Vector3 a = new Vector3(topBound.position.x, transform.position.y, transform.position.z);
                Vector3 b = new Vector3(downBound.position.x, transform.position.y, transform.position.z);
                Gizmos.DrawLine(a, b);
            }
        }

        // نمایش شعاع چک فیزیکی در موقعیت خود اسپاونر (برای درک مقیاس)
        Gizmos.DrawWireSphere(transform.position, enemyRadius);
    }
}