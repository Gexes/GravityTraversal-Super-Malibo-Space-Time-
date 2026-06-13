using UnityEngine;

public enum GravityShape { Sphere, Box, Capsule }

[ExecuteAlways]
public class PlanetGravity : MonoBehaviour
{
    [Header("Shape Definition")]
    public GravityShape gravityShape = GravityShape.Sphere;

    [Header("Planet Settings")]
    public float gravityStrength = 30f;

    [Header("Gravity Field Thickness")]
    [Tooltip("How far out past the object's physical edge scale the gravity pull extends.")]
    public float gravityFieldThickness = 15f;

    [Header("Debug")]
    public bool drawGizmos = true;
    public Color surfaceColor = new Color(0.3f, 0.7f, 1f, 0.4f);
    public Color fieldColor = new Color(1f, 0.5f, 0f, 0.15f);

    public Vector3 GetGravityDirection(Vector3 playerPos)
    {
        Vector3 closestPointOnCore = GetClosestPointOnCore(playerPos);
        Vector3 direction = closestPointOnCore - playerPos;

        // Core safety fallback to prevent division-by-zero camera spins
        if (direction.sqrMagnitude < 0.001f)
        {
            return -transform.up; // Default to local downward direction
        }

        return direction.normalized;
    }

    public float GetDistanceToSurface(Vector3 playerPos)
    {
        Vector3 closestPointOnCore = GetClosestPointOnCore(playerPos);
        float distanceToCore = Vector3.Distance(playerPos, closestPointOnCore);

        switch (gravityShape)
        {
            case GravityShape.Box:
                return distanceToCore;
            case GravityShape.Capsule:
                float radius = transform.lossyScale.x * 0.5f;
                return distanceToCore - radius;
            case GravityShape.Sphere:
            default:
                float sphereRadius = transform.lossyScale.x * 0.5f;
                return distanceToCore - sphereRadius;
        }
    }

    public bool IsPositionInField(Vector3 playerPos)
    {
        switch (gravityShape)
        {
            case GravityShape.Box:
                // Transform position to box local space to do an axis-aligned check using lossyScale
                Vector3 localPos = transform.InverseTransformPoint(playerPos);
                Vector3 halfSize = Vector3.one * 0.5f; // Box is 1x1x1 in local space

                // Convert the world thickness value into local space thickness based on scale
                Vector3 localThickness = new Vector3(
                    gravityFieldThickness / Mathf.Max(0.001f, transform.lossyScale.x),
                    gravityFieldThickness / Mathf.Max(0.001f, transform.lossyScale.y),
                    gravityFieldThickness / Mathf.Max(0.001f, transform.lossyScale.z)
                );
                Vector3 maxField = halfSize + localThickness;

                return Mathf.Abs(localPos.x) <= maxField.x &&
                       Mathf.Abs(localPos.y) <= maxField.y &&
                       Mathf.Abs(localPos.z) <= maxField.z;

            case GravityShape.Capsule:
                Vector3 closestCapsulePoint = GetClosestPointOnCapsuleSegment(playerPos);
                float rad = transform.lossyScale.x * 0.5f;
                return Vector3.Distance(playerPos, closestCapsulePoint) <= (rad + gravityFieldThickness);

            case GravityShape.Sphere:
            default:
                float radius = transform.lossyScale.x * 0.5f;
                return Vector3.Distance(playerPos, transform.position) <= (radius + gravityFieldThickness);
        }
    }

    private Vector3 GetClosestPointOnCore(Vector3 playerPos)
    {
        switch (gravityShape)
        {
            case GravityShape.Box:
                Vector3 localPos = transform.InverseTransformPoint(playerPos);
                Vector3 halfSize = Vector3.one * 0.5f; // Unit box dimensions

                float closestX = Mathf.Clamp(localPos.x, -halfSize.x, halfSize.x);
                float closestY = Mathf.Clamp(localPos.y, -halfSize.y, halfSize.y);
                float closestZ = Mathf.Clamp(localPos.z, -halfSize.z, halfSize.z);

                return transform.TransformPoint(new Vector3(closestX, closestY, closestZ));

            case GravityShape.Capsule:
                return GetClosestPointOnCapsuleSegment(playerPos);

            case GravityShape.Sphere:
            default:
                return transform.position;
        }
    }

