using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal.Internal;

/// <summary>
/// Helper class containing all kick angle and velocity calculation logic.
/// </summary>
public static class KickAngleCalculation
{
    public static float GetKickMagnitudeByAngle(float kickSpeed, float kickAngle, float kickMinSpeed, float kickMaxSpeed, float kickMaxAngleOffsetDeg)
    {
        if (Mathf.Abs(kickAngle - Mathf.PI / 2f) > kickMaxAngleOffsetDeg * Mathf.Deg2Rad || kickSpeed <= 0.0001f)
        {
            return 0f;
        }

        float r = kickMinSpeed + Mathf.Sin(2f * kickAngle - Mathf.PI * 0.5f) * (kickMaxSpeed - kickMinSpeed);
        return r * kickSpeed;
    }

    public static float GetKickModifiedAngle(float kickSpeed, float horizontalSpeed, float kickAngle, float kickMinSpeed, float kickMaxSpeed, float kickMaxAngleOffsetDeg)
    {
        float r = GetKickMagnitudeByAngle(kickSpeed, kickAngle, kickMinSpeed, kickMaxSpeed, kickMaxAngleOffsetDeg);
        float h = Mathf.Sin(kickAngle) * r;
        float dx = Mathf.Cos(kickAngle) * r;
        return Mathf.PI - Mathf.Atan2(h, horizontalSpeed - dx);
    }

    public static Vector2 GetKickVelocityByAngle(float kickSpeed, float kickAngle, float kickMinSpeed, float kickMaxSpeed, float kickMaxAngleOffsetDeg)
    {
        float r = GetKickMagnitudeByAngle(kickSpeed, kickAngle, kickMinSpeed, kickMaxSpeed, kickMaxAngleOffsetDeg);
        return new Vector2(Mathf.Cos(kickAngle), Mathf.Sin(kickAngle)) * r;
    }

    public static Vector2 GetTotalVelocityByAngle(float kickSpeed, float horizontalSpeed, float kickAngle, float kickMinSpeed, float kickMaxSpeed, float kickMaxAngleOffsetDeg)
    {
        return GetKickVelocityByAngle(kickSpeed, kickAngle, kickMinSpeed, kickMaxSpeed, kickMaxAngleOffsetDeg) + Vector2.left * horizontalSpeed;
        // Not the actual velocity, has to be rotated first.
    }

    /// <summary>
    /// Calculates possible range of angles after kick. Uses ternary search.
    /// </summary>
    public static List<float> CalculateKickBorderAngles(float kickSpeed, float horizontalSpeed, float kickMinSpeed, float kickMaxSpeed, float kickMaxAngleOffsetDeg)
    {
        float pi = (float)Math.PI;

        if (Mathf.Abs(horizontalSpeed) < 0.0001f)
        {
            return new List<float> {
                kickMaxAngleOffsetDeg * Mathf.Deg2Rad + pi / 2,
                -kickMaxAngleOffsetDeg * Mathf.Deg2Rad + pi / 2,
            };
        }

        if (horizontalSpeed < 0f)
        {
            List<float> res = CalculateKickBorderAngles(kickSpeed, -horizontalSpeed, kickMinSpeed, kickMaxSpeed, kickMaxAngleOffsetDeg);
            return new List<float> { pi - res[1], pi - res[0] };
        }

        float l = 0f, r = pi, EPS = 0.001f;
        float m1, m2, a1, a2;
        while (r - l > EPS)
        {
            m1 = l + (r - l) / 3;
            m2 = r - (r - l) / 3;
            a1 = GetKickModifiedAngle(kickSpeed, horizontalSpeed, m1, kickMinSpeed, kickMaxSpeed, kickMaxAngleOffsetDeg);
            a2 = GetKickModifiedAngle(kickSpeed, horizontalSpeed, m2, kickMinSpeed, kickMaxSpeed, kickMaxAngleOffsetDeg);
            if (a1 > a2)
                l = m1;
            else
                r = m2;
        }

        return new List<float> { kickMaxAngleOffsetDeg * Mathf.Deg2Rad + pi / 2, (l + r) * 0.5f };
    }

    /// <summary>
    /// Calculates extra velocity angle that gives needed final angle. Uses binary search.
    /// </summary>
    public static float CalculateKickAngleFromFinalAngle(float kickSpeed, float horizontalSpeed, float finalAngle, float angleL, float angleR, float kickMinSpeed, float kickMaxSpeed, float kickMaxAngleOffsetDeg)
    {
        float pi = (float)Math.PI;

        if (angleL < angleR)
        {
            Debug.LogError("incorrect border angles");
            return pi / 2;
        }

        float l = angleR, r = angleL, EPS = 0.001f;
        float m, a;
        while (r - l > EPS)
        {
            m = (l + r) / 2;
            a = GetKickModifiedAngle(kickSpeed, horizontalSpeed, m, kickMinSpeed, kickMaxSpeed, kickMaxAngleOffsetDeg);
            if (a < finalAngle)
                l = m;
            else
                r = m;
        }

        return (l + r) * 0.5f;
    }

