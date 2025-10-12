using System;
using UnityEngine;

public class PlayerVisuals : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject playerSprite;
    private Animator animator;

    private PlayerController controller;

    // Internal cached states
    private bool isGrounded;
    private bool isMoving;
    private bool isGliding;
    private bool isDashing;
    private bool isGrabbing;
    private float velocityY;
    private float velocityX;
    private void Start()
    {
        controller = PlayerController.instance;
        animator = playerSprite.GetComponent<Animator>();

        // Subscribe to controller events (if needed)
        controller.OnPlayerMove += OnPlayerMovement;
        controller.OnPlayerJump += OnPlayerJump;
        controller.OnPlayerDoubleJump += OnPlayerDoubleJump;
        controller.OnPlayerDash += OnPlayerDash;
        controller.OnPlayerGrab += OnPlayerGrab;
        controller.OnPlayerGlideStart += Controller_OnPlayerGlideStart;
        controller.OnPlayerGlideEnd += Controller_OnPlayerGlideEnd;

        PlayerHealthSystem.instance.OnPlayerDied += Instance_OnPlayerDied;
    }

    private void Instance_OnPlayerDied(Vector3 obj)
    {
        animator.SetBool("isMoving", false);
        animator.WriteDefaultValues();
    }

    private void Controller_OnPlayerGlideEnd(object sender, EventArgs e)
    {
        Debug.Log("Glide Enddddddd");
    }

    private void Controller_OnPlayerGlideStart(object sender, EventArgs e)
    {
        Debug.Log("Glide Staaaaart");
    }

    private void OnPlayerGrab(object sender, EventArgs e)
    {
        animator.SetTrigger("grabTrigger");
        
    }

    private void OnPlayerDash(object sender, EventArgs e)
    {
        animator.SetTrigger("dashTrigger");
    }

    private void OnDestroy()
    {

        controller.OnPlayerMove -= OnPlayerMovement;
        controller.OnPlayerJump -= OnPlayerJump;
    }


    private void OnPlayerMovement(object sender, EventArgs e)
    {
        isMoving = true;

    }


    private void OnPlayerJump(object sender, EventArgs e)
    {
        animator.SetTrigger("jumpTrigger");
    }

    private void OnPlayerDoubleJump(object sender, EventArgs e)
    {
        animator.SetTrigger("doubleJumpTrigger");
    }
    private void Update()
    {


        isGrounded = controller.GetIsGrounded();
        isGliding = controller.GetIsGliding();
        isDashing = controller.GetIsDashing();
        isGrabbing = controller.GetIsGrabbing();
        velocityY = controller.GetVelocityY();
        velocityX = Mathf.Abs(controller.GetVelocityX());

        float dir = controller.GetDirection();
        isMoving = Mathf.Abs(dir) > 0.1f && isGrounded;


        FlipPlayerSprite(dir);

        if (controller.GetIsGrounded())
        {
            animator.ResetTrigger("doubleJumpTrigger");
            animator.ResetTrigger("grabTrigger");
        }

        if (controller.GetIsGrabbing())
        {
            animator.ResetTrigger("grabTrigger");
        }
        animator.SetBool("isGrounded", isGrounded);
        animator.SetBool("isMoving", isMoving);
        animator.SetBool("isGliding", isGliding);
        animator.SetBool("isDashing", isDashing);
        animator.SetBool("isGrabbing", isGrabbing);
        animator.SetFloat("velocityY", velocityY);
        animator.SetFloat("velocityX", velocityX);

        isMoving = false;
       
    }

    private void FlipPlayerSprite(float dir)
    {
        if (Mathf.Abs(dir) > 0.01f)
        {
            playerSprite.transform.localScale = new Vector3(Mathf.Sign(dir) * 1, 1, 1);
        }
    }



    public void OnDoubleJump()
    {
        animator.SetTrigger("doubleJumpTrigger");
    }

    public void OnDashStart()
    {
        animator.SetTrigger("dashTrigger");
    }

    public void OnGrabStart()
    {
        animator.SetBool("isGrabbing", true);
    }

    public void OnGrabEnd()
    {
        animator.SetBool("isGrabbing", false);
    }
}
