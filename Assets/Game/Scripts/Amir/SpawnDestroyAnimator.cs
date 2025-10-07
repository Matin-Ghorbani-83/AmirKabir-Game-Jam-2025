using System.Collections;
using UnityEngine;

/// <summary>
/// Attach to prefab. Supports:
/// - PlaySpawn() : trigger "Spawn" or fallback single scale-in (same as before).
/// - BeginPreDestroy() : start a continuous pre-destroy visual (Animator trigger or fallback looping pulse/shake).
/// - EndPreDestroy() : stop the continuous pre-destroy visual (restore original transform).
/// - PlayDestroy() : trigger "Destroy" and wait for its completion (or fallback scale-out).
/// 
/// Important: BeginPreDestroy does NOT wait; it begins the visual. Call EndPreDestroy before PlayDestroy to stop it.
/// </summary>
public class SpawnDestroyAnimator : MonoBehaviour
{
    [Header("Animator (optional)")]
    public Animator animator;                // optional (can be set in inspector or auto-found)
    public string spawnTrigger = "Spawn";
    public string preDestroyTrigger = "PreDestroy";
    public string destroyTrigger = "Destroy";
    public string spawnStateName = "SpawnState";      // used by PlaySpawn when waitForAnimator = true
    public string destroyStateName = "DestroyState";  // used by PlayDestroy when waitForAnimator = true
    public int animatorLayer = 0;
    public bool waitForAnimator = true;      // only used by PlaySpawn / PlayDestroy to wait for animator states
    public float maxWaitTimeForAnimator = 4f;

    [Header("Fallback scale/pulse settings (no color changes)")]
    public float spawnScaleDuration = 0.28f;
    public float destroyScaleDuration = 0.28f;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Fallback pre-destroy loop (when no Animator)")]
    public float preDestroyPulseRate = 6f;    // oscillations per second
    public float preDestroyPulseAmount = 0.08f; // relative scale amount
    public float preDestroyShakeAmount = 0.06f; // optional position shake amplitude

    // internal
    private Coroutine preDestroyLoopCoroutine = null;
    private Vector3 savedLocalScale;
    private Vector3 savedLocalPosition;
    private bool hasSavedTransform = false;

