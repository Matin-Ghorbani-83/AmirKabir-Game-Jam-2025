using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerHealthSystem : MonoBehaviour
{
    public static PlayerHealthSystem instance { get; set; }
    // ---------- Config ----------
    [Header("Health")]
    [SerializeField] private int maxHearts = 2;       // visual hearts count (starts with 2)
    [SerializeField] private int coreHealth = 1;      // the player's own life (1 means alive, 0 dead)

    [Header("Damage Timing")]
    [Tooltip("Delay before applying shooter-collision damage (for hit cue).")]
    [SerializeField] private float shooterDamageDelay = 0.5f;
    [Tooltip("Invincibility duration after taking damage.")]
    [SerializeField] private float invincibilityDuration = 1.2f;

    [Header("Heart Regen")]
    [Tooltip("Delay until a heart starts regenerating when eligible.")]
    [SerializeField] private float heartRegenDelay = 15f;
    [Tooltip("Total time in seconds to visually fill a heart (split into steps).")]
    [SerializeField] private float heartFillDuration = 3f;
    [Tooltip("Number of visual steps to fill a heart (e.g. 3).")]
    [SerializeField] private int heartFillSteps = 3;

    [Header("Respawn")]
    [Tooltip("Layers considered 'safe platforms' to register last safe respawn position.")]
    [SerializeField] private LayerMask platformLayerMask;

    // ---------- State ----------
    private int currentHearts;
    private int currentCore; // 0..coreHealth initial
    private bool isInvincible = false;
    private bool isRegenRunning = false;

    private Vector3 lastSafeRespawnPos = Vector3.zero;

    // coroutine handles
    private Coroutine invincibleCoroutine;
    private Coroutine regenCoroutine;
    private Coroutine pendingDamageCoroutine = null;
    // ---------- Events (logic-only) ----------
    public enum DamageType { ShooterCollision, Projectile, Fall }

    /// <summary> Called immediately when a hit is detected (to play hit cue). Args: (type, hitPosition) </summary>
    public event Action<DamageType, Vector3> OnDamageTriggered;

    /// <summary> Called when damage actually applied: (type, heartsLeft, coreLeft) </summary>
    public event Action<DamageType, int, int> OnDamageApplied;

    /// <summary> Called when invincibility starts/stops </summary>
    public event Action<bool> OnInvincibilityChanged;

    /// <summary> Heart regen progress step: (heartIndex (1..maxHearts), progress 0..1) </summary>
    public event Action<int, float> OnHeartRegenProgress;

    /// <summary> A full heart was regenerated: (newHeartsCount) </summary>
    public event Action<int> OnHeartRegenerated;

    /// <summary> Health changed (hearts, core) </summary>
    public event Action<int, int> OnHealthChanged;

    /// <summary> Player died: supply lastSafeRespawnPos </summary>
    public event Action<Vector3> OnPlayerDied;

    /// <summary> Request a respawn to this position (listeners should handle actual teleport/reset) </summary>
    public event Action<Vector3, DamageType> OnRequestRespawn;

    // ---------- Unity callbacks ----------
    private void Awake()
    {
        instance = this;
        currentHearts = Mathf.Clamp(maxHearts, 0, maxHearts);
        currentCore = Mathf.Clamp(coreHealth, 0, coreHealth);
        Debug.Log($"[HealthSystem] Awake: hearts={currentHearts}, core={currentCore}");
        BroadcastHealth();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // process trigger collisions (projectiles or trigger-type enemies)
        ProcessCollision(other.gameObject, other.ClosestPoint(transform.position));
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        Vector2 point = Vector2.zero;
        if (col.contactCount > 0) point = col.GetContact(0).point;
        ProcessCollision(col.gameObject, point);
    }

    // ---------- Public helpers (call from PlayerController) ----------
    /// <summary> Call when player falls below level or is considered fallen. </summary>
    public void NotifyFallen()
    {
        Debug.Log("[HealthSystem] NotifyFallen called -> forcing Fall damage & respawn.");


        if (pendingDamageCoroutine != null)
        {
            StopCoroutine(pendingDamageCoroutine);
            pendingDamageCoroutine = null;
            Debug.Log("[HealthSystem] Pending damage coroutine stopped because of Fall.");
        }
        if (invincibleCoroutine != null)
        {
            StopCoroutine(invincibleCoroutine);
            invincibleCoroutine = null;
            isInvincible = false;
            OnInvincibilityChanged?.Invoke(false);
            Debug.Log("[HealthSystem] Invincibility canceled because of Fall.");
        }
        if (regenCoroutine != null)
        {
            StopCoroutine(regenCoroutine);
            regenCoroutine = null;
            isRegenRunning = false;
            Debug.Log("[HealthSystem] Heart regen canceled because of Fall.");
        }


        ApplyDamageImmediateForFall();
    }

    /// <summary> External systems (platforms) should call this to register last safe respawn pos. </summary>
    public void RegisterSafeRespawnPosition(Vector3 pos)
    {
        lastSafeRespawnPos = pos;

        Debug.Log($"[HealthSystem] RegisterSafeRespawnPosition: {pos}");
    }

    // ---------- Collision classification ----------
    private void ProcessCollision(GameObject other, Vector2 hitPoint)
    {
        if (other == gameObject) return;

        // platform registration (layer mask)
        if (((1 << other.layer) & platformLayerMask) != 0)
        {
            // register the player's current feet position as safe respawn (caller may override)
            RegisterSafeRespawnPosition(transform.position);
            return;
        }

        // check for shooter enemy (example: BirdEnemy marker component)
        if (other.TryGetComponent<BirdEnemy>(out BirdEnemy bird))
        {
            Debug.Log($"[HealthSystem] Collision with BirdEnemy detected at {hitPoint}. Scheduling shooter collision damage.");
            ApplyDamageInternal(DamageType.ShooterCollision, hitPoint, true, shooterDamageDelay, false);
            return;
        }

        // check for projectile
        if (other.TryGetComponent<Projectile>(out Projectile proj))
        {
            Debug.Log($"[HealthSystem] Collision with Projectile detected at {hitPoint}. Applying projectile damage.");
            ApplyDamageInternal(DamageType.Projectile, hitPoint, false, 0f, false);
            // optional: let projectile handle its destruction
            return;
        }

        // you can add more collision checks (spikes, traps...) here
        if (other.TryGetComponent<FallGround>(out FallGround fallGround))
        {
            NotifyFallen();
            return;
        }
    }

    // ---------- Central damage application pipeline ----------
    private void ApplyDamageInternal(DamageType type, Vector3 hitPoint, bool shouldRespawn, float delay, bool ignoreInvincibility)
    {
        if (isInvincible && !ignoreInvincibility)
        {
            Debug.Log($"[HealthSystem] Damage ignored because player is invincible. Type={type}");
            return;
        }

        Debug.Log($"[HealthSystem] OnDamageTriggered -> Type={type}, HitPoint={hitPoint}, Delay={delay}");
        OnDamageTriggered?.Invoke(type, hitPoint);

        if (pendingDamageCoroutine != null)
        {
            Debug.Log("[HealthSystem] Canceling previously pending damage coroutine.");
            StopCoroutine(pendingDamageCoroutine);
            pendingDamageCoroutine = null;
        }

        pendingDamageCoroutine = StartCoroutine(ApplyDamageDelayedCoroutine(delay, type, hitPoint, shouldRespawn));


    }

    private IEnumerator ApplyDamageDelayedCoroutine(float delay, DamageType type, Vector3 hitPoint, bool shouldRespawn)
    {
        if (delay > 0f)
        {
            Debug.Log($"[HealthSystem] Waiting {delay}s before applying damage (type={type})");
            yield return new WaitForSeconds(delay);
        }
        // clear pending ref (we're executing now)
        pendingDamageCoroutine = null;

        if (isInvincible)
        {
            Debug.Log($"[HealthSystem] During delay player became invincible -> cancel damage (type={type})");
            yield break;
        }
        // apply damage: hearts first, then core
        if (currentHearts > 0)
        {
            currentHearts = Mathf.Max(0, currentHearts - 1);
            Debug.Log($"[HealthSystem] DamageApplied: consumed 1 heart. heartsLeft={currentHearts}, core={currentCore}");
        }
        else
        {
            currentCore = Mathf.Max(0, currentCore - 1);
            Debug.Log($"[HealthSystem] DamageApplied: no hearts left -> core damaged. hearts={currentHearts}, coreLeft={currentCore}");
        }

        OnDamageApplied?.Invoke(type, currentHearts, currentCore);
        BroadcastHealth();

        // If hearts become 0 and coreHealth > 0, start regen timer
        if (currentHearts == 0 && currentCore > 0 && !isRegenRunning)
        {
            Debug.Log("[HealthSystem] Hearts exhausted and core still alive -> starting heart regen coroutine.");
            regenCoroutine = StartCoroutine(HeartRegenCoroutine());
        }

        // start invincibility (common after any applied damage)
        StartInvincibility(invincibilityDuration);

        if (shouldRespawn)
        {
            OnRequestRespawn?.Invoke(lastSafeRespawnPos, type);
        }

        // death handling
        if (currentCore <= 0)
        {
            //  Debug.Log($"[HealthSystem] Player died. Requesting respawn to lastSafeRespawnPos={lastSafeRespawnPos}");
            OnPlayerDied?.Invoke(lastSafeRespawnPos);
            //if (shouldRespawn)
            //{
            //    OnRequestRespawn?.Invoke(lastSafeRespawnPos, type);
            //}
        }
    }

    private void ApplyDamageImmediateForFall()
    {
        // apply fall damage ignoring invincibility and bypassing delay (hearts first then core)
        Debug.Log("[HealthSystem] Applying immediate FALL damage (bypass invincibility).");

        // apply damage: hearts first, then core
        if (currentHearts > 0)
        {
            currentHearts = Mathf.Max(0, currentHearts - 1);
        }
        else
        {
            currentCore = Mathf.Max(0, currentCore - 1);
        }

        OnDamageApplied?.Invoke(DamageType.Fall, currentHearts, currentCore);
        BroadcastHealth();

        // start invincibility as normal (so player won't immediately be re-damaged)
        StartInvincibility(invincibilityDuration);

        // If hearts exhausted and coreHealth > 0, start regen timer
        if (currentHearts == 0 && currentCore > 0 && !isRegenRunning)
        {
            regenCoroutine = StartCoroutine(HeartRegenCoroutine());
        }
        OnRequestRespawn?.Invoke(lastSafeRespawnPos, DamageType.Fall);
        // death handling
        if (currentCore <= 0)
        {
            Debug.Log($"[HealthSystem] Player died by FALL. Requesting respawn to lastSafeRespawnPos={lastSafeRespawnPos}");
            OnPlayerDied?.Invoke(lastSafeRespawnPos);

        }
    }

    // ---------- Invincibility ----------
    private void StartInvincibility(float duration)
    {
        if (invincibleCoroutine != null)
        {
            StopCoroutine(invincibleCoroutine);
            invincibleCoroutine = null;
        }
        invincibleCoroutine = StartCoroutine(InvincibleCoroutine(duration));
    }

    private IEnumerator InvincibleCoroutine(float duration)
    {
        isInvincible = true;
        Debug.Log($"[HealthSystem] Invincibility START for {duration}s");
        OnInvincibilityChanged?.Invoke(true);

        yield return new WaitForSeconds(duration);

        isInvincible = false;
        Debug.Log("[HealthSystem] Invincibility END");
        OnInvincibilityChanged?.Invoke(false);

        invincibleCoroutine = null;
    }

    // ---------- Heart regen ----------
    private IEnumerator HeartRegenCoroutine()
    {
        isRegenRunning = true;
        Debug.Log($"[HealthSystem] HeartRegen: waiting {heartRegenDelay}s before starting visual fill.");

        float t = 0f;
        while (t < heartRegenDelay)
        {
            // cancel conditions
            if (currentHearts > 0)
            {
                Debug.Log("[HealthSystem] HeartRegen cancelled: player gained a heart before regen finished.");
                isRegenRunning = false;
                regenCoroutine = null;
                yield break;
            }
            if (currentCore <= 0)
            {
                Debug.Log("[HealthSystem] HeartRegen cancelled: player dead before regen.");
                isRegenRunning = false;
                regenCoroutine = null;
                yield break;
            }

            t += Time.deltaTime;
            yield return null;
        }

        Debug.Log("[HealthSystem] HeartRegen: starting visual fill steps.");
        // visual gradual fill
        int heartIndex = Mathf.Clamp(currentHearts + 1, 1, maxHearts);
        int steps = Mathf.Max(1, heartFillSteps);
        float stepDur = Mathf.Max(0.01f, heartFillDuration / steps);

        for (int s = 1; s <= steps; s++)
        {
            float progress = (float)s / (float)steps;
            Debug.Log($"[HealthSystem] HeartRegen progress: heart#{heartIndex} progress={progress:F2}");
            OnHeartRegenProgress?.Invoke(heartIndex, progress);
            yield return new WaitForSeconds(stepDur);

            // if player got damaged during fill, cancel
            if (currentHearts > 0 || currentCore <= 0)
            {
                Debug.Log("[HealthSystem] HeartRegen aborted during fill (state changed).");
                isRegenRunning = false;
                regenCoroutine = null;
                yield break;
            }
        }

        // grant heart
        currentHearts = Mathf.Min(maxHearts, currentHearts + 1);
        Debug.Log($"[HealthSystem] HeartRegen complete -> new hearts={currentHearts}");
        OnHeartRegenerated?.Invoke(currentHearts);
        BroadcastHealth();

        isRegenRunning = false;
        regenCoroutine = null;
    }

    // ---------- Helpers ----------
    private void BroadcastHealth()
    {
        Debug.Log($"[HealthSystem] BroadcastHealth => hearts={currentHearts}, core={currentCore}");
        OnHealthChanged?.Invoke(currentHearts, currentCore);
    }

    // ---------- Debug/Dev utilities ----------
#if UNITY_EDITOR
    [ContextMenu("Debug: Apply Projectile Damage")]
    public void Debug_ApplyProjectileDamage() => ApplyDamageInternal(DamageType.Projectile, transform.position, false, 0f, false);

    [ContextMenu("Debug: Apply Shooter Collision (delayed)")]
    public void Debug_ApplyShooterCollision() => ApplyDamageInternal(DamageType.ShooterCollision, transform.position, true, shooterDamageDelay, false);

    [ContextMenu("Debug: Force Fall Damage")]
    public void Debug_ApplyFallDamage() => ApplyDamageInternal(DamageType.Fall, transform.position, true, 0f, true);

    [ContextMenu("Debug: Print Internal State")]
    public void Debug_PrintState()
    {
        Debug.Log($"[HealthSystem Debug] hearts={currentHearts}/{maxHearts}, core={currentCore}/{coreHealth}, invincible={isInvincible}, regenRunning={isRegenRunning}, lastSafePos={lastSafeRespawnPos}");
    }
#endif

    // Expose read-only state for other scripts
    public int GetCurrentHearts() => currentHearts;
    public int GetCoreHealth() => currentCore;
    public bool IsInvincible() => isInvincible;
    public bool IsRegenRunning() => isRegenRunning;
}
