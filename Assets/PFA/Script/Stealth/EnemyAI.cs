using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyDetection))]
public class EnemyAI : MonoBehaviour
{
// =====================================================================
// RÉFÉRENCES
// =====================================================================

[Header("Références")]
[SerializeField] private Animator animator;
[SerializeField] private GameObject alertBeamPrefab;
[SerializeField] private Transform alertLaunchPoint;
private int _currentPatrolIndex = 0;

// =====================================================================
// PATROUILLE
// =====================================================================

[Header("Patrouille")]
[SerializeField] private List<PatrolWaypoint> waypoints;

// =====================================================================
// DÉTECTION
// =====================================================================

[Header("Détection")]
[Tooltip("Rayon de détection autour de l'ennemi.")]
[SerializeField] private float detectionRadius = 3f;

[SerializeField] private bool showDetectionGizmo = true;

// =====================================================================
// POURSUITE / ATTAQUE
// =====================================================================

[Header("Poursuite / Attaque")]
[SerializeField] private float attackRange = 1.5f;

[Tooltip("Temps de préparation avant de déclencher l'attaque.")]
[SerializeField] private float attackAnimDuration = 1f;

[SerializeField] private int attackDamage = 10;

[SerializeField] private float attackCooldown = 1f;

[Tooltip("Vitesse de rotation pendant la préparation de l'attaque.")]

[Header("Rotation")]

[SerializeField] private float detectionRotationSpeed = 720f;
[SerializeField] private float attackRotationSpeed = 720f;

[Tooltip(
    "Distance supplémentaire que le joueur peut parcourir " +
    "pendant la préparation de l'attaque."
)]
[SerializeField] private float attackMoveTolerance = 1f;

[Tooltip(
    "Temps pendant lequel l'ennemi continue sa poursuite " +
    "après avoir perdu le joueur."
)]
[SerializeField] private float loseTargetTime = 3f;


private float attackTimer;
private float attackCooldownTimer;

private bool isPreparingAttack;

private Transform attackTarget;

// =====================================================================
// ALERTE
// =====================================================================

[Header("Alerte")]
[SerializeField] private float alertBeamSpeed = 15f;
[SerializeField] private Color alertBeamColor = Color.red;

// =====================================================================
// DEBUG
// =====================================================================

[Header("Debug — forcer un état en Play Mode")]
[SerializeField] private bool debugForceRoaming;
[SerializeField] private bool debugForceChasing;

// =====================================================================
// LECTURE SEULE
// =====================================================================

[Header("Lecture seule")]
[SerializeField] private EnemyState currentStateReadOnly;

[SerializeField] private Light detectionLight;

// =====================================================================
// BARRE D'ATTAQUE
// =====================================================================

[Header("Barre d'attaque")]
[SerializeField] private Slider attackProgressBar;

// =====================================================================
// LUMIÈRE
// =====================================================================

[Header("Lumière de vision")]
[SerializeField] private Color chasingLightColor =
    new Color(1f, 0.25f, 0.25f);

[SerializeField] private Color roamingLightColor =
    new Color(1f, 0.2f, 0.2f);

[SerializeField] private float lightIntensity = 3f;

// =====================================================================
// VARIABLES INTERNES
// =====================================================================

private NavMeshAgent _agent;
private EnemyDetection _detection;

private EnemyState _state = EnemyState.Roaming;

private bool _isOriginalDetector;

private Vector3 _lastKnownPlayerPosition;

private Coroutine _behaviourRoutine;

private float _loseTargetTimer;

private bool _prevDebugRoaming;
private bool _prevDebugChasing;

private bool _isSubscribedToPlayer;

// =====================================================================
// ANIMATOR HASH
// =====================================================================

private static readonly int AttackTrigger =
    Animator.StringToHash("Attack");

private static readonly int WalkSpeedParam =
    Animator.StringToHash("Speed");

// =====================================================================
// UNITY
// =====================================================================

private void Awake()
{
    _agent = GetComponent<NavMeshAgent>();
    _detection = GetComponent<EnemyDetection>();

    if (alertLaunchPoint == null)
        alertLaunchPoint = transform;
}

private void OnEnable()
{
    EnemyManager.Instance?.Register(this);
}

private void OnDisable()
{
    EnemyManager.Instance?.Unregister(this);

    UnsubscribeFromPlayer();

    StopBehaviourRoutine();
}

private void Start()
{
    if (attackProgressBar != null)
    {
        attackProgressBar.minValue = 0f;
        attackProgressBar.maxValue = 1f;
        attackProgressBar.value = 0f;
        attackProgressBar.gameObject.SetActive(false);
    }

    attackCooldownTimer = 0f;

    StartPatrol();
}

private void Update()
{
    HandleDebugToggles();

    if (attackCooldownTimer > 0f)
    {
        attackCooldownTimer -= Time.deltaTime;

        if (attackCooldownTimer < 0f)
            attackCooldownTimer = 0f;
    }

    if (animator != null)
    {
        animator.SetFloat(
            WalkSpeedParam,
            _agent.velocity.magnitude
        );
    }

    currentStateReadOnly = _state;

    switch (_state)
    {
        case EnemyState.Roaming:
            TickRoaming();
            break;

        case EnemyState.Chasing:
            TickChasing();
            break;
    }
}

private void LateUpdate()
{
    if (attackProgressBar == null)
        return;

    if (!attackProgressBar.gameObject.activeSelf)
        return;

    Camera mainCamera = Camera.main;

    if (mainCamera == null)
        return;

    Transform canvas = attackProgressBar.transform.parent;

    if (canvas == null)
        return;

    canvas.LookAt(
        canvas.position + mainCamera.transform.forward,
        mainCamera.transform.up
    );
}

// =====================================================================
// DEBUG
// =====================================================================

private void HandleDebugToggles()
{
    if (debugForceRoaming && !_prevDebugRoaming)
    {
        debugForceChasing = false;

        ReturnToPatrol();
    }
    else if (debugForceChasing && !_prevDebugChasing)
    {
        debugForceRoaming = false;

        Vector3 target =
            _detection.Player != null
                ? _detection.Player.position
                : transform.position;

        _isOriginalDetector = true;

        BeginChase(target);
    }

    _prevDebugRoaming = debugForceRoaming;
    _prevDebugChasing = debugForceChasing;
}

// =====================================================================
// ROAMING
// =====================================================================

private void TickRoaming()
{
    // Détection par cône de vision
    if (_detection.CanSeePlayer(out Vector3 playerPos))
    {
        OnPlayerDetected(playerPos);
        return;
    }

    if (IsPlayerInDetectionRadius(
    out Vector3 nearbyPlayerPos))
{
    // On tourne immédiatement vers le joueur
    RotateTowardsPosition(
        nearbyPlayerPos,
        detectionRotationSpeed
    );

    OnPlayerDetected(nearbyPlayerPos);
}
}

private bool IsPlayerInDetectionRadius(
    out Vector3 playerPosition)
{
    playerPosition = Vector3.zero;

    if (_detection.Player == null)
        return false;

    playerPosition = _detection.Player.position;

    float distance = Vector3.Distance(
        transform.position,
        playerPosition
    );

    return distance <= detectionRadius;
}

// =====================================================================
// PATROUILLE
// =====================================================================

private void StartPatrol()
{
    StopBehaviourRoutine();

    ResetAttackProgressBar();

    attackTarget = null;

    _loseTargetTimer = 0f;

    _agent.isStopped = false;
    _agent.updateRotation = true;

    SetState(EnemyState.Roaming);

    // On lance la patrouille
    _behaviourRoutine = StartCoroutine(PatrolRoutine());
}

private IEnumerator PatrolRoutine()
{
    {
    if (waypoints == null || waypoints.Count == 0)
    {
        Debug.LogWarning(
            $"[EnemyAI] {name} n'a aucun waypoint."
        );

        _behaviourRoutine = null;
        yield break;
    }

    while (_state == EnemyState.Roaming)
    {
        // Sécurité
        if (_currentPatrolIndex >= waypoints.Count)
            _currentPatrolIndex = 0;

        PatrolWaypoint wp =
            waypoints[_currentPatrolIndex];

        if (wp == null)
        {
            _currentPatrolIndex++;

            if (_currentPatrolIndex >= waypoints.Count)
                _currentPatrolIndex = 0;

            yield return null;
            continue;
        }

        // --------------------------------------------------------------
        // Préparation de l'agent
        // --------------------------------------------------------------

        _agent.isStopped = false;
        _agent.updateRotation = true;

        if (!_agent.isOnNavMesh)
        {
            yield return null;
            continue;
        }

        // --------------------------------------------------------------
        // ENVOI VERS LE WAYPOINT
        // --------------------------------------------------------------

        _agent.ResetPath();

        bool destinationSet =
            _agent.SetDestination(
                wp.transform.position
            );

        if (!destinationSet)
        {
            Debug.LogWarning(
                $"[EnemyAI] {name} ne peut pas définir la destination " +
                $"vers {wp.name}."
            );

            yield return null;
            continue;
        }

        // --------------------------------------------------------------
        // ATTENTE DE L'ARRIVÉE
        // --------------------------------------------------------------

        while (_state == EnemyState.Roaming)
        {
            if (_agent.pathPending)
            {
                yield return null;
                continue;
            }

            if (_agent.pathStatus ==
                NavMeshPathStatus.PathInvalid)
            {
                Debug.LogWarning(
                    $"[EnemyAI] {name} chemin invalide vers {wp.name}."
                );

                break;
            }

            if (_agent.remainingDistance <=
                _agent.stoppingDistance + 0.15f)
            {
                break;
            }

            yield return null;
        }

        // Si l'ennemi a détecté le joueur pendant le trajet,
        // on sort immédiatement de la patrouille.
        if (_state != EnemyState.Roaming)
            yield break;

        // --------------------------------------------------------------
        // ATTENTE SUR LE WAYPOINT
        // --------------------------------------------------------------

        if (wp.waitTime > 0f)
        {
            yield return new WaitForSeconds(
                wp.waitTime
            );
        }

        // --------------------------------------------------------------
        // WAYPOINT SUIVANT
        // --------------------------------------------------------------

        _currentPatrolIndex++;

        if (_currentPatrolIndex >= waypoints.Count)
            _currentPatrolIndex = 0;
    }

    _behaviourRoutine = null;
    }
}
private void RotateTowardsPosition(
    Vector3 targetPosition,
    float rotationSpeed)
{
    Vector3 direction =
        targetPosition - transform.position;

    direction.y = 0f;

    if (direction.sqrMagnitude < 0.001f)
        return;

    Quaternion targetRotation =
        Quaternion.LookRotation(direction);

    transform.rotation =
        Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
}


private void ReturnToPatrol()
{
    UnsubscribeFromPlayer();

    _loseTargetTimer = 0f;

    ResetAttackProgressBar();

    attackTarget = null;

    // On remet l'agent dans un état propre
    _agent.isStopped = false;
    _agent.updateRotation = true;

    // On repasse explicitement en roaming
    SetState(EnemyState.Roaming);

    // On arrête l'ancienne coroutine
    StopBehaviourRoutine();

    // On relance la patrouille
    _behaviourRoutine =
        StartCoroutine(PatrolRoutine());
}
// =====================================================================
// DÉTECTION / CHASE
// =====================================================================

private void OnPlayerDetected(
    Vector3 playerPosition)
{
    if (_state == EnemyState.Chasing)
        return;

    _isOriginalDetector = true;

    BeginChase(playerPosition);
}

public void OnAlerted(
    Vector3 lastKnownPlayerPosition)
{
    if (_state == EnemyState.Chasing)
        return;

    _isOriginalDetector = false;

    BeginChase(lastKnownPlayerPosition);
}

private void BeginChase(
    Vector3 playerPosition)
{
    StopBehaviourRoutine();

    ResetAttackProgressBar();

    attackTarget = null;

    _lastKnownPlayerPosition =
        playerPosition;

    _loseTargetTimer = 0f;

    SetState(EnemyState.Chasing);

    _agent.isStopped = false;
    _agent.updateRotation = true;

    if (_agent.isOnNavMesh)
    {
        _agent.ResetPath();

        _agent.SetDestination(
            playerPosition
        );
    }

    // Rotation immédiate vers le joueur
    RotateTowardsPositionInstant(
        playerPosition
    );

    SubscribeToPlayer();

    if (_isOriginalDetector)
    {
        BroadcastAlert();
    }
}
private void RotateTowardsPositionInstant(
    Vector3 targetPosition)
{
    Vector3 direction =
        targetPosition - transform.position;

    direction.y = 0f;

    if (direction.sqrMagnitude < 0.001f)
        return;

    transform.rotation =
        Quaternion.LookRotation(direction);
}

// =====================================================================
// CHASING
// =====================================================================

private void TickChasing()
{
    if (_detection.Player == null)
    {
        ResetAttackProgressBar();
        return;
    }

    Transform player =
        _detection.Player;

    // --------------------------------------------------------------
    // ATTAQUE EN COURS
    // --------------------------------------------------------------

    if (isPreparingAttack)
    {
        TickAttackPreparation(player);
        return;
    }

    // --------------------------------------------------------------
    // JOUEUR VISIBLE PAR LE CÔNE
    // --------------------------------------------------------------

    if (_detection.CanSeePlayer(
        out Vector3 currentPos))
    {
        HandlePlayerDetectedWhileChasing(
            player,
            currentPos
        );

        return;
    }

    // --------------------------------------------------------------
    // JOUEUR DANS LE RAYON DE PROXIMITÉ
    // --------------------------------------------------------------

    if (IsPlayerInDetectionRadius(
        out Vector3 nearbyPlayerPos))
    {
        HandlePlayerDetectedWhileChasing(
            player,
            nearbyPlayerPos
        );

        return;
    }

    // --------------------------------------------------------------
    // JOUEUR PERDU
    // --------------------------------------------------------------

    bool playerHidden =
        PlayerStealth.Instance != null &&
        PlayerStealth.Instance.IsHidden;

    if (playerHidden)
    {
        ResetAttackProgressBar();
        return;
    }

    ResetAttackProgressBar();

    _loseTargetTimer += Time.deltaTime;

    if (_loseTargetTimer >= loseTargetTime)
    {
        ReturnToPatrol();
    }
}

private void HandlePlayerDetectedWhileChasing(
    Transform player,
    Vector3 playerPosition)
{
    _loseTargetTimer = 0f;

    _lastKnownPlayerPosition =
        playerPosition;

    float distance =
        Vector3.Distance(
            transform.position,
            playerPosition
        );

    // --------------------------------------------------------------
    // ATTACK
    // --------------------------------------------------------------

    if (distance <= attackRange &&
        attackCooldownTimer <= 0f)
    {
        BeginAttackPreparation(player);

        return;
    }

    // --------------------------------------------------------------
    // CHASE
    // --------------------------------------------------------------

    _agent.isStopped = false;

    _agent.updateRotation = true;

    ResetAttackProgressBar();

    if (_agent.isOnNavMesh)
    {
        _agent.SetDestination(
            playerPosition
        );
    }
}

// =====================================================================
// ATTAQUE
// =====================================================================

private void BeginAttackPreparation(
    Transform player)
{
    if (player == null)
        return;

    if (isPreparingAttack)
        return;

    if (attackCooldownTimer > 0f)
        return;

    isPreparingAttack = true;

    attackTarget = player;

    attackTimer = 0f;

    _agent.isStopped = true;

    // On désactive temporairement la rotation automatique
    // uniquement pendant la préparation.
    _agent.updateRotation = false;

    if (attackProgressBar != null)
    {
        attackProgressBar.gameObject.SetActive(
            true
        );
    }

    RotateTowardsPlayer(player);
}

private void TickAttackPreparation(
    Transform player)
{
    if (attackTarget == null)
    {
        CancelAttackPreparation();
        return;
    }

    _agent.isStopped = true;

    // Rotation manuelle uniquement pendant l'attaque.
    RotateTowardsPlayer(
        attackTarget
    );

    float distance =
        Vector3.Distance(
            transform.position,
            attackTarget.position
        );

    // Le joueur doit réellement s'éloigner
    // pour annuler l'attaque.
    if (distance >
        attackRange + attackMoveTolerance)
    {
        CancelAttackPreparation();

        _agent.isStopped = false;

        _agent.updateRotation = true;

        return;
    }

    attackTimer += Time.deltaTime;

    UpdateAttackProgressBar();

    if (attackTimer >= attackAnimDuration)
    {
        PlayAttackAnimation();

        DealDamageToPlayer();

        attackCooldownTimer =
            attackCooldown;

        ResetAttackProgressBar();

        // ----------------------------------------------------------
        // TRÈS IMPORTANT :
        // on rend immédiatement la rotation au NavMeshAgent.
        // ----------------------------------------------------------

        _agent.updateRotation = true;

        _agent.isStopped = false;

        // Si le joueur est toujours là,
        // on repart immédiatement vers lui.
        if (_detection.Player != null)
        {
            _agent.SetDestination(
                _detection.Player.position
            );
        }
    }
}

private void CancelAttackPreparation()
{
    ResetAttackProgressBar();

    attackTarget = null;

    _agent.updateRotation = true;
}

private void RotateTowardsPlayer(
    Transform target)
{
    if (target == null)
        return;

    Vector3 direction =
        target.position -
        transform.position;

    direction.y = 0f;

    if (direction.sqrMagnitude <
        0.001f)
    {
        return;
    }

    Quaternion targetRotation =
        Quaternion.LookRotation(
            direction
        );

    transform.rotation =
        Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            attackRotationSpeed *
            Time.deltaTime
        );
}

