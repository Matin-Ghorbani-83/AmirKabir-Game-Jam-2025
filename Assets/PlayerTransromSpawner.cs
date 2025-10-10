using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTransromSpawner : MonoBehaviour
{
    public GameObject playerTransformPrefab;
    private GameObject transfromGameObject;

    private void Awake()
    {

        transfromGameObject = playerTransformPrefab;
    }
    private void Start()
    {
        Instantiate(transfromGameObject, transform.position, Quaternion.identity);
        GameManager.instance.OnWaveChanged += Instance_OnWaveChanged;
        PlayerHealthSystem.instance.OnPlayerDied += Instance_OnPlayerDied;
        PlayerHealthSystem.instance.OnRequestRespawn += Instance_OnRequestRespawn;
        PlayerHealthSystem.instance.lastSafeRespawnPos = transform.position;
        PlayerHealthSystem.instance.RegisterSafeRespawnPosition(transform.position + new Vector3(0, 5, 0));
    }

    private void Instance_OnRequestRespawn(Vector3 arg1, PlayerHealthSystem.DamageType arg2)
    {
        PlayerHealthSystem.instance.lastSafeRespawnPos = transform.position;
        PlayerHealthSystem.instance.RegisterSafeRespawnPosition(transform.position + new Vector3(0, 5, 0));
        Instantiate(transfromGameObject, transform.position, Quaternion.identity);
    }

    private void Instance_OnWaveChanged(object sender, System.EventArgs e)
    {



        PlayerHealthSystem.instance.lastSafeRespawnPos = transform.position;
        PlayerHealthSystem.instance.RegisterSafeRespawnPosition(transform.position + new Vector3(0, 5, 0));
        Instantiate(transfromGameObject, transform.position, Quaternion.identity);

        if (PlayerController.instance != null)
        {
            Vector3 respawnPos = transform.position + new Vector3(0, 5f, 0); // همون جایی که پلتفرم رو ثبت کردی
                                                                             // Teleport the player
            PlayerController.instance.transform.position = respawnPos;

            // reset physics so player won't keep falling
            var rb = PlayerController.instance.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            Debug.Log($"[PlayerTransromSpawner] Teleported player to {respawnPos} on wave change.");
        }

        print("WWWWWWWWAVVVVEEE Chaged");

    }

    private void Instance_OnPlayerDied(Vector3 obj)
    {
        PlayerHealthSystem.instance.lastSafeRespawnPos = transform.position;
        Instantiate(transfromGameObject, transform.position, Quaternion.identity);
        PlayerHealthSystem.instance.RegisterSafeRespawnPosition(transform.position + new Vector3(0, 5, 0));
    }
}
