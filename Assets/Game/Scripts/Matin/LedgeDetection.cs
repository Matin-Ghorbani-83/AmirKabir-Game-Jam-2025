using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LedgeDetection : MonoBehaviour
{
    public static LedgeDetection Instance { get; private set; }

    [SerializeField] float checkRadius = 0.12f;
    [SerializeField] LayerMask Ground;
    private bool canDetected;
    private void Awake()
    {
        Instance = this;
    }
    private void Update()
    {
        if (canDetected)
            PlayerController.instance.ledgeDetected = Physics2D.OverlapCircle(transform.position, checkRadius, Ground);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            canDetected = false;
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            canDetected = true;
        }
    }
    private void OnDrawGizmos()
    {
        //Grab
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, checkRadius);

    }

}
