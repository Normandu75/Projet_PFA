using UnityEngine;


public class EnemyDetection : MonoBehaviour
{
    [Header("Cône de détection")]
    [SerializeField] private Transform eyePoint;      // origine des raycasts (hauteur des yeux)
    [SerializeField] private float viewAngle = 70f;    // angle total du cône (en degrés)
    [SerializeField] private float viewDistance = 12f;
    [SerializeField] private LayerMask obstacleMask;   // murs / portes qui bloquent la vue

    private Transform _ally;

    public float ViewAngle => viewAngle;
    public float ViewDistance => viewDistance;
    public Vector3 EyePosition => eyePoint != null ? eyePoint.position : transform.position;

    private void Awake()
    {
        FindAlly();
        if (eyePoint == null) eyePoint = transform;
    }

    private Transform FindAlly()
    {
        if (_ally != null)
            return _ally;

        GameObject allyObject = GameObject.FindGameObjectWithTag("Ally");
        if (allyObject != null)
        {
            _ally = allyObject.transform;
            return _ally;
        }

        S_Robot_Controller robotController =
            FindAnyObjectByType<S_Robot_Controller>();
        if (robotController != null)
        {
            _ally = robotController.transform;
            return _ally;
        }

        S_Control_Ally allyController =
            FindAnyObjectByType<S_Control_Ally>();
        if (allyController != null)
            _ally = allyController.transform;

        return _ally;
    }

    /// <summary>Vrai si le point est dans l'angle ET dans la distance du cône (sans check de mur).</summary>
    public bool IsPointInCone(Vector3 point)
    {
        Vector3 toPoint = point - EyePosition;
        toPoint.y = 0f;
        float distance = toPoint.magnitude;
        if (distance > viewDistance) return false;

        float angle = Vector3.Angle(transform.forward, toPoint);
        return angle <= viewAngle * 0.5f;
    }

    /// <summary>Vrai si rien (mur) ne bloque la ligne entre l'ennemi et le point.</summary>
    public bool HasLineOfSight(Vector3 point)
    {
        Vector3 origin = EyePosition;
        Vector3 direction = point - origin;
        float distance = direction.magnitude;

        if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, distance, obstacleMask))
        {
            // Un obstacle est touché avant d'atteindre le point -> vue bloquée.
            return false;
        }
        return true;
    }


    public bool CanSeeAlly(out Vector3 allyPosition)
    {
        allyPosition = default;
        Transform ally = FindAlly();
        if (ally == null) return false;

        return CanSeeTarget(ally, out allyPosition);
    }

    public bool CanSeeTarget(Transform target, out Vector3 targetPosition)
    {
        targetPosition = default;
        if (target == null) return false;
        if (!IsPointInCone(target.position)) return false;
        if (!HasLineOfSight(target.position)) return false;

        targetPosition = target.position;
        return true;
    }
    
    public Transform Ally => FindAlly();
}
