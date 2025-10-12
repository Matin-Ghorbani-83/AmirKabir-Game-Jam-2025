using UnityEngine;

public class StaticPlatformSpawner : MonoBehaviour
{
    [Header("Settings")]
    public bool spawnActive; // اینو از Inspector تیک می‌زنی یا برمی‌داری
    public GameObject prefab; // چیزی که می‌خوای اسپاون شه
    public Transform spawnPointTopRight;
    public Transform spawnPointTopLeft;
    public Transform spawnPointBottomRight;
    public Transform spawnPointBottomLeft;

    private GameObject spawnedTopRight;
    private GameObject spawnedTopLeft;

    private GameObject spawnedBottomRight;
    private GameObject spawnedBottomLeft;

    private bool lastState;

    void Update()
    {
        //Debug.Log(spawnActive);
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
        if (prefab == null || spawnPointTopRight == null || spawnPointTopLeft == null)
        {
            Debug.LogWarning("⚠️ Please assign prefab and spawn points!");
            return;
        }

        spawnedTopRight = Instantiate(prefab, spawnPointTopRight.position, spawnPointTopRight.rotation);

        spawnedTopLeft = Instantiate(prefab, spawnPointTopLeft.position, spawnPointTopLeft.rotation);


        spawnedBottomRight = Instantiate(prefab, spawnPointBottomRight.position, spawnPointBottomRight.rotation);
        spawnedBottomLeft = Instantiate(prefab, spawnPointBottomLeft.position, spawnPointBottomLeft.rotation);

    }

    void DestroyObjects()
    {
        Debug.Log("Static ObjectsDestroy");

        if (spawnedTopRight != null)
        {

            Destroy(spawnedTopRight);
        }
        if (spawnedTopLeft != null)
        {

            Destroy(spawnedTopLeft);
        }
        if(spawnedBottomRight != null)
        {
            Destroy (spawnedBottomRight);
        }
        if(spawnPointBottomLeft != null)
        {
            Destroy(spawnedBottomLeft);
        }
    }
}
