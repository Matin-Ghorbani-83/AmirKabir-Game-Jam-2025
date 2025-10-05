using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerController : MonoBehaviour
{
    public static PlayerController instance { get; private set; }

    public EventHandler OnPlayerMovement;

    [Header("Movement")]
    [SerializeField] float moveSpeed = 6f;
    [SerializeField] float accel = 30f;
    [SerializeField] float deccel = 40f;

    [Header("Jump")]
    [SerializeField] Transform groundCheck;
    [SerializeField] float groundCheckRadius = 0.12f;
    [SerializeField] LayerMask groundLayer;
    [SerializeField] float jumpImpulse = 10f;
    [SerializeField] float variableJumpMultiplier = 0.6f;
    [SerializeField] float maxHoldTime = 0.25f;
    [SerializeField] float glideGravityScale = 0.8f;

    [Header("Dash")]
    [SerializeField] float dashForce = 12f;
    [SerializeField] float dashTime = 0.18f;
    [SerializeField] float dashCooldown = 0.8f;

    [Header("Grab")]

    [SerializeField] Vector2 offset1Right;
    [SerializeField] Vector2 offset2Right;

    [SerializeField] Vector2 offset1Left;
    [SerializeField] Vector2 offset2Left;

    [HideInInspector] public bool ledgeDetected;

    [Header("UX tweaks")]
    [SerializeField] float baseGravityScale = 3.5f;
    [SerializeField] float gravityChangeSpeed = 8f;
    [SerializeField] float coyoteTime = 0.12f;

    private Rigidbody2D rb;
    //Movement

    private float targetDir = 0f;

    //jump
    private KeyCode jumpKey = KeyCode.Space;
    private float holdTimer = 0f;
    private bool canDoubleJump = true;
    private bool isGrounded;
    private bool didDoubleJump;
    private bool jumpReleasedSoon;

    // internal flags jump
    bool jumpRequested = false;
    float coyoteTimer = 0f;
    bool isGliding = false;

    //Dash
    private bool isDashing = false;
    private float dashTimer = 0f;
    private float cooldownTimer = 0f;
    private bool dashRequested;

    //Grab
    private Vector2 climbBegunPosition;
    private Vector2 climbOverPosition;
    private bool canGrabLedge = true;
    private bool canClimb;
    private bool doneGrab = false;
    // private bool isGrabbing = false;
    private void Awake()
    {
        instance = this;

        rb = GetComponent<Rigidbody2D>();
    }
    private void Update()
    {

        ///<summary
        ///Movement>
        ///</summary>

        float dir = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            dir -= 1f;
            OnPlayerMovement?.Invoke(this, EventArgs.Empty);
        }
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            dir += 1f;
            OnPlayerMovement?.Invoke(this, EventArgs.Empty);
        }
        SetDirection(dir);

        ///<summary
        ///Jump
        ///DoubleJump
        ///Glide
        ///</summary>

        // --- ground check (Update ok) ---
        if (groundCheck != null)
        {
            bool groundedNow = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
            //  bool platformNow = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, ledgeLayer);
            if (groundedNow)
            {
                isGrounded = true;
                didDoubleJump = false;
                coyoteTimer = coyoteTime; // reset coyote
            }
            else
            {
                // tick down coyote timer
                coyoteTimer -= Time.deltaTime;
                isGrounded = false;
            }
        }

        if (Input.GetKeyDown(jumpKey))
        {
            jumpRequested = true;
        }
        HandleJumpHold(Input.GetKey(jumpKey));


        ///<summary>
        ///Dash
        ///</summary>

        cooldownTimer -= Time.deltaTime;
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f) EndDash();

        }
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            dashRequested = true;
        }
        ///<summary>
        ///Grab
        ///</summary>
        ///
        Debug.Log(ledgeDetected);
        CheckForLedge();
        LedgeClimbOver();
    }
    private void FixedUpdate()
    {
        //Movement
        PlayerMovement();

        //Jump,DoubleJump,Glide
        if (jumpRequested)
        {
            TryStartJump();
            jumpRequested = false;
        }
        if (jumpReleasedSoon)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * variableJumpMultiplier);

            jumpReleasedSoon = false;
        }

        //Dash
        if (dashRequested)
        {
            TryDash();
            dashRequested = false;
        }

        // Glide
        if (isGliding) rb.gravityScale = glideGravityScale;
    }

    private void OnDrawGizmos()
    {
        //jump
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

    }
    public float GetDirection()
    {
        return targetDir;
    }
    //Movement Methods
    private void SetDirection(float dir)
    {
        targetDir = Mathf.Clamp(dir, -1f, 1f);
    }
    private void PlayerMovement()
    {

        Vector2 v = rb.velocity;
        float targetX = targetDir * moveSpeed;
        float delta = targetX - v.x;
        float accelRate = (Mathf.Abs(targetX) > 0.01f) ? accel : deccel;
        float change = Mathf.Sign(delta) * Mathf.Min(Mathf.Abs(delta), accelRate * Time.fixedDeltaTime);
        v.x += change;
        rb.velocity = v;
    }

    //Jumping Methods
    private void TryStartJump()
    {
        if (coyoteTimer > 0f || isGrounded)
        {
            DoJump();
            coyoteTimer = 0f;
        }
        else if (!didDoubleJump && canDoubleJump)
        {
            DoJump();
            didDoubleJump = true;
        }
    }
    private void DoJump()
    {
        rb.velocity = new Vector2(rb.velocity.x, 0f);
        rb.AddForce(Vector2.up * jumpImpulse, ForceMode2D.Impulse);
        // reset hold timer when jump starts
        holdTimer = 0f;
        // Exit Glide Mode
        isGliding = false;
        rb.gravityScale = baseGravityScale;
    }
    private void HandleJumpHold(bool holding)
    {
        if (holding)
        {
            holdTimer += Time.deltaTime;

            if (holdTimer <= maxHoldTime)
            {

                //  rb.gravityScale = Mathf.Lerp(rb.gravityScale, 1.8f, 0.2f);
                float targetGravity = Mathf.Lerp(baseGravityScale, baseGravityScale * 0.55f, 0.6f);
                rb.gravityScale = Mathf.MoveTowards(rb.gravityScale, targetGravity, gravityChangeSpeed * Time.deltaTime);
            }
        }
        else
        {
            if (holdTimer > 0f && holdTimer < maxHoldTime)
            {
                jumpReleasedSoon = true;

            }
            holdTimer = 0f;
            // Reset Gravity if not Gliding
            if (!isGliding) rb.gravityScale = baseGravityScale;

            // if (!IsGliding()) rb.gravityScale = baseGravityScale;
        }



        if (!isGrounded && rb.velocity.y < 0f && holding && Input.GetKey(jumpKey))
        {
            isGliding = true;

            Debug.Log("Player is Gliding");
            //  rb.gravityScale = Mathf.MoveTowards(rb.gravityScale, glideGravityScale, gravityChangeSpeed * Time.deltaTime);

        }
        else
        {
            if (isGliding && (isGrounded || !(holding)))
            {
                isGliding = false;
            }
            if (!isGliding)
            {
                rb.gravityScale = Mathf.MoveTowards(rb.gravityScale, baseGravityScale, gravityChangeSpeed * Time.deltaTime);
            }
        }

    }



    //Dash Methods

    private void TryDash()
    {
        float direction = targetDir;
        if (cooldownTimer > 0f || isDashing) return;
        if (Mathf.Abs(direction) < 0.1f) return;


        StartDash(Mathf.Sign(direction));
    }


    private void StartDash(float dir)
    {
        isDashing = true;
        dashTimer = dashTime;
        cooldownTimer = dashCooldown;
        rb.velocity = new Vector2(dir * dashForce, rb.velocity.y);

    }


    private void EndDash()
    {
        isDashing = false;
        // resume normal physics; no-op
    }

    //Grab Methods

    private void CheckForLedge()
    {
        if ((ledgeDetected && canGrabLedge))
        {
            if (targetDir == 0) return;

            canGrabLedge = false;
            isGliding = false;
            rb.gravityScale = 0f;
            Vector2 ledgePosition = LedgeDetection.Instance.transform.position;
            if (targetDir == 1)
            {
                climbBegunPosition = ledgePosition + offset1Right;
                climbOverPosition = ledgePosition + offset2Right;
            }
            if (targetDir == -1)
            {
                climbBegunPosition = ledgePosition + offset1Left;
                climbOverPosition = ledgePosition + offset2Left;
            }

            canClimb = true;
        }
        if (canClimb)
        {
            transform.position = climbBegunPosition;
            rb.gravityScale = baseGravityScale;
            doneGrab = true;
        }
    }
    //This Method Should Call In Last Frame Of Animation Grap By Event
    private void LedgeClimbOver()
    {
        if ((doneGrab))
        {
            canClimb = false;
            transform.position = climbOverPosition;
            Invoke("AllowLedgeGrab", .1f);
        }
        if (!isGrounded)
        {
            doneGrab = false;
        }


    }
    private void AllowLedgeGrab() => canGrabLedge = true;
}
