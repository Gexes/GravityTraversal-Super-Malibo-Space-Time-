using UnityEngine;
using UnityEngine.InputSystem;
using Animancer;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Capsule / Physics")]
    public CapsuleCollider capsule;
    public LayerMask collisionMask;

    [Header("Movement")]
    public float moveSpeed = 6f;
    public float sprintSpeed = 9f;
    public float rotationSmooth = 15f;

    [Header("Jumping")]
    public float jumpSpeed = 7f;
    public float coyoteTime = 0.15f;

    [Header("Gravity")]
    public float gravityMultiplier = 1f;
    public float groundSnapDistance = 0.3f;

    [Header("Input")]
    public InputActionReference moveAction;
    public InputActionReference jumpAction;
    public InputActionReference sprintAction;

    [Header("Camera")]
    public Transform cameraTransform;
    public Transform cameraTarget;

    [Header("Animancer")]
    public AnimancerComponent animancer;
    public AnimationClip idleClip;
    public AnimationClip walkClip;
    public AnimationClip sprintClip;
    public AnimationClip jumpClip;

    private Rigidbody rb;
    private Vector3 surfaceNormal = Vector3.up;
    private float verticalVel;
    private float coyoteCounter;
    private bool grounded;

    // Input Caching
    private Vector2 inputVector;
    private bool isSprinting;
    private bool jumpRequested;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // CRITICAL: Prevent Unity's physics from tipping Mario over globally
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    void Update()
    {
        // Gather input values every frame smoothly
        inputVector = moveAction.action.ReadValue<Vector2>();
        isSprinting = sprintAction.action.ReadValue<float>() > 0.5f;

        if (jumpAction.action.triggered)
        {
            jumpRequested = true;
        }

        // Keep camera target tracked over frame cycles
        PlanetGravity planet = GravityManager.GetNearestPlanet(transform.position);
        if (planet != null)
        {
            Vector3 gravityDir = planet.GetGravityDirection(transform.position);
            cameraTarget.up = -gravityDir;
        }

        // Drive animations based on grounded status
        if (!grounded)
        {
            animancer.Play(jumpClip, 0.1f);
        }
        else if (inputVector.sqrMagnitude > 0.01f)
        {
            animancer.Play(isSprinting ? sprintClip : walkClip, 0.1f);
        }
        else
        {
            animancer.Play(idleClip, 0.2f);
        }
    }

    void FixedUpdate()
    {
        PlanetGravity planet = GravityManager.GetNearestPlanet(transform.position);
        if (planet == null) return;

        Vector3 gravityDir = planet.GetGravityDirection(transform.position);

        // 1. Resolve Surface Normal (Safe multi-sampling)
        surfaceNormal = SurfaceNormalResolver.ResolveSurfaceNormal(
            transform.position,
            gravityDir,
            3f,
            collisionMask,
            surfaceNormal,
            capsule.radius
        );

        // 2. Mario Galaxy Core-to-Normal Blending
        Vector3 coreUp = -gravityDir;
        Vector3 playerUp = Vector3.Slerp(coreUp, surfaceNormal, 0.3f).normalized;

        // 3. Keep Player Oriented Upwards Relative to the Sphere
        Quaternion currentRotationWithoutYaw = Quaternion.FromToRotation(transform.up, playerUp) * transform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, currentRotationWithoutYaw, rotationSmooth * Time.fixedDeltaTime);

        // 4. ROBUST CAM-TARGET COORDINATE SYSTEM (Independent of Camera Pitch/Tilt Flips)
        // Instead of sampling the raw camera transform (which skews at the poles and bottom),
        // we use the stable, non-tilting orientation frame of your cameraTarget anchor.
        Vector3 targetRight = cameraTarget.right;

        // Flatten the right vector onto the player's current standing planet tangent plane
        Vector3 cleanPlaneRight = Vector3.ProjectOnPlane(targetRight, playerUp).normalized;

        // Derive forward by crossing the clean planar right axis with your local planet up vector.
        // This builds a perfect 2D grid that smoothly curves all the way around to the bottom of the sphere.
        Vector3 cleanPlaneForward = Vector3.Cross(cleanPlaneRight, playerUp).normalized;

        // Ensure baseline fallbacks if tracking vectors experience rounding errors
        if (cleanPlaneForward.sqrMagnitude < 0.01f) cleanPlaneForward = transform.forward;
        if (cleanPlaneRight.sqrMagnitude < 0.01f) cleanPlaneRight = transform.right;

        // Map inputs directly to your custom spherical plane coordinate system
        // Pushing "Up" on the stick will now seamlessly follow the planet's contour forward!
        Vector3 moveDir = (cleanPlaneForward * inputVector.y + cleanPlaneRight * inputVector.x).normalized;



        // [Steps 5 & 6 handle ground-checking and jumping via your working Rigidbody setup...]
        Vector3 rayOrigin = transform.position + playerUp * (capsule.height * 0.5f);
        float totalRayLength = (capsule.height * 0.5f) + groundSnapDistance;
        grounded = Physics.Raycast(rayOrigin, -playerUp, out RaycastHit groundHit, totalRayLength, collisionMask);

        if (grounded)
        {
            coyoteCounter = coyoteTime;
            rb.AddForce(gravityDir * planet.gravityStrength * gravityMultiplier, ForceMode.Acceleration);

            if (jumpRequested && coyoteCounter > 0f)
            {
                rb.linearVelocity = Vector3.ProjectOnPlane(rb.linearVelocity, playerUp) + (playerUp * jumpSpeed);
                grounded = false;
                coyoteCounter = 0f;
            }
        }
        else
        {
            coyoteCounter -= Time.fixedDeltaTime;
            rb.AddForce(gravityDir * planet.gravityStrength * gravityMultiplier, ForceMode.Acceleration);
        }
        jumpRequested = false;

        // 7. Rigidbody Movement Velocity Translation
        float targetSpeed = isSprinting ? sprintSpeed : moveSpeed;
        Vector3 targetHorizontalVelocity = moveDir * (inputVector.magnitude * targetSpeed);

        // Maintain falling/jumping momentum cleanly along the current playerUp axis
        Vector3 currentVerticalVelocity = Vector3.Project(rb.linearVelocity, playerUp);
        rb.linearVelocity = targetHorizontalVelocity + currentVerticalVelocity;

        // 8. Independent Heading Turning (Stops the right-becomes-forward orientation loop)
        if (moveDir.sqrMagnitude > 0.01f)
        {
            // Calculate a clean look rotation that matches your input path relative to the spherical frame
            Quaternion targetFacingRot = Quaternion.LookRotation(moveDir, playerUp);

            // Turn Mario's physical core transform toward the vector independently of the camera's anchor targets
            transform.rotation = Quaternion.Slerp(transform.rotation, targetFacingRot, rotationSmooth * Time.fixedDeltaTime);
        }

        // Keep the baseline tracking target oriented flat with your cosmic gravity vector
        cameraTarget.up = playerUp;

    }
}
