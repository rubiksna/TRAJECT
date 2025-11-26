using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]

    // Movement speed variables
    private float moveSpeed;
    public float walkSpeed;
    public float sprintSpeed;

    // Drag applied when the player is on the ground
    public float groundDrag;

    // Jumping variables
    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    bool readyToJump;

    [Header("Crouching")]
    public float crouchSpeed;
    public float crouchYScale;
    private float startYScale;

    // Continuous jump when holding the jump key
    [Header("Continuous Jump")]
    [Tooltip("Delay (seconds) to start automatic repeated jumps when holding the jump key.")]
    public float continuousJumpDelay = 0.5f;
    [Tooltip("Minimum interval (seconds) between automatic jumps while holding. The effective interval is at least this value, but jumps also respect jumpCooldown and grounded state.")]
    public float continuousJumpInterval = 0.05f;
    [Tooltip("Additional delay added to the initial continuousJumpDelay when the player holds the jump immediately after performing a jump.")]
    public float holdJumpExtraDelay = 0.5f;

    bool isJumpHeld;
    Coroutine jumpHoldCoroutine;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;

    [Header("Ground Check")]

    // various ground check variables
    public float playerHeight = 2f;
    public LayerMask whatIsGround;
    public bool grounded;

    [Header("Slope Handling")]
    public float maxSlopeAngle;
    private RaycastHit slopeHit;
    bool exitingSlope;

    // Reference to the orientation transform
    public Transform orientation;

    // Input variables
    float horizontalInput;
    float verticalInput;

    // Movement direction vector
    Vector3 moveDirection;

    // Reference to the Rigidbody component
    Rigidbody rb;

    // Current movement state
    public MovementState state;

    // defines different movement states
    public enum MovementState
    {
        Walking,
        Sprinting,
        Crouching,
        Air
    }

    // Tracks the time the last jump was performed
    private float _lastJumpTime = -999f;
    // Threshold to consider "immediately after jump" (seconds)
    private const float JustJumpedThreshold = 0.15f;

    void Start()
    {
        // Get the Rigidbody component and freeze rotation
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody not found on player.");
            enabled = false;
            return;
        }

        rb.freezeRotation = true;
        // Ensure gravity and non-kinematic are enabled
        if (rb.isKinematic)
            Debug.LogWarning("Rigidbody is kinematic. Jump will not work while kinematic is true.");
        if (!rb.useGravity)
            Debug.LogWarning("Rigidbody.useGravity is false. Jump will not behave as expected.");

        // Allow jumping immediately at start
        readyToJump = true;

        startYScale = transform.localScale.y;
    }

    // MyInput is called every frame to handle user input
    private void Update()
    {
        // Ground check using Raycast from slightly above transform to avoid origin below feet due to pivot
        float rayOriginOffset = 0.1f;
        float rayLength = playerHeight * 0.5f + 0.2f;
        Vector3 rayOrigin = transform.position + Vector3.up * rayOriginOffset;

        grounded = Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayLength, whatIsGround);

        // Visualize the ground check (Scene view)
        Debug.DrawRay(rayOrigin, Vector3.down * rayLength, grounded ? Color.green : Color.red);

        MyInput();
        SpeedControl();
        StateHandler();

        // Handle drag using common API
        rb.linearDamping = grounded ? groundDrag : 0f;
    }

    // FixedUpdate is called at a fixed interval and is independent of frame rate
    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void MyInput()
    {
        // Get horizontal and vertical input
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // When to jump - use GetKeyDown so jump triggers on press
        if (Input.GetKeyDown(jumpKey) && readyToJump && grounded)
        {
            readyToJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }

        // start crouch
        if (Input.GetKeyDown(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        }

        // stop crouch
        if (Input.GetKeyUp(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
        }

        // Handle jump hold start
        if (Input.GetKeyDown(jumpKey))
        {
            isJumpHeld = true;
            // Start coroutine to handle continuous jumps after the configured delay
            if (jumpHoldCoroutine == null)
                jumpHoldCoroutine = StartCoroutine(HandleJumpHold());
        }

        // Handle jump hold end
        if (Input.GetKeyUp(jumpKey))
        {
            isJumpHeld = false;
            if (jumpHoldCoroutine != null)
            {
                StopCoroutine(jumpHoldCoroutine);
                jumpHoldCoroutine = null;
            }
        }
    }

    private IEnumerator HandleJumpHold()
    {
        // Determine effective initial delay.
        float initialDelay = continuousJumpDelay;
        if (Time.time - _lastJumpTime <= JustJumpedThreshold)
        {
            // Player started holding immediately after a jump -> add extra delay
            initialDelay += holdJumpExtraDelay;
        }

        // initial delay before automatic jumps begin
        float elapsed = 0f;
        while (elapsed < initialDelay)
        {
            if (!isJumpHeld)
            {
                jumpHoldCoroutine = null;
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // After the delay, keep attempting jumps while the key is held.
        // Respect grounded and readyToJump. Use a small interval to avoid busy-waiting.
        while (isJumpHeld)
        {
            // Wait until we are allowed and grounded
            yield return new WaitUntil(() => (!isJumpHeld) || (readyToJump && grounded));

            if (!isJumpHeld)
                break;

            // If still holding and conditions met, perform a jump
            if (readyToJump && grounded)
            {
                readyToJump = false;
                Jump();
                Invoke(nameof(ResetJump), jumpCooldown);
            }

            // Ensure we don't loop too tightly; allow either the jumpCooldown or a small interval to elapse
            float wait = Mathf.Max(jumpCooldown, continuousJumpInterval);
            float timer = 0f;
            while (isJumpHeld && timer < wait)
            {
                timer += Time.deltaTime;
                yield return null;
            }
        }

        jumpHoldCoroutine = null;
    }

    private void StateHandler()
    {
        // Mode - Crouching
        if (Input.GetKey(crouchKey))
        {
            state = MovementState.Crouching;
            moveSpeed = crouchSpeed;
            return;
        }

        // Mode - Sprinting
        if (grounded && Input.GetKey(sprintKey))
        {
            state = MovementState.Sprinting;
            moveSpeed = sprintSpeed;
        }
        // Mode - Walking
        else if (grounded)
        {
            state = MovementState.Walking;
            moveSpeed = walkSpeed;
        }
        // Mode - Air
        else
        {
            state = MovementState.Air;
        }
    }

    private void MovePlayer()
    {
        // Calculate movement direction based on input and orientation
        if (orientation == null)
        {
            Debug.LogWarning("Orientation transform is not set. Movement will use transform.forward/right.");
            orientation = transform;
        }

        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        // on slope
        if (OnSlope() && !exitingSlope)
        {
            rb.AddForce(GetSlopeMoveDirection() * moveSpeed * 10f, ForceMode.Force);

            if (rb.linearVelocity.y > 0)
            {
                rb.AddForce(Vector3.down * 80f, ForceMode.Force);
            }
        }

        // On ground vs Air - use braces to avoid accidental mis-association
        if (grounded)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        }
        else
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
        }

        // turn off gravity while on slope
        rb.useGravity = !OnSlope();
    }

    private void SpeedControl()
    {
        // limiting speed on slope (project velocity to slope plane)
        if (OnSlope() && !exitingSlope && grounded)
        {
            Vector3 slopeVel = Vector3.ProjectOnPlane(rb.linearVelocity, slopeHit.normal);
            if (slopeVel.magnitude > moveSpeed)
            {
                Vector3 limited = slopeVel.normalized * moveSpeed;
                // preserve vertical velocity component
                rb.linearVelocity = new Vector3(limited.x, rb.linearVelocity.y, limited.z);
            }
        }
        else
        {
            // limiting speed on ground or in air
            Vector2 flatVel = new Vector2(rb.linearVelocity.x, rb.linearVelocity.z);

            // Limit velocity if needed
            if (flatVel.magnitude > moveSpeed)
            {
                Vector2 limitedFlat = flatVel.normalized * moveSpeed;
                rb.linearVelocity = new Vector3(limitedFlat.x, rb.linearVelocity.y, limitedFlat.y);
            }
        }
    }

    private void Jump()
    {
        exitingSlope = true;

        // Reset y velocity before jumping
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        // Apply jump force
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

        // Record time of this jump for hold-delay logic
        _lastJumpTime = Time.time;
    }

    private void ResetJump()
    {
        readyToJump = true;
        exitingSlope = false;
    }

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }

        return false;
    }

    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
    }
}
