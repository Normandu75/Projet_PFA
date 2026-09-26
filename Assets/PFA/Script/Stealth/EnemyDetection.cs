using UnityEngine;

/// <summary>
/// Gère le cône de vision de l'ennemi : angle, distance, et occlusion par raycast.
/// Les méthodes IsPointInCone / HasLineOfSight sont génériques : elles servent
/// aussi bien à détecter le joueur qu'à vérifier si un casier est visible.
/// </summary>
public class EnemyDetection : MonoBehaviour
{
    [Header("Cône de détection")]
    [SerializeField] private Transform eyePoint;      // origine des raycasts (hauteur des yeux)
    [SerializeField] private float viewAngle = 70f;    // angle total du cône (en degrés)
    [SerializeField] private float viewDistance = 12f;
    [SerializeField] private LayerMask obstacleMask;   // murs / portes qui bloquent la vue
    [SerializeField] private LayerMask playerMask;

    private Transform _player;

    public float ViewAngle => viewAngle;
    public float ViewDistance => viewDistance;
    public Vector3 EyePosition => eyePoint != null ? eyePoint.position : transform.position;

    private void Awake()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) _player = playerObj.transform;
        if (eyePoint == null) eyePoint = transform;
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

    /// <summary>
    /// Détection "standard" du joueur pendant la patrouille : ne détecte jamais
    /// un joueur actuellement caché dans un casier (il n'est pas visible du tout).
    /// </summary>
    public bool CanSeePlayer(out Vector3 playerPosition)
    {
        playerPosition = default;
        if (_player == null) return false;
        if (PlayerStealth.Instance != null && PlayerStealth.Instance.IsHidden) return false;

        if (!IsPointInCone(_player.position)) return false;
        if (!HasLineOfSight(_player.position)) return false;

        playerPosition = _player.position;
        return true;
    }

    public Transform Player => _player;

    // ------------------------------------------------------------------
    // DEBUG — visible uniquement dans la Scene view de l'éditeur, jamais
    // en Game view ni dans un build. Rien n'est instancié/rendu en jeu.
    // ------------------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = eyePoint != null ? eyePoint.position : transform.position;
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);

        int segments = 20;
        float half = viewAngle * 0.5f;
        Vector3 prevPoint = origin + Quaternion.Euler(0f, -half, 0f) * transform.forward * viewDistance;

        for (int i = 1; i <= segments; i++)
        {
            float angle = -half + (viewAngle / segments) * i;
            Vector3 point = origin + Quaternion.Euler(0f, angle, 0f) * transform.forward * viewDistance;
            Gizmos.DrawLine(origin, point);
            Gizmos.DrawLine(prevPoint, point);
            prevPoint = point;
        }
    }
}
