using UnityEngine;

public static class SurfaceNormalResolver
{
    public static Vector3 ResolveSurfaceNormal(
        Vector3 position,
        Vector3 gravityDir,
        float rayDistance,
        LayerMask mask,
        Vector3 previousNormal)
    {
        if (Physics.Raycast(position, gravityDir, out RaycastHit hit, rayDistance, mask))
        {
            return Vector3.Slerp(previousNormal, hit.normal, 0.5f);
        }

        return previousNormal;
    }
}
