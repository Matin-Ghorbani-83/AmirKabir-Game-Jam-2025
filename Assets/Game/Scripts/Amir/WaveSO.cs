using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("Bird Enemy Setting Left Up")]
    public int minimumCountUp;
    public int maximumCountUp;
    public float maxSpawnTimeUp;
    public float minSpawnTimeUp;

    [Header("Rail Enemy Settings")]
    public float maxSpawnTimeforrail = 5f;
    public float minSpawnTimeforrail = 1f;

    [Header("Wave Compatibilitues")]
    public bool inputChanging;
    public bool platformChanging;
    public bool staticPlatform;


    public bool isDobleJumpActivated;
    public bool isGlideActivated;
    public bool isDashActivated;

    [Header("Level Discriptions")]
    public GameObject backGround;
    public string givenSkillDiscriptionstxt;
    public string nextUnstableThingtxt;

    
}