private void PlayAttackAnimation()
{
    if (animator != null)
    {
        animator.SetTrigger(
            AttackTrigger
        );
    }
}

private void DealDamageToPlayer()
{
    Transform target =
        attackTarget != null
            ? attackTarget
            : _detection.Player;

    if (target == null)
        return;

    S_HealthBar playerHealth =
        target.GetComponentInParent<S_HealthBar>();

    if (playerHealth != null)
    {
        playerHealth.TakeDamage(
            attackDamage
        );

        Debug.Log(
            "Enemy attacks player : -" +
            attackDamage
        );
    }
    else
    {
        Debug.LogWarning(
            "S_HealthBar introuvable sur le Player."
        );
    }
}

// =====================================================================
// ALERTE
// =====================================================================

private void BroadcastAlert()
{
    if (EnemyManager.Instance == null ||
        alertBeamPrefab == null)
    {
        return;
    }

    foreach (
        EnemyAI other
        in EnemyManager.Instance.GetOtherEnemies(this))
    {
        if (other == null)
            continue;

        GameObject beamObj =
            Instantiate(
                alertBeamPrefab,
                alertLaunchPoint.position,
                Quaternion.identity
            );

        Renderer renderer =
            beamObj.GetComponentInChildren<Renderer>();

        if (renderer != null)
        {
            renderer.material.color =
                alertBeamColor;
        }

        EnemyAlertBeam beam =
            beamObj.GetComponent<EnemyAlertBeam>();

        if (beam == null)
            continue;

        Vector3 alertPosition =
            _lastKnownPlayerPosition;

        beam.Launch(
            other.transform,
            alertBeamSpeed,
            () => other.OnAlerted(
                alertPosition
            )
        );
    }
}

