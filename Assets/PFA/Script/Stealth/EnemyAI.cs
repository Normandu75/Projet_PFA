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
    // CLone
    // =====================================================================
    [Header("Clone")]
    private Transform _cloneTarget;
    private Vector3 _cloneLastPosition;
    private bool _isDistractedByClone;
    [SerializeField] private float cloneStoppingDistance = 1.5f;
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
    private Transform _activeDecoy;
    private float _decoyStoppingDistance = 1.5f;
    private float _defaultStoppingDistance;
    private bool HasActiveDecoy =>
    _isInvestigatingDecoy && _activeDecoy != null;

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
    // BARRE DE DÉTECTION
    // =====================================================================
    [Header("Barre de détection")]
    [SerializeField] private Slider detectionProgressBar;
    [Header("Vitesses de détection")]
    [SerializeField, Min(0.01f)]
    private float flashlightDetectionTime = 3f;
    [SerializeField, Min(0.01f)]
    private float visionDetectionTime = 1.5f;
    [SerializeField, Min(0.01f)]
    private float detectionDecayTime = 2f;
    private float detectionProgress;
    private readonly HashSet<Transform> pendingObservers =
    new HashSet<Transform>();

    // LUMIÈRE 
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
        _defaultStoppingDistance = _agent.stoppingDistance;
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
        if(detectionProgressBar != null)
        {
            detectionProgressBar.minValue = 0f;
            detectionProgressBar.maxValue = 1f;
            ResetDetectionProgress();
        }
        ConfigureAttackProgressBar();
        attackCooldownTimer = 0f;
        UpdateDetectionLight(_state);
        StartPatrol();
    }
    private void Update()
    {
        attackCooldownTimer = Mathf.Max(
            0f,
            attackCooldownTimer - Time.deltaTime
        );
        currentStateReadOnly = _state;
        // Le leurre est prioritaire sur tous les comportements.
        if (HasActiveDecoy)
            return;
        HandleDebugToggles();
        if (DecoyClone.IsActive)
        {
            if (!_isDistractedByClone)
                CheckForClone();
        }
        else if (_isDistractedByClone)
        {
            EndCloneDistraction();
        }
        UpdateDetectionProgress();
        switch (_state)
        {
            case EnemyState.Chasing:
                TickChasing();
                break;
        }
    }
    private void LateUpdate()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return;
        FaceBarToCamera(attackProgressBar, mainCamera);
        FaceBarToCamera(detectionProgressBar, mainCamera);
    }
    private void FaceBarToCamera(Slider bar, Camera cam)
    {
        if (bar == null || !bar.gameObject.activeSelf)
            return;
        Transform canvas = bar.transform.parent;
        if (canvas == null)
            return;
        canvas.LookAt(
            canvas.position + cam.transform.forward,
            cam.transform.up
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
            Transform chaseTarget = _detection.Player;
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
        target = null;
        targetPosition = Vector3.zero;
        // Pendant le clone, aucune détection
        // du joueur ou de l'allié.
        if (DecoyClone.IsActive)
            return false;
        Transform player = _detection.Player;
        bool playerHidden =
            PlayerStealth.Instance != null &&
            PlayerStealth.Instance.IsHidden;
        if (player != null && !playerHidden)
        {
            if (_detection.CanSeePlayer(out targetPosition) ||
                IsTargetInDetectionRadius(player, out targetPosition))
            {
                target = player;
                return true;
            }
        }
        return false;
    }
    
    // =====================================================================
    // PATROUILLE
    // =====================================================================
    private void StartPatrol()
    {
        ResetDetectionProgress();
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
        if (HasActiveDecoy)
            return;
        _chaseTarget = null;
        _loseTargetTimer = 0f;
        ResetAttackProgressBar();
        ResetDetectionProgress();
        SetAgentMovement(true, true);
        SetState(EnemyState.Roaming);
        StopBehaviourRoutine();
        _behaviourRoutine =
            StartCoroutine(PatrolRoutine());
    }

    // =====================================================================
    // LEURRE / DISTRACTION
    // =====================================================================
   public void InvestigateDecoy(Transform decoy)
    {
        if (decoy == null || HasActiveDecoy)
            return;
        if (!_agent.isOnNavMesh)
            return;
        StopBehaviourRoutine();
        if (_decoyRoutine != null)
        {
            StopCoroutine(_decoyRoutine);
            _decoyRoutine = null;
        }
        // La grenade interrompt toute attaque.
        ResetAttackProgressBar();
        // La grenade est prioritaire sur le clone.
        _isDistractedByClone = false;
        _cloneTarget = null;
        // Annule la poursuite du joueur.
        _chaseTarget = null;
        _activeDecoy = decoy;
        _isInvestigatingDecoy = true;
        // Arrête l'ancien déplacement.
        _agent.isStopped = false;
        _agent.updateRotation = true;
        _agent.ResetPath();
        _decoyRoutine = StartCoroutine(
            InvestigateDecoyRoutine()
        );
    }
    private IEnumerator InvestigateDecoyRoutine()
    {
        while (HasActiveDecoy)
        {
            if (!_agent.isOnNavMesh)
            {
                yield return null;
                continue;
            }
            // Recherche la position actuelle de la grenade.
            if (NavMesh.SamplePosition(
                _activeDecoy.position,
                out NavMeshHit hit,
                5f,
                NavMesh.AllAreas))
            {
                float distance = Vector3.Distance(
                    transform.position,
                    hit.position
                );
                if (distance > _decoyStoppingDistance)
                {
                    // Suit la grenade pendant qu'elle roule.
                    _agent.isStopped = false;
                    _agent.updateRotation = true;
                    _agent.SetDestination(hit.position);
                }
                else
                {
                    // Arrivé à proximité : regarde la grenade.
                    _agent.isStopped = true;
                    RotateTowardsPosition(
                        _activeDecoy.position,
                        detectionRotationSpeed
                    );
                }
            }
            yield return null;
        }
        _decoyRoutine = null;
    }
    public void StopInvestigatingDecoy(Transform decoy)
    {
        if (!_isInvestigatingDecoy || _activeDecoy != decoy)
            return;
        _isInvestigatingDecoy = false;
        _activeDecoy = null;
        if (_decoyRoutine != null)
        {
            StopCoroutine(_decoyRoutine);
            _decoyRoutine = null;
        }
        if (_agent.isOnNavMesh)
        {
            _agent.isStopped = false;
            _agent.ResetPath();
        }
        _agent.stoppingDistance = _defaultStoppingDistance;
        ReturnToPatrol();
    }

    // =====================================================================
    // DÉTECTION / CHASE
    // =====================================================================
    public void OnAlerted(
        Vector3 lastKnownPlayerPosition)
    {
        if (HasActiveDecoy)
            return;
        if (DecoyClone.IsActive)
            return;
        if (_state == EnemyState.Chasing)
            return;
        _isOriginalDetector = false;
        Transform target = _detection.Player;
        BeginChase(target, lastKnownPlayerPosition);
    }
    private void BeginChase(Transform target, Vector3 playerPosition)
    {
        if (HasActiveDecoy || DecoyClone.IsActive)
            return;
        // Une poursuite normale ne cible que le joueur.
        Transform player = _detection.Player;
        if (player == null)
            return;
        _chaseTarget = player;
        StopBehaviourRoutine();
        ResetDetectionProgress();
        ResetAttackProgressBar();
        _lastKnownPlayerPosition = playerPosition;
        _loseTargetTimer = 0f;
        _agent.stoppingDistance = _defaultStoppingDistance;
        SetState(EnemyState.Chasing);
        if (_agent.isOnNavMesh)
        {
            SetAgentMovement(true, true);
            _agent.ResetPath();
            _agent.SetDestination(player.position);
        }

        RotateTowardsPositionInstant(player.position);
    }
    public void OnSpottedBy(Transform observer)
    {
        if (HasActiveDecoy || DecoyClone.IsActive)
            return;

        if (_state == EnemyState.Chasing)
            return;

        // L'observateur (lampe/allié) n'est jamais la cible.
        Transform player = _detection.Player;
        if (player == null)
            return;

        _isOriginalDetector = true;
        BeginChase(player, player.position);
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
        if (HasActiveDecoy)
            return;

        if (_isDistractedByClone)
        {
            TickCloneDistraction();
            return;
        }
        // On ignore _chaseTarget si un ancien script y a placé l'Allié.
        Transform player = _detection.Player;
        _chaseTarget = player;
        if (player == null)
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
        bool playerHidden =
            PlayerStealth.Instance != null &&
            PlayerStealth.Instance.IsHidden;

        bool canSeePlayer = !playerHidden &&
            _detection.CanSeePlayer(out _);

        bool playerNearby = !playerHidden &&
            IsTargetInDetectionRadius(player, out _);

        bool playerDetected = canSeePlayer || playerNearby;

        if (playerDetected)
        {
            _lastKnownPlayerPosition = player.position;
            _loseTargetTimer = 0f;
        }
        else
        {
            _loseTargetTimer += Time.deltaTime;
            if (_loseTargetTimer >= loseTargetTime)
            {
                ReturnToPatrol();
                return;
            }
        }

        float distance = Vector3.Distance(
            transform.position, player.position);

        if (playerDetected &&
            distance <= attackRange &&
            attackCooldownTimer <= 0f)
        {
            BeginAttackPreparation(player);
            return;
        }

        ResetAttackProgressBar();

        if (!_agent.isOnNavMesh)
            return;

        SetAgentMovement(true, true);

        // Recalcul chaque frame : position actuelle si visible,
        // sinon dernière position connue pendant loseTargetTime.
        Vector3 destination = playerDetected
            ? player.position
            : _lastKnownPlayerPosition;

        _agent.SetDestination(destination);
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

    private void UpdateDetectionLight(EnemyState state)

    {

        if (detectionLight == null)

            return;

        // Lumière uniquement pendant la poursuite.

        bool isChasing = state == EnemyState.Chasing;

        detectionLight.enabled = isChasing;

        if (isChasing)

        {

            detectionLight.color = chasingLightColor;

            detectionLight.intensity = lightIntensity;

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

    // =====================================================================

    // CLONE

    // =====================================================================

    private void CheckForClone()

    {

        if (!DecoyClone.IsActive || _isDistractedByClone)

            return;

        Transform clone = DecoyClone.Target;

        if (clone == null)

            return;

        bool canSeeClone = _detection.CanSeeTarget(

            clone,

            out Vector3 clonePosition

        );

        bool isNearby = IsTargetInDetectionRadius(

            clone,

            out Vector3 nearbyPosition

        );

        if (!canSeeClone && !isNearby)

            return;

        OnCloneDetected(clone);

    }    

    private void OnCloneDetected(Transform clone)

    {

        if (HasActiveDecoy)

            return;

        if (clone == null)

            return;

        StopBehaviourRoutine();

        ResetAttackProgressBar();

        if (_decoyRoutine != null)

        {

            StopCoroutine(_decoyRoutine);

            _decoyRoutine = null;

        }

        _isInvestigatingDecoy = false;

        _cloneTarget = clone;

        _isDistractedByClone = true;

        SetState(EnemyState.Chasing);

        SetAgentMovement(true, true);

        if (_agent.isOnNavMesh &&

            NavMesh.SamplePosition(

                clone.position,

                out NavMeshHit hit,

                3f,

                NavMesh.AllAreas))

        {

            _agent.ResetPath();

            _agent.SetDestination(hit.position);

        }

    }

   private void TickCloneDistraction()

    {

        if (_cloneTarget == null)

        {

            EndCloneDistraction();

            return;

        }

        if (!_agent.isOnNavMesh)

            return;

        Vector3 clonePosition = _cloneTarget.position;

        if (!NavMesh.SamplePosition(

            clonePosition,

            out NavMeshHit hit,

            3f,

            NavMesh.AllAreas))

            return;

        _agent.stoppingDistance = cloneStoppingDistance;

        if (!_agent.pathPending &&

            _agent.hasPath &&

            _agent.remainingDistance <=

            _agent.stoppingDistance + 0.15f)

        {

            SetAgentMovement(false, false);

            _agent.ResetPath();

            RotateTowardsPosition(

                clonePosition,

                detectionRotationSpeed

            );

            return;

        }

        SetAgentMovement(true, true);

        _agent.SetDestination(hit.position);

    }

    private void EndCloneDistraction()

    {

        if (HasActiveDecoy)

            return;

        _cloneTarget = null;

        _isDistractedByClone = false;

        ResetAttackProgressBar();

        ReturnToPatrol();

    }

    public void SetObservedBy(Transform observer, bool visible)

    {

        if (observer == null)

            return;

        if (visible)

            pendingObservers.Add(observer);

        else

            pendingObservers.Remove(observer);

    }

    private void UpdateDetectionProgress()
    {
        if (_state != EnemyState.Roaming ||
            DecoyClone.IsActive ||
            debugForceRoaming)
        {
            ResetDetectionProgress();
            return;
        }

        pendingObservers.RemoveWhere(observer => observer == null);

        Transform player = _detection.Player;
        bool playerHidden =
            PlayerStealth.Instance != null &&
            PlayerStealth.Instance.IsHidden;

        bool detectedByVision = false;
        if (player != null && !playerHidden)
        {
            detectedByVision =
                _detection.CanSeePlayer(out _) ||
                IsTargetInDetectionRadius(player, out _);
        }

        // S_Field_Of_View signale un éclairage, pas une cible.
        bool detectedByFlashlight = pendingObservers.Count > 0;

        float speed = 0f;
        if (detectedByVision)
            speed = 1f / Mathf.Max(0.01f, visionDetectionTime);
        if (detectedByFlashlight)
            speed = Mathf.Max(speed,
                1f / Mathf.Max(0.01f, flashlightDetectionTime));

        if (speed <= 0f)
            speed = -1f / Mathf.Max(0.01f, detectionDecayTime);

        detectionProgress = Mathf.Clamp01(
            detectionProgress + speed * Time.deltaTime);

        UpdateDetectionBar();

        if (detectionProgress < 1f || player == null)
            return;

        _isOriginalDetector = true;
        BeginChase(player, player.position);
    }

    private void UpdateDetectionBar()

    {

        if (detectionProgressBar == null)

            return;

        detectionProgressBar.gameObject.SetActive(

            detectionProgress > 0f

        );

        detectionProgressBar.value = detectionProgress;

    }

    private void ResetDetectionProgress()

    {

        detectionProgress = 0f;

        if (detectionProgressBar != null)

        {

            detectionProgressBar.value = 0f;

            detectionProgressBar.gameObject.SetActive(false);

        }

    }

}