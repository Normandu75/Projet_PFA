using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Machine à états de l'ennemi : patrouille, détection, poursuite, alerte des
/// autres ennemis, surveillance des casiers, casse de casier.
/// Aucun élément visuel de gameplay lié à l'IA (pas de cône affiché, pas d'UI) :
/// seul le modèle de l'ennemi est visible. Le cône de détection reste dessiné
/// en Gizmo dans l'éditeur (EnemyDetection.OnDrawGizmosSelected), jamais en jeu.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyDetection))]
public class EnemyAI : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject alertBeamPrefab;
    [SerializeField] private Transform alertLaunchPoint;

    [Header("Patrouille")]
    [SerializeField] private List<PatrolWaypoint> waypoints;

    [Header("Poursuite / Attaque")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackAnimDuration = 1f;

    [Tooltip("Temps (secondes) sans revoir le joueur dans le cône avant que " +
             "l'ennemi arrête de le détecter et retourne en patrouille.")]
    [SerializeField] private float loseTargetTime = 3f;

    [Header("Alerte")]
    [SerializeField] private float alertBeamSpeed = 15f;
    [SerializeField] private Color alertBeamColor = Color.red;

    [Header("Surveillance")]
    [SerializeField] private float surveillanceRadius = 8f;
    [SerializeField] private float lockerCheckDuration = 1.2f;
    [SerializeField] private LayerMask lockerLayer;

    [Header("Debug — forcer un état en Play Mode")]
    [Tooltip("Activer un toggle force l'état correspondant et désactive les autres.")]
    [SerializeField] private bool debugForceRoaming;
    [SerializeField] private bool debugForceChasing;
    [SerializeField] private bool debugForceSurveillance;

    [Header("Lecture seule")]
    [SerializeField] private EnemyState currentStateReadOnly;

    private NavMeshAgent _agent;
    private EnemyDetection _detection;

    private EnemyState _state = EnemyState.Roaming;
    private bool _isOriginalDetector;
    private Vector3 _lastKnownPlayerPosition;
    private Coroutine _behaviourRoutine;
    private float _loseTargetTimer;

    private bool _prevDebugRoaming, _prevDebugChasing, _prevDebugSurveillance;

    private static readonly int AttackTrigger = Animator.StringToHash("Attack");
    private static readonly int WalkSpeedParam = Animator.StringToHash("Speed");

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _detection = GetComponent<EnemyDetection>();

        if (alertLaunchPoint == null) alertLaunchPoint = transform;
    }

    private void OnEnable()
    {
        EnemyManager.Instance?.Register(this);
    }

    private void OnDisable()
    {
        EnemyManager.Instance?.Unregister(this);
        UnsubscribeFromPlayer();
    }

    private void Start()
    {
        StartPatrol();
    }

    private void Update()
    {
        HandleDebugToggles();

        if (animator != null)
            animator.SetFloat(WalkSpeedParam, _agent.velocity.magnitude);

        currentStateReadOnly = _state;

        switch (_state)
        {
            case EnemyState.Roaming:
                TickRoaming();
                break;

            case EnemyState.Chasing:
                TickChasing();
                break;

            // Surveillance et BreakingLocker sont pilotés entièrement par coroutine.
        }
    }

    // ------------------------------------------------------------------
    // DEBUG — TOGGLES D'ÉTAT
    // ------------------------------------------------------------------

    private void HandleDebugToggles()
    {
        if (debugForceRoaming && !_prevDebugRoaming)
        {
            debugForceChasing = false;
            debugForceSurveillance = false;
            ReturnToPatrol();
        }
        else if (debugForceChasing && !_prevDebugChasing)
        {
            debugForceRoaming = false;
            debugForceSurveillance = false;

            Vector3 target = _detection.Player != null ? _detection.Player.position : transform.position;
            _isOriginalDetector = true;
            BeginChase(target);
        }
        else if (debugForceSurveillance && !_prevDebugSurveillance)
        {
            debugForceRoaming = false;
            debugForceChasing = false;

            if (_behaviourRoutine != null) StopCoroutine(_behaviourRoutine);
            _behaviourRoutine = StartCoroutine(SurveillanceRoutine(transform.position));
        }

        _prevDebugRoaming = debugForceRoaming;
        _prevDebugChasing = debugForceChasing;
        _prevDebugSurveillance = debugForceSurveillance;
    }

    // ------------------------------------------------------------------
    // ROAMING / PATROUILLE
    // ------------------------------------------------------------------

    private void TickRoaming()
    {
        if (_detection.CanSeePlayer(out Vector3 playerPos))
        {
            OnPlayerDetected(playerPos);
        }
    }

    private void StartPatrol()
    {
        SetState(EnemyState.Roaming);

        if (_behaviourRoutine != null) StopCoroutine(_behaviourRoutine);
        _behaviourRoutine = StartCoroutine(PatrolRoutine());
    }

    private IEnumerator PatrolRoutine()
    {
        if (waypoints == null || waypoints.Count == 0)
            yield break;

        int index = 0;
        while (_state == EnemyState.Roaming)
        {
            PatrolWaypoint wp = waypoints[index];

            _agent.SetDestination(wp.transform.position);

            while (_state == EnemyState.Roaming &&
                   (_agent.pathPending || _agent.remainingDistance > _agent.stoppingDistance))
            {
                yield return null;
            }

            if (_state != EnemyState.Roaming) yield break;

            yield return new WaitForSeconds(wp.waitTime);

            index = (index + 1) % waypoints.Count;
        }
    }

    private void ReturnToPatrol()
    {
        UnsubscribeFromPlayer();
        _loseTargetTimer = 0f;
        StartPatrol();
    }

    // ------------------------------------------------------------------
    // DÉTECTION -> POURSUITE -> ALERTE
    // ------------------------------------------------------------------

    private void OnPlayerDetected(Vector3 playerPosition)
    {
        if (_state == EnemyState.Chasing) return;

        _isOriginalDetector = true;
        BeginChase(playerPosition);
    }

    /// <summary>Appelé par le rayon d'alerte d'un autre ennemi.</summary>
    public void OnAlerted(Vector3 lastKnownPlayerPosition)
    {
        if (_state == EnemyState.Chasing) return;

        _isOriginalDetector = false;
        BeginChase(lastKnownPlayerPosition);
    }

    private void BeginChase(Vector3 playerPosition)
    {
        if (_behaviourRoutine != null) StopCoroutine(_behaviourRoutine);

        _lastKnownPlayerPosition = playerPosition;
        _loseTargetTimer = 0f;
        SetState(EnemyState.Chasing);
        _agent.isStopped = false;
        _agent.SetDestination(playerPosition);

        SubscribeToPlayer();

        // Seul l'ennemi qui a détecté le joueur en premier alerte les autres.
        if (_isOriginalDetector)
        {
            BroadcastAlert();
        }
    }

    private void TickChasing()
    {
        if (_detection.Player == null) return;

        if (_detection.CanSeePlayer(out Vector3 currentPos))
        {
            _loseTargetTimer = 0f;
            _lastKnownPlayerPosition = currentPos;
            _agent.SetDestination(_lastKnownPlayerPosition);

            float distance = Vector3.Distance(transform.position, currentPos);
            if (distance <= attackRange)
            {
                PlayAttackAnimation();
            }
            return;
        }

        bool playerHidden = PlayerStealth.Instance != null && PlayerStealth.Instance.IsHidden;
        if (playerHidden)
        {
            // Cas géré séparément par HandlePlayerHidden (casier) : on ne décompte
            // pas le timer ici, la disparition est due à la cachette, pas à la distance.
            return;
        }

        // Le joueur n'est plus dans le cône (et n'est pas caché) : décompte avant abandon.
        _loseTargetTimer += Time.deltaTime;
        if (_loseTargetTimer >= loseTargetTime)
        {
            ReturnToPatrol();
        }
    }

    private void PlayAttackAnimation()
    {
        if (animator != null)
            animator.SetTrigger(AttackTrigger);
    }

    private void BroadcastAlert()
    {
        if (EnemyManager.Instance == null || alertBeamPrefab == null) return;

        foreach (EnemyAI other in EnemyManager.Instance.GetOtherEnemies(this))
        {
            GameObject beamObj = Instantiate(alertBeamPrefab, alertLaunchPoint.position, Quaternion.identity);

            var renderer = beamObj.GetComponentInChildren<Renderer>();
            if (renderer != null) renderer.material.color = alertBeamColor;

            var beam = beamObj.GetComponent<EnemyAlertBeam>();
            Vector3 alertPosition = _lastKnownPlayerPosition;
            beam.Launch(other.transform, alertBeamSpeed, () => other.OnAlerted(alertPosition));
        }
    }

    // ------------------------------------------------------------------
    // GESTION DU JOUEUR QUI SE CACHE
    // ------------------------------------------------------------------

    private void SubscribeToPlayer()
    {
        if (PlayerStealth.Instance != null)
            PlayerStealth.Instance.OnPlayerHidden += HandlePlayerHidden;
    }

    private void UnsubscribeFromPlayer()
    {
        if (PlayerStealth.Instance != null)
            PlayerStealth.Instance.OnPlayerHidden -= HandlePlayerHidden;
    }

    private void HandlePlayerHidden(HidingLocker locker)
    {
        if (_state != EnemyState.Chasing) return;

        bool lockerVisible = _detection.IsPointInCone(locker.Position) && _detection.HasLineOfSight(locker.Position);

        if (lockerVisible)
        {
            if (_behaviourRoutine != null) StopCoroutine(_behaviourRoutine);
            _behaviourRoutine = StartCoroutine(BreakLockerRoutine(locker));
        }
        else if (_isOriginalDetector)
        {
            if (_behaviourRoutine != null) StopCoroutine(_behaviourRoutine);
            _behaviourRoutine = StartCoroutine(SurveillanceRoutine(_lastKnownPlayerPosition));
        }
        else
        {
            ReturnToPatrol();
        }
    }

    // ------------------------------------------------------------------
    // CASSE DE CASIER
    // ------------------------------------------------------------------

    private IEnumerator BreakLockerRoutine(HidingLocker locker)
    {
        SetState(EnemyState.BreakingLocker);
        _agent.isStopped = true;

        Vector3 lookDir = locker.Position - transform.position;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir);
            float t = 0f;
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, t / 0.3f);
                yield return null;
            }
        }

        PlayAttackAnimation();
        yield return new WaitForSeconds(attackAnimDuration);

        locker.Break();

        yield return new WaitForSeconds(1f);
        _agent.isStopped = false;
        ReturnToPatrol();
    }

    // ------------------------------------------------------------------
    // SURVEILLANCE
    // ------------------------------------------------------------------

    private IEnumerator SurveillanceRoutine(Vector3 zoneCenter)
    {
        SetState(EnemyState.Surveillance);
        _agent.isStopped = false;

        List<HidingLocker> lockers = FindNearbyLockers(zoneCenter);

        foreach (HidingLocker locker in lockers)
        {
            if (locker == null || locker.IsBroken) continue;

            _agent.SetDestination(locker.Position);
            while (_agent.pathPending || _agent.remainingDistance > _agent.stoppingDistance + 0.1f)
                yield return null;

            yield return new WaitForSeconds(lockerCheckDuration);

            if (locker.IsOccupied)
            {
                yield return StartCoroutine(BreakLockerRoutine(locker));
                yield break;
            }
        }

        ReturnToPatrol();
    }

    private List<HidingLocker> FindNearbyLockers(Vector3 center)
    {
        Collider[] hits = Physics.OverlapSphere(center, surveillanceRadius, lockerLayer);
        return hits
            .Select(h => h.GetComponentInParent<HidingLocker>())
            .Where(l => l != null)
            .OrderBy(l => Vector3.Distance(transform.position, l.Position))
            .Distinct()
            .ToList();
    }

    // ------------------------------------------------------------------
    // UTILITAIRES
    // ------------------------------------------------------------------

    private void SetState(EnemyState newState)
    {
        _state = newState;
    }
}