// =====================================================================
// GESTION DU JOUEUR CACHÉ
// =====================================================================

private void SubscribeToPlayer()
{
    if (_isSubscribedToPlayer)
        return;

    if (PlayerStealth.Instance == null)
        return;

    PlayerStealth.Instance.OnPlayerHidden +=
        HandlePlayerHidden;

    _isSubscribedToPlayer = true;
}

private void UnsubscribeFromPlayer()
{
    if (!_isSubscribedToPlayer)
        return;

    if (PlayerStealth.Instance != null)
    {
        PlayerStealth.Instance.OnPlayerHidden -=
            HandlePlayerHidden;
    }

    _isSubscribedToPlayer = false;
}

private void HandlePlayerHidden(
    HidingLocker locker)
{
    if (_state != EnemyState.Chasing)
        return;

    // Il n'y a plus de système de Surveillance.
    // Lorsque le joueur se cache, l'ennemi perd simplement
    // progressivement sa cible.

    ResetAttackProgressBar();
}

// =====================================================================
// BARRE D'ATTAQUE
// =====================================================================

private void UpdateAttackProgressBar()
{
    if (attackProgressBar == null)
        return;

    if (attackAnimDuration <= 0f)
    {
        attackProgressBar.value = 1f;
        return;
    }

    attackProgressBar.value =
        Mathf.Clamp01(
            attackTimer /
            attackAnimDuration
        );
}

