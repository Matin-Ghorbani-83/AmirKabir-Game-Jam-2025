using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class WaveSO : ScriptableObject
{
    [Header("Platforms Settings")]
    public float initialDelay;
    public float minDestroyInterval;
    public float maxDestroyInterval;
    public float respawnDelay;

    [Header("Bird Enemy Setting Left")]
    public int minimumCountLeft;
    public int maximumCountLeft;
    public float maxSpawnTimeLeft;
    public float minSpawnTimeLeft;

    [Header("Bird Enemy Setting Left Right")]
    public int minimumCountRight;
    public int maximumCountRight;
    public float maxSpawnTimeRight;
    public float minSpawnTimeRight;

    [Header("Rail Enemy Settings")]
    public float maxSpawnTimeforrail = 5f;
    public float minSpawnTimeforrail = 1f;

    [Header("Wave Compatibilitues")]
    public bool inputChanging;
    public bool platformChanging;

    public bool isDobleJumpActivated;
    public bool isGlideActivated;
    public bool isDashActivated;
}
