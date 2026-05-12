using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using System.Linq;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float maxDefaultSpeed = 5f; // Maximum speed the player can reach
    [SerializeField] private float maxAcceleration = 1f; // How fast the player accelerates in input direction
    [SerializeField] private float brakeDeceleration = 10f; // Extra deceleration while brake action is held
    // friction acceleration is equal to maxAcceleration at maxSpeed

    [Header("Dash Settings")]
    [SerializeField] private float dashCooldown = 1f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashSpeed = 15f;
    
    [Header("Kick Settings")]
    [SerializeField] private float kickBufferTime = 0.2f; // How long before touching surface can kick be buffered
    [SerializeField] private LayerMask enemyLayerMask = 0; // Layer mask for enemies (optional, for differentiation)
    // [SerializeField] private float staticKickSpeed = 5f; // If speed it very low, kick is performed in surface direction with this speed
    // [SerializeField] private float kickMinSpeed = 0.5f; // Min speed added to kick. Added at +- kickMaxAngleOffset
    // [SerializeField] private float kickMaxSpeed = 1f; // Max speed added to kick. Added at 0 degree
    [SerializeField] private float kickAngleMaxOffset = 45f; // How much you can change direction in degrees
    [SerializeField] private int kickRangeDisplayVectorAmount = 9; // Max possible kick direction offset, in degrees. Adds kickMinSpeed.
    // [SerializeField] private float kickAngleFreeOffset = 10f; // How much you can change direction in degrees without losing speed
    // [SerializeField] private float kickExtraSpeedMultiplier = 1.1f; // Speed added to buffer, before angle calculations.
    [SerializeField] private float kickExtraSpeedComponent = 5f; // Speed added to buffer, before angle calculations.
    // kickspeed = buff * mult + component
    [SerializeField] private bool kickOnHold = true; // true: kick while button is held, false: kick on press

    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private CircularMouseLimiter circularMouseLimiter;
    [SerializeField] private bool useCircularMouseLimiterForDirection = false;
    [SerializeField] private PlayerSteering playerSteering;
    
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 mousePosition;
    
    // Custom velocity system - calculated manually, then applied to Rigidbody2D
    private Vector2 velocity;
    private float currentSpeed;
    private bool isDashing = false;
    private float dashCooldownTimer = 0f;
    private Vector2 dashDirection;
    
    // Kick system: attach to a single surface at a time
    private Collider2D attachedCollider;
    private Vector2 attachedNormal = Vector2.zero;
    // Magnitude of velocity perpendicular to wall (into it) at attach time
    // Used later to reconstruct full incoming velocity for kick calculations
    private float bufferedNormalSpeed = 0f;
    private float kickBufferTimer = 0f;
    private bool kickInputBuffered = false;
    private bool isKickActive = false; // current kick input state (held or not)
    private bool isKickAimHeld = false;
    private bool isSteeringHeld = false;
    private bool isTimeStoppedForKickAim = false;
    private bool isBrakeHeld = false;
    
    // Kick visualization (for arrow indicator)
    // private Vector2 movementTargetDirection = Vector2.zero;
    private Vector2 kickInitialDirection = Vector2.zero;
    private Vector2 kickFinalDirection = Vector2.zero;

    private List<Vector2> kickAvailableRange = new List<Vector2>();

    public class AttachInfo{
        public bool isAttached;
        public Vector2 leftKickBorder;
        public Vector2 rightKickBorder;
        public Vector2 initialDirection;
        public Vector2 finalDirection;
        public Vector2 rightSurfDirection;

    }
    /// <summary>
    /// Fired whenever kick direction vectors are (re)calculated.
    /// Args: kickInitialDirection (clamped aim from CircularMouseLimiter when used), kickFinalDirection, movementDirection (CalculateMoveDirection).
    /// </summary>
    public static event Action<Vector2, Vector2, Vector2> KickVectorsUpdated;
    public static event Action<AttachInfo> AttachInfoUpdated;

    // initial direction
    public static event Action<Vector2> AttachStarted;
    public static event Action AttachFinished;

    // kick direction
    public static event Action<Vector2> KickPerformed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (playerSteering == null)
        {
            playerSteering = GetComponent<PlayerSteering>();
        }
        
        // Keep Rigidbody2D as Dynamic for collisions, but we'll control velocity manually
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f; // No gravity for top-down
        rb.linearDamping = 0f; // No damping - we handle friction manually
        rb.angularDamping = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation; // Prevent rotation
        
        // Initialize velocity
        velocity = Vector2.zero;
        
        // If no camera is assigned, try to find the main camera
        if (playerCamera == null)
        {
            playerCamera = Camera.main; 
        }
    }
    
    /// <summary>
    /// Set the move input from PlayerControls
    /// </summary>
    public void SetMoveInput(Vector2 input)
    {
        moveInput = input;
    }
    
    /// <summary>
    /// Try to perform a dash. Returns true if dash was successful.
    /// </summary>
    public bool TryDash()
    {
        if (!isDashing && dashCooldownTimer <= 0f)
        {
            StartDash();
            return true;
        }
        return false;
    }
    
    public void SetKickActive(bool active)
    {
        if (active == isKickActive)
            return;
        isKickActive = active;
        if (isKickActive){
            kickInputBuffered = false;
            kickBufferTimer = 0f;
            TryKick();
        }
        else{
            kickInputBuffered = true;
            kickBufferTimer = kickBufferTime;
        }
    }

    /// <summary>
    /// Handle kick input when the kick action is pressed or held.
    /// In hold mode, this should be called on press/hold; release is handled by ReleaseKick.
    /// </summary>
    public void OnPressKick()
    {
        SetKickActive(kickOnHold);
    }

    /// <summary>
    /// Release the kick input. When kick state switches to false, start coyote time.
    /// </summary>
    public void OnReleaseKick()
    {
        SetKickActive(!kickOnHold);
    }

    public void SetKickAimHold(bool isHeld)
    {
        isKickAimHeld = isHeld;
        UpdateKickAimTimeScale();
    }

    public void SetSteeringHold(bool isHeld)
    {
        isSteeringHeld = isHeld;
    }

    public void SetBrakeHold(bool isHeld)
    {
        isBrakeHeld = isHeld;
    }
    
    public void TryKick()
    {
        if (!isKickActive)
            return;


        // Old behaviour: treat as a single press with buffering
        if (attachedCollider != null)
        {
            PerformKick();
            kickInputBuffered = false;
        }
        else
        {
            kickInputBuffered = true;
            kickBufferTimer = kickBufferTime;
        }
        return;
    }

    /// <summary>
    /// Get the current velocity vector.
    /// </summary>
    public Vector2 GetVelocity()
    {
        return velocity;
    }

    /// <summary>
    /// Get current movement direction (input/mouse-derived, normalized).
    /// </summary>
    public Vector2 GetMovementDirection()
    {
        return CalculateMoveDirection();
    }
    
    /// <summary>
    /// Set the velocity directly (useful for collision handling).
    /// </summary>
    public void SetVelocity(Vector2 newVelocity)
    {
        velocity = newVelocity;
    }
    
    private void Update()
    {
        // Update dash cooldown timer
        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }
        
        // Update kick buffer timer
        if (kickBufferTimer > 0f)
        {
            kickBufferTimer -= Time.deltaTime;
            if (kickBufferTimer <= 0f)
            {
                kickInputBuffered = false;
            }
        }
        
        // Check if buffered kick can be executed
        if ((isKickActive || kickInputBuffered) && attachedCollider != null)
        {
            PerformKick();
            kickInputBuffered = false;
            kickBufferTimer = 0f;
        }

        UpdateKickAimTimeScale();
        
        // Get mouse position in world space for cursor-based movement (updated in CalculateMoveDirection)
        // This is kept for gizmo visualization
        if (playerCamera != null && Mouse.current != null)
        {
            // For visualization we use the same position as CalculateMoveDirection.
            if (useCircularMouseLimiterForDirection && circularMouseLimiter != null)
            {
                Vector2 targerDir = GetMouseLimiterMoveDirection();
                mousePosition = (Vector2)transform.position + targerDir;
            }
            else
            {
                mousePosition = playerCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            }
        }

        // Keep kick preview vectors updated (used by arrow indicator / gizmos)
        UpdateKickVectors();
    }
    
    private void FixedUpdate()
    {
        float deltaTime = Time.fixedDeltaTime;
        
        if (isDashing)
        {
            playerSteering?.ResetDriftBoardAngle();
            // During dash, maintain moving direction
            // velocity = dashDirection * dashSpeed;
        }
        else
        {
            // While attached to a wall, don't apply movement acceleration or friction
            // This keeps speed constant while choosing kick direction
            if (attachedCollider == null)
            {
                // Calculate movement direction
                Vector2 moveDirection = isSteeringHeld ? CalculateMoveDirection() : velocity.normalized;

                if (isBrakeHeld){
                    velocity = CalculateSkateboardSteeringNewVelocity(-velocity.normalized, deltaTime);
                }
                // else if (isSteeringHeld && moveDirection.magnitude > 0.1f)
                else if (isSteeringHeld)
                {
                    velocity = CalculateSkateboardSteeringNewVelocity(moveDirection, deltaTime);
                }
                else
                {
                    playerSteering?.ResetDriftBoardAngle();
                }

                velocity = ApplyMovementTotalAcceleration(deltaTime);

            }
            else
            {
                playerSteering?.ResetDriftBoardAngle();
            }
        }
        
        // If attached to a surface, project velocity onto the surface (lose normal component)
        if (attachedCollider != null && attachedNormal != Vector2.zero)
        {
            // While attached, we only keep motion along the wall (tangent).
            // Normal component was buffered on attach and removed there.
            Vector2 n = attachedNormal.normalized;
            float normalComponent = Vector2.Dot(velocity, n);
            velocity -= normalComponent * n;
        }

        // Apply calculated velocity to Rigidbody2D for physics collisions
        // We calculate velocity manually, then apply it so collisions work properly
        currentSpeed = velocity.magnitude;
        rb.linearVelocity = velocity;
    }
    
    /// <summary>
    /// Calculate speed delta by calculating acceleration and decceleration.
    /// </summary>
    private Vector2 ApplyMovementTotalAcceleration(float deltaTime)
    {
        Vector2 newVelocity = velocity;
        
        newVelocity += CalculateMovementAcceleration(velocity.normalized, deltaTime);
        newVelocity += CalculateFrictionAcceleration(deltaTime);
        
        return newVelocity;
    }

    /// <summary>
    /// Calculate movement acceleration in the input direction.
    /// </summary>
    private Vector2 CalculateMovementAcceleration(Vector2 moveDirection, float deltaTime)
    {
        if (moveDirection.magnitude < 0.1f)
        {
            return Vector2.zero;
        }
        
        // Acceleration in the direction of input
        Vector2 accelerationVector = moveDirection.normalized * maxAcceleration * deltaTime;
        return accelerationVector;
    }

    /// <summary>
    /// Applies <see cref="PlayerSteering"/> angle and carve speed deltas to velocity.
    /// </summary>
    private Vector2 CalculateSkateboardSteeringNewVelocity(Vector2 moveDirection, float deltaTime)
    {
        moveDirection.Normalize();
        Vector2 newVelocity = playerSteering.CalculateSteeringNewVelocity(velocity, moveDirection, deltaTime);
        return newVelocity;
    }
    

    /// <summary>
    /// Calculate the friction coefficient based on current speed. Should be 1 at maxSpeed.
    /// </summary>
    private float CalculateFrictionMagnitude(float currentSpeed){
        float x = currentSpeed / maxDefaultSpeed;
        if (x <= 1f){
            return maxAcceleration * (x * x);
        }
        else{
            // return maxAcceleration * (1 + Mathf.Log(x));
            return maxAcceleration;
        }
    }

    /// <summary>
    /// Calculate friction acceleration. Friction is opposite to velocity direction
    /// and its magnitude depends on current speed (faster = more friction).
    /// </summary>
    private Vector2 CalculateFrictionAcceleration(float deltaTime)
    {
        float currentSpeed = velocity.magnitude;
        
        // If not moving, no friction
        // if (currentSpeed < 0.01f)
        // {
        //     return Vector2.zero;
        // }
        
        // Friction magnitude scales with speed (faster = more friction)
        float frictionMagnitude = CalculateFrictionMagnitude(currentSpeed) * deltaTime;
        
        // Friction direction is opposite to velocity
        Vector2 frictionDirection = -velocity.normalized;
        
        // Calculate friction acceleration
        Vector2 frictionAcceleration = frictionDirection * frictionMagnitude;
        
        // Don't let friction reverse the direction of velocity
        if (frictionAcceleration.magnitude > currentSpeed)
        {
            // If friction would reverse direction, just stop
            return -velocity;
        }
        
        return frictionAcceleration;
    }

    // private Vector2 CalculateBrakeDeceleration(float deltaTime)
    // {
    //     float speed = velocity.magnitude;
    //     if (speed <= 0.0001f)
    //     {
    //         return Vector2.zero;
    //     }

    //     Vector2 decel = -velocity.normalized * brakeDeceleration * deltaTime;
    //     if (decel.magnitude > speed)
    //     {
    //         return -velocity;
    //     }

    //     return decel;
    // }
    
    private Vector2 GetMouseLimiterMoveDirection()
    {
        if (useCircularMouseLimiterForDirection && circularMouseLimiter != null)
        {
                // capped direction, might be zero if moving against the kick range
                Vector2 targetDirection = circularMouseLimiter.GetVirtualTargetDirection(); //uncapped, same as mouse direction
                Vector2 actualDirection = circularMouseLimiter.GetVirtualClampedDirection(); // clamped by kick borders when attached to surface

                // movementTargetDirection = targetDirection;
                kickInitialDirection = actualDirection;
                return actualDirection.normalized;
        }
        return Vector2.zero;
    }

    private Vector2 CalculateMoveDirection()
    {
        // If there's gamepad/keyboard input, use it directly
        if (moveInput.magnitude > 0.1f)
        {
            return moveInput.normalized;
        }
        
        // Otherwise, if using mouse controls, use mouse direction.
        if (playerCamera != null && Mouse.current != null)
        {
            if (useCircularMouseLimiterForDirection && circularMouseLimiter != null)
            {
                return GetMouseLimiterMoveDirection();
            }
            else
            {
                // Fallback: use the real hardware cursor position
                Vector2 currentMousePos = playerCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                Vector2 directionToMouse = currentMousePos - (Vector2)transform.position;
                
                // Only move towards cursor if it's far enough away (prevents jittery movement)
                if (directionToMouse.magnitude > 1f)
                {
                    return directionToMouse.normalized;
                }
            }
        }
        
        return Vector2.zero;
    }
    
    private void StartDash()
    {
        isDashing = true;
        dashCooldownTimer = dashCooldown;
        
        // Calculate dash direction
        Vector2 moveDirection = CalculateMoveDirection();
        
        // If no movement input, dash in the direction of current velocity, or right if stationary
        if (moveDirection.magnitude < 0.1f)
        {
            if (velocity.magnitude > 0.1f)
            {
                dashDirection = velocity.normalized;
            }
            else
            {
                dashDirection = Vector2.right;
            }
        }
        else
        {
            dashDirection = moveDirection;
        }
        
        // Set dash velocity immediately
        velocity += dashDirection * dashSpeed;
        
        // Invoke end dash after dash duration
        Invoke(nameof(EndDash), dashDuration);
    }
    
    private void EndDash()
    {
        isDashing = false;
        // Keep current velocity instead of zeroing it, for smoother transition
    }
    

    private float CalculateKickSpeed(float initialSpeed){
        float x = initialSpeed / maxDefaultSpeed;
        // float m1 = 1f / (x + 1f);
        // float finalMult = 1 + (kickExtraSpeedMultiplier - 1) * m1;
        // return bufferedNormalSpeed * finalMult + kickExtraSpeedComponent;
        float finalMult = Mathf.Pow(0.75f,  x);
        return initialSpeed + kickExtraSpeedComponent * finalMult;

    }

    /// <summary>
    /// Update kick preview vectors based on the single attached surface.
    /// </summary>
    private void UpdateKickVectors()
    {
        if (attachedCollider == null || attachedNormal == Vector2.zero)
        {
            kickInitialDirection = Vector2.zero;
            kickFinalDirection = Vector2.zero;
            KickVectorsUpdated?.Invoke(kickInitialDirection, kickFinalDirection, CalculateMoveDirection());
            AttachInfoUpdated?.Invoke(new AttachInfo{
                isAttached=false,
            });

            return;
        }

        Vector2 normal = attachedNormal.normalized;
        Vector2 surfRight = new Vector2(normal.y, -normal.x); // perpendicular to normal

        // Use buffered speed and current surface normal; fallback to default if no buffered speed
        float horizontalSpeed = -Vector2.Dot(velocity, surfRight);

        Vector2 moveDirection = CalculateMoveDirection();
        float moveAngle = Vector2.SignedAngle(surfRight, moveDirection);

        Vector2 initialDir = -surfRight * horizontalSpeed + normal * bufferedNormalSpeed; // basically a reflection
        // Vector2 initialDir = kickInitialDirection;

        float initKickSpeed = initialDir.magnitude;
        float kickSpeed = CalculateKickSpeed(initKickSpeed);
        initialDir = initialDir.normalized * kickSpeed;

        float defaultAngle = Vector2.SignedAngle(surfRight, initialDir);

        KickAngleCalculationSimple kickCalc = new KickAngleCalculationSimple();
        KickAngleCalculationSimple.Result result = kickCalc.Calculate(
            defaultAngle,
            moveAngle,
            surfRight,
            kickAngleMaxOffset,
            kickRangeDisplayVectorAmount
        );

        Vector2 finalDir = result.finalKickDirection * kickSpeed;

        // kickInitialDirection = initialDir;
        kickFinalDirection = finalDir;


        List<Vector2> rangeVectors = result.rangeVectors;
        for (int i = 0; i < rangeVectors.Count; i++){
            rangeVectors[i] *= kickSpeed;
        }
        kickAvailableRange = rangeVectors;
        
        Vector2 leftBorder = result.leftBorder;
        Vector2 rightBorder = result.rightBorder;

        KickVectorsUpdated?.Invoke(kickInitialDirection, kickFinalDirection, moveDirection);
        AttachInfoUpdated?.Invoke(new AttachInfo{
            isAttached=true,
            leftKickBorder=leftBorder,
            rightKickBorder=rightBorder,
            initialDirection=kickInitialDirection,
            finalDirection=kickFinalDirection,
            rightSurfDirection=surfRight,
        });

    }

    /// <summary>
    /// Perform the kick from the single attached surface. Fails if kick would be into any surface.
    /// </summary>
    private void PerformKick()
    {
        if (attachedCollider == null || attachedNormal == Vector2.zero)
            return;

        // Vector2 normal = attachedNormal.normalized;
        UpdateKickVectors();
        
        Vector2 direction = kickFinalDirection;
        velocity = direction;

        attachedCollider = null;
        attachedNormal = Vector2.zero;
        bufferedNormalSpeed = 0f;
        kickInitialDirection = Vector2.zero;
        kickFinalDirection = Vector2.zero;

        KickPerformed?.Invoke(direction);
        AttachFinished?.Invoke();
        KickVectorsUpdated?.Invoke(kickInitialDirection, kickFinalDirection, CalculateMoveDirection());
        AttachInfoUpdated?.Invoke(new AttachInfo{
            isAttached=false,
        });
    }
    
    /// <summary>
    /// Handle collision with surfaces. Called by Unity's physics system.
    /// </summary>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleCollision(collision);
    }
    
    /// <summary>
    /// Handle collision stay with surfaces. Called by Unity's physics system.
    /// </summary>
    private void OnCollisionStay2D(Collision2D collision)
    {
        HandleCollision(collision);
    }
    
    /// <summary>
    /// Handle collision exit. Called by Unity's physics system.
    /// Detach when leaving the currently attached surface.
    /// </summary>
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (attachedCollider != null && collision.collider == attachedCollider)
        {
            attachedCollider = null;
            attachedNormal = Vector2.zero;
            bufferedNormalSpeed = 0f;
            AttachFinished?.Invoke();
        }
    }
    
    /// <summary>
    /// Process collision: attach to the first valid surface; ignore others until detached.
    /// </summary>
    private void HandleCollision(Collision2D collision)
    {
        bool isEnemy = enemyLayerMask != 0 && ((1 << collision.gameObject.layer) & enemyLayerMask) != 0;
        if (isEnemy || collision.contactCount == 0) return;

        // If already attached to some surface, do not switch until we detach
        if (attachedCollider != null && collision.collider != attachedCollider)
            return;

        // bool attachStarted = attachedCollider == null;

        Vector2 normalSum = Vector2.zero;
        for (int i = 0; i < collision.contactCount; i++)
        {
            normalSum += collision.GetContact(i).normal;
        }

        Vector2 n = normalSum.normalized;
        float intoWall = Vector2.Dot(velocity, -n); // component into wall

        // moving away from the surface
        if (intoWall < 0){
            return;
        }

        // Attach to this surface
        attachedCollider = collision.collider;
        attachedNormal = n;

        bufferedNormalSpeed += intoWall;
        velocity -= intoWall * n;

        UpdateKickVectors();
        
        // if(attachStarted){
        //     AttachStarted?.Invoke(kickInitialDirection);
        // } 
    }

    private void UpdateKickAimTimeScale()
    {
        GameTimeManager TSManager = GameTimeManager.Instance;
        if (TSManager == null)
            return;

        bool shouldStopTime = isKickAimHeld && attachedCollider != null;

        if (shouldStopTime == isTimeStoppedForKickAim)
            return;

        isTimeStoppedForKickAim = shouldStopTime;
        if (isTimeStoppedForKickAim)
        {
            TSManager.SetAimingMode(GameTimeManager.AimMode.Default);
        }
        else
        {
            TSManager.SetAimingMode(GameTimeManager.AimMode.None);
        }
    }

    private void OnDisable()
    {
        isKickAimHeld = false;
        if (isTimeStoppedForKickAim && GameTimeManager.Instance != null)
        {
            GameTimeManager.Instance.SetAimingMode(GameTimeManager.AimMode.None);
        }
        isTimeStoppedForKickAim = false;
    }
    
    // Gizmos for debugging
    private void OnDrawGizmosSelected()
    {
        if (playerCamera != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, mousePosition);
        }

        // Draw current velocity vector
        if (velocity.sqrMagnitude > 0.0001f)
        {
            Gizmos.color = Color.cyan;
            Vector3 velEnd = transform.position + (Vector3)velocity * 0.25f;
            Gizmos.DrawLine(transform.position, velEnd);
        }
        
        // Draw attached surface normal if any
        if (attachedCollider != null && attachedNormal != Vector2.zero)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)attachedNormal.normalized * bufferedNormalSpeed);

            Gizmos.color = Color.white;
            foreach(Vector2 v in kickAvailableRange){
                Gizmos.DrawLine(transform.position, transform.position + (Vector3)v * 0.25f);
            }
        }
    }

}
