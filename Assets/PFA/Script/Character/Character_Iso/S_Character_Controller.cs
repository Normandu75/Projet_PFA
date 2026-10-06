using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class S_Character_Controller : MonoBehaviour
{
    [System.Serializable]
    public class LayerDetectionEntry
    {
        public string label;
        public LayerMask layerMask;
        public List<GameObject> detectedObjects = new List<GameObject>();

        public void Clear()
        {
            detectedObjects.Clear();
        }

        public void Add(GameObject detectedObject)
        {
            if (!detectedObjects.Contains(detectedObject))
                detectedObjects.Add(detectedObject);
        }
    }

   public static S_Character_Controller instance;

    [Header("Movement")]
    public float speed = 6f;
    public bool canMove = true;

    [Header("Components")]
    public Rigidbody rigidBody;
    public Camera cam;
    public GameObject target;
    public LayerMask MyLayerMask;
    public float detectionRadius;

    [Header("Overlap Sphere - Layer Detection")]
    [SerializeField]
    private List<LayerDetectionEntry> detectionLayers =     new List<LayerDetectionEntry>
    {
        new LayerDetectionEntry
        {
            label = "Ally"
        },
        new LayerDetectionEntry
        {
            label = "Enemy"
        }
    };

    [Header("See Through Walls")]
    public float sphereMaxScale = 4.23831606f;
    public float sphereScaleSpeed = 8f;

    [Header("Gamepad")]
    public float gamepadDeadzone = 0.2f;
    public bool detected;
    public bool pickedUp;

    private int allyLayer = -1;
    
    void Awake()
    {
        allyLayer = LayerMask.NameToLayer("Ally");
        if (allyLayer < 0)
            Debug.LogWarning("La layer 'Ally' n'existe pas dans les paramètres du projet.", this);

        InitializeDetectionLayerMasks();

        if (instance == null)
        {
            instance = this;
        }
        else
        {
            DontDestroyOnLoad(gameObject);
        }

        rigidBody = GetComponent<Rigidbody>();
        rigidBody.constraints = RigidbodyConstraints.FreezeRotation;
        cam = Camera.main;
        target = GameObject.Find("See_Thrg_Wall");
    }

    private void InitializeDetectionLayerMasks()
    {
        foreach (LayerDetectionEntry entry in detectionLayers)
        {
            if (entry == null || entry.layerMask.value != 0)
                continue;

            int layer = LayerMask.NameToLayer(entry.label);
            if (layer >= 0)
            {
                entry.layerMask = 1 << layer;
            }
            else
            {
                Debug.LogWarning(
                    $"La layer '{entry.label}' n'existe pas dans les paramètres du projet.",
                    this
                );
            }
        }
    }

    void Update()
    {
        Detection();
    }

    public void CursorToCamera()
    {
        Gamepad gamepad = Gamepad.current;
        Vector2 rightStick = gamepad != null ? gamepad.rightStick.ReadValue() : Vector2.zero;

        if (rightStick.sqrMagnitude > gamepadDeadzone * gamepadDeadzone)
        {
            Vector3 cameraRight = cam.transform.right;
            Vector3 cameraForward = cam.transform.forward;
            cameraRight.y = 0f;
            cameraForward.y = 0f;

            Vector3 gamepadDirection = cameraRight.normalized * rightStick.x + cameraForward.normalized * rightStick.y;

            if (gamepadDirection.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(gamepadDirection);
            }

            return;
        }

        if (Mouse.current == null)
        {
            return;
        }

        float distanceFromCamera = 0f;

        if (!cam.orthographic && Mathf.Abs(cam.transform.forward.y) > 0.001f)
        {
            distanceFromCamera = (transform.position.y - cam.transform.position.y) / cam.transform.forward.y;
        }

        Vector2 mousePositionOnScreen = Mouse.current.position.ReadValue();
        Vector3 mousePosition = cam.ScreenToWorldPoint(new Vector3(mousePositionOnScreen.x, mousePositionOnScreen.y, distanceFromCamera));
        Vector3 direction = mousePosition - transform.position;
        
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    public void Movement()
    { if (canMove)
        {
            

            Vector2 keyboardInput = Vector2.zero;

            if (Keyboard.current != null)
            {
                keyboardInput = new Vector2(
                    (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed ? 1f : 0f) -
                    (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed ? 1f : 0f),
                    (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed ? 1f : 0f) -
                    (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed ? 1f : 0f));
            }

            Vector2 gamepadInput = Gamepad.current != null? Gamepad.current.leftStick.ReadValue() : Vector2.zero;
        
            Vector2 input = gamepadInput.sqrMagnitude > keyboardInput.sqrMagnitude? gamepadInput : keyboardInput;
                
            if (cam == null)
            {
                return;
            }

            Vector3 cameraForward = cam.transform.forward;
            Vector3 cameraRight = cam.transform.right;

            cameraForward.y = 0f;
            cameraRight.y = 0f;

            cameraForward.Normalize();
            cameraRight.Normalize();

            Vector3 moveDirection = cameraRight * input.x + cameraForward * input.y;
            Vector3 velocity = Vector3.ClampMagnitude(moveDirection, 1f) * speed;

            rigidBody.MovePosition(rigidBody.position + velocity * Time.fixedDeltaTime);
        }
    }

    public void SeeThroughWalls()
    {
        if (target == null || cam == null)
        {
            return;
        }

        Vector3 toPlayer = transform.position - cam.transform.position;

        float distanceToPlayer = toPlayer.magnitude;
        
        bool playerIsBlocked = false;

        if (distanceToPlayer > 0.001f)
        {
            RaycastHit[] hits = Physics.RaycastAll( cam.transform.position, toPlayer.normalized, distanceToPlayer, MyLayerMask);

            foreach (RaycastHit hit in hits)
            {
                Transform hitTransform = hit.collider.transform;

                if (!hitTransform.IsChildOf(transform) && !hitTransform.IsChildOf(target.transform) && hitTransform != transform && hitTransform != target.transform)
                {
                    playerIsBlocked = true;
                    break;
                }
            }
        }

        float targetScale = playerIsBlocked ? sphereMaxScale : 0f;
        float newScale = Mathf.MoveTowards( target.transform.localScale.x, targetScale, sphereScaleSpeed * Time.deltaTime);

        target.transform.localScale = Vector3.one * newScale;
    }

    public void PickUpRobotAlly()
    {
        if (detected)
        {
            if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame || Keyboard.current.fKey.wasPressedThisFrame)
                pickedUp = true;

            if (!pickedUp)
                return;

            GameObject robot = GameObject.Find("Robot_Ally");
            Transform robotPos = GameObject.Find("Robot_Pos")?.transform;

            if (robot == null || robotPos == null)
                return;

            robot.transform.position = robotPos.position;
            robot.transform.rotation = transform.rotation;
        }
    }

    private void Detection()
    {
        int combinedLayerMask = allyLayer >= 0 ? 1 << allyLayer : 0;

        foreach (LayerDetectionEntry entry in detectionLayers)
        {
            if (entry == null)
                continue;

            entry.Clear();
            combinedLayerMask |= entry.layerMask.value;
        }

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            detectionRadius,
            combinedLayerMask
        );

        detected = false;
        foreach (Collider hit in hits)
        {
            int hitLayerMask = 1 << hit.gameObject.layer;
            if (allyLayer >= 0 && hit.gameObject.layer == allyLayer)
                detected = true;

            foreach (LayerDetectionEntry entry in detectionLayers)
            {
                if (entry != null &&
                    (entry.layerMask.value & hitLayerMask) != 0)
                {
                    entry.Add(hit.gameObject);
                }
            }
        }

        if (!detected)
            pickedUp = false;
    }
}
