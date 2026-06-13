using UnityEngine;

public class GravityManager : MonoBehaviour
{
    private static PlanetGravity[] _planets;

    private void Awake()
    {
        _planets = FindObjectsOfType<PlanetGravity>();
    }

    public static PlanetGravity GetNearestPlanet(Vector3 playerPos)
    {
        PlanetGravity nearest = null;
        float bestDist = float.MaxValue;

        foreach (var p in _planets)
        {
            float dist = Vector3.Distance(playerPos, p.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                nearest = p;
            }
        }

        return nearest;
    }
}
