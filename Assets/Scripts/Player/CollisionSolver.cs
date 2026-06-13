using UnityEngine;

public static class CollisionSolver
{
    const float MinMove = 0.0001f;
    const float SkinWidth = 0.02f;
    const int MaxIterations = 6;

    public struct SweepResult
    {
        public Vector3 position;
        public Vector3 normal;
        public bool hit;
    }

    public static SweepResult MoveCapsule(
        Vector3 position,
        Vector3 capsuleCenter,
        float height,
        float radius,
        Vector3 velocity,
        LayerMask mask,
        float stepHeight)
    {
        Vector3 finalPos = position;
        Vector3 remaining = velocity;
        Vector3 up = Vector3.up;

        for (int i = 0; i < MaxIterations; i++)
        {
            if (remaining.sqrMagnitude < MinMove)
                break;

            Vector3 p1, p2;
            GetCapsulePoints(finalPos + capsuleCenter, height, radius, out p1, out p2);

            if (Physics.CapsuleCast(p1, p2, radius, remaining.normalized, out RaycastHit hit, remaining.magnitude + SkinWidth, mask))
            {
                float moveDist = Mathf.Max(hit.distance - SkinWidth, 0f);
                finalPos += remaining.normalized * moveDist;

                Vector3 slideNormal = hit.normal;
                remaining = Vector3.ProjectOnPlane(remaining, slideNormal);

                // Step up attempt
                if (TryStepUp(ref finalPos, capsuleCenter, height, radius, remaining, stepHeight, mask))
                    continue;

                continue;
            }
            else
            {
                finalPos += remaining;
                break;
            }
        }

        return new SweepResult
        {
            position = finalPos,
            normal = Vector3.up,
            hit = false
        };
    }

    static bool TryStepUp(
        ref Vector3 pos,
        Vector3 capsuleCenter,
        float height,
        float radius,
        Vector3 remaining,
        float stepHeight,
        LayerMask mask)
    {
        if (remaining.sqrMagnitude < MinMove)
            return false;

        Vector3 stepUp = Vector3.up * stepHeight;

        Vector3 p1, p2;
        GetCapsulePoints(pos + capsuleCenter + stepUp, height, radius, out p1, out p2);

        if (!Physics.CheckCapsule(p1, p2, radius, mask))
        {
            pos += stepUp;

            Vector3 p1b, p2b;
            GetCapsulePoints(pos + capsuleCenter, height, radius, out p1b, out p2b);

            if (!Physics.CapsuleCast(p1b, p2b, radius, remaining.normalized, out RaycastHit hit, remaining.magnitude, mask))
            {
                pos += remaining;
                return true;
            }
        }

        return false;
    }

    static void GetCapsulePoints(Vector3 center, float height, float radius, out Vector3 p1, out Vector3 p2)
    {
        float half = (height * 0.5f) - radius;
        p1 = center + Vector3.up * half;
        p2 = center - Vector3.up * half;
    }
}
