using UnityEngine;
using Unity.Cinemachine;

public class S_Camera_Controller : MonoBehaviour
{
    public static S_Camera_Controller instance;

    [Header("Player")]
    public Transform player;

    [Header("Camera")]
    public Transform cam;

    [Header("Parameters")]
    public float distance;
    public float height; 
    public float rotationSpeed;
    public float orbitAngle;

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
        orbitalFollow = cam.GetComponent<CinemachineOrbitalFollow>();
        cameraOffset = cam.position - player.position;
        distance = new Vector3(cameraOffset.x, 0f, cameraOffset.z).magnitude;
        height = cameraOffset.y;

        cam.LookAt(player);
    }

    public void CameraRotation()
    {
        float rotation = 0;

        if (Input.GetKey(KeyCode.E))
        {
            rotation = 1;

            Debug.Log(rotation);
        }
        else if (Input.GetKey(KeyCode.Q))
        {
            rotation = -1;

            Debug.Log(rotation);
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
}
