using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class S_Character_Controller : MonoBehaviour
{
   public static S_Character_Controller instance;

    [Header("Movement")]
    public float speed = 6f;

    [Header("Components")]
    public Rigidbody rigidBody;
    public Camera cam;
    public GameObject target;
    public LayerMask MyLayerMask;

    [Header("See Through Walls")]
    public float sphereMaxScale = 4.23831606f;
    public float sphereScaleSpeed = 8f;
    
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

        rigidBody = GetComponent<Rigidbody>();
        rigidBody.constraints = RigidbodyConstraints.FreezeRotation;
        cam = Camera.main;
        target = GameObject.Find("See_Thrg_Wall");
    }

    public void CursorToCamera()
    {
        float distanceFromCamera = 0f;

        if (!cam.orthographic && Mathf.Abs(cam.transform.forward.y) > 0.001f)
        {
            distanceFromCamera = (transform.position.y - cam.transform.position.y) / cam.transform.forward.y;
        }

        Vector3 mousePosition = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, distanceFromCamera));
        Vector3 direction = mousePosition - transform.position;
        
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    public void Movement()
    {
        Vector3 velocity = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical")).normalized * speed;

        rigidBody.MovePosition(rigidBody.position + velocity * Time.fixedDeltaTime);
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
}
