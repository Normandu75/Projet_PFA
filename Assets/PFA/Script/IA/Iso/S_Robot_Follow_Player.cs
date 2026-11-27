using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[DisallowMultipleComponent, DefaultExecutionOrder(100)]
public class S_Robot_Follow_Player : MonoBehaviour
{
    public static readonly Quaternion FollowRotation =
        Quaternion.Euler(0f, 90f, 0f)
        * Quaternion.Euler(-90f, 0f, 0f);

    [Header("Following")]
    [SerializeField, Min(0f)] float destinationRefreshInterval = 0.2f;
    [SerializeField, Min(0f)] float navMeshSampleDistance = 2f;

    NavMeshAgent agent;
    Rigidbody body;
    Transform player;
    Transform followTarget;
    Transform startPosition;
    bool following;
    bool initialized;
    bool hasPlacedAtStartPosition;
    bool hasWarnedAboutNavMesh;
    bool hasWarnedAboutDestination;
    float nextDestinationUpdateTime;
    float visualHeightOffset;
    float relativeYawOffset;
    float followYaw;

    public bool IsFollowing => following;
    public float FollowYaw => Mathf.DeltaAngle(0f, followYaw + relativeYawOffset);
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
        if (!following || !agent.enabled || !agent.isOnNavMesh || followTarget == null)
            return;

        if (Time.time < nextDestinationUpdateTime)
            return;

        nextDestinationUpdateTime = Time.time + destinationRefreshInterval;
        if (!NavMesh.SamplePosition(
                followTarget.position,
                out NavMeshHit targetHit,
                navMeshSampleDistance,
                agent.areaMask))
        {
            if (!hasWarnedAboutDestination)
            {
                Debug.LogWarning("S_Robot_Follow_Player : Robot_Pos est trop loin du NavMesh pour être suivi.", this);
                hasWarnedAboutDestination = true;
            }

            return;
        }

        if (!agent.SetDestination(targetHit.position) && !hasWarnedAboutDestination)
        {
            Debug.LogWarning(
                "S_Robot_Follow_Player : le NavMeshAgent n'a pas accepté la destination de Robot_Pos.",
                this);
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
        followTarget = initialPosition != null ? initialPosition : playerTransform;
        startPosition = initialPosition;
        followYaw = GetPlayerYaw();
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

    public void RotateRelativeYaw(float deltaYaw)
    {
        if (!following)
            return;

        relativeYawOffset = Mathf.DeltaAngle(0f, relativeYawOffset + deltaYaw);
    }

    public void SetFollowYaw(float yaw)
    {
        followYaw = Mathf.DeltaAngle(0f, yaw);
        relativeYawOffset = 0f;
    }

    public void ResetRelativeYaw()
    {
        relativeYawOffset = 0f;
    }

    void EnableFollowing(bool placeAtStartPosition)
    {
        if (followTarget == null)
        {
            Debug.LogError("S_Robot_Follow_Player : Robot_Pos ou le personnage à suivre n'est pas assigné.", this);
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
                followTarget.position,
                out NavMeshHit followTargetHit,
                navMeshSampleDistance,
                agent.areaMask))
        {
            startHit = followTargetHit;
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

        transform.rotation = GetFollowRotation();

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
        agent.stoppingDistance = 0f;

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
        Quaternion playerRotation =
            Quaternion.AngleAxis(FollowYaw, Vector3.up);

        return playerRotation * FollowRotation;
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
