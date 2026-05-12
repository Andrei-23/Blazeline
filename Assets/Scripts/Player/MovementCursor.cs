using UnityEngine;

public class MovementCursor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject cursorEndPoint;
    [SerializeField] private GameObject cursorRotationPoint;
    [SerializeField] private Transform directionOrigin;
    [SerializeField] private CircularMouseLimiter circularMouseLimiter;
    [SerializeField] private float cursorDistanceFromOrigin = 2f;

    // [Header("Rotation")]
    // [SerializeField] private float angleOffsetDegrees = -90f;

    public enum CursorType
    {
        Move,
        Kick,
    }

    private CursorType currentType = CursorType.Move;
    private Vector2 cachedKickInitialDirection = Vector2.zero;
    private bool hasKickInitialDirection = false;

    private void Awake()
    {
        if (cursorRotationPoint == null)
        {
            cursorRotationPoint = gameObject;
        }
        if (cursorEndPoint == null)
        {
            cursorEndPoint = gameObject;
        }

    }

    private void Update()
    {
        UpdateDirection();
    }

    private void OnEnable()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void OnDisable()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
    
    public void UpdateDirection(){
        
        if (cursorRotationPoint == null || circularMouseLimiter == null)
        {
            return;
        }

        Vector2 direction = hasKickInitialDirection
            ? cachedKickInitialDirection
            : circularMouseLimiter.GetVirtualTargetDirection();
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        if (directionOrigin != null && cursorEndPoint != null)
        {
            cursorEndPoint.transform.position = directionOrigin.position + (Vector3)(direction.normalized * cursorDistanceFromOrigin);
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        SetRotation(angle);
    }

    public void SetRotation(float angle)
    {
        if (cursorRotationPoint == null)
        {
            return;
        }

        cursorRotationPoint.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }


    public void SetType(CursorType type)
    {
        currentType = type;
    }
}
