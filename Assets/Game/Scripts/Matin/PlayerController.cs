using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController instance { get; private set; }

    public event EventHandler OnPlayerMove;
    public event EventHandler OnPlayerJump;
    public event EventHandler OnPlayerDoubleJump;
    public event EventHandler OnPlayerDash;
    public event EventHandler OnPlayerGrab;
    public event EventHandler OnPlayerGlideStart;
    public event EventHandler OnPlayerGlideEnd;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float accel = 30f;
    [SerializeField] private float deccel = 40f;

    [Header("Jump")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.12f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float jumpImpulse = 10f;
    [SerializeField] private float variableJumpMultiplier = 0.6f;
    [SerializeField] private float maxHoldTime = 0.25f;
    [SerializeField] private float glideGravityScale = 0.1f;

    [Header("Dash")]
    [SerializeField] private float dashForce = 12f;
    [SerializeField] private float dashTime = 0.18f;
    [SerializeField] private float dashCooldown = 0.8f;

    [Header("Grab")]
    [SerializeField] private Vector2 offset1Right;

    [SerializeField] private Vector2 offset2Right;
    [SerializeField] private Vector2 offset1Left;
    [SerializeField] private Vector2 offset2Left;
    [SerializeField] private GameObject playerDirGameObject;
    [HideInInspector] public bool ledgeDetected;

    [Header("Gravity & Tweaks")]
    [SerializeField] private float baseGravityScale = 3.5f;
    [SerializeField] private float gravityChangeSpeed = 8f;
    [SerializeField] private float coyoteTime = 0.12f;

    private Rigidbody2D rb;

    // Direction
    private float targetDir = 0f;

    // Jump
    private bool isGrounded;
    private bool canDoubleJump = true;
    private bool didDoubleJump;
    private bool jumpReleasedEarly;
    private float holdTimer = 0f;
    private float coyoteTimer = 0f;
    private bool jumpRequested;
    private bool isGliding = false;

    // Dash
    private bool isDashing = false;
    private float dashTimer = 0f;
    private float cooldownTimer = 0f;
    private bool dashRequested = false;
    private bool canStartDash = false;
    // Grab
    private bool canGrabLedge = true;
    private bool canClimb;

    private Vector2 climbBegunPosition;
    private Vector2 climbOverPosition;

    private void Awake()
    {
        instance = this;
        rb = GetComponent<Rigidbody2D>();
        // ensure starting gravity is base
        rb.gravityScale = baseGravityScale;
    }

    private void Update()
    {
        HandleInput();
        GroundCheck();
        HandleDashTimers();
        CheckForLedge();
        //LedgeClimbOver();
    }

    private void FixedUpdate()
    {
        ApplyGravityScale();


        HandleMovement();

        HandleJump();

        HandleDashPhysics();

        HandleGlideGravity();

        HandleGrabDirection();
    }

    // -------------------------------
    // INPUT HANDLING
    // -------------------------------
    private void HandleInput()
    {
        float dir = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) dir -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) dir += 1f;
        SetDirection(dir);

        if (Mathf.Abs(dir) > 0.01f)
            OnPlayerMove?.Invoke(this, EventArgs.Empty);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpRequested = true;



        }

        if (didDoubleJump)
            OnPlayerDoubleJump?.Invoke(this, EventArgs.Empty);

        if (Input.GetKeyDown(KeyCode.LeftShift))
            dashRequested = true;

        HandleJumpHold(Input.GetKey(KeyCode.Space));
    }

    // -------------------------------
    // MOVEMENT
    // -------------------------------
    private void HandleMovement()
    {
        if (isDashing) return;

        Vector2 v = rb.velocity;
        float targetX = targetDir * moveSpeed;
        float delta = targetX - v.x;
        float accelRate = (Mathf.Abs(targetX) > 0.01f) ? accel : deccel;
        float change = Mathf.Sign(delta) * Mathf.Min(Mathf.Abs(delta), accelRate * Time.fixedDeltaTime);

        v.x += change;
        rb.velocity = v;
    }

    private void SetDirection(float dir)
    {
        targetDir = Mathf.Clamp(dir, -1f, 1f);
    }

    public bool GetIsGliding() => isGliding;
    public bool GetIsDashing() => isDashing;
    public bool GetIsGrabbing() => canClimb;
    public bool GetIsGrounded() => isGrounded;
    public float GetDirection() => targetDir;
    public float GetVelocityY() => rb.velocity.y;
    public float GetVelocityX() => rb.velocity.x;

    // -------------------------------
    // GROUND CHECK
    // -------------------------------
    private void GroundCheck()
    {
        if (!groundCheck) return;
        bool groundedNow = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (groundedNow)
        {
            isGrounded = true;
            didDoubleJump = false;
            coyoteTimer = coyoteTime;
        }
        else
        {
            isGrounded = false;
            coyoteTimer -= Time.deltaTime;
        }
    }

    // -------------------------------
    // JUMP
    // -------------------------------
    private void HandleJump()
    {
        if (jumpRequested)
        {
            if (coyoteTimer > 0f || isGrounded)
            {
                DoJump();
                OnPlayerJump?.Invoke(this, EventArgs.Empty);
            }
            else if (!didDoubleJump && canDoubleJump)
            {
                DoJump();
                didDoubleJump = true;

            }
            jumpRequested = false;
        }

        if (jumpReleasedEarly)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * variableJumpMultiplier);
            jumpReleasedEarly = false;
        }
    }

    private void DoJump()
    {
        rb.velocity = new Vector2(rb.velocity.x, 0f);
        rb.AddForce(Vector2.up * jumpImpulse, ForceMode2D.Impulse);
        holdTimer = 0f;
        isGliding = false;
        rb.gravityScale = baseGravityScale;
    }


    private void HandleJumpHold(bool holding)
    {
        // Manage hold timer for variable jump — do not change gravity here
        if (holding)
        {
            holdTimer += Time.deltaTime;
            if (holdTimer > maxHoldTime) holdTimer = maxHoldTime;
        }
        else
        {
            if (holdTimer > 0f && holdTimer < maxHoldTime)
                jumpReleasedEarly = true;

            holdTimer = 0f;
        }


        if (!isGrounded && rb.velocity.y < 0f && holding)
        {
            if (!isGliding)
            {
                isGliding = true;
                OnPlayerGlideStart?.Invoke(this, EventArgs.Empty);
            }
        }
        else
        {
            if (isGliding)
            {
                isGliding = false;
                OnPlayerGlideEnd?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private void ApplyGravityScale()
    {


        // 2) If dashing, gravity should be 0 (dash controls movement)
        if (isDashing)
        {
            if (rb.gravityScale != 0f) rb.gravityScale = 0f;
            return;
        }

        // 3) If gliding, we want a fixed glide gravity (immediate)
        if (isGliding)
        {
            if (rb.gravityScale != glideGravityScale) rb.gravityScale = glideGravityScale;
            return;
        }

        // 4) Variable jump hold: when player is holding jump while going up, reduce gravity smoothly
        // Only apply while ascending to extend jump arc
        if (holdTimer > 0f && holdTimer <= maxHoldTime && rb.velocity.y > 0f)
        {
            float targetGravity = baseGravityScale * 0.55f; // or compute differently
            rb.gravityScale = Mathf.MoveTowards(rb.gravityScale, targetGravity, gravityChangeSpeed * Time.fixedDeltaTime);
            return;
        }

        // 5) Default: move gravity back to base smoothly
        rb.gravityScale = Mathf.MoveTowards(rb.gravityScale, baseGravityScale, gravityChangeSpeed * Time.fixedDeltaTime);
    }

    private void HandleGlideGravity()
    {
        //if (isGliding)
        //    rb.gravityScale = glideGravityScale;
        //else
        //    rb.gravityScale = Mathf.MoveTowards(rb.gravityScale, baseGravityScale, gravityChangeSpeed * Time.deltaTime);
    }

    // -------------------------------
    // DASH
    // -------------------------------
    private void HandleDashTimers()
    {
        cooldownTimer -= Time.deltaTime;
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
                EndDash();
        }

        if (dashRequested)
        {
            TryDash();
            dashRequested = false;

        }
    }

    private void TryDash()
    {
        if (cooldownTimer > 0f || isDashing) return;
        if (Mathf.Abs(targetDir) < 0.1f) return;

        canStartDash = true;
        OnPlayerDash?.Invoke(this, EventArgs.Empty);
    }

    private void StartDash()
    {
        isDashing = true;
        dashTimer = dashTime;
        cooldownTimer = dashCooldown;
        float dir = Mathf.Sign(targetDir);
        //rb.gravityScale = 0f;
        rb.velocity = new Vector2(dir * dashForce, 0f);
    }

    private void EndDash()
    {
        isDashing = false;
        //rb.gravityScale = baseGravityScale;
    }

    private void HandleDashPhysics()
    {
        if (canStartDash)
        {
            StartDash();
            canStartDash = false;
        }
    }

    // -------------------------------
    // LEDGE GRAB
    // -------------------------------

    private void HandleGrabDirection()
    {
        if (targetDir == 1)
        {
            Debug.Log("Looking Right");
            playerDirGameObject.transform.localScale = Vector3.right;
        }
        else if (targetDir == -1)
        {
            Debug.Log("Looking Left");
            playerDirGameObject.transform.localScale = Vector3.left;
        }
    }
    private void CheckForLedge()
    {
        if ((ledgeDetected && canGrabLedge))
        {
            //rb.gravityScale = baseGravityScale;
            canGrabLedge = false;
            isGliding = false;

            Vector2 ledgePosition = LedgeDetection.Instance.transform.position;
            if (playerDirGameObject.transform.localScale == Vector3.right)
            {
                climbBegunPosition = ledgePosition + offset1Right;
                climbOverPosition = ledgePosition + offset2Right;
            }
            if (playerDirGameObject.transform.localScale == Vector3.left)
            {
                climbBegunPosition = ledgePosition + offset1Left;
                climbOverPosition = ledgePosition + offset2Left;
            }
            canClimb = true;
        }

        if (canClimb)
        {
            rb.velocity = new Vector3(0, 0, 0);
            transform.position = climbBegunPosition;

            isGrounded = false;
            isGliding = false;
            isDashing = false;

            //rb.gravityScale = baseGravityScale;
            OnPlayerGrab?.Invoke(this, EventArgs.Empty);
            //oneGrab = true;
        }
    }
    public void LedgeClimbOver()
    {

        canClimb = false;
        transform.position = climbOverPosition;
        Invoke(nameof(AllowLedgeGrab), 0.1f);

        Debug.Log("Trigger Animation");

    }

    private void AllowLedgeGrab() => canGrabLedge = true;

    private void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
