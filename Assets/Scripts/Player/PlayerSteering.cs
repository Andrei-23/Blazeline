using UnityEngine;

/// <summary>
/// Skateboard-style steering math: turn rate vs angle and speed, plus carve speed loss vs angle.
/// </summary>
public class PlayerSteering : MonoBehaviour
{
    [Header("Steering")]
    [Tooltip("Desired turn rate at 180° between velocity and input (deg/s). Scales linearly down toward 0°.")]
    [SerializeField] private float baseTurnRateDegPerSec = 420f;

    // [Tooltip("Higher = max turn rate drops faster as speed approaches the reference max speed.")]
    // [SerializeField] private float speedTurnLimit = 1.5f;

    [Tooltip("If the speed is higher, max turn rate is reduced.")]
    [SerializeField] private float unlimitedSteeringMaxSpeed = 20f;


    [Tooltip("If angle delta is smaller, player does not turn.")]
    [SerializeField] private float safeAngleDeltaDeg = 5f;

    [Tooltip("If the deltaAngle is higher, tutn rate is reduced, down to 0 at 180 degree.")]
    [SerializeField] private float minSteeringDecreaseAngle = 130f;

    [Header("Carve")]
    [Tooltip("Extra speed loss per second when carving at 180° (scales with angle²).")]
    [SerializeField] private float carveDeceleration = 4f;

    [Tooltip("If player is slower than that, decceleration is reduced.")]
    [SerializeField] private float maxDecelerationLimitSpeed = 10f;

    [Tooltip("Deceleration multiplyer at zero player speed")]
    [SerializeField] private float minDecelerationMult = 0.3f;


    /// <summary>Last computed drift offset for (signed degrees, yaw).</summary>
    public float DriftBoardExtraAngleDeg { get; private set; }

    public void ResetDriftBoardAngle()
    {
        DriftBoardExtraAngleDeg = 0f;
    }

    float GetMaxAngleDeltaCoeff(float speed)
    {
        if (speed <= unlimitedSteeringMaxSpeed)
        {
            return 1f;
        }

        return unlimitedSteeringMaxSpeed / speed;
    }

    /// <summary>Same cap as steering: 90° × speed coeff.</summary>
    public float GetMaxDeltaAngleForSpeed(float speed)
    {
        return 90f * GetMaxAngleDeltaCoeff(speed);
    }

    /// <summary>
    /// Signed extra yaw for drift look. Scales 0→1 as angle between velocity and input goes from
    /// maxDeltaAngle/2 to maxDeltaAngle (same maxDeltaAngle as steering).
    /// </summary>
    public float CalculateDriftBoardExtraAngleDeg(float angleBetweenVelAndInputDeg, float signedAngleDeg, float speed)
    {
        if (speed < 0.01f)
        {
            return 0f;
        }

        float maxDeltaAngle = GetMaxDeltaAngleForSpeed(speed);
        float l = maxDeltaAngle * 0.5f;
        float r = minSteeringDecreaseAngle;

        if (angleBetweenVelAndInputDeg < l)
        {
            return 0f;
        }

        float t = angleBetweenVelAndInputDeg >= r
            ? 1f
            : Mathf.InverseLerp(l, r, angleBetweenVelAndInputDeg);


        float p = 0.5f * (1f - Mathf.Cos(t * Mathf.PI)); // smooth
        return Mathf.Sign(signedAngleDeg) * p;
    }

    void SetDriftFromCurrentSteering(float angleDeg, float signedAngleDeg, float speed)
    {
        DriftBoardExtraAngleDeg = CalculateDriftBoardExtraAngleDeg(angleDeg, signedAngleDeg, speed);
    }

