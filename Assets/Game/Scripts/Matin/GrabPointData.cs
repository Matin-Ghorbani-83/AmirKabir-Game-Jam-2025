using UnityEngine;

[System.Serializable]
public class GrabPointData
{
    public Transform grabTransform;
    public Transform switchTransform;
    public GrabPosition side;
    public int priority;

    public GrabPointData(Transform grab, Transform sw, GrabPosition side, int priority)
    {
        this.grabTransform = grab;
        this.switchTransform = sw;
        this.side = side;
        this.priority = priority;
    }
}
public enum GrabPosition { Right, Left }