using UnityEngine;

public class MovingPlatformParent : MonoBehaviour
{
    [SerializeField] private LayerMask platformMask;
    [SerializeField] private float rayDistance = 1.2f;

    private Transform currentPlatform;

    void FixedUpdate()
    {
        Ray ray = new Ray(transform.position + Vector3.up * 0.1f, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, platformMask))
        {
            Transform platform = hit.collider.transform;

            if (platform != currentPlatform)
            {
                transform.SetParent(platform, true);
                currentPlatform = platform;
            }
        }
        else
        {
            if (currentPlatform != null)
            {
                transform.SetParent(null, true);
                currentPlatform = null;
            }
        }
    }
}