    /// <summary>
    /// Generates velocity vectors in angle range. Used for visuals.
    /// </summary>
    public static List<Vector2> CalculateKickRangeVectors(float kickSpeed, float horizontalSpeed, int amount, float kickMinSpeed, float kickMaxSpeed, float kickMaxAngleOffsetDeg)
    {
        List<float> borders = CalculateKickBorderAngles(kickSpeed, horizontalSpeed, kickMinSpeed, kickMaxSpeed, kickMaxAngleOffsetDeg);
        float angleL = borders[0];
        float angleR = borders[1];

        // 2pi >= angleL > angleR >= 0
        if (amount < 2 || angleL < angleR)
        {
            return new List<Vector2>();
        }

        float d = (angleL - angleR) / (amount - 1);
        List<Vector2> res = new List<Vector2>();
        float a = angleR + 0.001f;

        for (int i = 0; i < amount; i++)
        {
            float a1 = Mathf.Clamp(a, angleR, angleL - 0.001f);
            float m = GetKickMagnitudeByAngle(kickSpeed, a1, kickMinSpeed, kickMaxSpeed, kickMaxAngleOffsetDeg);
            Vector2 v = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * m;
            v += Vector2.left * horizontalSpeed;
            v *= m / v.magnitude;
            res.Add(v);
            a += d;
        }
        return res;
    }

    /// <summary>
    /// Calculates kick optimal direction with given movement angle.
    /// </summary>
    public static Vector2 CalculateFinalKickDirection(float kickSpeed, float horizontalSpeed, float movementAngle, float kickMinSpeed, float kickMaxSpeed, float kickMaxAngleOffsetDeg)
    {
        List<float> borders = CalculateKickBorderAngles(kickSpeed, horizontalSpeed, kickMinSpeed, kickMaxSpeed, kickMaxAngleOffsetDeg);
        float angleInit = GetKickModifiedAngle(kickSpeed, horizontalSpeed, Mathf.PI / 2f, kickMinSpeed, kickMaxSpeed, kickMaxAngleOffsetDeg);
        float angleL = borders[0];
        float angleR = borders[1];
        float finalAngleL = GetKickModifiedAngle(kickSpeed, horizontalSpeed, angleL, kickMinSpeed, kickMaxSpeed, kickMaxAngleOffsetDeg);
        float finalAngleR = GetKickModifiedAngle(kickSpeed, horizontalSpeed, angleR, kickMinSpeed, kickMaxSpeed, kickMaxAngleOffsetDeg);

        float finalMovementAngle = Mathf.Clamp(movementAngle, finalAngleR, finalAngleL);
        float finalKickAngle = CalculateKickAngleFromFinalAngle(kickSpeed, horizontalSpeed, finalMovementAngle, angleL, angleR, kickMinSpeed, kickMaxSpeed, kickMaxAngleOffsetDeg);
        return GetTotalVelocityByAngle(kickSpeed, horizontalSpeed, finalKickAngle, kickMinSpeed, kickMaxSpeed, kickMaxAngleOffsetDeg);
    }

    public static List<Vector2> CalculateKickBorderDirections(float kickSpeed, float horizontalSpeed, float kickMinSpeed, float kickMaxSpeed, float kickMaxAngleOffsetDeg)
    {
        List<float> borders = CalculateKickBorderAngles(kickSpeed, horizontalSpeed, kickMinSpeed, kickMaxSpeed, kickMaxAngleOffsetDeg);
        float angleL = borders[0];
        float angleR = borders[1];
        List<Vector2> res = new List<Vector2>();
        for(int i = 0; i < 2; i++)
        {
            float kick_angle = CalculateKickAngleFromFinalAngle(kickSpeed, horizontalSpeed, borders[i], angleL, angleR, kickMinSpeed, kickMaxSpeed, kickMaxAngleOffsetDeg);
            res.Add(new Vector2(Mathf.Cos(kick_angle), Mathf.Sin(kick_angle)));
        }
        return res;
    }

    public static Vector2 RotateVectorFromSurface(Vector2 velocity, Vector2 surfRight)
    {
        surfRight = surfRight.normalized;
        float a = velocity.x;
        float b = velocity.y;
        float c = surfRight.x;
        float d = surfRight.y;
        return new Vector2(a * c - b * d, a * d + b * c);
    }
}

