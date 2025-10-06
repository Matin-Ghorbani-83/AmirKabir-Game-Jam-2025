using System.Collections;
using UnityEngine;

/// <summary>
/// Attach to prefab. Does:
/// - PlaySpawn() : trigger "Spawn" or fallback scale-in.
/// - PlayPreDestroy() : trigger "PreDestroy" (or fallback small shake/scale pulse) and wait until finished.
/// - PlayDestroy() : trigger "Destroy" or fallback scale-out.
/// This version NEVER changes colors; all visual hints should be done in Animator states.
/// </summary>
public class SpawnDestroyAnimator : MonoBehaviour
{
    [Header("Animator (optional)")]
    public Animator animator;                // optional (can be set in inspector or auto-found)
    public string spawnTrigger = "Spawn";
    public string preDestroyTrigger = "PreDestroy";
    public string destroyTrigger = "Destroy";
    public string spawnStateName = "SpawnState";
    public string preDestroyStateName = "PreDestroyState";
    public string destroyStateName = "DestroyState";
    public int animatorLayer = 0;
    public bool waitForAnimator = true;

    [Header("Fallback timings (when no Animator or can't detect states)")]
    public float spawnScaleDuration = 0.28f;
    public float preDestroyFallbackDuration = 2f; // default 2s warning
    public float destroyScaleDuration = 0.28f;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Safety")]
    public float maxWaitTimeForAnimator = 4f;

    void Reset()
    {
        if (animator == null) animator = GetComponent<Animator>();
    }

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
    }

    public IEnumerator PlaySpawn()
    {
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

    // NEW: Pre-destroy visual hint. Waits until pre-destroy animation/state ends (or fallback duration)
    public IEnumerator PlayPreDestroy()
    {
        if (animator != null && waitForAnimator && !string.IsNullOrEmpty(preDestroyStateName))
        {
            animator.ResetTrigger(spawnTrigger);
            animator.SetTrigger(preDestroyTrigger);

            float timer = 0f;
            bool entered = false;
            while (timer < maxWaitTimeForAnimator)
            {
                var info = animator.GetCurrentAnimatorStateInfo(animatorLayer);
                if (IsStateMatch(info, preDestroyStateName)) { entered = true; break; }
                timer += Time.deltaTime;
                yield return null;
            }

            if (!entered)
            {
                // fallback wait short time rather than blocking forever
                float fallback = Mathf.Min(preDestroyFallbackDuration, maxWaitTimeForAnimator);
                yield return new WaitForSeconds(fallback);
                yield break;
            }

            timer = 0f;
            while (timer < maxWaitTimeForAnimator)
            {
                var info = animator.GetCurrentAnimatorStateInfo(animatorLayer);
                if (!IsStateMatch(info, preDestroyStateName)) yield break;
                if (info.normalizedTime >= 1f) yield break;
                timer += Time.deltaTime;
                yield return null;
            }

            yield break;
        }

        // Fallback: a simple pulse/shake effect implemented via coroutine (scale pulse)
        yield return StartCoroutine(PreDestroyFallbackPulse(preDestroyFallbackDuration));
    }

    public IEnumerator PlayDestroy()
    {
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

    // A gentle pulse for the warning phase (no color changes). Scales slightly up/down to indicate "about to die".
    private IEnumerator PreDestroyFallbackPulse(float duration)
    {
        if (duration <= 0f) yield break;
        Vector3 baseScale = transform.localScale;
        float elapsed = 0f;
        float pulseRate = 6f; // how many oscillations per second (feel free to tweak)
        float pulseAmount = 0.08f; // relative scale amount

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float phase = Mathf.Sin(elapsed * pulseRate * Mathf.PI * 2f) * pulseAmount;
            transform.localScale = baseScale * (1f + phase);
            yield return null;
        }

        // restore original scale
        transform.localScale = baseScale;
    }

    // utility: checks if AnimatorStateInfo corresponds to provided state name
    private bool IsStateMatch(AnimatorStateInfo info, string stateName)
    {
        if (string.IsNullOrEmpty(stateName)) return false;
        int hash = Animator.StringToHash(stateName);
        if (info.fullPathHash == hash || info.shortNameHash == hash) return true;
        if (info.IsName(stateName)) return true;
        return false;
    }
}
