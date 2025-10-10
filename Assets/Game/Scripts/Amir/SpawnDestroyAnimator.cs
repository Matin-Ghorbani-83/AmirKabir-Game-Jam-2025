using System.Collections;
using UnityEngine;

/// <summary>
/// Spawn = Scale-In (fallback)
/// PreDestroy = Pulse/Shake (fallback)
/// Destroy = Animator (اگر Animator ست باشه، وگرنه fallback Scale-Out)
/// </summary>
public class SpawnDestroyAnimator : MonoBehaviour
{
    [Header("Animator (optional)")]
    public Animator animator;
    public string destroyTrigger = "Destroy";
    public string destroyStateName = "DestroyState";
    public int animatorLayer = 0;
    public bool waitForAnimator = true;
    public float maxWaitTimeForAnimator = 4f;

    [Header("Fallback scale/pulse settings")]
    public float spawnScaleDuration = 0.28f;
    public float destroyScaleDuration = 0.28f;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Fallback pre-destroy loop")]
    public float preDestroyPulseRate = 6f;
    public float preDestroyPulseAmount = 0.08f;
    public float preDestroyShakeAmount = 0.06f;

    // internal
    private Coroutine preDestroyLoopCoroutine = null;
    private Vector3 savedLocalScale;
    private Vector3 savedLocalPosition;
    private bool hasSavedTransform = false;

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
    }

    // -------- SPAWN --------
    public IEnumerator PlaySpawn()
    {
        EnsureSavedTransform();
        yield return StartCoroutine(ScaleInFallback());
    }

    // -------- PREDESTROY --------
    public void BeginPreDestroy()
    {
        EnsureSavedTransform();
        if (preDestroyLoopCoroutine == null)
            preDestroyLoopCoroutine = StartCoroutine(PreDestroyLoopCoroutine());
    }

    public void EndPreDestroy()
    {
        if (preDestroyLoopCoroutine != null)
        {
            StopCoroutine(preDestroyLoopCoroutine);
            preDestroyLoopCoroutine = null;
        }

        if (hasSavedTransform)
        {
            transform.localScale = savedLocalScale;
            transform.localPosition = savedLocalPosition;
        }
    }

    private IEnumerator PreDestroyLoopCoroutine()
    {
        Vector3 baseScale = transform.localScale;
        Vector3 basePos = transform.localPosition;
        float elapsed = 0f;

        while (true)
        {
            elapsed += Time.deltaTime;

            // scale pulse
            float phase = Mathf.Sin(elapsed * preDestroyPulseRate * Mathf.PI * 2f);
            float scaleOffset = phase * preDestroyPulseAmount;
            transform.localScale = baseScale * (1f + scaleOffset);

            // subtle vertical shake
            float shake = Mathf.PerlinNoise(Time.time * 8f, 0f) * 2f - 1f;
            transform.localPosition = basePos + Vector3.up * (shake * preDestroyShakeAmount);

            yield return null;
        }
    }

    // -------- DESTROY --------
    public IEnumerator PlayDestroy()
    {
        if (animator != null && waitForAnimator && !string.IsNullOrEmpty(destroyStateName))
        {
            animator.SetTrigger(destroyTrigger);

            // wait enter
            float timer = 0f;
            bool entered = false;
            while (timer < maxWaitTimeForAnimator)
            {
                var info = animator.GetCurrentAnimatorStateInfo(animatorLayer);
                if (IsStateMatch(info, destroyStateName)) { entered = true; break; }
                timer += Time.deltaTime;
                yield return null;
            }
            if (!entered) yield break;

            // wait exit
            timer = 0f;
            while (timer < maxWaitTimeForAnimator)
            {
                var info = animator.GetCurrentAnimatorStateInfo(animatorLayer);
                if (!IsStateMatch(info, destroyStateName) || info.normalizedTime >= 1f) yield break;
                timer += Time.deltaTime;
                yield return null;
            }
            yield break;
        }

        // fallback if no animator
        yield return StartCoroutine(ScaleOutFallback());
    }

    // -------- FALLBACK HELPERS --------
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

    private bool IsStateMatch(AnimatorStateInfo info, string stateName)
    {
        if (string.IsNullOrEmpty(stateName)) return false;
        return info.IsName(stateName);
    }

    private void EnsureSavedTransform()
    {
        if (hasSavedTransform) return;
        savedLocalScale = transform.localScale;
        savedLocalPosition = transform.localPosition;
        hasSavedTransform = true;
    }
}
