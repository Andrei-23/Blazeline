using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Creates a camera-independent virtual mouse direction.
/// Intended for mouse movement where direction is always at full magnitude.
///
/// Behaviour:
/// - Stores a persistent angle + normalized direction.
/// - Mouse delta changes target angle and steers towards it.
/// - The hardware cursor is hidden/locked; no screen-edge barrier is needed.
/// </summary>
public class CircularMouseLimiter : MonoBehaviour
{
    [Header("Direction Settings")]
    [Tooltip("How many degrees the virtual angle can turn per pixel of mouse delta.")]
    public float rotationSensitivity = 0.25f;

    /// <summary>
    /// Current angle of the virtual direction in degrees.
    /// </summary>
    public float CurrentAngleDegrees { get; private set; }
    public Vector2 LeftBorder { get; private set; }
    public Vector2 RightBorder { get; private set; }
    public bool UseBorders { get; private set; }

    
    /// <summary>
    /// Current normalized virtual direction.
    /// </summary>
    // public Vector2 CurrentDirection { get; private set; }

    private void Awake()
    {
        // CurrentDirection = Vector2.up;
        CurrentAngleDegrees = 90f;

        UseBorders = false;
        LeftBorder = Vector2.up;
        RightBorder = Vector2.up;
    }

    private void Update()
    {
        if (Mouse.current == null)
        {
            return;
        }

        // Steer current angle towards mouse delta direction.
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        if (mouseDelta.sqrMagnitude > 0.000001f)
        {
            float targetAngle = Mathf.Atan2(mouseDelta.y, mouseDelta.x) * Mathf.Rad2Deg;
            float signedAngleToTarget = Mathf.DeltaAngle(CurrentAngleDegrees, targetAngle);

            float maxTurnThisFrame = mouseDelta.magnitude * rotationSensitivity;
            float turnAmount = Mathf.Clamp(signedAngleToTarget, -maxTurnThisFrame, maxTurnThisFrame);
            CurrentAngleDegrees += turnAmount;
            CurrentAngleDegrees = Mathf.Repeat(CurrentAngleDegrees, 360f);

        }
    }

    
    private void OnAttachStarted(Vector2 initialDirection)
    {
        CurrentAngleDegrees = VectorToAngle(initialDirection);
    }

    private float VectorToAngle(Vector2 dir){
        if (dir.sqrMagnitude < 0.0001f)
        {
            return 0f;
        }
        return Vector2.SignedAngle(Vector2.right, dir);
    }
    private Vector2 AngleToVector(float angle){
        float angleRad = angle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
    }

    private void OnAttachInfoUpdated(PlayerMovement.AttachInfo info)
    {
        if(!info.isAttached){
            UseBorders = false;
            return;
        }

        UseBorders = true;
        LeftBorder = info.leftKickBorder;
        RightBorder = info.rightKickBorder;
    }

    /// <summary>
    /// Uncapped steering direction from mouse delta (same as movement direction).
    /// </summary>
    public Vector2 GetVirtualTargetDirection()
    {
        return AngleToVector(CurrentAngleDegrees);
    }

    /// <summary>
    /// Initial direction clamped to the sector between LeftBorder and RightBorder when attached.
    /// </summary>
    public Vector2 GetVirtualClampedDirection()
    {
        Vector2 initial = GetVirtualTargetDirection();
        if (!UseBorders || initial.sqrMagnitude < 0.0001f)
        {
            return initial;
        }
        return ClampDirectionToKickBorders(initial, LeftBorder, RightBorder);
    }

    private static Vector2 ClampDirectionToKickBorders(Vector2 cur, Vector2 left, Vector2 right)
    {
        cur = cur.normalized;
        left = left.normalized;
        right = right.normalized;
        float la = Vector2.Angle(left, cur);
        float ra = Vector2.Angle(right, cur);
        if (la + ra - Vector2.Angle(left, right) >= 0.001f)
        {
            return la < ra ? left : right;
        }
        return cur;
    }

    /// <summary>
    /// Returns normalized direction from screen center to virtual position.
    /// Always non-zero while enabled.
    /// </summary>
    /// 
    private void OnDrawGizmosSelected()
    {
        // Draw the saved direction as a gizmo ray.
        Vector3 origin = transform.position;

        Gizmos.color = Color.yellow;
        Vector3 endInitial = origin + (Vector3)GetVirtualTargetDirection();
        Gizmos.DrawLine(origin, endInitial);
        Gizmos.DrawSphere(endInitial, 0.1f);

        if (UseBorders)
        {
            Gizmos.color = Color.cyan;
            Vector3 endFinal = origin + (Vector3)GetVirtualClampedDirection();
            Gizmos.DrawLine(origin, endFinal);
            Gizmos.DrawSphere(endFinal, 0.08f);
        }
    }

    private void OnEnable()
    {
        PlayerMovement.AttachStarted += OnAttachStarted;
        PlayerMovement.AttachInfoUpdated += OnAttachInfoUpdated;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void OnDisable()
    {
        PlayerMovement.AttachStarted -= OnAttachStarted;
        PlayerMovement.AttachInfoUpdated -= OnAttachInfoUpdated;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}

