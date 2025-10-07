using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SpawnGroup
{
    public string groupName;
    public Transform[] points;
}

[System.Serializable]
public class PrefabSpawnRule
{
    public GameObject prefab;
    public int[] allowedGroupIndices;
}

[System.Serializable]
public class SpawnExclusionRule
{
    public Transform sourceTransform;
    public Transform[] excludeNext;
}

[System.Serializable]
public class OccupancyExclusionRule
{
    public Transform sourceTransform;
    public Transform[] excludeWhenOccupied;
}

public class SpawnCycleManager : MonoBehaviour
{
    [Header("Prefabs & Spawn Points (fallback)")]
    public GameObject[] prefabs;
    public Transform[] spawnPoints;

    [Header("Spawn Groups (optional)")]
    public SpawnGroup[] spawnGroups;
    public int defaultGroupIndex = 0;

    [Header("Per-prefab rules (optional)")]
    public PrefabSpawnRule[] prefabSpawnRules;

    [Header("Initial-only spawn points (used only at Start)")]
    public Transform[] initialOnlySpawnPoints;
    public bool useInitialOnlySpawnPoints = true;

    [Header("Pre-destroy warning")]
    [Tooltip("How many seconds BEFORE the actual destroy the pre-destroy visual should run.")]
    public float preDestroyWarningTime = 2f;

    [Header("Exclusion rules (after an object was spawned at sourceTransform, exclude excludeNext for next spawn)")]
    public SpawnExclusionRule[] exclusionRules;

    [Header("Occupancy-based exclusions (if sourceTransform is currently occupied, exclude these next)")]
    public OccupancyExclusionRule[] occupancyExclusions;

    [Header("Policy when exclusions remove all free candidates")]
    public NoCandidatePolicy noCandidatePolicy = NoCandidatePolicy.PickAny;
    public enum NoCandidatePolicy { PickAny, AllowExcluded, WaitForFree }

    [Tooltip("If WaitForFree selected: how long (sec) to wait before falling back. 0 = wait indefinitely.")]
    public float waitForFreeTimeout = 3f;

    [Header("Timing (seconds)")]
    public float initialDelay = 1f;
    public float minDestroyInterval = 3f;
    public float maxDestroyInterval = 6f;
    public float respawnDelay = 2f;

    // internal state for active instances
    private class ActiveEntry
    {
        public GameObject obj;
        public Transform spawnTransform;
        public GameObject prefabUsed;
    }
    private List<ActiveEntry> active = new List<ActiveEntry>();

    void Start()
    {
        if ((spawnGroups == null || spawnGroups.Length == 0)
            && (spawnPoints == null || spawnPoints.Length == 0)
            && (initialOnlySpawnPoints == null || initialOnlySpawnPoints.Length == 0))
        {
            Debug.LogError("[SpawnCycleManager] No spawn points/groups/initial-only points assigned!");
            return;
        }

        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogError("[SpawnCycleManager] No prefabs assigned!");
            return;
        }

