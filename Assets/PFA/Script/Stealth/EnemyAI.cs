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

    [SerializeField]
    private Color chasingLightColor =

        new Color(1f, 0.25f, 0.25f);

    [SerializeField]
    private Color roamingLightColor =

        new Color();

    [SerializeField] private float lightIntensity = 3f;

    // =====================================================================
    // VARIABLES INTERNES
    // =====================================================================
    private NavMeshAgent _agent;

    private Rigidbody _body;

    private EnemyDetection _detection;

    private EnemyState _state = EnemyState.Roaming;
    private Transform _chaseTarget;

    private bool _isOriginalDetector;
    private Transform _fieldOfViewTarget;
    private bool _isFieldOfViewChase;
    private bool _hasSeenFieldOfViewTarget;
    private Vector3 _lastKnownPlayerPosition;
    private Coroutine _behaviourRoutine;
    private Coroutine _decoyRoutine;
    private bool _isInvestigatingDecoy;
    private float _loseTargetTimer;
    private bool _prevDebugRoaming;
    private bool _prevDebugChasing;
    // =====================================================================
    // UNITY
    // =====================================================================
    private void Awake()

    {
        _agent = GetComponent<NavMeshAgent>();
        _body = GetComponent<Rigidbody>();
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
        StopBehaviourRoutine();
    }
    private void Start()
    {
        ConfigureAttackProgressBar();
        attackCooldownTimer = 0f;
        StartPatrol();
    }
    private void Update()
    {
        HandleDebugToggles();
        attackCooldownTimer =
            Mathf.Max(0f, attackCooldownTimer - Time.deltaTime);
        currentStateReadOnly = _state;
        if (_state == EnemyState.Roaming &&
            !debugForceRoaming &&
            TryDetectTarget(out Transform detectedTarget, out Vector3 targetPosition))
        {
            BeginChase(detectedTarget, targetPosition);
        }
        switch (_state)
        {
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
            Transform chaseTarget = _detection.Player != null
                ? _detection.Player
                : _detection.Ally;
            Vector3 target =
                chaseTarget != null
                    ? chaseTarget.position
                    : transform.position;
            _isOriginalDetector = true;
            BeginChase(chaseTarget, target);
        }
        _prevDebugRoaming = debugForceRoaming;
        _prevDebugChasing = debugForceChasing;
    }
    private bool IsTargetInDetectionRadius(
        Transform target,
        out Vector3 targetPosition)
    {
        targetPosition = Vector3.zero;
        if (target == null)
            return false;
        targetPosition = target.position;
        float distance = Vector3.Distance(
            transform.position,
            targetPosition
        );
        return distance <= detectionRadius &&
               _detection.HasLineOfSight(targetPosition);
    }

    private bool TryDetectTarget(
        out Transform target,
        out Vector3 targetPosition)
    {
        target = _detection.Player;
        bool playerHidden =
            PlayerStealth.Instance != null &&
            PlayerStealth.Instance.IsHidden;
        if (!playerHidden)
        {
            if (_detection.CanSeePlayer(out targetPosition))
                return true;
            if (IsTargetInDetectionRadius(target, out targetPosition))
                return true;
        }

        target = _detection.Ally;
        if (_detection.CanSeeAlly(out targetPosition))
            return true;
        if (IsTargetInDetectionRadius(target, out targetPosition))
            return true;

        target = null;
        targetPosition = Vector3.zero;
        return false;
    }
    // =====================================================================
    // PATROUILLE
    // =====================================================================
    private void StartPatrol()

    {

        StopBehaviourRoutine();

        ResetAttackProgressBar();

        _loseTargetTimer = 0f;

        _chaseTarget = null;
        SetAgentMovement(true, true);

        SetState(EnemyState.Roaming);

        _behaviourRoutine = StartCoroutine(PatrolRoutine());

    }

    private IEnumerator PatrolRoutine()

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

            SetAgentMovement(true, true);

            if (!_agent.isOnNavMesh)

            {

                yield return null;

                continue;

            }

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

            if (_state != EnemyState.Roaming)

                yield break;

            if (wp.waitTime > 0f)

            {

                yield return new WaitForSeconds(

                    wp.waitTime

                );

            }

            _currentPatrolIndex++;

            if (_currentPatrolIndex >= waypoints.Count)

                _currentPatrolIndex = 0;

        }

        _behaviourRoutine = null;

    }

    private void SetAgentMovement(bool moving, bool automaticRotation)

    {

        _agent.isStopped = !moving;

        _agent.updateRotation = automaticRotation;

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

        _chaseTarget = null;
        _fieldOfViewTarget = null;
        _isFieldOfViewChase = false;
        _hasSeenFieldOfViewTarget = false;

        _loseTargetTimer = 0f;

        ResetAttackProgressBar();

        SetAgentMovement(true, true);

        SetState(EnemyState.Roaming);

        StopBehaviourRoutine();

        _behaviourRoutine =

            StartCoroutine(PatrolRoutine());

    }
    // =====================================================================
    // LEURRE / DISTRACTION
    // =====================================================================

    public void InvestigateDecoy(Vector3 decoyPosition, float waitDuration)
    {
        // Un ennemi qui poursuit déjà le joueur ignore le leurre.
        if (_state == EnemyState.Chasing)
            return;

        StopBehaviourRoutine();

        if (_decoyRoutine != null)
        {
            StopCoroutine(_decoyRoutine);
            _decoyRoutine = null;
        }

        _decoyRoutine = StartCoroutine(
            InvestigateDecoyRoutine(decoyPosition, waitDuration)
        );
    }
    private IEnumerator InvestigateDecoyRoutine(
        Vector3 decoyPosition,
        float waitDuration)
    {
        _isInvestigatingDecoy = true;

        ResetAttackProgressBar();
        SetAgentMovement(true, true);

        if (!NavMesh.SamplePosition(
                decoyPosition,
                out NavMeshHit hit,
                5f,
                NavMesh.AllAreas))
        {
            _isInvestigatingDecoy = false;
            _decoyRoutine = null;
            ReturnToPatrol();
            yield break;
        }
        if (!_agent.isOnNavMesh)
        {
            _isInvestigatingDecoy = false;
            _decoyRoutine = null;
            yield break;
        }
        _agent.ResetPath();
        if (!_agent.SetDestination(hit.position))
        {
            _isInvestigatingDecoy = false;
            _decoyRoutine = null;
            ReturnToPatrol();
            yield break;
        }
        // Aller jusqu'au leurre.
        while (_isInvestigatingDecoy)
        {
            if (!_agent.pathPending)
            {
                if (_agent.pathStatus == NavMeshPathStatus.PathInvalid)
                {
                    _isInvestigatingDecoy = false;
                    _decoyRoutine = null;
                    ReturnToPatrol();
                    yield break;
                }
                if (_agent.remainingDistance <=
                    _agent.stoppingDistance + 0.15f)
                {
                    break;
                }
            }
            yield return null;
        }
        // Rester sur place pendant la durée configurée.
        SetAgentMovement(false, true);
        float timer = 0f;
        while (timer < waitDuration)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        _isInvestigatingDecoy = false;
        _decoyRoutine = null;
        // _currentPatrolIndex est conservé : reprise du roaming normal.
        ReturnToPatrol();
    }
    // =====================================================================
    // DÉTECTION / CHASE
    // =====================================================================
    public void OnAlerted(
        Vector3 lastKnownPlayerPosition)
    {
        if (_state == EnemyState.Chasing)
            return;
        _isOriginalDetector = false;
        Transform target = _detection.Player != null
            ? _detection.Player
            : _detection.Ally;
        BeginChase(target, lastKnownPlayerPosition);
    }
    private void BeginChase(
        Transform target,
        Vector3 playerPosition)
    {
        _chaseTarget = target;
        _fieldOfViewTarget = null;
        _isFieldOfViewChase = false;
        _hasSeenFieldOfViewTarget = false;
        StopBehaviourRoutine();
        ResetAttackProgressBar();
        _lastKnownPlayerPosition =
            playerPosition;
        _loseTargetTimer = 0f;
        SetState(EnemyState.Chasing);
        SetAgentMovement(true, true);
        if (_agent.isOnNavMesh)
        {
            _agent.ResetPath();
            _agent.SetDestination(
                playerPosition
            );
        }
        RotateTowardsPositionInstant(
            playerPosition
        );
    }
    public void OnSpottedBy(Transform observer)
    {
        if (observer == null)
            return;

        if (_fieldOfViewTarget == observer && _state == EnemyState.Chasing)
            return;

        _fieldOfViewTarget = observer;
        _chaseTarget = observer;
        _isFieldOfViewChase = true;
        _hasSeenFieldOfViewTarget = false;
        StopBehaviourRoutine();
        ResetAttackProgressBar();
        _loseTargetTimer = 0f;
        SetState(EnemyState.Chasing);
        SetAgentMovement(true, true);

        if (_agent.isOnNavMesh)
        {
            _agent.ResetPath();
            _agent.SetDestination(observer.position);
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
        if (_isFieldOfViewChase)
        {
            if (_fieldOfViewTarget == null)
            {
                ReturnToPatrol();
                return;
            }

            if (_detection.CanSeeTarget(
                    _fieldOfViewTarget,
                    out Vector3 targetPosition))
            {
                _hasSeenFieldOfViewTarget = true;
                if (_agent.isOnNavMesh)
                    _agent.SetDestination(targetPosition);
            }
            else if (_hasSeenFieldOfViewTarget)
            {
                ReturnToPatrol();
            }
            else if (_agent.isOnNavMesh)
            {
                _agent.SetDestination(_fieldOfViewTarget.position);
            }
            return;
        }
        Transform target = _chaseTarget;
        if (target == null)
        {
            target = _detection.Player != null
                ? _detection.Player
                : _detection.Ally;
            _chaseTarget = target;
        }
        if (target == null)
        {
            ResetAttackProgressBar();
            _loseTargetTimer += Time.deltaTime;
            if (_loseTargetTimer >= loseTargetTime)
                ReturnToPatrol();
            return;
        }
        if (isPreparingAttack)
        {
            TickAttackPreparation();
            return;
        }
        if (_detection.CanSeeTarget(target, out Vector3 currentPos))
        {
            HandlePlayerDetectedWhileChasing(
                target,
                currentPos
            );
            return;
        }
        if (IsTargetInDetectionRadius(
            target,
            out Vector3 nearbyPlayerPos))
        {
            HandlePlayerDetectedWhileChasing(
                target,
                nearbyPlayerPos
            );
            return;
        }
        bool playerHidden =
            target == _detection.Player &&
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
        if (distance <= attackRange &&
            attackCooldownTimer <= 0f)
        {
            BeginAttackPreparation(player);
            return;
        }
        SetAgentMovement(true, true);
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
        SetAgentMovement(false, false);
        if (attackProgressBar != null)
        {
          attackProgressBar.gameObject.SetActive(
                true
            );
        }
        RotateTowardsPlayer(player);
    }
    private void TickAttackPreparation()
    {
        if (attackTarget == null)
        {
            CancelAttackPreparation();
            return;
        }
        SetAgentMovement(false, false);
        RotateTowardsPlayer(
            attackTarget
        );
        float distance =
            Vector3.Distance(
                transform.position,
                attackTarget.position
            );
        if (distance >
            attackRange + attackMoveTolerance)
        {
            CancelAttackPreparation();
            SetAgentMovement(true, true);
            return;
        }

        attackTimer += Time.deltaTime;

        UpdateAttackProgressBar();

        if (attackTimer >= attackAnimDuration)
        {
            DealDamageToPlayer();

            attackCooldownTimer = attackCooldown;
                
            ResetAttackProgressBar();

            SetAgentMovement(true, true);

            if (_chaseTarget != null)
            {
                _agent.SetDestination(_chaseTarget.position);
            }
        }
    }
    private void CancelAttackPreparation()
    {
        ResetAttackProgressBar();
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
    private void DealDamageToPlayer()
    {
        Transform target =
            attackTarget != null
                ? attackTarget
                : _chaseTarget;
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
            PlayerHealth modernPlayerHealth =
                target.GetComponentInParent<PlayerHealth>();
            if (modernPlayerHealth != null)
            {
                modernPlayerHealth.TakeDamage(attackDamage);
                Debug.Log(
                    "Enemy attacks player : -" +
                    attackDamage
                );
                return;
            }

            Debug.LogWarning(
                "Aucun composant de santé trouvé sur la cible de l'ennemi."
            );
        }
    }
    // =====================================================================
    // BARRE D'ATTAQUE
    // =====================================================================
    private void ConfigureAttackProgressBar()
    {
        if (attackProgressBar == null)
            return;
        attackProgressBar.minValue = 0f;
        attackProgressBar.maxValue = 1f;
        ResetAttackProgressBar();
    }
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
    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Player") && _body != null)
            _body.isKinematic = true;

    }

    private void OnCollisionExit(Collision other)
    {
        if (other.gameObject.CompareTag("Player") && _body != null)
            _body.isKinematic = false;
    }
}