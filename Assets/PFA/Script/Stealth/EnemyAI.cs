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

    [SerializeField]
    private Color chasingLightColor =

        new Color(1f, 0.25f, 0.25f);

    [SerializeField]
    private Color roamingLightColor =

        new Color(1f, 0.2f, 0.2f);

    [SerializeField] private float lightIntensity = 3f;

    // =====================================================================
    // VARIABLES INTERNES
    // =====================================================================
    private NavMeshAgent _agent;

    private Rigidbody _body;

    private EnemyDetection _detection;

    private EnemyState _state = EnemyState.Roaming;

    private bool _isOriginalDetector;

    private Vector3 _lastKnownPlayerPosition;

    private Coroutine _behaviourRoutine;

    // =====================================================================
    // LEURRE / DISTRACTION
    // =====================================================================

    private Coroutine _decoyRoutine;
    private bool _isInvestigatingDecoy;

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

        UnsubscribeFromPlayer();

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
        // La coroutine du leurre contrôle le déplacement pendant l'investigation.
        if (_isInvestigatingDecoy)
            return;

        if (_detection.CanSeePlayer(out Vector3 playerPos))

        {

            OnPlayerDetected(playerPos);

            return;

        }

        if (!IsPlayerInDetectionRadius(out Vector3 nearbyPlayerPos))

            return;

        RotateTowardsPosition(nearbyPlayerPos, detectionRotationSpeed);

        OnPlayerDetected(nearbyPlayerPos);

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

        _loseTargetTimer = 0f;

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

        UnsubscribeFromPlayer();

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
            // Le joueur reste prioritaire s'il est réellement vu.
            if (_detection.CanSeePlayer(out Vector3 playerPos))
            {
                _isInvestigatingDecoy = false;
                _decoyRoutine = null;
                OnPlayerDetected(playerPos);
                yield break;
            }

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
            if (_detection.CanSeePlayer(out Vector3 playerPos))
            {
                _isInvestigatingDecoy = false;
                _decoyRoutine = null;
                SetAgentMovement(true, true);
                OnPlayerDetected(playerPos);
                yield break;
            }

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

        if (isPreparingAttack)

        {

            TickAttackPreparation();

            return;

        }

        if (_detection.CanSeePlayer(

            out Vector3 currentPos))

        {

            HandlePlayerDetectedWhileChasing(

                player,

                currentPos

            );

            return;

        }

        if (IsPlayerInDetectionRadius(

            out Vector3 nearbyPlayerPos))

        {

            HandlePlayerDetectedWhileChasing(

                player,

                nearbyPlayerPos

            );

            return;

        }

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

            PlayAttackAnimation();

            DealDamageToPlayer();

            attackCooldownTimer =

                attackCooldown;

            ResetAttackProgressBar();

            SetAgentMovement(true, true);

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

        ResetAttackProgressBar();

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