    private Vector3 GetClosestPointOnCapsuleSegment(Vector3 playerPos)
    {
        Vector3 localPos = transform.InverseTransformPoint(playerPos);

        // Calculate the capsule line segment based directly on local scale values
        float radius = 0.5f; // Unit capsule radius is 0.5
        float totalHeight = transform.lossyScale.y / Mathf.Max(0.001f, transform.lossyScale.x); // Height relative to width
        float capsuleLineHalfLength = Mathf.Max(0f, (totalHeight * 0.5f) - radius);

        localPos.y = Mathf.Clamp(localPos.y, -capsuleLineHalfLength, capsuleLineHalfLength);
        localPos.x = 0f;
        localPos.z = 0f;

        return transform.TransformPoint(localPos);
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Matrix4x4 originalMatrix = Gizmos.matrix;

        // Set the gizmo drawing matrix to look at our object's exact world scale transformations
        Gizmos.matrix = transform.localToWorldMatrix;

        switch (gravityShape)
        {
            case GravityShape.Sphere:
                Gizmos.color = surfaceColor;
                Gizmos.DrawWireSphere(Vector3.zero, 0.5f); // 0.5 local radius * scale = true size

                // Calculate field thickness boundary accurately matching scale
                float localSphereFieldRadius = 0.5f + (gravityFieldThickness / Mathf.Max(0.001f, transform.lossyScale.x));
                Gizmos.color = fieldColor;
                Gizmos.DrawWireSphere(Vector3.zero, localSphereFieldRadius);
                break;

            case GravityShape.Box:
                Gizmos.color = surfaceColor;
                Gizmos.DrawWireCube(Vector3.zero, Vector3.one);

                Vector3 localBoxFieldThickness = new Vector3(
                    (gravityFieldThickness * 2f) / Mathf.Max(0.001f, transform.lossyScale.x),
                    (gravityFieldThickness * 2f) / Mathf.Max(0.001f, transform.lossyScale.y),
                    (gravityFieldThickness * 2f) / Mathf.Max(0.001f, transform.lossyScale.z)
                );
                Gizmos.color = fieldColor;
                Gizmos.DrawWireCube(Vector3.zero, Vector3.one + localBoxFieldThickness);
                break;

            case GravityShape.Capsule:
                DrawCapsuleGizmo(surfaceColor, 0.5f, 2f); // Default unit capsule proportions

                float radOffset = gravityFieldThickness / Mathf.Max(0.001f, transform.lossyScale.x);
                float heightOffset = (gravityFieldThickness * 2f) / Mathf.Max(0.001f, transform.lossyScale.y);
                DrawCapsuleGizmo(fieldColor, 0.5f + radOffset, 2f + heightOffset);
                break;
        }

        Gizmos.matrix = originalMatrix;
    }

    private void DrawCapsuleGizmo(Color color, float radius, float height)
    {
        Gizmos.color = color;
        float halfHeight = Mathf.Max(0f, (height * 0.5f) - radius);

        Gizmos.DrawLine(new Vector3(-radius, -halfHeight, 0), new Vector3(-radius, halfHeight, 0));
        Gizmos.DrawLine(new Vector3(radius, -halfHeight, 0), new Vector3(radius, halfHeight, 0));
        Gizmos.DrawLine(new Vector3(0, -halfHeight, -radius), new Vector3(0, halfHeight, -radius));
        Gizmos.DrawLine(new Vector3(0, -halfHeight, radius), new Vector3(0, halfHeight, radius));

        Gizmos.DrawWireSphere(new Vector3(0, halfHeight, 0), radius);
        Gizmos.DrawWireSphere(new Vector3(0, -halfHeight, 0), radius);
    }
}
