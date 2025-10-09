using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("References")]
 
    [SerializeField] private Transform heartsContainer;
    [SerializeField] private Image heartPrefab;


    [Header("Colors")]
    [SerializeField] private Color fullColor = Color.red;
    [SerializeField] private Color emptyColor = new Color(0.2f, 0.2f, 0.2f, 0.4f);
    [SerializeField] private Color fillingColor = new Color(1f, 0.5f, 0.5f, 1f);

    private Image[] hearts;
    private PlayerHealthSystem healthSystem;
    private void Awake()
    {
        healthSystem = PlayerHealthSystem.instance;
        if (healthSystem == null)
        {
            healthSystem = FindObjectOfType<PlayerHealthSystem>();
        }
    }

    private void Start()
    {
        InitHearts();
        SubscribeToEvents();
        UpdateHeartsInstant(healthSystem.GetCurrentHearts());
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void InitHearts()
    {
        foreach (Transform child in heartsContainer)
            Destroy(child.gameObject);

        int max = 2; // default fallback
        if (healthSystem != null)
            max = Mathf.Max(healthSystem.GetCurrentHearts(), 2);

        hearts = new Image[max];
        for (int i = 0; i < max; i++)
        {
            var img = Instantiate(heartPrefab, heartsContainer);
            img.color = emptyColor;
            hearts[i] = img;
        }
    }

    private void SubscribeToEvents()
    {
        healthSystem.OnHealthChanged += HandleHealthChanged; // taghir
        healthSystem.OnHeartRegenProgress += HandleHeartRegenProgress; // por
        healthSystem.OnHeartRegenerated += HandleHeartRegenerated;
        healthSystem.OnPlayerDied += HandlePlayerDied;
    }

    private void UnsubscribeFromEvents()
    {
        healthSystem.OnHealthChanged -= HandleHealthChanged;
        healthSystem.OnHeartRegenProgress -= HandleHeartRegenProgress;
        healthSystem.OnHeartRegenerated -= HandleHeartRegenerated;
        healthSystem.OnPlayerDied -= HandlePlayerDied;
    }

    private void HandleHealthChanged(int heartsLeft, int core)
    {
        UpdateHeartsInstant(heartsLeft);
        PrintDebug($"HealthChanged  Shildes ={heartsLeft}, Core={core}");
    }

    private void HandleHeartRegenProgress(int index, float progress)
    {
        if (index - 1 < 0 || index - 1 >= hearts.Length) return;

        var img = hearts[index - 1];
        img.color = Color.Lerp(emptyColor, fillingColor, progress);
        PrintDebug($"HeartRegenProgress heart#{index} progress={progress:F2}");
    }

    private void HandleHeartRegenerated(int heartsCount)
    {
        UpdateHeartsInstant(heartsCount);
        PrintDebug($"HeartRegenerated  hearts={heartsCount}");
    }

    private void HandlePlayerDied(Vector3 pos)
    {
        PrintDebug($" Player Died! Respawn at {pos}");
    }

    private void UpdateHeartsInstant(int heartsLeft)
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            hearts[i].color = i < heartsLeft ? fullColor : emptyColor;
        }
    }

    private void PrintDebug(string msg)
    {

    }
}
