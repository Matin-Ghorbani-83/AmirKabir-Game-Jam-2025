using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class LedgeDetection : MonoBehaviour
{
    public static LedgeDetection Instance { get; private set; }

    [SerializeField] float checkRadius = 0.12f;
    [SerializeField] LayerMask Ground;
    private bool canDetected;
    private bool isGliding = false;
    BoxCollider2D BoxCollider2D;
    [SerializeField] private float boxColliderGlidingSizeX = 2.17077f;
    [SerializeField] private float boxColliderGlidingSizeY = 1.039483f;
    [SerializeField] private float boxColliderNormalSizeX = 0.5327392f;
    [SerializeField] private float boxColliderNormalSizeY = 0.4152613f;

    private void Awake()
    {
        BoxCollider2D = GetComponent<BoxCollider2D>();
        checkRadius = 0.12f;
        Instance = this;
    }
    private void Start()
    {
        PlayerController.instance.OnPlayerGlideStart += PlayerControllerOnPlayerGlideStart;

        PlayerController.instance.OnPlayerGlideEnd += PlayerControllerOnPlayerGlideEnd;
    }

    private void PlayerControllerOnPlayerGlideEnd(object sender, System.EventArgs e)
    {
        isGliding = false;
    }

    private void PlayerControllerOnPlayerGlideStart(object sender, System.EventArgs e)
    {
        isGliding = true;
    }


    private void Update()
    {
        if (canDetected)
            PlayerController.instance.ledgeDetected = Physics2D.OverlapCircle(transform.position, checkRadius, Ground);
        if (isGliding)
        {
            checkRadius = .5f;
            BoxCollider2D.size = new Vector2(boxColliderGlidingSizeX, boxColliderGlidingSizeY);
            
        }
        else
        {
            checkRadius = 0.12f;
            BoxCollider2D.size = new Vector2(boxColliderNormalSizeX, boxColliderNormalSizeY);
        }

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
