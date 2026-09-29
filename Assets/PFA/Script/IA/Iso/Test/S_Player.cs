using UnityEngine;


[DefaultExecutionOrder(0)]
public class S_Player : MonoBehaviour
{
    [SerializeField] S_Controller_Iso_Test controller;
    [SerializeField] float accelerationForward = 40;
    [SerializeField] float accelerationBack = 40;
    [SerializeField] float accelerationSide = 40;
    [SerializeField] float friction = 5;
    [SerializeField, Min(0.01f)] float surfaceProbeDistance = 0.75f;
    [SerializeField] float surfaceOffset = 0.02f;
    [SerializeField] float surfaceAlignmentSpeed = 180f;
    [SerializeField, Min(0f)] float surfaceGraceTime = 0.2f;
    [SerializeField] Vector2 addVelocity = Vector2.zero;
    Vector2 velocityNoAdd = Vector2.zero;
    Vector2 velocity = Vector2.zero;
    float speed = 0;
    float maxSpeedEstimation;
    float speedProgress;
    Rigidbody playerBody;
    Collider playerCollider;
    float lastSurfaceContactTime = float.NegativeInfinity;

    [SerializeField] float rotationSpeed = 90;

    [SerializeField, Range(0, 360)] float arcAngle = 270;
    [SerializeField] int arcResolution = 6;
    [SerializeField] LayerMask arcLayer;
    [SerializeField] Transform arcTransformRotation;

    public S_Controller_Iso_Test Controller { get => controller; }
    public Vector2 VelocityNoAdd { get => VelocityNoAdd;
        set {
            velocityNoAdd = value;
            UpdateVeclocity();
        }
    }
    public Vector2 Velocity { get => velocity; }
    public Vector3 Velocity3 { get => new Vector3(velocity.x, 0, velocity.y); }
    public float Speed { get => speed; }
    public float SpeedProgress { get => speedProgress; }


    void OnValidate()
    {
        EstimateMaxSpeed();
    }

    void Awake()
    {
        playerBody = GetComponent<Rigidbody>();
        playerCollider = GetComponent<Collider>();
        if (playerBody != null)
            playerBody.interpolation = RigidbodyInterpolation.Interpolate;

        EstimateMaxSpeed();
    }


    void OnDisable()
    {
        velocityNoAdd = Vector3.zero;
        UpdateVeclocity();
    }

    void FixedUpdate()
    {
        if (playerBody != null)
            playerBody.useGravity = Time.time - lastSurfaceContactTime > surfaceGraceTime;

        ApplyAcceleration();
        ApplyFriction();
        UpdateVeclocity();
        ApplyVelocity();
    }

    void EstimateMaxSpeed()
    {
        // forward
        float v = 0, s;

        for (float t = 0; t < 10; t += Time.fixedDeltaTime)
        {
            v += Time.fixedDeltaTime * accelerationForward;
            v -= Time.fixedDeltaTime * friction * v;
        }
        v += addVelocity.y;
        s = Mathf.Abs(v);

        maxSpeedEstimation = s;

        // back
        v = 0;

        for (float t = 0; t < 10; t += Time.fixedDeltaTime)
        {
            v -= Time.fixedDeltaTime * accelerationBack;
            v -= Time.fixedDeltaTime * friction * v;
        }
        v += addVelocity.y;
        s = Mathf.Abs(v);

        maxSpeedEstimation = Mathf.Max(maxSpeedEstimation, s);

        // side
        v = 0;

        for (float t = 0; t < 10; t += Time.fixedDeltaTime)
        {
            v += Time.fixedDeltaTime * accelerationSide;
            v -= Time.fixedDeltaTime * friction * v;
        }
        v += addVelocity.x * (addVelocity.x > 0 == v > 0 ? 1 : -1);
        s = Mathf.Abs(v);

        maxSpeedEstimation = Mathf.Max(maxSpeedEstimation, s);
    }

    void ApplyAcceleration()
    {
        if (!controller)
            return;

        Vector2 stickL = controller.StickL;

        if (stickL != Vector2.zero)
            velocityNoAdd += Time.fixedDeltaTime * new Vector2(accelerationSide, stickL.y > 0 ? accelerationForward : accelerationBack) * stickL;
    }

    void ApplyFriction()
    {
        velocityNoAdd -= Time.fixedDeltaTime * friction * velocityNoAdd;
    }

    void UpdateVeclocity()
    {
        velocity = velocityNoAdd + addVelocity;
        UpdateSpeed();
    }

    void UpdateSpeed()
    {
        speed = velocity.magnitude;
        speedProgress = Mathf.Clamp01(speed / maxSpeedEstimation);
    }

    void ApplyVelocity()
    {
        if (playerBody == null)
            return;

        Transform movementFrame = arcTransformRotation != null ? arcTransformRotation : transform;

        float deltaTime = Time.fixedDeltaTime;
        
        Vector3 worldVelocity = movementFrame.TransformVector(Velocity3);
        Vector3 targetPosition = playerBody.position + worldVelocity * deltaTime;
        Quaternion targetRotation = playerBody.rotation;
        Vector3 castDirection = worldVelocity.sqrMagnitude > 0.000001f ? worldVelocity : movementFrame.forward;

        if (S_Physics_Extension.ArcCast(playerBody.position, Quaternion.LookRotation(castDirection, movementFrame.up), arcAngle, surfaceProbeDistance, arcResolution, arcLayer, out RaycastHit hit, ignoredRoot: transform))
        {
            lastSurfaceContactTime = Time.time;
            playerBody.useGravity = false;
            playerBody.linearVelocity = Vector3.ProjectOnPlane(playerBody.linearVelocity, hit.normal);
            worldVelocity = Vector3.ProjectOnPlane(worldVelocity, hit.normal);
            targetPosition = playerBody.position + worldVelocity * deltaTime;

            if (playerCollider != null)
            {
                Vector3 normalAbs = new Vector3(Mathf.Abs(hit.normal.x), Mathf.Abs(hit.normal.y), Mathf.Abs(hit.normal.z));
                float colliderExtent = Vector3.Dot(normalAbs, playerCollider.bounds.extents);
                float distanceToSurface = Vector3.Dot(targetPosition - hit.point, hit.normal);
                targetPosition += hit.normal * (colliderExtent + surfaceOffset - distanceToSurface);
            }

            Quaternion surfaceRotation = Quaternion.FromToRotation(targetRotation * Vector3.up, hit.normal) * targetRotation;
            targetRotation = Quaternion.RotateTowards(targetRotation, surfaceRotation, surfaceAlignmentSpeed * deltaTime);
        }

        if (controller != null)
        {
            Vector2 stickR = controller.StickR;
            targetRotation *= Quaternion.Euler(0f, rotationSpeed * deltaTime * stickR.x, 0f);
        }

        playerBody.MovePosition(targetPosition);
        playerBody.MoveRotation(targetRotation);
    }
}