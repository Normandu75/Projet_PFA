using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[DefaultExecutionOrder(-1000)]
public class S_Procedural_Robot_Movement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0f)] float moveSpeed = 3.5f;
    [SerializeField, Min(0f)] float gamepadDeadzone = 0.15f;
    [SerializeField] Transform movementReference;
    [SerializeField] bool onlyWhenRobotMode;
    [SerializeField] bool isGround;

    [Header("Wall Movement")]
    [SerializeField, Min(0.01f)] float surfaceProbeDistance = 0.75f;
    [SerializeField, Min(0f)] float surfaceOffset = 0.02f;
    [SerializeField, Min(0f)] float surfaceAlignmentSpeed = 180f;
    [SerializeField, Min(0f)] float surfaceGraceTime = 0.2f;
    [SerializeField, Range(0f, 360f)] float arcAngle = 270f;
    [SerializeField, Min(1)] int arcResolution = 12;
    [SerializeField] LayerMask surfaceLayers;
    [SerializeField] bool isOnWall;

    public Rigidbody rb;
    public Collider robotCollider;
    Collider[] robotColliders;
    Vector2 moveInput;
    bool hasWarnedAboutMissingCamera;
    Vector3 surfaceNormal = Vector3.up;
    float lastSurfaceContactTime = float.NegativeInfinity;
    int activeSurfaceLayer = -1;
    int lastReportedSurfaceLayer = int.MinValue;

    public Vector3 SurfaceNormal => surfaceNormal;
    public bool IsOnWall => isOnWall;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        robotCollider = GetComponent<Collider>();
        robotColliders = GetComponentsInChildren<Collider>();
        surfaceNormal = Vector3.up;
        if (surfaceLayers.value == 0)
            surfaceLayers = GetDefaultSurfaceLayers();
    }

    void Start()
    {
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints &= ~RigidbodyConstraints.FreezeRotation;
    }

    void Update()
    {
        if (onlyWhenRobotMode && S_Camera_Controller.instance != null && !S_Camera_Controller.instance.robotMode)
        {
            moveInput = Vector2.zero;
            return;
        }

        moveInput = ReadMoveInput();
    }

    void FixedUpdate()
    {
        if (rb.isKinematic)
            return;

        if (onlyWhenRobotMode && S_Camera_Controller.instance != null && !S_Camera_Controller.instance.robotMode)
        {
            moveInput = Vector2.zero;
            rb.useGravity = true;
            surfaceNormal = Vector3.up;
            lastSurfaceContactTime = float.NegativeInfinity;
            activeSurfaceLayer = -1;
            return;
        }

        Transform movementReference = GetMovementReference();
        Vector3 forward;
        Vector3 right;
        GetMovementBasis(movementReference, surfaceNormal, out forward, out right);

        Vector3 moveDirection = right * moveInput.x + forward * moveInput.y;
        moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);
        Vector3 worldVelocity = moveDirection * moveSpeed;
        Vector3 castDirection = worldVelocity.sqrMagnitude > 0.0001f ? worldVelocity : forward;
        Quaternion castRotation = Quaternion.LookRotation(castDirection, surfaceNormal);
        RaycastHit surfaceHit = default;

        bool hasSurface = surfaceLayers.value != 0
            && S_Physics_Extension.ArcCast(
                rb.position,
                castRotation,
                arcAngle,
                surfaceProbeDistance,
                arcResolution,
                surfaceLayers,
                out surfaceHit,
                ignoredRoot: transform,
                referenceNormal: surfaceNormal,
                transitionDirection: moveDirection,
                prioritizeNormalTransition: true,
                preferredTransitionLayer: LayerMask.NameToLayer("Wall"));

        if (hasSurface)
        {
            int groundLayer = LayerMask.NameToLayer("Ground");
            int detectedWallLayer = LayerMask.NameToLayer("Wall");
            if (S_Physics_Extension.IsInLayerHierarchy(surfaceHit.transform, detectedWallLayer))
                activeSurfaceLayer = detectedWallLayer;
            else if (S_Physics_Extension.IsInLayerHierarchy(surfaceHit.transform, groundLayer))
                activeSurfaceLayer = groundLayer;
            else
                activeSurfaceLayer = surfaceHit.collider.gameObject.layer;

            if (activeSurfaceLayer != lastReportedSurfaceLayer)
            {
                Debug.Log(
                    $"Surface détectée : {surfaceHit.collider.name}, layer collider={LayerMask.LayerToName(surfaceHit.collider.gameObject.layer)}, layer retenu={LayerMask.LayerToName(activeSurfaceLayer)}, normale={surfaceHit.normal}",
                    this);
                lastReportedSurfaceLayer = activeSurfaceLayer;
            }

            isGround = activeSurfaceLayer == LayerMask.NameToLayer("Ground");
            surfaceNormal = isGround
                ? Vector3.up
                : surfaceHit.normal;
            lastSurfaceContactTime = Time.time;
            rb.useGravity = isGround;
            worldVelocity = Vector3.ProjectOnPlane(worldVelocity, surfaceNormal);
            if (isGround)
            {
                float verticalVelocity = Mathf.Min(Vector3.Dot(rb.linearVelocity, Vector3.up), 0f);
                worldVelocity += Vector3.up * verticalVelocity;
            }
            else
            {
                if (moveDirection.sqrMagnitude > 0.0001f)
                    worldVelocity += surfaceNormal * GetSurfaceCorrectionVelocity(surfaceHit, surfaceNormal);
            }

            rb.linearVelocity = worldVelocity;
        }
        else if (Time.time - lastSurfaceContactTime <= surfaceGraceTime)
        {
            bool wasOnGround = activeSurfaceLayer == LayerMask.NameToLayer("Ground");
            rb.useGravity = wasOnGround;
            worldVelocity = Vector3.ProjectOnPlane(worldVelocity, surfaceNormal);
            if (wasOnGround)
            {
                float verticalVelocity = Mathf.Min(Vector3.Dot(rb.linearVelocity, Vector3.up), 0f);
                worldVelocity += Vector3.up * verticalVelocity;
            }

            rb.linearVelocity = worldVelocity;
        }
        else
        {
            rb.useGravity = true;
            surfaceNormal = Vector3.up;
            activeSurfaceLayer = -1;
            Vector3 verticalVelocity = Vector3.Project(rb.linearVelocity, Vector3.up);
            rb.linearVelocity = worldVelocity + verticalVelocity;
        }

        int wallLayer = LayerMask.NameToLayer("Wall");
        isOnWall = wallLayer >= 0 && activeSurfaceLayer == wallLayer;
        Quaternion targetRotation = isOnWall
            ? Quaternion.FromToRotation(rb.rotation * Vector3.forward, surfaceNormal) * rb.rotation
            : Quaternion.Euler(-90f, 0f, 0f);
        rb.angularVelocity = Vector3.zero;
        rb.MoveRotation(!isOnWall
            ? targetRotation
            : Quaternion.RotateTowards(
                rb.rotation,
                targetRotation,
                surfaceAlignmentSpeed * Time.fixedDeltaTime));
    }

    float GetSurfaceCorrectionVelocity(RaycastHit hit, Vector3 contactNormal)
    {
        float supportOffset = float.PositiveInfinity;
        Vector3 normalAbs = new Vector3(
            Mathf.Abs(contactNormal.x),
            Mathf.Abs(contactNormal.y),
            Mathf.Abs(contactNormal.z));

        foreach (Collider collider in robotColliders)
        {
            if (collider == null || !collider.enabled || collider.isTrigger || collider.attachedRigidbody != rb)
                continue;

            Bounds bounds = collider.bounds;
            float colliderSupport = Vector3.Dot(bounds.center - rb.position, contactNormal)
                - Vector3.Dot(normalAbs, bounds.extents);
            supportOffset = Mathf.Min(supportOffset, colliderSupport);
        }

        if (float.IsPositiveInfinity(supportOffset))
            return 0f;

        float currentSeparation = Vector3.Dot(rb.position - hit.point, contactNormal) + supportOffset;
        float correctionDistance = surfaceOffset - currentSeparation;
        if (correctionDistance >= 0f)
            return 0f;

        float maxCorrectionPerStep = Mathf.Max(moveSpeed * 0.25f, 0.25f) * Time.fixedDeltaTime;
        correctionDistance = Mathf.Max(correctionDistance, -maxCorrectionPerStep);
        return correctionDistance / Time.fixedDeltaTime;
    }

    Vector2 ReadMoveInput()
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
        if (gamepadInput.sqrMagnitude < gamepadDeadzone * gamepadDeadzone)
            gamepadInput = Vector2.zero;

        Vector2 input = gamepadInput.sqrMagnitude > keyboardInput.sqrMagnitude
            ? gamepadInput
            : keyboardInput;
        input = Vector2.ClampMagnitude(input, 1f);
        if (input.sqrMagnitude < gamepadDeadzone * gamepadDeadzone)
            return Vector2.zero;

        return input;
    }

    Transform GetMovementReference()
    {
        Transform reference = null;
        if (S_Camera_Controller.instance != null && S_Camera_Controller.instance.robotMode)
            reference = S_Camera_Controller.instance.RobotMovementReference;
        if (reference == null)
            reference = movementReference;
        if (reference == null && Camera.main != null)
            reference = Camera.main.transform;

        if (reference == null)
        {
            if (!hasWarnedAboutMissingCamera)
            {
                Debug.LogWarning("S_Procedural_Robot_Movement n'a trouvé aucune caméra ; le déplacement utilisera l'orientation du robot.", this);
                hasWarnedAboutMissingCamera = true;
            }

            reference = transform;
        }

        return reference;
    }

    void GetMovementBasis(Transform reference, Vector3 up, out Vector3 forward, out Vector3 right)
    {
        forward = Vector3.ProjectOnPlane(reference.forward, up).normalized;
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.ProjectOnPlane(transform.forward, up).normalized;
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.ProjectOnPlane(Vector3.forward, up).normalized;
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.ProjectOnPlane(Vector3.right, up).normalized;

        right = Vector3.Cross(up, forward).normalized;
    }

    static LayerMask GetDefaultSurfaceLayers()
    {
        int layerMask = 0;
        int groundLayer = LayerMask.NameToLayer("Ground");
        int wallLayer = LayerMask.NameToLayer("Wall");

        if (groundLayer >= 0)
            layerMask |= 1 << groundLayer;
        if (wallLayer >= 0)
            layerMask |= 1 << wallLayer;

        return layerMask;
    }
}
