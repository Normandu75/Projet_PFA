using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using Unity.VisualScripting;

public class S_Camera_Controller : MonoBehaviour
{
    public static S_Camera_Controller instance;

    [Header("Player")]
    public Transform player;

    [Header("Camera")]
    public Transform cam;
    public Transform robotCam;
    [SerializeField] Transform robotTarget;

    [Header("Parameters")]
    public float distance;
    public float height; 
    public float rotationSpeed;
    public float orbitAngle;
    public bool robotMode { get; private set; }

    private Vector3 cameraOffset;
    private CinemachineOrbitalFollow orbitalFollow;
    private Rigidbody robotRigidbody;
    private Collider robotCollider;
    private S_Robot_Controller robotController;
    private S_CamController_Robot robotCameraController;

    public bool IsProceduralRobotMode => robotTarget != null
        && (robotTarget.GetComponent<S_Procedural_Animation>() != null
            || robotTarget.GetComponent<S_Procedural_Robot_Movement>() != null);
    public Transform RobotMovementReference => robotCameraController != null
        ? robotCameraController.orientation
        : null;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            DontDestroyOnLoad(gameObject);
        }

        player = GameObject.Find("Character_Iso")?.transform;
        cam = GameObject.Find("Follow Camera")?.transform;

        if (robotTarget == null)
            robotTarget = FindTransformIncludingInactive("Robot_Procédural")
                ?? FindTransformIncludingInactive("Robot_Ally");

        if (robotTarget != null)
        {
            robotRigidbody = robotTarget.GetComponent<Rigidbody>();
            robotCollider = robotTarget.GetComponent<Collider>();
            robotController = robotTarget.GetComponent<S_Robot_Controller>();
            robotCameraController = robotTarget.GetComponentInChildren<S_CamController_Robot>(true);
        }

        if (robotCam == null)
        {
            robotCam = FindChildTransformIncludingInactive(robotTarget, "Camera_Robot_Ally")
                ?? FindTransformIncludingInactive("Camera_Robot_Ally");
        }

        if (cam == null || player == null)
        {
            Debug.LogError("S_Camera_Controller: Follow Camera ou Character_Iso est introuvable.");
            return;
        }

        if (robotCameraController != null)
            robotCameraController.ConfigureCamera(robotCam);

        orbitalFollow = cam.GetComponent<CinemachineOrbitalFollow>();
        cameraOffset = cam.position - player.position;
        distance = new Vector3(cameraOffset.x, 0f, cameraOffset.z).magnitude;
        height = cameraOffset.y;

        cam.LookAt(player);
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
                robotCameraController.SetLookInput(gamepadLook, mouseLook);
                robotCameraController.MoveCamera();
            }
        }
        else
        {
            CameraRotation();
        }
    }

    public void CameraRotation()
    {
        if (cam == null || player == null)
            return;

        float rotation = 0f;

        if (Keyboard.current != null && Keyboard.current.eKey.isPressed)
        {
            rotation = 1;
        }
        else if (Keyboard.current != null && Keyboard.current.qKey.isPressed)
        {
            rotation = -1;
        }

        if (Gamepad.current != null)
        {
            if (Gamepad.current.rightShoulder.isPressed)
            {
                rotation = 1f;
            }
            else if (Gamepad.current.leftShoulder.isPressed)
            {
                rotation = -1f;
            }
        }

        orbitAngle += rotation * rotationSpeed * Time.deltaTime;

        if (orbitalFollow != null)
        {
            orbitalFollow.HorizontalAxis.Value += rotation * rotationSpeed * Time.deltaTime;
            return;
        }

        Quaternion orbitRotation = Quaternion.Euler(0f, orbitAngle, 0f);
        cam.position = player.position + orbitRotation * cameraOffset;
        
        cam.LookAt(player);
    }

    public void SwitchCamera()
    {
        bool switchRequested = Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame;

        if (Gamepad.current != null)
            switchRequested |= Gamepad.current.dpad.down.wasPressedThisFrame;

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

        if (cam != null)
        {
            cam.gameObject.SetActive(!robotMode);
        }

        if (robotCam != null)
        {
            robotCam.gameObject.SetActive(robotMode);
        }

        if (S_Character_Controller.instance != null)
        {
            S_Character_Controller.instance.canMove = !robotMode;

            if (robotMode)
            {
                S_Character_Controller.instance.pickedUp = false;
            }
        }

        if (robotController != null)
            robotController.SetRobotMode(robotMode);
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

    private Transform FindChildTransformIncludingInactive(Transform root, string objectName)
    {
        if (root == null)
            return null;

        foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
        {
            if (candidate.name == objectName)
                return candidate;
        }

        return null;
    }

    public bool IsRobotCameraController(S_CamController_Robot candidate)
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
