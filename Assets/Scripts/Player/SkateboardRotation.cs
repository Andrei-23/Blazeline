using UnityEngine;

public class SkateboardRotation : MonoBehaviour
{
    [SerializeField] private float maxRotationSpeed = 1080f; // degree per sec
    [SerializeField] private Transform RotationPoint;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerSteering playerSteering;
    [SerializeField] private WheelsParticles wheelsParticles;

    [Header("Drift visual")]
    [Tooltip("Max extra board yaw (degrees) when velocity↔input angle is in [maxDeltaAngle/2, maxDeltaAngle].")]
    [SerializeField] private float maxDriftBoardAngleDeg = 22f;
    [Tooltip("How fast drift yaw catches up to PlayerSteering (higher = snappier).")]
    [SerializeField] private float driftAngleSmoothing = 14f;

    private float requiredAngle;
    private float currentAngle = 0f;
    private float currentDriftYawDeg = 0f;

    private const float FullCircle = 360f;

    private static float NormalizeAngle360(float angle)
    {
        // Keeps angles in [0, 360) so wrap-around logic behaves predictably.
        return Mathf.Repeat(angle, FullCircle);
    }

    private void Awake()
    {
        if (playerSteering == null && playerMovement != null)
        {
            playerSteering = playerMovement.GetComponent<PlayerSteering>();
        }
    }

    private void UpdateRotation()
    {
        // float yawDeg = 90f - currentAngle + currentDriftYawDeg;
        RotationPoint.localRotation = Quaternion.Euler(0f, 90f - currentAngle, 0f);
    }

    void Start()
    {
        if (RotationPoint == null)
        {
            RotationPoint = transform;
        }

        // Initialize currentAngle from existing RotationPoint rotation to avoid snapping.
        // We use the inverse of: xRot = 90 - currentAngle
        currentAngle = NormalizeAngle360(90f - RotationPoint.eulerAngles.y);
        requiredAngle = NormalizeAngle360(requiredAngle);

        UpdateRotation();
    }


    void Update()
    {
        if (RotationPoint == null)
        {
            return;
        }


        float targetDriftCoeff = playerSteering != null ? playerSteering.DriftBoardExtraAngleDeg : 0f;
        float targetDrift = targetDriftCoeff * maxDriftBoardAngleDeg;
        float driftLerp = 1f - Mathf.Exp(-driftAngleSmoothing * Time.deltaTime);
        currentDriftYawDeg = Mathf.Lerp(currentDriftYawDeg, targetDrift, driftLerp);

        Vector2 velocity = playerMovement.GetVelocity();
        requiredAngle = Vector2.SignedAngle(Vector2.right, velocity);
        requiredAngle += currentDriftYawDeg;
        requiredAngle = NormalizeAngle360(requiredAngle);

        // delta is the signed shortest path in degrees (-180..180].
        float delta = Mathf.DeltaAngle(currentAngle, requiredAngle);
        float step = maxRotationSpeed * Time.deltaTime;

        if (Mathf.Abs(delta) <= step)
        {
            currentAngle = requiredAngle;
        }
        else
        {
            // Move along the shorter arc (clockwise/counterclockwise depending on delta sign).
            currentAngle = NormalizeAngle360(currentAngle + Mathf.Sign(delta) * step);
        }

        wheelsParticles?.SetParticlePower(targetDriftCoeff);
        UpdateRotation();
    }

    // Optional API for other scripts to set the target angle at runtime.
    public void SetRequiredAngle(float angle)
    {
        requiredAngle = NormalizeAngle360(angle);
    }
}
