using System;
using System.Collections.Generic;
using UnityEngine;

public class PlatformInfoDetector : MonoBehaviour
{
    public static PlatformInfoDetector Instance { get; private set; }

    [SerializeField] private LayerMask platformLayer;
    [SerializeField] private bool debugLogs = true;

    private GameObject currentPlatform;
   // private bool isActive = true;

    public event Action<List<GrabPointData>> OnGrabPointsCollected;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
       // if (!isActive) return;
        if ((platformLayer.value & (1 << collision.gameObject.layer)) == 0) return;

        currentPlatform = collision.gameObject;
        CollectPointsFromPlatform(currentPlatform);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject == currentPlatform)
        {
            //if (debugLogs) Debug.Log($"[PlatformInfoDetector] Left platform: {collision.name}");
            currentPlatform = null;
            OnGrabPointsCollected?.Invoke(new List<GrabPointData>());
        }
    }

    private void CollectPointsFromPlatform(GameObject platform)
    {
       
        var grabPoints = platform.GetComponentsInChildren<GrabPlatformPosition>(true);
        var transformPoints = platform.GetComponentsInChildren<TransformPlayerPosition>(true);

        if (debugLogs)
            Debug.Log($"[PlatformInfoDetector] '{platform.name}' - Grab:{grabPoints.Length}  Transform:{transformPoints.Length}");

        var results = new List<GrabPointData>();

        foreach (var grab in grabPoints)
        {
            string grabName = grab.name.ToLower();

            
            TransformPlayerPosition matchedSwitch = null;

            foreach (var tPoint in transformPoints)
            {
                string tName = tPoint.name.ToLower();

                bool bothLeft = grabName.Contains("left") && tName.Contains("left");
                bool bothRight = grabName.Contains("right") && tName.Contains("right");

                if (bothLeft || bothRight)
                {
                    matchedSwitch = tPoint;
                    break;
                }
            }

           
            if (matchedSwitch == null && transformPoints.Length > 0)
                matchedSwitch = transformPoints[0];

            var data = new GrabPointData(
                grab.transform,
                matchedSwitch != null ? matchedSwitch.transform : null,
                grab.side,
                grab.priority
            );

            results.Add(data);

            if (debugLogs)
            {
                string sideName = grab.side.ToString();
                string switchName = matchedSwitch != null ? matchedSwitch.name : "null";
                //Debug.Log($"[PlatformInfoDetector] → Grab:{grab.name} | Side:{sideName} | Switch:{switchName}");
            }
        }

        OnGrabPointsCollected?.Invoke(results);
    }

    public GameObject GetCurrentPlatform() => currentPlatform;
}
