using UnityEngine;

[ExecuteAlways]
public class PlanetGravity : MonoBehaviour
{
    [Header("Planet Settings")]
    public float gravityStrength = 30f;
    public float planetRadius = 10f;

    [Header("Debug")]
    public bool drawGizmos = true;
    public Color gizmoColor = new Color(0.3f, 0.7f, 1f, 0.4f);

    public Vector3 GetGravityDirection(Vector3 playerPos)
    {
        return (transform.position - playerPos).normalized;
    }

    public float GetDistanceToSurface(Vector3 playerPos)
    {
        float distToCenter = Vector3.Distance(playerPos, transform.position);
        return distToCenter - planetRadius;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, planetRadius);
    }
}
