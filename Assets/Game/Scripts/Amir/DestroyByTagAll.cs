using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DestroyByTagAll : MonoBehaviour
{
    [Header("Tags to Destroy")]
    public string tag1 = "Enemy";
    public string tag2 = "Obstacle";
    public string tag3 = "Bullets";
    public static DestroyByTagAll instance;

    private void Awake()
    {
        instance = this;
    }

    public void DestroyAllWithTwoTags()
    {
        // پیدا کردن همه آبجکت‌های فعال با tag1
        GameObject[] objs1 = GameObject.FindGameObjectsWithTag(tag1);
        foreach (var obj in objs1)
        {
            Destroy(obj);
        }

        // پیدا کردن همه آبجکت‌های فعال با tag2
        GameObject[] objs2 = GameObject.FindGameObjectsWithTag(tag2);
        foreach (var obj in objs2)
        {
            Destroy(obj);
        }

        GameObject[] objs3 = GameObject.FindGameObjectsWithTag(tag3);
        foreach (var obj in objs3)
        {
            Destroy(obj);
        }

        Debug.Log($"Destroyed {objs1.Length + objs2.Length} objects with tags {tag1} or {tag2}");
    }

    // برای تست سریع از اینسپکتور
    [ContextMenu("Destroy All With Two Tags")]
    private void ContextDestroy() => DestroyAllWithTwoTags();

}