private void ResetAttackProgressBar()
{
    attackTimer = 0f;

    isPreparingAttack = false;

    attackTarget = null;

    if (attackProgressBar != null)
    {
        attackProgressBar.value = 0f;

        attackProgressBar.gameObject.SetActive(
            false
        );
    }
}

// =====================================================================
// COROUTINE
// =====================================================================

private void StopBehaviourRoutine()
{
    if (_behaviourRoutine == null)
        return;

    StopCoroutine(
        _behaviourRoutine
    );

    _behaviourRoutine = null;
}

// =====================================================================
// ÉTAT
// =====================================================================

private void SetState(
    EnemyState newState)
{
    _state = newState;

    UpdateDetectionLight(
        newState
    );
}

private void UpdateDetectionLight(
    EnemyState state)
{
    if (detectionLight == null)
        return;

    detectionLight.intensity =
        lightIntensity;

    switch (state)
    {
        case EnemyState.Chasing:

            detectionLight.enabled = true;

            detectionLight.color =
                chasingLightColor;

            break;

        case EnemyState.Roaming:

            detectionLight.enabled = true;

            detectionLight.color =
                roamingLightColor;

            break;

        default:

            detectionLight.enabled = false;

            break;
    }
}

// =====================================================================
// GIZMOS
// =====================================================================

private void OnDrawGizmosSelected()
{
    if (showDetectionGizmo)
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            detectionRadius
        );
    }

    Gizmos.color = Color.red;

    Gizmos.DrawWireSphere(
        transform.position,
        attackRange
    );

}
public void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.tag == "Player")
        {
            Rigidbody rgb = GameObject.Find("Enemy").GetComponent<Rigidbody>();
            rgb.isKinematic = true;
            Debug.Log("ZIZI PUANT");
        }
    }
public void OnCollisionExit (Collision other)
    {
        Rigidbody rgb = GameObject.Find("Enemy").GetComponent<Rigidbody>();
        rgb.isKinematic = false;
    }

}