    void Reset()
    {
        if (animator == null) animator = GetComponent<Animator>();
    }

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
    }

    #region Spawn / Destroy (existing behavior)
    public IEnumerator PlaySpawn()
    {
        if (!hasSavedTransform)
        {
            savedLocalScale = transform.localScale;
            savedLocalPosition = transform.localPosition;
            hasSavedTransform = true;
        }

        if (animator != null && waitForAnimator && !string.IsNullOrEmpty(spawnStateName))
        {
            animator.ResetTrigger(destroyTrigger);
            animator.SetTrigger(spawnTrigger);

            float timer = 0f;
            bool entered = false;
            while (timer < maxWaitTimeForAnimator)
            {
                var info = animator.GetCurrentAnimatorStateInfo(animatorLayer);
                if (IsStateMatch(info, spawnStateName)) { entered = true; break; }
                timer += Time.deltaTime;
                yield return null;
            }
            if (!entered) { yield return new WaitForSeconds(Mathf.Min(spawnScaleDuration, maxWaitTimeForAnimator)); yield break; }

            timer = 0f;
            while (timer < maxWaitTimeForAnimator)
            {
                var info = animator.GetCurrentAnimatorStateInfo(animatorLayer);
                if (!IsStateMatch(info, spawnStateName)) yield break;
                if (info.normalizedTime >= 1f) yield break;
                timer += Time.deltaTime;
                yield return null;
            }
            yield break;
        }

        yield return StartCoroutine(ScaleInFallback());
    }

    public IEnumerator PlayDestroy()
    {
        // Ensure pre-destroy loop has been stopped by caller (EndPreDestroy) if using fallback.
        if (animator != null && waitForAnimator && !string.IsNullOrEmpty(destroyStateName))
        {
            animator.ResetTrigger(preDestroyTrigger);
            animator.SetTrigger(destroyTrigger);

            float timer = 0f;
            bool entered = false;
            while (timer < maxWaitTimeForAnimator)
            {
                var info = animator.GetCurrentAnimatorStateInfo(animatorLayer);
                if (IsStateMatch(info, destroyStateName)) { entered = true; break; }
                timer += Time.deltaTime;
                yield return null;
            }
            if (!entered) { yield return new WaitForSeconds(Mathf.Min(destroyScaleDuration, maxWaitTimeForAnimator)); yield break; }

            timer = 0f;
            while (timer < maxWaitTimeForAnimator)
            {
                var info = animator.GetCurrentAnimatorStateInfo(animatorLayer);
                if (!IsStateMatch(info, destroyStateName)) yield break;
                if (info.normalizedTime >= 1f) yield break;
                timer += Time.deltaTime;
                yield return null;
            }
            yield break;
        }

        yield return StartCoroutine(ScaleOutFallback());
    }
    #endregion

    #region Pre-destroy begin / end (new)
    /// <summary>
    /// Start a continuous pre-destroy visual.
    /// - If Animator exists: sets preDestroyTrigger (does NOT wait).
    /// - Else: starts a looping scale+optional shake coroutine until EndPreDestroy() is called.
    /// </summary>
    public void BeginPreDestroy()
    {
        if (!hasSavedTransform)
        {
            savedLocalScale = transform.localScale;
            savedLocalPosition = transform.localPosition;
            hasSavedTransform = true;
        }

        if (animator != null)
        {
            // Just fire the trigger; animator should transition into a looping pre-destroy state if you set it up.
            animator.ResetTrigger(destroyTrigger);
            animator.SetTrigger(preDestroyTrigger);
            // Do not wait here — the animator should visually loop until Destroy is triggered.
            return;
        }

        // fallback: start continuous pulse/shake
        if (preDestroyLoopCoroutine == null)
            preDestroyLoopCoroutine = StartCoroutine(PreDestroyLoopCoroutine());
    }

    /// <summary>
    /// Stop the continuous pre-destroy visual and restore transform.
    /// Call this before PlayDestroy() if you want fallback visuals to cease.
    /// </summary>
    public void EndPreDestroy()
    {
        if (animator != null)
        {
            // Optionally reset preDestroy trigger so animator transitions elsewhere on Destroy
            animator.ResetTrigger(preDestroyTrigger);
            return;
        }

        if (preDestroyLoopCoroutine != null)
        {
            StopCoroutine(preDestroyLoopCoroutine);
            preDestroyLoopCoroutine = null;
        }

        // restore original transform values (in case pulse/shake modified them)
        if (hasSavedTransform)
        {
            transform.localScale = savedLocalScale;
            transform.localPosition = savedLocalPosition;
        }
    }

    private IEnumerator PreDestroyLoopCoroutine()
    {
        // continuous oscillation until stopped
        Vector3 baseScale = transform.localScale;
        Vector3 basePos = transform.localPosition;
        float elapsed = 0f;

        while (true)
        {
            elapsed += Time.deltaTime;
            float phase = Mathf.Sin(elapsed * preDestroyPulseRate * Mathf.PI * 2f);
            float scaleOffset = phase * preDestroyPulseAmount;
            transform.localScale = baseScale * (1f + scaleOffset);

            // gentle positional shake (optional)
            float shake = Mathf.PerlinNoise(Time.time * 8f, 0f) * 2f - 1f; // -1..1
            transform.localPosition = basePos + Vector3.up * (shake * preDestroyShakeAmount);

            yield return null;
        }
    }
    #endregion

    #region Fallback helpers
    private IEnumerator ScaleInFallback()
    {
        Vector3 target = transform.localScale;
        transform.localScale = Vector3.zero;
        float t = 0f;
        while (t < spawnScaleDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / spawnScaleDuration);
            transform.localScale = target * scaleCurve.Evaluate(p);
            yield return null;
        }
        transform.localScale = target;
    }

    private IEnumerator ScaleOutFallback()
    {
        Vector3 initial = transform.localScale;
        float t = 0f;
        while (t < destroyScaleDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / destroyScaleDuration);
            float s = 1f - scaleCurve.Evaluate(p);
            transform.localScale = initial * s;
            yield return null;
        }
        transform.localScale = Vector3.zero;
    }
    #endregion

    private bool IsStateMatch(AnimatorStateInfo info, string stateName)
    {
        if (string.IsNullOrEmpty(stateName)) return false;
        int hash = Animator.StringToHash(stateName);
        if (info.fullPathHash == hash || info.shortNameHash == hash) return true;
        if (info.IsName(stateName)) return true;
        return false;
    }
}
