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

    [Header("Parameters")]
    public float distance;
    public float height; 
    public float rotationSpeed;
    public float orbitAngle;
    public bool robotMode { get; private set; }

    private Vector3 cameraOffset;
    private CinemachineOrbitalFollow orbitalFollow;

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

        if (robotCam == null)
        {
            robotCam = FindTransformIncludingInactive("Camera_Robot_Ally");
        }

        if (cam == null || player == null)
        {
            Debug.LogError("S_Camera_Controller: Follow Camera ou Character_Iso est introuvable.");
            return;
        }

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
    }

    public void CameraRotation()
    {
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
        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
        {   
            SetCameraMode(!robotMode);
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
        }

        if (S_Robot_Controller.instance != null)
        {
            S_Robot_Controller.instance.canMove = robotMode;
        }
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
}
