using UnityEngine;

public class S_Robot_Controller : MonoBehaviour
{
    public static S_Robot_Controller instance;

    [Header("Movement")]
    [SerializeField] float accelerationForward = 40f;
    [SerializeField] float accelerationBack = 40f;
    [SerializeField] float accelerationSide = 40f;
    [SerializeField] float friction = 5f;
    [SerializeField] Vector2 addVelocity = Vector2.zero;
    [SerializeField] float rotationSpeed = 90f;
    public bool canMove = false;
    public Transform orientation;

    [Header("Surface Movement")]
    [SerializeField, Min(0.01f)] float surfaceProbeDistance = 0.75f;
    [SerializeField] float surfaceOffset = 0.02f;
    [SerializeField] float surfaceAlignmentSpeed = 180f;
    [SerializeField, Min(0f)] float surfaceGraceTime = 0.2f;
    [SerializeField, Range(0f, 360f)] float arcAngle = 270f;
    [SerializeField, Min(1)] int arcResolution = 6;
    [SerializeField] LayerMask arcLayer;
    [SerializeField] Transform arcTransformRotation;

    Vector2 velocityNoAdd;
    Vector2 velocity;
    Vector2 moveInput;
    float speed;
    float maxSpeedEstimation;
    float speedProgress;
    Rigidbody rb;
    Collider playerCollider;
    float lastSurfaceContactTime = float.NegativeInfinity;

    public Vector2 VelocityNoAdd
    {
        get => velocityNoAdd;
        set
        {
            velocityNoAdd = value;

            UpdateVelocity();
        }
    }

    public Vector2 Velocity => velocity;
    public Vector3 Velocity3 => new Vector3(velocity.x, 0f, velocity.y);
    public float Speed => speed;
    public float SpeedProgress => speedProgress;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<Collider>();

        if (rb != null)
            rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (orientation == null)
            orientation = transform;

        EstimateMaxSpeed();
    }

    void OnValidate()
    {
        EstimateMaxSpeed();
    }

    void OnDisable()
    {
        velocityNoAdd = Vector2.zero;

        UpdateVelocity();
        
        moveInput = Vector2.zero;
    }

    public void SetMoveInput(Vector2 input)
    {
        moveInput = Vector2.ClampMagnitude(input, 1f);
    }

    public void Move()
    {
        if (rb == null)
            return;

        if (!canMove)
        {
            moveInput = Vector2.zero;
            velocityNoAdd = Vector2.zero;
        }

        if (Time.time - lastSurfaceContactTime > surfaceGraceTime)
            rb.useGravity = true;

        ApplyAcceleration();
        ApplyFriction();
        UpdateVelocity();
        ApplyVelocity();
    }

    void EstimateMaxSpeed()
    {
        float velocityEstimate = 0f;
        for (float time = 0f; time < 10f; time += Time.fixedDeltaTime)
        {
            velocityEstimate += Time.fixedDeltaTime * accelerationForward;
            velocityEstimate -= Time.fixedDeltaTime * friction * velocityEstimate;
        }
        maxSpeedEstimation = Mathf.Abs(velocityEstimate + addVelocity.y);

        velocityEstimate = 0f;
        for (float time = 0f; time < 10f; time += Time.fixedDeltaTime)
        {
            velocityEstimate -= Time.fixedDeltaTime * accelerationBack;
            velocityEstimate -= Time.fixedDeltaTime * friction * velocityEstimate;
        }
        maxSpeedEstimation = Mathf.Max(maxSpeedEstimation, Mathf.Abs(velocityEstimate + addVelocity.y));

        velocityEstimate = 0f;
        for (float time = 0f; time < 10f; time += Time.fixedDeltaTime)
        {
            velocityEstimate += Time.fixedDeltaTime * accelerationSide;
            velocityEstimate -= Time.fixedDeltaTime * friction * velocityEstimate;
        }
        float sideVelocity = velocityEstimate + Mathf.Abs(addVelocity.x);
        maxSpeedEstimation = Mathf.Max(maxSpeedEstimation, Mathf.Abs(sideVelocity));
    }

    void ApplyAcceleration()
    {
        if (moveInput == Vector2.zero)
            return;

        Vector2 acceleration = new Vector2(accelerationSide, moveInput.y > 0f ? accelerationForward : accelerationBack);
        velocityNoAdd += Time.fixedDeltaTime * acceleration * moveInput;
    }

    void ApplyFriction()
    {
        velocityNoAdd -= Time.fixedDeltaTime * friction * velocityNoAdd;
    }

    void UpdateVelocity()
    {
        velocity = velocityNoAdd + addVelocity;
        speed = velocity.magnitude;
        speedProgress = maxSpeedEstimation > 0f ? Mathf.Clamp01(speed / maxSpeedEstimation) : 0f;
    }

    void ApplyVelocity()
    {
        Transform movementFrame = arcTransformRotation != null ? arcTransformRotation : orientation != null ? orientation : transform;
        float deltaTime = Time.fixedDeltaTime;
        Vector3 worldVelocity = movementFrame.TransformVector(Velocity3);
        Vector3 targetPosition = rb.position + worldVelocity * deltaTime;
        Quaternion targetRotation = rb.rotation;
        Vector3 castDirection = worldVelocity.sqrMagnitude > 0.000001f ? worldVelocity : movementFrame.forward;

        if (S_Physics_Extension.ArcCast(rb.position, Quaternion.LookRotation(castDirection, movementFrame.up), arcAngle, surfaceProbeDistance, arcResolution, arcLayer, out RaycastHit hit, ignoredRoot: transform))
        {
            lastSurfaceContactTime = Time.time;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.ProjectOnPlane(rb.linearVelocity, hit.normal);
            worldVelocity = Vector3.ProjectOnPlane(worldVelocity, hit.normal);
            targetPosition = rb.position + worldVelocity * deltaTime;

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

        S_CamController_Robot cameraController = S_CamController_Robot.instance;
        Transform cameraOrientation = cameraController != null ? cameraController.orientation : null;
        if (canMove && cameraOrientation != null && cameraOrientation.IsChildOf(transform))
        {
            Vector3 surfaceUp = targetRotation * Vector3.up;
            Vector3 cameraForward = Vector3.ProjectOnPlane(cameraOrientation.forward, surfaceUp);
            if (cameraForward.sqrMagnitude > 0.0001f)
            {
                Quaternion facingRotation = Quaternion.LookRotation(cameraForward.normalized, surfaceUp);
                Quaternion nextRotation = Quaternion.RotateTowards(targetRotation, facingRotation, rotationSpeed * deltaTime);
                Vector3 currentForward = Vector3.ProjectOnPlane(targetRotation * Vector3.forward, surfaceUp);
                Vector3 nextForward = Vector3.ProjectOnPlane(nextRotation * Vector3.forward, surfaceUp);
                float bodyYawDelta = Vector3.SignedAngle(currentForward, nextForward, surfaceUp);
                cameraController.AdjustYawForBodyTurn(bodyYawDelta);
                targetRotation = nextRotation;
            }
        }

        rb.MovePosition(targetPosition);
        rb.MoveRotation(targetRotation);
    }
}