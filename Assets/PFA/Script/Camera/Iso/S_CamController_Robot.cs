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
    public float minPitch = -80f;
    public float maxPitch = 80f;

    Vector2 lookInput;
    float pitch;
    float yaw;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (cam == null)
        {
            cam = FindTransformIncludingInactive("Camera_Robot_Ally");
        }

        if (camPos == null)
        {
            camPos = transform.parent;
        }

        if (orientation == null && camPos != null)
        {
            orientation = camPos;
        }

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
        {
            cam.position = camPos.position;
        }

        pitch = cam.localEulerAngles.x;
        if (pitch > 180f)
        {
            pitch -= 360f;
        }

        yaw = orientation != null ? orientation.eulerAngles.y : cam.eulerAngles.y;
    }

    public void MoveCamera()
    {
        if (cam == null)
        {
            return;
        }

        if (camPos != null)
        {
            cam.position = camPos.position;
        }

        if (lookInput.sqrMagnitude < 0.0001f)
        {
            return;
        }

        yaw += lookInput.x * lookSensitivity;
        pitch = Mathf.Clamp(pitch - lookInput.y * lookSensitivity, minPitch, maxPitch);

        if (orientation != null)
        {
            orientation.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        cam.rotation = Quaternion.Euler(pitch, yaw, 0f);
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
}
