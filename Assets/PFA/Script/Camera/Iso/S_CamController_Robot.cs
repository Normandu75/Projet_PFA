using UnityEngine;
using UnityEngine.InputSystem;

public class S_CamController_Robot : MonoBehaviour
{
    public static S_CamController_Robot instance;

    [Header("References")]
    public Transform cam;
    public Transform camPos;
    public Transform orientation;

    [Header("First Person Camera")]
    public Vector3 cameraLocalPosition = new Vector3(0f, 1.6f, 0f);
    public float lookSensitivity = 2.5f;
    public float mouseLookSensitivity = 0.05f;
    [SerializeField, Min(0f)] float groundTransitionRotationSpeed = 120f;

    Vector2 lookInput;
    float yaw;
    Quaternion orientationLocalRotation;
    S_Procedural_Robot_Movement proceduralMovement;
    bool wasOnWall;
    bool returningToGround;

    void Awake()
    {
        if (instance == null)
            instance = this;
    }

    private void Start()
    {
        S_Camera_Controller cameraController = S_Camera_Controller.instance;
        if (cameraController != null && !cameraController.IsRobotCameraController(this))
            return;

        if (cam == null)
        {
            cam = cameraController != null
                ? cameraController.robotCam
                : FindTransformIncludingInactive("Camera_Robot_Ally");
        }

        if (camPos == null)
        {
            camPos = transform;
        }

        if (orientation == null && camPos != null)
        {
            orientation = camPos;
        }

        proceduralMovement = GetComponentInParent<S_Procedural_Robot_Movement>();
        if (orientation != null)
            orientationLocalRotation = orientation.localRotation;

        if (cam == null)
        {
            Debug.LogError("S_CamController_Robot: Camera_Robot_Ally est introuvable.");
            return;
        }

        if (camPos != null && !cam.IsChildOf(camPos))
        {
            cam.SetParent(camPos);
        }

        if (camPos != null)
            cam.localPosition = cameraLocalPosition;

        wasOnWall = proceduralMovement != null && proceduralMovement.IsOnWall;
        yaw = wasOnWall ? 0f : orientation != null ? orientation.eulerAngles.y : cam.eulerAngles.y;
        if (!wasOnWall && yaw > 180f)
        {
            yaw -= 360f;
        }

        cam.localRotation = Quaternion.identity;
    }

    public void MoveCamera()
    {
        if (cam == null)
        {
            return;
        }

        if (lookInput.sqrMagnitude >= 0.0001f)
            yaw += lookInput.x * lookSensitivity;

        bool isOnWall = proceduralMovement != null && proceduralMovement.IsOnWall;
        if (isOnWall != wasOnWall)
        {
            returningToGround = wasOnWall && !isOnWall;
            yaw = isOnWall || orientation == null
                ? 0f
                : Mathf.DeltaAngle(0f, orientation.eulerAngles.y);
            wasOnWall = isOnWall;
        }

        if (orientation != null)
        {
            if (isOnWall)
            {
                orientation.rotation = proceduralMovement.transform.rotation
                    * orientationLocalRotation
                    * Quaternion.Euler(0f, yaw, 0f);
            }
            else
            {
                Quaternion groundRotation = Quaternion.Euler(0f, yaw, 0f);
                if (returningToGround)
                {
                    orientation.rotation = Quaternion.RotateTowards(
                        orientation.rotation,
                        groundRotation,
                        groundTransitionRotationSpeed * Time.deltaTime);

                    if (Quaternion.Angle(orientation.rotation, groundRotation) < 0.1f)
                    {
                        orientation.rotation = groundRotation;
                        returningToGround = false;
                    }
                }
                else
                {
                    orientation.rotation = groundRotation;
                }
            }
        }

        cam.localRotation = Quaternion.identity;
        lookInput = Vector2.zero;
    }

    private Transform FindTransformIncludingInactive(string objectName)
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Transform candidate in transforms)
        {
            if (candidate.name == objectName)
            {
                return candidate;
            }
        }

        return null;
    }

    public void SetLookInput(Vector2 gamepadInput, Vector2 mouseInput)
    {
        lookInput = gamepadInput + mouseInput * mouseLookSensitivity;
    }

    public void ConfigureCamera(Transform cameraTransform)
    {
        cam = cameraTransform;
    }

    public void AdjustYawForBodyTurn(float bodyYawDelta)
    {
        yaw = Mathf.DeltaAngle(0f, yaw - bodyYawDelta);

        if (orientation != null)
        {
            bool isOnWall = proceduralMovement != null && proceduralMovement.IsOnWall;
            orientation.rotation = isOnWall
                ? proceduralMovement.transform.rotation
                    * orientationLocalRotation
                    * Quaternion.Euler(0f, yaw, 0f)
                : Quaternion.Euler(0f, yaw, 0f);
        }
    }
}
