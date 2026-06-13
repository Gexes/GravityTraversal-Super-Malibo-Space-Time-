using UnityEngine;
using UnityEngine.InputSystem;
using Animancer;

public class PlayerController : MonoBehaviour
{
    [Header("Capsule")]
    public CapsuleCollider capsule;
    public LayerMask collisionMask;

    [Header("Movement")]
    public float moveSpeed = 6f;
    public float sprintSpeed = 9f;
    public float rotationSmooth = 10f;

    [Header("Jumping")]
    public float jumpSpeed = 7f;
    public float coyoteTime = 0.15f;

    [Header("Gravity")]
    public float gravityMultiplier = 1f;
    public float groundSnapDistance = 0.3f;

    [Header("Step")]
    public float stepHeight = 0.3f;

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

    Vector3 velocity;
    Vector3 surfaceNormal = Vector3.up;
    float verticalVel;
    float coyoteCounter;
    bool grounded;
    private bool _initializedGroundSnap = false;

    void Update()
    {
        Vector2 input = moveAction.action.ReadValue<Vector2>();
        bool sprint = sprintAction.action.ReadValue<float>() > 0.5f;

        PlanetGravity planet = GravityManager.GetNearestPlanet(transform.position);
        Vector3 gravityDir = planet.GetGravityDirection(transform.position);

        // Surface normal
        surfaceNormal = SurfaceNormalResolver.ResolveSurfaceNormal(
            transform.position,
            gravityDir,
            3f,
            collisionMask,
            surfaceNormal
        );

        // -------------------------
        // TRUE MARIO GALAXY INPUT SYSTEM
        // -------------------------
        Vector3 up = surfaceNormal;

        // 1. Extract camera yaw-only forward (pitch removed)
        Vector3 camForward = cameraTransform.forward;
        camForward = Vector3.ProjectOnPlane(camForward, up).normalized;

        // 2. If projection collapses, fallback to player forward
        if (camForward.sqrMagnitude < 0.01f)
            camForward = Vector3.ProjectOnPlane(transform.forward, up).normalized;

        // 3. Extract camera yaw-only right (pitch removed)
        Vector3 camRight = cameraTransform.right;
        camRight = Vector3.ProjectOnPlane(camRight, up).normalized;

        // 4. Build movement direction from yaw-only frame
        Vector3 moveDir = camForward * input.y + camRight * input.x;

        // 5. Normalize if needed
        if (moveDir.sqrMagnitude > 0.001f)
            moveDir.Normalize();

        float speed = sprint ? sprintSpeed : moveSpeed;
        Vector3 horizontalVel = moveDir * speed;

        // Jump
        if (jumpAction.action.triggered && coyoteCounter > 0f)
        {
            verticalVel = jumpSpeed;
            grounded = false;
            coyoteCounter = 0f;
            animancer.Play(jumpClip, 0.1f);
        }

        // Gravity
        if (!grounded)
            verticalVel -= planet.gravityStrength * gravityMultiplier * Time.deltaTime;

        // Combine
        velocity = horizontalVel + (-gravityDir * verticalVel);

        // Capsule sweep
        CollisionSolver.SweepResult sweep = CollisionSolver.MoveCapsule(
            transform.position,
            capsule.center,
            capsule.height,
            capsule.radius,
            velocity * Time.deltaTime,
            collisionMask,
            stepHeight
        );

        transform.position = sweep.position;

        // Ground check
        grounded = Physics.Raycast(transform.position, gravityDir, out RaycastHit hit, groundSnapDistance, collisionMask);

        if (grounded)
        {
            verticalVel = 0f;
            coyoteCounter = coyoteTime;
        }
        else
        {
            coyoteCounter -= Time.deltaTime;
        }

        // Rotation
        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir, up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSmooth * Time.deltaTime);
        }
        else
        {
            Quaternion targetRot = Quaternion.FromToRotation(transform.up, up) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSmooth * Time.deltaTime);
        }

        // Align camera target with gravity
        cameraTarget.up = up;

        // Animations
        if (!grounded)
        {
            animancer.Play(jumpClip, 0.1f);
        }
        else if (moveDir.sqrMagnitude > 0.1f)
        {
            animancer.Play(sprint ? sprintClip : walkClip, 0.1f);
        }
        else
        {
            animancer.Play(idleClip, 0.2f);
        }
    }

    void LateUpdate()
    {
        if (!_initializedGroundSnap)
        {
            PlanetGravity planet = GravityManager.GetNearestPlanet(transform.position);
            Vector3 gravityDir = planet.GetGravityDirection(transform.position);

            if (Physics.Raycast(transform.position + (-gravityDir * 0.1f), gravityDir, out RaycastHit hit, 5f, collisionMask))
            {
                transform.position = hit.point - gravityDir * (capsule.height * 0.5f - capsule.radius);
            }

            _initializedGroundSnap = true;
        }
    }
}
