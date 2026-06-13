using UnityEngine;

[ExecuteAlways]
public class PlanetGravity : MonoBehaviour
{
    [Header("Planet Settings")]
    public float gravityStrength = 30f;
    public float planetRadius = 10f;

    // Added to prevent a planet from pulling you from infinite distances
    public float gravityFieldRadius = 25f;

    [Header("Debug")]
    public bool drawGizmos = true;
    public Color surfaceColor = new Color(0.3f, 0.7f, 1f, 0.4f);
    public Color fieldColor = new Color(1f, 0.5f, 0f, 0.15f);

    public Vector3 GetGravityDirection(Vector3 playerPos)
    {
        return (transform.position - playerPos).normalized;
    }

    public float GetDistanceToSurface(Vector3 playerPos)
    {
        float distToCenter = Vector3.Distance(playerPos, transform.position);
        return distToCenter - planetRadius;
    }

    // Helps your GravityManager check if the player is within range
    public bool IsPositionInGravityField(Vector3 playerPos)
    {
        return Vector3.Distance(playerPos, transform.position) <= gravityFieldRadius;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        // Draw solid ground radius
        Gizmos.color = surfaceColor;
        Gizmos.DrawWireSphere(transform.position, planetRadius);

        // Draw outer gravity threshold field
        Gizmos.color = fieldColor;
        Gizmos.DrawWireSphere(transform.position, gravityFieldRadius);
    }
}
