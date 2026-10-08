using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-900)]
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
    [SerializeField, Min(0f)] float proceduralMoveSpeed = 3.5f;
    [SerializeField, Range(0f, 1f)] float proceduralGamepadDeadzone = 0.15f;
    public bool canMove = false;
    public Transform orientation;

    [Header("Surface Movement")]
    [SerializeField, Min(0.01f)] float surfaceProbeDistance = 0.75f;
    [SerializeField] float surfaceOffset = 0.02f;
    [SerializeField] float surfaceAlignmentSpeed = 90f;
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
    bool hasScanSurfaceTarget;
    Quaternion scanSurfaceTarget;
    bool usesProceduralMovement;
    S_CamController_Robot proceduralCamera;

    public bool UsesProceduralMovement => usesProceduralMovement;
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

    public void SetScanSurfaceTarget(Quaternion targetRotation, bool hasSurface)
    {
        hasScanSurfaceTarget = hasSurface;
        if (hasSurface)
            scanSurfaceTarget = targetRotation;
    }

    void Awake()
    {
        usesProceduralMovement = GetComponent<S_Procedural_Animation>() != null
            || GetComponent<S_Procedural_Robot_Movement>() != null;

        if (usesProceduralMovement)
        {
            if (instance != null && instance != this)
            {
                instance.enabled = false;
                Debug.LogWarning("S_Robot_Controller : le contrôleur du robot procédural remplace l'instance précédente.", this);
            }

            instance = this;
        }
        else if (instance != null && instance != this)
        {
            enabled = false;
            return;
        }
        else
        {
            instance = this;
        }

        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<Collider>();

        if (usesProceduralMovement)
        {
            S_Procedural_Robot_Movement oldMovement = GetComponent<S_Procedural_Robot_Movement>();
            if (oldMovement != null)
                oldMovement.enabled = false;

            rb.isKinematic = false;
            rb.useGravity = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            proceduralCamera = GetComponentInChildren<S_CamController_Robot>(true);
        }
        else if (rb != null && playerCollider != null)
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.isKinematic = true;
        }

        if (orientation == null)
            orientation = transform;

        EstimateMaxSpeed();
    }

    void Update()
    {
        if (!usesProceduralMovement)
            return;

        if (!canMove)
        {
            moveInput = Vector2.zero;
            return;
        }

        moveInput = ReadProceduralMoveInput();

    }

    void FixedUpdate()
    {
        if (usesProceduralMovement)
            Move();
    }

    void OnValidate()
    {
        EstimateMaxSpeed();
    }

    void OnDisable()
    {
        if (instance == this)
            instance = null;

        velocityNoAdd = Vector2.zero;

        UpdateVelocity();

        moveInput = Vector2.zero;
    }

    public void SetMoveInput(Vector2 input)
    {
        moveInput = Vector2.ClampMagnitude(input, 1f);
    }

    public void SetRobotMode(bool enabledForPlayer)
    {
        canMove = enabledForPlayer;
        if (usesProceduralMovement && rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
    }

    public void Move()
    {
        if (rb == null)
            return;

        if (usesProceduralMovement)
        {
            MoveProceduralRobot();
            return;
        }

        if (!canMove)
        {
            moveInput = Vector2.zero;
            velocityNoAdd = Vector2.zero;
            UpdateVelocity();
            return;
        }

        if (Time.time - lastSurfaceContactTime > surfaceGraceTime)
            rb.useGravity = true;

        ApplyAcceleration();
        ApplyFriction();
        UpdateVelocity();
        ApplyVelocity();
    }

    Vector2 ReadProceduralMoveInput()
    {
        Vector2 keyboardInput = Vector2.zero;
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            keyboardInput = new Vector2(
                (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
                    - (keyboard.aKey.isPressed || keyboard.qKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f),
                (keyboard.wKey.isPressed || keyboard.zKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f)
                    - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f));
        }

        Vector2 gamepadInput = Gamepad.current != null
            ? Gamepad.current.leftStick.ReadValue()
            : Vector2.zero;
        if (gamepadInput.sqrMagnitude < proceduralGamepadDeadzone * proceduralGamepadDeadzone)
            gamepadInput = Vector2.zero;

        Vector2 input = gamepadInput.sqrMagnitude > keyboardInput.sqrMagnitude
            ? gamepadInput
            : keyboardInput;
        return Vector2.ClampMagnitude(input, 1f);
    }

    void MoveProceduralRobot()
    {
        if (!canMove)
            moveInput = Vector2.zero;

        Transform movementFrame = S_Camera_Controller.instance != null
            && S_Camera_Controller.instance.robotMode
            && S_Camera_Controller.instance.RobotMovementReference != null
                ? S_Camera_Controller.instance.RobotMovementReference
                : proceduralCamera != null && proceduralCamera.orientation != null
                    ? proceduralCamera.orientation
                    : orientation != null ? orientation : transform;
        Vector3 forward = Vector3.ProjectOnPlane(movementFrame.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        Vector3 horizontalVelocity = (right * moveInput.x + forward * moveInput.y) * proceduralMoveSpeed;
        Vector3 verticalVelocity = Vector3.Project(rb.linearVelocity, Vector3.up);
        rb.linearVelocity = horizontalVelocity + verticalVelocity;
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

        bool hasSurfaceHit = S_Physics_Extension.ArcCast(rb.position, Quaternion.LookRotation(castDirection, movementFrame.up), arcAngle, surfaceProbeDistance, arcResolution, arcLayer, out RaycastHit hit, ignoredRoot: transform);
        if (hasSurfaceHit)
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

        }

        if (hasScanSurfaceTarget)
        {
            targetRotation = Quaternion.RotateTowards(targetRotation, scanSurfaceTarget, surfaceAlignmentSpeed * deltaTime);
        }
        else if (hasSurfaceHit)
        {
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