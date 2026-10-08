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

    Rigidbody rb;
    Vector2 moveInput;
    bool hasWarnedAboutMissingCamera;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        S_Robot_Controller controller = GetComponent<S_Robot_Controller>();
        if (controller != null)
            enabled = false;
    }

    void Start()
    {
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints |= RigidbodyConstraints.FreezeRotation;
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
        Vector3 forward;
        Vector3 right;
        GetMovementBasis(out forward, out right);

        Vector3 moveDirection = right * moveInput.x + forward * moveInput.y;
        moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);

        Vector3 verticalVelocity = Vector3.Project(rb.linearVelocity, Vector3.up);
        rb.linearVelocity = moveDirection * moveSpeed + verticalVelocity;
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
        return Vector2.ClampMagnitude(input, 1f);
    }

    void GetMovementBasis(out Vector3 forward, out Vector3 right)
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

        forward = Vector3.ProjectOnPlane(reference.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;

        right = Vector3.Cross(Vector3.up, forward).normalized;
    }
}
