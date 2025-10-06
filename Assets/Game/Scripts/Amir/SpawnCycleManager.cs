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
    public Transform sourceTransform;            // اگر این transform فعلاً اشغال باشه
    public Transform[] excludeWhenOccupied;     // این transformها را از انتخاب حذف کن
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

    // internal
    private class ActiveEntry
    {
        public GameObject obj;
        public Transform spawnTransform;
        public GameObject prefabUsed;
    }
    private List<ActiveEntry> active = new List<ActiveEntry>();

    void Start()
    {
        if ((spawnGroups == null || spawnGroups.Length == 0) && (spawnPoints == null || spawnPoints.Length == 0) && (initialOnlySpawnPoints == null || initialOnlySpawnPoints.Length == 0))
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

    // --- initial spawn (two) ---
    private void SpawnInitialTwo()
    {
        List<Transform> allTransforms = GatherAllSpawnTransforms();
        if (allTransforms.Count == 0) { Debug.LogError("[SpawnCycleManager] No spawn transforms available!"); return; }

        List<Transform> chosen = new List<Transform>();

        // use initialOnlySpawnPoints for the initial two if enabled
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

        // fill the rest from global pool
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

    // --- main loop ---
    private IEnumerator CycleRoutine()
    {
        yield return new WaitForSeconds(initialDelay);

        while (true)
        {
            if (active.Count == 0) { SpawnInitialTwo(); yield return null; continue; }

            float waitBeforeDestroy = UnityEngine.Random.Range(minDestroyInterval, maxDestroyInterval);

            // wait until pre-destroy phase
            float timeUntilPre = Mathf.Max(waitBeforeDestroy - preDestroyWarningTime, 0f);
            if (timeUntilPre > 0f) yield return new WaitForSeconds(timeUntilPre);

            int chosenToDestroy = (active.Count == 1) ? 0 : UnityEngine.Random.Range(0, active.Count);
            ActiveEntry toDestroy = active[chosenToDestroy];

            // pre-destroy hint
            if (toDestroy.obj != null)
            {
                var sda = toDestroy.obj.GetComponent<SpawnDestroyAnimator>();
                if (sda != null) yield return StartCoroutine(sda.PlayPreDestroy());
                else if (preDestroyWarningTime > 0f) yield return StartCoroutine(PreDestroyFallbackPulse(toDestroy.obj, preDestroyWarningTime));
            }

            // extra remainder wait if any
            float extraWait = (waitBeforeDestroy - preDestroyWarningTime);
            if (extraWait > 0f) yield return new WaitForSeconds(extraWait);

            // actual destroy
            if (toDestroy.obj != null)
            {
                var sda2 = toDestroy.obj.GetComponent<SpawnDestroyAnimator>();
                if (sda2 != null) { yield return StartCoroutine(sda2.PlayDestroy()); Destroy(toDestroy.obj); }
                else { yield return StartCoroutine(SimpleScaleOut(toDestroy.obj, 0.28f)); Destroy(toDestroy.obj); }
            }

            // remember the transform that was just freed
            Transform justFreed = toDestroy.spawnTransform;
            active.RemoveAt(chosenToDestroy);

            // wait before respawn
            yield return new WaitForSeconds(respawnDelay);

            // choose spawn while considering exclusion rules & occupancy rules
            Transform chosenSpawn = null;
            yield return StartCoroutine(ChooseSpawnTransformAfterDestroyCoroutine(toDestroy.prefabUsed, justFreed, (t) => chosenSpawn = t));

            // instantiate new object at chosenSpawn
            GameObject prefabToSpawn = prefabs[UnityEngine.Random.Range(0, prefabs.Length)];
            GameObject newGo = Instantiate(prefabToSpawn, chosenSpawn.position, chosenSpawn.rotation);
            var animComp = newGo.GetComponent<SpawnDestroyAnimator>();
            if (animComp != null) StartCoroutine(animComp.PlaySpawn());
            else StartCoroutine(SimpleScaleIn(newGo, 0.28f));
            active.Add(new ActiveEntry { obj = newGo, spawnTransform = chosenSpawn, prefabUsed = prefabToSpawn });
        }
    }

    /// <summary>
    /// Coroutine that picks a spawn transform according to:
    /// - prefab rules / groups / spawnPoints
    /// - temporary exclusion for the transform that was just freed
    /// - exclusionRules (based on sourceTransform)
    /// - occupancyExclusions (based on currently occupied transforms)
    /// - policies: PickAny, AllowExcluded, WaitForFree (with timeout)
    /// Calls onChosen(transform) when selection is ready.
    /// </summary>
    private IEnumerator ChooseSpawnTransformAfterDestroyCoroutine(GameObject destroyedPrefab, Transform excludeTransform, Action<Transform> onChosen)
    {
        // 1) build candidate list
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

        // 2) build exclusion set (excludeTransform + exclusionRules[source] + occupancyExclusions based on current active)
        HashSet<Transform> excluded = new HashSet<Transform>();
        if (excludeTransform != null) excluded.Add(excludeTransform);

        // exclusionRules for source transform
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

        // occupancyExclusions: if some sourceTransform is currently occupied, add its excludeWhenOccupied
        if (occupancyExclusions != null && occupancyExclusions.Length > 0)
        {
            HashSet<Transform> currentlyOccupied = new HashSet<Transform>();
            foreach (var a in active)
                if (a.spawnTransform != null) currentlyOccupied.Add(a.spawnTransform);

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

        // local function to compute free & not excluded
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

        // local compute freeAny
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

        // 3) Try immediate selection: free & not excluded
        List<Transform> freeNotExcluded = computeFreeAndNotExcluded();
        if (freeNotExcluded.Count > 0)
        {
            onChosen(freeNotExcluded[UnityEngine.Random.Range(0, freeNotExcluded.Count)]);
            yield break;
        }

        // 4) If none, compute freeAny
        List<Transform> freeAny = computeFreeAny();
        if (freeAny.Count > 0)
        {
            // If policy == WaitForFree, and the reason there was no freeNotExcluded is that all freeAny are excluded,
            // then we may wait for one of excluded to become free & non-excluded (or timeout)
            if (noCandidatePolicy == NoCandidatePolicy.WaitForFree)
            {
                float start = Time.time;
                bool infinite = Mathf.Approximately(waitForFreeTimeout, 0f);
                float pollInterval = 0.12f;

                while (true)
                {
                    // recompute excluded dynamic part that depends on currently occupied (occupancyExclusions)
                    // rebuild excluded set partially (source exclude + exclusionRules + occupancyExclusions)
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
                    {
                        // timeout -> break to fallback behaviour
                        break;
                    }

                    yield return new WaitForSeconds(pollInterval);
                }
            }
            // if we get here, either policy is not WaitForFree or WaitForFree timed out; fall through
        }

        // 5) Policy handling
        if (noCandidatePolicy == NoCandidatePolicy.AllowExcluded)
        {
            // pick any free (even if excluded)
            if (freeAny.Count > 0)
            {
                onChosen(freeAny[UnityEngine.Random.Range(0, freeAny.Count)]);
                yield break;
            }
        }

        if (noCandidatePolicy == NoCandidatePolicy.PickAny || (noCandidatePolicy == NoCandidatePolicy.AllowExcluded && freeAny.Count == 0))
        {
            // fallback: pick any candidate (even if occupied/excluded) to avoid deadlock
            onChosen(candidates[UnityEngine.Random.Range(0, candidates.Count)]);
            yield break;
        }

        // fallback default
        onChosen(candidates[UnityEngine.Random.Range(0, candidates.Count)]);
        yield break;
    }

    // Helpers: gather transforms, random fallback, simple scale anims and pre-destroy pulse
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

        // include initial-only points so they are part of the normal pool after first spawn
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

    private IEnumerator PreDestroyFallbackPulse(GameObject go, float duration)
    {
        if (go == null || duration <= 0f) yield break;
        Vector3 baseScale = go.transform.localScale;
        float elapsed = 0f;
        float pulseRate = 6f;
        float pulseAmount = 0.08f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float phase = Mathf.Sin(elapsed * pulseRate * Mathf.PI * 2f) * pulseAmount;
            go.transform.localScale = baseScale * (1f + phase);
            yield return null;
        }
        go.transform.localScale = baseScale;
    }
}
