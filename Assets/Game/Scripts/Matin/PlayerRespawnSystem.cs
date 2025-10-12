using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerRespawnSystem : MonoBehaviour
{
   
    [SerializeField] private float respawnDelay = 1f;
    [SerializeField] private float fadeDuration = 0.5f;

    private PlayerHealthSystem healthSystem;
    private Rigidbody2D rb;
    private SpriteRenderer sprite;

    private void Awake()
    {
        healthSystem = PlayerHealthSystem.instance;
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponentInChildren<SpriteRenderer>();

       
           
    }

    private void Start()
    {
       
        if (PlayerHealthSystem.instance == null)
        {
            Debug.LogError("[RespawnSystem]  PlayerHealthSystem.instance is null! Make sure PlayerHealthSystem is in the scene.");
            return;
        }

        healthSystem = PlayerHealthSystem.instance;
        healthSystem.OnRequestRespawn += HandleRespawnRequest;
        healthSystem.OnPlayerDied += HealthSystem_OnPlayerDied;
        //Debug.Log("[RespawnSystem]  Subscribed to OnRequestRespawn event.");
    }

    private void HealthSystem_OnPlayerDied(Vector3 obj)
    {
        //SceneManager.LoadScene(0);
       // StartCoroutine(RespawnCoroutine(obj));
    }

    private void OnDisable()
    {
        healthSystem.OnRequestRespawn -= HandleRespawnRequest;
    }

    private void HandleRespawnRequest(Vector3 safePos, PlayerHealthSystem.DamageType type)
    {
        //Debug.Log("OnRespawnRequest Invoked");
        //Debug.Log($"[Respawn] Player died from {type}, teleporting to {safePos} after {respawnDelay}s");
        StartCoroutine(RespawnCoroutine(safePos));
    }

    private IEnumerator RespawnCoroutine(Vector3 safePos)
    {
        // stop player movement
        rb.velocity = Vector2.zero;
        rb.simulated = false;

        // fade out (optional)
        if (sprite != null)
        {
            float t = 0;
            Color c = sprite.color;
            while (t < fadeDuration)
            {
                sprite.color = new Color(c.r, c.g, c.b, Mathf.Lerp(1, 0, t / fadeDuration));
                t += Time.deltaTime;
                yield return null;
            }
            sprite.color = new Color(c.r, c.g, c.b, 0);
        }

        yield return new WaitForSeconds(respawnDelay);

        // move to safe spot
        transform.position = safePos;
        rb.simulated = true;
        rb.velocity = Vector2.zero;

        // fade in
        if (sprite != null)
        {
            float t = 0;
            Color c = sprite.color;
            while (t < fadeDuration)
            {
                sprite.color = new Color(c.r, c.g, c.b, Mathf.Lerp(0, 1, t / fadeDuration));
                t += Time.deltaTime;
                yield return null;
            }
            sprite.color = new Color(c.r, c.g, c.b, 1);
        }

        //Debug.Log($"[Respawn] Player respawned successfully at {safePos}");
    }
}
