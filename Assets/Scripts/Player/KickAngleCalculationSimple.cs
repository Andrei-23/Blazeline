using System.Collections.Generic;
using UnityEngine;

public class KickAngleCalculationSimple
{
    public class Result{
        public float leftAngleNorm;
        public float rightAngleNorm;
        public Vector2 leftBorder;
        public Vector2 rightBorder;
        public Vector2 finalKickDirection;
        public List<Vector2> rangeVectors;
    }

    private float VectorToAngle(Vector2 dir){
        return Vector2.SignedAngle(Vector2.right, dir);
    }
    private Vector2 AngleToVector(float angle){
        float angleRad = angle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
    }
    private Vector2 VectorApplyNorm(Vector2 dir, Vector2 surfRight){
        surfRight = surfRight.normalized;
        float a = dir.x;
        float b = dir.y;
        float c = surfRight.x;
        float d = surfRight.y;
        return new Vector2(a * c - b * d, a * d + b * c);
    }
    private Vector2 VectorApplyNorm(float angle, Vector2 surfRight){
        return VectorApplyNorm(AngleToVector(angle), surfRight);
    }
    

    private float GetRightBorderAngle(float defaultAngle, float maxAngleOffset){
        if (defaultAngle >= maxAngleOffset * 2){
            return defaultAngle - maxAngleOffset;
        }
        return 0.25f * defaultAngle * defaultAngle / maxAngleOffset;
    }

    private float GetLeftBorderAngle(float defaultAngle, float maxAngleOffset){
        return 180f - GetRightBorderAngle(180f - defaultAngle, maxAngleOffset);
    }

    // private List<float> GetBorderAngles(float defaultAngle, float maxAngleOffset){
    //     return new List<float> {
    //         GetLeftBorderAngle(defaultAngle, maxAngleOffset),
    //         GetRightBorderAngle(defaultAngle, maxAngleOffset),
    //     };
    // }

    private Vector2 CalculateFinalKickDirection(float angleL, float angleR, float moveAngle, Vector2 surfRight){
        float finalAngle = Mathf.Clamp(moveAngle, angleR, angleL);
        return VectorApplyNorm(finalAngle, surfRight);
    }

    private List<Vector2> CalculateRangeVectors(float angleL, float angleR, int rangeVectorAmount, Vector2 surfRight){
        List<Vector2> res = new List<Vector2>();
        for(int i = 0; i < rangeVectorAmount; i++){
            float a = angleR + (angleL - angleR) * ((float)i / (rangeVectorAmount - 1));
            a = Mathf.Clamp(a, angleR, angleL);
            res.Add(VectorApplyNorm(a, surfRight));
        }
        return res;
    }

    public Result Calculate(float defaultAngle, float moveAngle, Vector2 surfRight, float maxAngleOffset, int rangeVectorAmount){
        float angleL = GetLeftBorderAngle(defaultAngle, maxAngleOffset);
        float angleR = GetRightBorderAngle(defaultAngle, maxAngleOffset);
        Vector2 finalKickDir = CalculateFinalKickDirection(angleL, angleR, moveAngle, surfRight);
        List<Vector2> rangeVectors = CalculateRangeVectors(angleL, angleR, rangeVectorAmount, surfRight);

        return new Result{
            leftAngleNorm = angleL,
            rightAngleNorm = angleR,
            leftBorder = VectorApplyNorm(angleL, surfRight),
            rightBorder = VectorApplyNorm(angleR, surfRight),
            finalKickDirection = finalKickDir,
            rangeVectors = rangeVectors,
        };
    }
}
