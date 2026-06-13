using UnityEngine;

public static class SurfaceNormalResolver
{
    public static Vector3 ResolveSurfaceNormal(
        Vector3 position,
        Vector3 gravityDir,
        float rayDistance,
        LayerMask mask,
        Vector3 previousNormal,
        float capsuleRadius)
    {
        Vector3 upDirection = -gravityDir;
        Vector3 accumulatedNormal = Vector3.zero;
        int hitCount = 0;

        // 1. Center Point Sample
        if (Physics.Raycast(position + (upDirection * 0.1f), gravityDir, out RaycastHit centerHit, rayDistance, mask))
        {
            accumulatedNormal += centerHit.normal;
            hitCount++;
        }

        // 2. Perimeter Multi-sampling (Stops edge catches)
        float offsetDist = capsuleRadius * 0.7f;
        Vector3[] offsets = new Vector3[]
        {
            Vector3.Cross(upDirection, Vector3.forward).normalized * offsetDist,
            Vector3.Cross(upDirection, Vector3.back).normalized * offsetDist,
            Vector3.Cross(upDirection, Vector3.right).normalized * offsetDist,
            Vector3.Cross(upDirection, Vector3.left).normalized * offsetDist
        };

        foreach (Vector3 offset in offsets)
        {
            Vector3 rayOrigin = position + offset + (upDirection * 0.1f);
            if (Physics.Raycast(rayOrigin, gravityDir, out RaycastHit edgeHit, rayDistance, mask))
            {
                accumulatedNormal += edgeHit.normal;
                hitCount++;
            }
        }

        if (hitCount > 0)
        {
            Vector3 averageNormal = (accumulatedNormal / hitCount).normalized;
            return Vector3.Slerp(previousNormal, averageNormal, 10f * Time.deltaTime);
        }

        return Vector3.Slerp(previousNormal, upDirection, 5f * Time.deltaTime);
    }
}