    /// <summary>
    /// Signed angle in degrees to rotate the velocity direction toward input this step (shortest path).
    /// </summary>
    /// <param name="deltaAngleDeg">Unsigned angle between velocity and input (0..180).</param>
    /// <param name="signedAngleDeg">Signed angle from velocity to input (-180..180).</param>
    /// <param name="speed">Current speed magnitude.</param>
    /// <param name="maxReferenceSpeed">Typical max speed used to scale the turn-rate cap.</param>
    /// <param name="deltaTime">Fixed timestep.</param>
    public float CalculateSteeringDeltaAngleDeg(
        float deltaAngleDeg,
        // float signedAngleDeg,
        float speed,
        // float maxReferenceSpeed,
        float deltaTime)
    {
        if (deltaAngleDeg < safeAngleDeltaDeg || speed < 0.01f)
        {
            return 0f;
        }

        // float angleNorm = angleDeg / 180f;
        // float omegaDesired = baseTurnRateDegPerSec * angleNorm;
        // float speedRatio = Mathf.Clamp01(speed / Mathf.Max(0.01f, maxReferenceSpeed));
        // float omegaCap = baseTurnRateDegPerSec / (1f + speedTurnLimit * speedRatio * speedRatio);
        // float omegaDegPerSec = Mathf.Min(omegaDesired, omegaCap);

        // float maxTurnThisFrame = omegaDegPerSec * deltaTime;
        // float turnDeg = Mathf.Min(Mathf.Abs(signedAngleDeg), maxTurnThisFrame);
        // return Mathf.Sign(signedAngleDeg) * turnDeg;
        float maxAngleDeltaCoeff = GetMaxAngleDeltaCoeff(speed);
        float maxDeltaAngle = 90f * maxAngleDeltaCoeff; // AngleDeg is capped at this value
        if (maxDeltaAngle < safeAngleDeltaDeg){
            Debug.LogWarning("Max delta angle is too small");
            return 0f;
        }

        float steeringPower;
        if (deltaAngleDeg > minSteeringDecreaseAngle){
            steeringPower = 1f - Mathf.InverseLerp(minSteeringDecreaseAngle, 180f, deltaAngleDeg);
        }
        else if (deltaAngleDeg > maxDeltaAngle){
            steeringPower = 1;
        }
        else{
            steeringPower = Mathf.InverseLerp(safeAngleDeltaDeg, maxDeltaAngle, deltaAngleDeg);
        }
        float smoothSteeringPower = 0.5f * (1f - Mathf.Cos(steeringPower * Mathf.PI));
        float turnDeg = smoothSteeringPower * baseTurnRateDegPerSec * maxAngleDeltaCoeff * deltaTime;
        turnDeg = Mathf.Clamp(turnDeg, 0f, deltaAngleDeg);
        return turnDeg;
    }

    /// <summary>
    /// Positive speed to subtract this step from carve drag (larger angle between velocity and input = more loss).
    /// </summary>
    /// <param name="angleDeg">Unsigned angle between velocity and input (0..180).</param>
    public float CalculateCarveDecelerationDeltaSpeed(float angleDeg, float deltaTime, float speed)
    {
        float speedMult = speed / maxDecelerationLimitSpeed;
        speedMult = Mathf.Clamp(speedMult, 0f, 1f);
        speedMult = Mathf.Lerp(minDecelerationMult, 1f, speedMult);

        float angleNorm = angleDeg / 180f;
        return carveDeceleration * angleNorm * angleNorm * speedMult * deltaTime * PlayerBuffManager.Instance.GetSpeedBoost();
    }

    /// <summary>
    /// Rotates velocity toward <paramref name="moveDirection"/> by up to
    /// <see cref="CalculateSteeringDeltaAngleDeg"/> (signed), then applies carve deceleration.
    /// </summary>
    public Vector2 CalculateSteeringNewVelocity(Vector2 velocity, Vector2 moveDirection, float deltaTime)
    {
        float speed = velocity.magnitude;
        if (speed < 0.001f)
        {
            ResetDriftBoardAngle();
            return moveDirection.normalized * 0.1f;
        }

        if (moveDirection.sqrMagnitude < 0.0001f)
        {
            ResetDriftBoardAngle();
            return velocity;
        }

        moveDirection.Normalize();
        Vector2 velDir = velocity / speed;

        float angleDeg = Vector2.Angle(velDir, moveDirection);
        float signedAngleDeg = Vector2.SignedAngle(velDir, moveDirection);

        SetDriftFromCurrentSteering(angleDeg, signedAngleDeg, speed);

        float decelerationDelta = CalculateCarveDecelerationDeltaSpeed(angleDeg, deltaTime, speed);
        float turnMagDeg = CalculateSteeringDeltaAngleDeg(angleDeg, speed, deltaTime);
        float signedTurnDeg = Mathf.Sign(signedAngleDeg) * turnMagDeg;

        float turnRad = signedTurnDeg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(turnRad);
        float sin = Mathf.Sin(turnRad);
        Vector2 newDir = new Vector2(
            velDir.x * cos - velDir.y * sin,
            velDir.x * sin + velDir.y * cos
        );
        newDir.Normalize();

        float finalSpeed = Mathf.Max(0f, speed - decelerationDelta);
        return newDir * finalSpeed;
    }
}
