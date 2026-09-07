using UnityEngine;

public static class NumericGuard
{
    public const float MinDenominator = 0.00001f;

    public static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    public static bool IsFinite(Vector3 value)
    {
        return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
    }

    public static float SafeDivide(float numerator, float denominator, float fallback = 0f)
    {
        if (!IsFinite(numerator) || !IsFinite(denominator) ||
            Mathf.Abs(denominator) <= MinDenominator)
        {
            return fallback;
        }

        float result = numerator / denominator;
        return IsFinite(result) ? result : fallback;
    }

    public static void SetFloatSafe(
        this Animator animator,
        int parameterHash,
        float value,
        float dampTime,
        float deltaTime)
    {
        float safeValue = IsFinite(value) ? value : 0f;

        if (!IsFinite(deltaTime) || deltaTime <= 0f ||
            !IsFinite(dampTime) || dampTime < 0f)
        {
            animator.SetFloat(parameterHash, safeValue);
            return;
        }

        animator.SetFloat(parameterHash, safeValue, dampTime, deltaTime);
    }
}