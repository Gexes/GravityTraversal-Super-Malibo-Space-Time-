using UnityEngine;

public class CameraUpStabilizer : MonoBehaviour
{
    [Header("Tracking Focus")]
    public Transform playerTransform;

    [Header("Orientation Blending")]
    [Tooltip("How smoothly the camera horizon rolls and tilts to accommodate a new planet's gravity angle.")]
    public float gravityRotationSmooth = 3f;

    [Header("Cinemachine Y-Rotation Control")]
    [Tooltip("Check this TRUE to stop Cinemachine from executing a 180-degree Y rotation (yaw) spin flip when Mario goes upside down between planets!")]
    public bool preventYFlipOnHop = true;

    // Cache the orientation matrix across frames
    private Quaternion smoothedRotation = Quaternion.identity;

    // Permanent world reference direction to serve as our locked global camera compass pole
    private readonly Vector3 globalCompassForward = Vector3.forward;

    void Start()
    {
        if (playerTransform != null)
        {
            PlanetGravity planet = GravityManager.GetNearestPlanet(playerTransform.position);
            if (planet != null)
            {
                Vector3 initialPlanetaryUp = (playerTransform.position - planet.transform.position).normalized;
                smoothedRotation = Quaternion.LookRotation(playerTransform.forward, initialPlanetaryUp);
                transform.rotation = smoothedRotation;
            }
        }
    }

    void FixedUpdate()
    {
        if (playerTransform == null) return;

        PlanetGravity planet = GravityManager.GetNearestPlanet(playerTransform.position);
        if (planet == null) return;

        // 1. Calculate the raw vertical normal vector pointing straight away from the planet's core
        Vector3 rawPlanetaryUp = (playerTransform.position - planet.transform.position).normalized;

        // 2. THE CHOSEN HEADING ENGINE
        Vector3 cleanForward;

        if (preventYFlipOnHop)
        {
            // THE ULTIMATE NO-Y-FLIP SOLUTION: Instead of reading where Mario's shoulders/face are looking
            // (which flips 180 degrees upside down), we project a permanent global world coordinate axis flat.
            // Because this reference never flips in the universe, Cinemachine's Orbital Follow sees a completely 
            // stable look target, forcing its local Y rotation to stay flat and refuse to spin!
            cleanForward = Vector3.ProjectOnPlane(globalCompassForward, rawPlanetaryUp).normalized;

            // Handle absolute edge case pole alignments safely
            if (cleanForward.sqrMagnitude < 0.01f)
            {
                cleanForward = Vector3.ProjectOnPlane(Vector3.up, rawPlanetaryUp).normalized;
            }
        }
        else
        {
            // Standard tracking mode: Camera follows Mario's direct character heading direction
            cleanForward = Vector3.ProjectOnPlane(playerTransform.forward, rawPlanetaryUp).normalized;
            if (cleanForward.sqrMagnitude < 0.01f) cleanForward = transform.forward;
        }

        // 3. Construct and smoothly blend the finalized stable tracking rotation matrix
        Quaternion targetRotation = Quaternion.LookRotation(cleanForward, rawPlanetaryUp);
        smoothedRotation = Quaternion.Slerp(smoothedRotation, targetRotation, gravityRotationSmooth * Time.fixedDeltaTime);

        transform.rotation = smoothedRotation;
    }
}
