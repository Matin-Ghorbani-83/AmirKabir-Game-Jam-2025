using UnityEngine;

public class StaticPlatformSpawner : MonoBehaviour
{
    [Header("Settings")]
    public bool spawnActive; // اینو از Inspector تیک می‌زنی یا برمی‌داری
    public GameObject prefab; // چیزی که می‌خوای اسپاون شه
    public Transform spawnPointA;
    public Transform spawnPointB;

    private GameObject spawnedA;
    private GameObject spawnedB;
    private bool lastState;

    void Update()
    {
        // فقط وقتی وضعیت تیک تغییر کرد (از true به false یا برعکس) اجرا کنه
        if (spawnActive != lastState)
        {
            if (spawnActive)
            {
                SpawnObjects();
            }
            else
            {
                DestroyObjects();
            }
            lastState = spawnActive;
        }
    }

    void SpawnObjects()
    {
        if (prefab == null || spawnPointA == null || spawnPointB == null)
        {
            Debug.LogWarning("⚠️ Please assign prefab and spawn points!");
            return;
        }

        spawnedA = Instantiate(prefab, spawnPointA.position, spawnPointA.rotation);
        spawnedB = Instantiate(prefab, spawnPointB.position, spawnPointB.rotation);
    }

    void DestroyObjects()
    {
        if (spawnedA != null) Destroy(spawnedA);
        if (spawnedB != null) Destroy(spawnedB);
    }
}
