using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[DisallowMultipleComponent, DefaultExecutionOrder(100)]
public class S_Robot_Follow_Player : MonoBehaviour
{
    static readonly Quaternion FollowRotation = Quaternion.Euler(-90f, 0f, 0f);

    [Header("Following")]
    [SerializeField, Min(0f)] float destinationRefreshInterval = 0.2f;
    [SerializeField, Min(0f)] float navMeshSampleDistance = 2f;
    [SerializeField, Min(0f)] float followStoppingDistance = 1.25f;

    NavMeshAgent agent;
    Rigidbody body;
    Transform player;
    Transform startPosition;
    bool following;
    bool initialized;
    bool hasPlacedAtStartPosition;
    bool hasWarnedAboutNavMesh;
    bool hasWarnedAboutDestination;
    float nextDestinationUpdateTime;
    float visualHeightOffset;
    float playerYawAtFollowStart;
    Quaternion robotRotationAtFollowStart;

    public bool IsFollowing => following;
    public Vector3 MovementVelocity => following && agent != null && agent.enabled && agent.isOnNavMesh
        ? agent.velocity
        : Vector3.zero;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        body = GetComponent<Rigidbody>();
    }

    void Start()
    {
        initialized = true;
        if (following)
            EnableFollowing(true);
    }

    void Update()
    {
        if (!following || !agent.enabled || !agent.isOnNavMesh || player == null)
            return;

        if (Time.time < nextDestinationUpdateTime)
            return;

        nextDestinationUpdateTime = Time.time + destinationRefreshInterval;
        if (!NavMesh.SamplePosition(player.position, out NavMeshHit targetHit, navMeshSampleDistance, agent.areaMask))
        {
            if (!hasWarnedAboutDestination)
            {
                Debug.LogWarning("S_Robot_Follow_Player : le personnage est trop loin du NavMesh pour être suivi.", this);
                hasWarnedAboutDestination = true;
            }

            return;
        }

        if (!agent.SetDestination(targetHit.position) && !hasWarnedAboutDestination)
        {
            Debug.LogWarning("S_Robot_Follow_Player : le NavMeshAgent n'a pas accepté la destination du personnage.", this);
            hasWarnedAboutDestination = true;
        }
    }

    void LateUpdate()
    {
        if (!following || !agent.enabled || !agent.isOnNavMesh)
            return;

        transform.SetPositionAndRotation(
            agent.nextPosition + Vector3.up * visualHeightOffset,
            GetFollowRotation());
    }

    public void Configure(Transform playerTransform, Transform initialPosition)
    {
        player = playerTransform;
        startPosition = initialPosition;
    }

    public void SetFollowing(bool shouldFollow)
    {
        following = shouldFollow;
        if (!initialized)
            return;

        if (following)
            EnableFollowing(!hasPlacedAtStartPosition);
        else
            DisableFollowing();
    }

    void EnableFollowing(bool placeAtStartPosition)
    {
        if (player == null)
        {
            Debug.LogError("S_Robot_Follow_Player : le personnage à suivre n'est pas assigné.", this);
            following = false;
            return;
        }

        Vector3 desiredPosition = placeAtStartPosition && startPosition != null
            ? startPosition.position
            : transform.position;

        bool hasStartPosition = NavMesh.SamplePosition(
            desiredPosition,
            out NavMeshHit startHit,
            navMeshSampleDistance,
            agent.areaMask);
        if (!hasStartPosition && NavMesh.SamplePosition(
                player.position,
                out NavMeshHit playerHit,
                navMeshSampleDistance,
                agent.areaMask))
        {
            startHit = playerHit;
            hasStartPosition = true;
        }

        if (!hasStartPosition)
        {
            if (!hasWarnedAboutNavMesh)
            {
                Debug.LogError(
                    "S_Robot_Follow_Player : aucun NavMesh trouvé près du robot. Vérifie que le NavMesh est bien cuit et que Robot_Pos est dessus.",
                    this);
                hasWarnedAboutNavMesh = true;
            }

            following = false;
            DisableFollowing();
            return;
        }

        agent.enabled = false;
        transform.position = startHit.position;
        if (placeAtStartPosition)
            transform.rotation = FollowRotation;
        playerYawAtFollowStart = GetPlayerYaw();
        robotRotationAtFollowStart = transform.rotation;

        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
        }

        agent.updatePosition = false;
        agent.updateRotation = false;
        agent.enabled = true;
        agent.isStopped = false;
        agent.stoppingDistance = followStoppingDistance;

        if (!agent.Warp(startHit.position) || !agent.isOnNavMesh)
        {
            Debug.LogError("S_Robot_Follow_Player : l'agent n'a pas pu rejoindre le NavMesh à sa position de départ.", this);
            following = false;
            DisableFollowing();
            return;
        }

        if (placeAtStartPosition && !hasPlacedAtStartPosition)
            visualHeightOffset = CalculateVisualHeightOffset(startHit.position);

        transform.position = startHit.position + Vector3.up * visualHeightOffset;
        hasPlacedAtStartPosition |= placeAtStartPosition;
        nextDestinationUpdateTime = 0f;
    }

    float GetPlayerYaw()
    {
        if (player == null)
            return 0f;

        Vector3 forward = Vector3.ProjectOnPlane(player.forward, Vector3.up);
        return forward.sqrMagnitude > 0.0001f
            ? Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg
            : player.eulerAngles.y;
    }

    Quaternion GetFollowRotation()
    {
        float yawDelta = Mathf.DeltaAngle(playerYawAtFollowStart, GetPlayerYaw());
        return Quaternion.AngleAxis(yawDelta + 180f, Vector3.up) * robotRotationAtFollowStart;
    }

    float CalculateVisualHeightOffset(Vector3 navMeshPosition)
    {
        Bounds visualBounds = default;
        bool hasVisualBounds = false;

        foreach (Renderer childRenderer in GetComponentsInChildren<Renderer>())
        {
            if (!childRenderer.enabled)
                continue;

            if (!hasVisualBounds)
            {
                visualBounds = childRenderer.bounds;
                hasVisualBounds = true;
            }
            else
            {
                visualBounds.Encapsulate(childRenderer.bounds);
            }
        }

        return hasVisualBounds
            ? Mathf.Max(0f, navMeshPosition.y - visualBounds.min.y)
            : 0f;
    }

    void DisableFollowing()
    {
        if (agent.enabled)
        {
            if (agent.isOnNavMesh)
                agent.isStopped = true;
            agent.enabled = false;
        }

        if (body != null)
        {
            body.isKinematic = false;
            body.useGravity = true;
        }
    }
}
