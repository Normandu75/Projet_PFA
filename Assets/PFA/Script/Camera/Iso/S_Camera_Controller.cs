
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class S_Camera_Controller : MonoBehaviour
{
    public static S_Camera_Controller instance;

    [Header("Player")]
    public Transform player;

    [Header("Camera")]
    public Transform cam;
    public Transform robotCam;
    [SerializeField] private Transform robotTarget;

    [Header("Parameters")]
    public float distance;
    public float height;
    public float rotationSpeed = 90f;
    public float orbitAngle;
    public bool robotMode { get; private set; }

    [Header("Camera Smoothing")]
    [SerializeField] private Vector3 cameraDamping =
        new Vector3(0.35f, 0.45f, 0.35f);

    [Header("Camera Zoom")]
    [SerializeField] private float targetZoom = 4f;
    [SerializeField] private float zoomSmoothTime = 0.2f;

    private float normalZoom;
    private float currentZoom;
    private float zoomVelocity;

    private Vector3 cameraOffset;
    private CinemachinePositionComposer positionComposer;

    private Rigidbody robotRigidbody;
    private Collider robotCollider;
    private S_Robot_Controller robotController;
    private S_CamController_Robot robotCameraController;
    private S_Robot_Follow_Player robotFollower;

    public bool IsProceduralRobotMode => robotTarget != null
        && (robotTarget.GetComponent<S_Procedural_Animation>() != null
            || robotTarget.GetComponent<S_Procedural_Robot_Movement>() != null);

    public Transform RobotMovementReference => robotCameraController != null
        ? robotCameraController.orientation
        : null;

    public float RobotCameraYaw => robotCameraController != null
        ? robotCameraController.Yaw
        : 0f;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Recherche du personnage et de la caméra
        player = FindTransformIncludingInactive("Character_Iso");
        cam = FindTransformIncludingInactive("Follow Camera");

        if (player == null || cam == null)
        {
            Debug.LogError(
                "S_Camera_Controller : Character_Iso ou Follow Camera introuvable."
            );
            enabled = false;
            return;
        }

        // Recherche du Cinemachine Position Composer
        positionComposer =
            cam.GetComponent<CinemachinePositionComposer>();

        if (positionComposer == null)
        {
            positionComposer =
                cam.GetComponentInChildren<CinemachinePositionComposer>(true);
        }

        if (positionComposer != null)
        {
            Debug.Log("Cinemachine Position Composer trouvé !");

            // Camera Smoothing
            positionComposer.Damping = cameraDamping;

            // Distance initiale
            normalZoom = positionComposer.CameraDistance;
            currentZoom = normalZoom;
        }
        else
        {
            Debug.LogWarning(
                "Cinemachine Position Composer introuvable sur Follow Camera."
            );
        }

        // Recherche du robot
        if (robotTarget == null)
        {
            robotTarget =
                FindTransformIncludingInactive("Robot_Procedural");
        }

        if (robotTarget != null)
        {
            robotRigidbody = robotTarget.GetComponent<Rigidbody>();
            robotCollider = robotTarget.GetComponent<Collider>();
            robotController = robotTarget.GetComponent<S_Robot_Controller>();

            robotCameraController =
                robotTarget.GetComponentInChildren<S_CamController_Robot>(true);

            if (robotTarget.GetComponent<S_Procedural_Robot_Movement>() != null)
            {
                robotFollower =
                    robotTarget.GetComponent<S_Robot_Follow_Player>();

                if (robotFollower == null)
                {
                    robotFollower =
                        robotTarget.gameObject.AddComponent<S_Robot_Follow_Player>();
                }

                robotFollower.Configure(
                    player,
                    FindChildTransformIncludingInactive(player, "Robot_Pos")
                );
            }
        }

        if (robotCam == null)
        {
            robotCam =
                FindChildTransformIncludingInactive(
                    robotTarget,
                    "Camera_Robot_Ally"
                )
                ?? FindTransformIncludingInactive("Camera_Robot_Ally");
        }

        if (robotCameraController != null)
            robotCameraController.ConfigureCamera(robotCam);

        cameraOffset = cam.position - player.position;

        distance = new Vector3(
            cameraOffset.x,
            0f,
            cameraOffset.z
        ).magnitude;

        height = cameraOffset.y;

        SetCameraMode(false);
    }

    private void Update()
    {
        SwitchCamera();

        if (robotMode)
        {
            if (robotCameraController != null)
            {
                Vector2 gamepadLook = Gamepad.current != null
                    ? Gamepad.current.rightStick.ReadValue()
                    : Vector2.zero;

                Vector2 mouseLook = Mouse.current != null
                    ? Mouse.current.delta.ReadValue()
                    : Vector2.zero;

                robotCameraController.SetLookInput(
                    gamepadLook,
                    mouseLook
                );

                robotCameraController.MoveCamera();
            }
        }
        else
        {
            CameraRotation();
            CameraZoom();
        }
    }

    public void CameraRotation()
    {
        if (cam == null || player == null)
            return;

        float rotation = 0f;

        // Rotation clavier Q / E
        if (Keyboard.current != null)
        {
            if (Keyboard.current.eKey.isPressed)
                rotation = 1f;
            else if (Keyboard.current.qKey.isPressed)
                rotation = -1f;
        }

        // Rotation manette LB / RB
        if (Gamepad.current != null)
        {
            if (Gamepad.current.rightShoulder.isPressed)
                rotation = 1f;
            else if (Gamepad.current.leftShoulder.isPressed)
                rotation = -1f;
        }

        orbitAngle += rotation * rotationSpeed * Time.deltaTime;

        // Rotation autour du personnage
        Quaternion orbitRotation =
            Quaternion.Euler(0f, orbitAngle, 0f);

        Vector3 rotatedOffset =
            orbitRotation * cameraOffset;

        // Avec Position Composer, on modifie
        // l'orientation de la caméra virtuelle.
        // Cinemachine gère ensuite son suivi.
        cam.rotation = Quaternion.LookRotation(
            -rotatedOffset.normalized,
            Vector3.up
        );
    }

    private void CameraZoom()
    {
        if (positionComposer == null)
            return;

        // Zoom LT manette
        bool zoomGamepad = Gamepad.current != null
            && Gamepad.current.leftTrigger.ReadValue() > 0.1f;

        // Zoom Z clavier
        bool zoomKeyboard = Keyboard.current != null
            && Keyboard.current.zKey.isPressed;

        bool isZooming = zoomGamepad || zoomKeyboard;

        float desiredZoom = isZooming
            ? targetZoom
            : normalZoom;

        desiredZoom = Mathf.Max(0.1f, desiredZoom);

        // Zoom progressif
        currentZoom = Mathf.SmoothDamp(
            currentZoom,
            desiredZoom,
            ref zoomVelocity,
            Mathf.Max(0.01f, zoomSmoothTime)
        );

        positionComposer.CameraDistance = currentZoom;
    }

    public void SwitchCamera()
    {
        bool switchRequested =
            Keyboard.current != null
            && Keyboard.current.iKey.wasPressedThisFrame;

        if (Gamepad.current != null)
        {
            switchRequested |=
                Gamepad.current.dpad.down.wasPressedThisFrame;
        }

        if (switchRequested)
        {
            bool useRobotCamera = !robotMode;

            if (robotController != null)
            {
                robotController.SetRobotMode(useRobotCamera);
            }
            else
            {
                if (robotRigidbody != null)
                    robotRigidbody.isKinematic = !useRobotCamera;

                if (robotCollider != null)
                    robotCollider.isTrigger = !useRobotCamera;
            }

            SetCameraMode(useRobotCamera);
        }
    }

    private void SetCameraMode(bool useRobotCamera)
    {
        robotMode = useRobotCamera;

        if (robotFollower != null)
            robotFollower.SetFollowing(!robotMode);

        if (cam != null)
            cam.gameObject.SetActive(!robotMode);

        if (robotCam != null)
            robotCam.gameObject.SetActive(robotMode);

        if (S_Character_Controller.instance != null)
        {
            S_Character_Controller.instance.canMove = !robotMode;

            if (robotMode)
                S_Character_Controller.instance.pickedUp = false;
        }

        if (robotController != null)
            robotController.SetRobotMode(robotMode);
    }

    private Transform FindTransformIncludingInactive(string objectName)
    {
        Transform[] transforms =
            FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (Transform candidate in transforms)
        {
            if (candidate.name == objectName)
                return candidate;
        }

        return null;
    }

    private Transform FindChildTransformIncludingInactive(
        Transform root,
        string objectName
    )
    {
        if (root == null)
            return null;

        foreach (
            Transform candidate in
            root.GetComponentsInChildren<Transform>(true)
        )
        {
            if (candidate.name == objectName)
                return candidate;
        }

        return null;
    }

    public bool IsRobotCameraController(
        S_CamController_Robot candidate
    )
    {
        return candidate != null
            && robotTarget != null
            && candidate.transform.IsChildOf(robotTarget);
    }

    public void RemoveMouseCursor()
    {
        Cursor.visible = false;
    }
}
