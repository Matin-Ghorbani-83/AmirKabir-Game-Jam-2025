using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerStartTransform : MonoBehaviour
{
    public float destroyTime ;
    private bool startToDestroy = false;
    private bool isPlayerTouched;

    private bool isPlayerDied = false;
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        PlayerHealthSystem.instance.OnPlayerDied += Instance_OnPlayerDied;
       

    }

  
    private void Update()
    {
        if (isPlayerDied) StopCoroutine(SelfDesytroy());
    }
    private void Instance_OnPlayerDied(Vector3 obj)
    {
        isPlayerDied = true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.TryGetComponent<PlayerController>(out PlayerController controller))
        {
            isPlayerTouched = true;
            if (isPlayerTouched && !isPlayerDied)
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
     

        animator.SetTrigger("Destroy");
        yield return new WaitForSeconds(destroyTime);
        
        Destroy(gameObject);


    }

}
