using UnityEngine;
using UnityEngine.InputSystem;

public class S_CamController_Robot : MonoBehaviour
{
    public static S_CamController_Robot instance;

    [Header("References")]
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
        if (camPos == null)
        {
            camPos = transform.parent;
        }

        if (orientation == null && camPos != null)
        {
            orientation = camPos;
        }

        if (camPos != null && !transform.IsChildOf(camPos))
        {
            transform.SetParent(camPos);
            transform.localPosition = cameraLocalPosition;
        }

        pitch = transform.localEulerAngles.x;
        if (pitch > 180f)
        {
            pitch -= 360f;
        }

        yaw = orientation != null ? orientation.eulerAngles.y : transform.eulerAngles.y;
    }

    public void MoveCamera()
    {
        transform.position = camPos != null ? camPos.position + camPos.TransformVector(cameraLocalPosition) : transform.position;

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

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        lookInput = Vector2.zero;
    }

    public void SetLookInput(Vector2 gamepadInput, Vector2 mouseInput)
    {
        lookInput = gamepadInput + mouseInput * mouseLookSensitivity;
    }
}
