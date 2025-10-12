using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;

public class PlayerStartTransform : MonoBehaviour
{
    public int destroyTime = 2;
    private bool startToDestroy = false;
    private bool isPlayerTouched;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.TryGetComponent<PlayerController>(out PlayerController controller))
        {
            isPlayerTouched = true;
            if (isPlayerTouched)
            {
                StartCoroutine(SelfDesytroy());
            }
        }
    }
    private void OnCollisionExit2D(Collision2D collision)
    {
        isPlayerTouched = false;
    }
    IEnumerator SelfDesytroy()
    {
        yield return new WaitForSeconds(destroyTime);
        Destroy(gameObject);
    }
}