        SpawnInitialTwo();
        StartCoroutine(CycleRoutine());
    }

    // ---------- initial spawn ----------
    private void SpawnInitialTwo()
    {
        List<Transform> allTransforms = GatherAllSpawnTransforms();
        if (allTransforms.Count == 0) { Debug.LogError("[SpawnCycleManager] No spawn transforms available!"); return; }

        List<Transform> chosen = new List<Transform>();

        if (useInitialOnlySpawnPoints && initialOnlySpawnPoints != null && initialOnlySpawnPoints.Length > 0)
        {
            List<Transform> initialValid = new List<Transform>();
            foreach (var t in initialOnlySpawnPoints) if (t != null) initialValid.Add(t);
            List<Transform> temp = new List<Transform>(initialValid);
            for (int i = 0; i < 2 && temp.Count > 0; i++)
            {
                int r = UnityEngine.Random.Range(0, temp.Count);
                chosen.Add(temp[r]);
                temp.RemoveAt(r);
            }
        }

        if (chosen.Count < 2)
        {
            List<Transform> available = new List<Transform>(allTransforms);
            available.RemoveAll(t => chosen.Contains(t));
            while (chosen.Count < 2 && available.Count > 0)
            {
                int r = UnityEngine.Random.Range(0, available.Count);
                chosen.Add(available[r]);
                available.RemoveAt(r);
            }
        }

        while (chosen.Count < 2)
            chosen.Add(allTransforms[UnityEngine.Random.Range(0, allTransforms.Count)]);

        SpawnAtTransform(chosen[0]);
        SpawnAtTransform(chosen[1]);
    }

    private void SpawnAtTransform(Transform t)
    {
        GameObject prefab = prefabs[UnityEngine.Random.Range(0, prefabs.Length)];
        GameObject go = Instantiate(prefab, t.position, t.rotation);
        var sda = go.GetComponent<SpawnDestroyAnimator>();
        if (sda != null) StartCoroutine(sda.PlaySpawn());
        else StartCoroutine(SimpleScaleIn(go, 0.28f));
        active.Add(new ActiveEntry { obj = go, spawnTransform = t, prefabUsed = prefab });
    }

    // ---------- main loop ----------
    private IEnumerator CycleRoutine()
    {
        yield return new WaitForSeconds(initialDelay);

        while (true)
        {
            if (active.Count == 0) { SpawnInitialTwo(); yield return null; continue; }

            float waitBeforeDestroy = UnityEngine.Random.Range(minDestroyInterval, maxDestroyInterval);

            // wait until pre-destroy should start
            float timeUntilPre = Mathf.Max(waitBeforeDestroy - preDestroyWarningTime, 0f);
            if (timeUntilPre > 0f) yield return new WaitForSeconds(timeUntilPre);

            // select which active to destroy
            int chosenToDestroy = (active.Count == 1) ? 0 : UnityEngine.Random.Range(0, active.Count);
            ActiveEntry toDestroy = active[chosenToDestroy];

            // START pre-destroy (begin continuous visual) - do NOT wait here
            if (toDestroy.obj != null)
            {
                var sda = toDestroy.obj.GetComponent<SpawnDestroyAnimator>();
                if (sda != null)
                {
                    sda.BeginPreDestroy(); // start continuous pre-destroy visual (looping)
                }
                else
                {
                    // if no SpawnDestroyAnimator, use local pulse coroutine (non-blocking)
                    // start a coroutine that pulses until we stop it right before destruction
                    StartCoroutine(LocalBeginPreDestroyFallback(toDestroy.obj));
                }
            }

            // wait remaining time until actual destroy
            float remaining = waitBeforeDestroy - timeUntilPre;
            if (remaining > 0f) yield return new WaitForSeconds(remaining);

            // NOW cut pre-destroy visual and play destroy animation (wait for it)
            if (toDestroy.obj != null)
            {
                var sda2 = toDestroy.obj.GetComponent<SpawnDestroyAnimator>();
                if (sda2 != null)
                {
                    // stop the continuous visual then run destroy animation (PlayDestroy waits)
                    sda2.EndPreDestroy();
                    yield return StartCoroutine(sda2.PlayDestroy());
                    Destroy(toDestroy.obj);
                }
                else
                {
                    // stop local fallback pulse
                    yield return StartCoroutine(LocalEndPreDestroyFallbackAndDestroy(toDestroy.obj, 0.28f));
                }
            }

            // remember transform freed
            Transform justFreed = toDestroy.spawnTransform;
            active.RemoveAt(chosenToDestroy);

            // wait before respawn
            yield return new WaitForSeconds(respawnDelay);

            // choose next spawn transform (may involve waiting if policy=WaitForFree)
            Transform chosenSpawn = null;
            yield return StartCoroutine(ChooseSpawnTransformAfterDestroyCoroutine(toDestroy.prefabUsed, justFreed, (t) => chosenSpawn = t));

            // spawn new object there
            GameObject prefabToSpawn = prefabs[UnityEngine.Random.Range(0, prefabs.Length)];
            GameObject newGo = Instantiate(prefabToSpawn, chosenSpawn.position, chosenSpawn.rotation);
            var animComp = newGo.GetComponent<SpawnDestroyAnimator>();
            if (animComp != null) StartCoroutine(animComp.PlaySpawn());
            else StartCoroutine(SimpleScaleIn(newGo, 0.28f));
            active.Add(new ActiveEntry { obj = newGo, spawnTransform = chosenSpawn, prefabUsed = prefabToSpawn });
        }
    }

    // ---------- Choose spawn coroutine (same logic as قبلی, but coroutine for WaitForFree) ----------
    private IEnumerator ChooseSpawnTransformAfterDestroyCoroutine(GameObject destroyedPrefab, Transform excludeTransform, Action<Transform> onChosen)
    {
        List<Transform> candidates = new List<Transform>();

        PrefabSpawnRule matchedPrefabRule = null;
        if (prefabSpawnRules != null && prefabSpawnRules.Length > 0 && destroyedPrefab != null)
        {
            foreach (var r in prefabSpawnRules) if (r != null && r.prefab == destroyedPrefab) { matchedPrefabRule = r; break; }
        }

        if (matchedPrefabRule != null && spawnGroups != null && spawnGroups.Length > 0)
        {
            foreach (int gi in matchedPrefabRule.allowedGroupIndices)
                if (gi >= 0 && gi < spawnGroups.Length)
                {
                    var g = spawnGroups[gi];
                    if (g != null && g.points != null)
                        foreach (var t in g.points) if (t != null && !candidates.Contains(t)) candidates.Add(t);
                }
        }
        else if (spawnGroups != null && spawnGroups.Length > 0)
        {
            if (defaultGroupIndex >= 0 && defaultGroupIndex < spawnGroups.Length)
            {
                var g = spawnGroups[defaultGroupIndex];
                if (g != null && g.points != null)
                    foreach (var t in g.points) if (t != null && !candidates.Contains(t)) candidates.Add(t);
            }
            else
            {
                foreach (var g in spawnGroups)
                    if (g != null && g.points != null)
                        foreach (var t in g.points) if (t != null && !candidates.Contains(t)) candidates.Add(t);
            }
        }
        else
        {
            if (spawnPoints != null)
                foreach (var t in spawnPoints) if (t != null && !candidates.Contains(t)) candidates.Add(t);
        }

        // include initialOnlySpawnPoints in pool
        if (initialOnlySpawnPoints != null)
            foreach (var t in initialOnlySpawnPoints) if (t != null && !candidates.Contains(t)) candidates.Add(t);

        if (candidates.Count == 0)
        {
            onChosen(GetRandomAnySpawnTransform());
            yield break;
        }

        // build exclusion set: excludeTransform + exclusionRules[source] + occupancyExclusions (current)
        HashSet<Transform> excluded = new HashSet<Transform>();
        if (excludeTransform != null) excluded.Add(excludeTransform);

        if (exclusionRules != null && exclusionRules.Length > 0 && excludeTransform != null)
        {
            foreach (var rule in exclusionRules)
            {
                if (rule == null || rule.sourceTransform == null) continue;
                if (rule.sourceTransform == excludeTransform)
                {
                    if (rule.excludeNext != null)
                        foreach (var ex in rule.excludeNext) if (ex != null) excluded.Add(ex);
                }
            }
        }

        if (occupancyExclusions != null && occupancyExclusions.Length > 0)
        {
            HashSet<Transform> currentlyOccupied = new HashSet<Transform>();
            foreach (var a in active) if (a.spawnTransform != null) currentlyOccupied.Add(a.spawnTransform);

            foreach (var orule in occupancyExclusions)
            {
                if (orule == null || orule.sourceTransform == null) continue;
                if (currentlyOccupied.Contains(orule.sourceTransform))
                {
                    if (orule.excludeWhenOccupied != null)
                        foreach (var ex in orule.excludeWhenOccupied) if (ex != null) excluded.Add(ex);
                }
            }
        }

        // helper lambdas
        Func<List<Transform>> computeFreeAndNotExcluded = () =>
        {
            List<Transform> outList = new List<Transform>();
            foreach (var c in candidates)
            {
                if (excluded.Contains(c)) continue;
                bool occupied = false;
                foreach (var a in active) if (a.spawnTransform == c) { occupied = true; break; }
                if (!occupied) outList.Add(c);
            }
            return outList;
        };

        Func<List<Transform>> computeFreeAny = () =>
        {
            List<Transform> outList = new List<Transform>();
            foreach (var c in candidates)
            {
                bool occupied = false;
                foreach (var a in active) if (a.spawnTransform == c) { occupied = true; break; }
                if (!occupied) outList.Add(c);
            }
            return outList;
        };

        // try immediate selection
        List<Transform> freeNotExcluded = computeFreeAndNotExcluded();
        if (freeNotExcluded.Count > 0)
        {
            onChosen(freeNotExcluded[UnityEngine.Random.Range(0, freeNotExcluded.Count)]);
            yield break;
        }

        List<Transform> freeAny = computeFreeAny();
        if (freeAny.Count > 0)
        {
            if (noCandidatePolicy == NoCandidatePolicy.WaitForFree)
            {
                float start = Time.time;
                bool infinite = Mathf.Approximately(waitForFreeTimeout, 0f);
                float pollInterval = 0.12f;

                while (true)
                {
                    // recompute dynamic exclusions from occupancy rules
                    excluded.Clear();
                    if (excludeTransform != null) excluded.Add(excludeTransform);
                    if (exclusionRules != null && exclusionRules.Length > 0 && excludeTransform != null)
                    {
                        foreach (var rule in exclusionRules)
                        {
                            if (rule == null || rule.sourceTransform == null) continue;
                            if (rule.sourceTransform == excludeTransform)
                            {
                                if (rule.excludeNext != null)
                                    foreach (var ex in rule.excludeNext) if (ex != null) excluded.Add(ex);
                            }
                        }
                    }
                    if (occupancyExclusions != null && occupancyExclusions.Length > 0)
                    {
                        HashSet<Transform> currentlyOccupied = new HashSet<Transform>();
                        foreach (var a in active) if (a.spawnTransform != null) currentlyOccupied.Add(a.spawnTransform);

                        foreach (var orule in occupancyExclusions)
                        {
                            if (orule == null || orule.sourceTransform == null) continue;
                            if (currentlyOccupied.Contains(orule.sourceTransform))
                            {
                                if (orule.excludeWhenOccupied != null)
                                    foreach (var ex in orule.excludeWhenOccupied) if (ex != null) excluded.Add(ex);
                            }
                        }
                    }

                    freeNotExcluded = computeFreeAndNotExcluded();
                    if (freeNotExcluded.Count > 0)
                    {
                        onChosen(freeNotExcluded[UnityEngine.Random.Range(0, freeNotExcluded.Count)]);
                        yield break;
                    }

                    if (!infinite && Time.time - start > waitForFreeTimeout)
                        break;

                    yield return new WaitForSeconds(pollInterval);
                }
            }
            // else fall through to policy handling
        }

        // policy handling
        if (noCandidatePolicy == NoCandidatePolicy.AllowExcluded)
        {
            if (freeAny.Count > 0)
            {
                onChosen(freeAny[UnityEngine.Random.Range(0, freeAny.Count)]);
                yield break;
            }
        }

        if (noCandidatePolicy == NoCandidatePolicy.PickAny || (noCandidatePolicy == NoCandidatePolicy.AllowExcluded && freeAny.Count == 0))
        {
            onChosen(candidates[UnityEngine.Random.Range(0, candidates.Count)]);
            yield break;
        }

        // fallback
        onChosen(candidates[UnityEngine.Random.Range(0, candidates.Count)]);
        yield break;
    }

    // ---------- helpers ----------
    private List<Transform> GatherAllSpawnTransforms()
    {
        List<Transform> all = new List<Transform>();
        if (spawnGroups != null && spawnGroups.Length > 0)
        {
            foreach (var g in spawnGroups)
                if (g != null && g.points != null)
                    foreach (var t in g.points) if (t != null && !all.Contains(t)) all.Add(t);
        }

        if (all.Count == 0 && spawnPoints != null)
            foreach (var t in spawnPoints) if (t != null && !all.Contains(t)) all.Add(t);

        if (initialOnlySpawnPoints != null)
            foreach (var t in initialOnlySpawnPoints) if (t != null && !all.Contains(t)) all.Add(t);

        return all;
    }

    private Transform GetRandomAnySpawnTransform()
    {
        var all = GatherAllSpawnTransforms();
        if (all.Count == 0) { Debug.LogWarning("[SpawnCycleManager] No spawn transforms at all; using manager.transform."); return this.transform; }
        return all[UnityEngine.Random.Range(0, all.Count)];
    }

    private IEnumerator SimpleScaleIn(GameObject go, float duration)
    {
        if (go == null) yield break;
        Vector3 target = go.transform.localScale;
        go.transform.localScale = Vector3.zero;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            go.transform.localScale = target * p;
            yield return null;
        }
        go.transform.localScale = target;
    }

    private IEnumerator SimpleScaleOut(GameObject go, float duration)
    {
        if (go == null) yield break;
        Vector3 init = go.transform.localScale;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            go.transform.localScale = Vector3.Lerp(init, Vector3.zero, p);
            yield return null;
        }
        go.transform.localScale = Vector3.zero;
    }

    // ---------- local fallback pre-destroy: non-blocking start ----------
    private Dictionary<GameObject, Coroutine> localPreDestroyCoros = new Dictionary<GameObject, Coroutine>();

    private IEnumerator LocalBeginPreDestroyCoroutine(GameObject go, float durationInfiniteFlag)
    {
        // continuous pulse until explicitly stopped by LocalEndPreDestroyFallbackAndDestroy
        Vector3 baseScale = go.transform.localScale;
        Vector3 basePos = go.transform.localPosition;
        float elapsed = 0f;
        float pulseRate = 6f;
        float pulseAmount = 0.08f;
        float shakeAmount = 0.06f;

        while (true)
        {
            elapsed += Time.deltaTime;
            float phase = Mathf.Sin(elapsed * pulseRate * Mathf.PI * 2f);
            float scaleOffset = phase * pulseAmount;
            go.transform.localScale = baseScale * (1f + scaleOffset);

            float shake = (Mathf.PerlinNoise(Time.time * 8f, 0f) * 2f - 1f);
            go.transform.localPosition = basePos + Vector3.up * (shake * shakeAmount);

            yield return null;
        }
    }

    private IEnumerator LocalBeginPreDestroyFallback(GameObject go)
    {
        if (go == null) yield break;
        if (localPreDestroyCoros.ContainsKey(go)) yield break; // already running

        Coroutine c = StartCoroutine(LocalBeginPreDestroyCoroutine(go, 0f));
        localPreDestroyCoros[go] = c;
        yield break;
    }

    private IEnumerator LocalEndPreDestroyFallbackAndDestroy(GameObject go, float destroyScaleDur)
    {
        if (go == null) yield break;

        // stop local pre-destroy coroutine if exists
        if (localPreDestroyCoros.ContainsKey(go))
        {
            StopCoroutine(localPreDestroyCoros[go]);
            localPreDestroyCoros.Remove(go);
        }

        // optional short wait (give a small gap to restore transform if needed)
        yield return null;

        // then destroy with scale out
        yield return StartCoroutine(SimpleScaleOut(go, destroyScaleDur));
        Destroy(go);
    }

    // Public utility: reset active list (if needed in debug)
    [ContextMenu("Clear Active (editor only)")]
    public void ClearActiveForEditor()
    {
#if UNITY_EDITOR
        active.Clear();
        Debug.Log("[SpawnCycleManager] active cleared (editor).");
#endif
    }
}
