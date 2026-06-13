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
        float stepHeight,
        Vector3 playerUp) // Added playerUp parameter
    {
        Vector3 finalPos = position;
        Vector3 remaining = velocity;
        Vector3 hitNormal = playerUp;
        bool hasHit = false;

        for (int i = 0; i < MaxIterations; i++)
        {
            if (remaining.sqrMagnitude < MinMove)
                break;

            Vector3 p1, p2;
            // Use current playerUp instead of Vector3.up
            GetCapsulePoints(finalPos + capsuleCenter, height, radius, playerUp, out p1, out p2);

            if (Physics.CapsuleCast(p1, p2, radius, remaining.normalized, out RaycastHit hit, remaining.magnitude + SkinWidth, mask))
            {
                hasHit = true;
                float moveDist = Mathf.Max(hit.distance - SkinWidth, 0f);
                finalPos += remaining.normalized * moveDist;

                hitNormal = hit.normal;

                // FIX: Only attempt a Step-Up if the wall angle is steep (like a real ledge or wall)!
                // If you are running on a smooth sphere primitive, the dot product will be near 0, meaning it's a floor.
                float wallSteepness = Vector3.Dot(hitNormal, playerUp);

                // A value below 0.3f means the surface normal is nearly horizontal (a steep wall / step)
                if (wallSteepness < 0.3f && TryStepUp(ref finalPos, capsuleCenter, height, radius, remaining, stepHeight, mask, playerUp))
                {
                    continue;
                }

                // Otherwise, slide along the primitive surface curvature smoothly
                remaining = Vector3.ProjectOnPlane(remaining, hitNormal);
                continue;
            }
        }

        return new SweepResult
        {
            position = finalPos,
            normal = hitNormal,
            hit = hasHit
        };
    }

    static bool TryStepUp(
        ref Vector3 pos,
        Vector3 capsuleCenter,
        float height,
        float radius,
        Vector3 remaining,
        float stepHeight,
        LayerMask mask,
        Vector3 playerUp) // Added playerUp parameter
    {
        if (remaining.sqrMagnitude < MinMove)
            return false;

        // Step direction is now relative to the planet surface
        Vector3 stepUp = playerUp * stepHeight;

        Vector3 p1, p2;
        GetCapsulePoints(pos + capsuleCenter + stepUp, height, radius, playerUp, out p1, out p2);

        if (!Physics.CheckCapsule(p1, p2, radius, mask))
        {
            pos += stepUp;

            Vector3 p1b, p2b;
            GetCapsulePoints(pos + capsuleCenter, height, radius, playerUp, out p1b, out p2b);

            if (!Physics.CapsuleCast(p1b, p2b, radius, remaining.normalized, out RaycastHit hit, remaining.magnitude, mask))
            {
                pos += remaining;
                return true;
            }
        }

        return false;
    }

    // Rewritten to split along the sphere's local normal frame
    static void GetCapsulePoints(Vector3 center, float height, float radius, Vector3 playerUp, out Vector3 p1, out Vector3 p2)
    {
        float half = (height * 0.5f) - radius;
        p1 = center + playerUp * half;
        p2 = center - playerUp * half;
    }
}
