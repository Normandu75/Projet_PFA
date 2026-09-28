using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

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
    private Camera mainCamera;


    private float attackTimer = 0f;

    [Header("Poursuite / Attaque")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackAnimDuration = 1f;
    
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackCooldown = 1f;

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
    [SerializeField] private Light detectionLight;
    [Header("Barre d'attaque")]
    [SerializeField] private Slider attackProgressBar;
    private bool isPreparingAttack = false;
    [Header("Lumière de vision")]
    [SerializeField] private Color chasingLightColor = new Color(1f, 0.25f, 0.25f);
    [SerializeField] private Color surveillanceLightColor = Color.yellow;

    [SerializeField] private Color roamingLightColor = new Color(1f, 0.2f, 0.2f);
    [SerializeField] private float lightIntensity = 3f;

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

        mainCamera = Camera.main;
        if (attackProgressBar != null)
        {
            attackProgressBar.minValue = 0f;
            attackProgressBar.maxValue = 1f;
            attackProgressBar.value = 0f;
            attackProgressBar.gameObject.SetActive(false);
        }
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
    private void LateUpdate()
    {
        if (attackProgressBar == null)
            return; 

        if (!attackProgressBar.gameObject.activeSelf)
            return; 

        if (mainCamera == null)
            return; 

        Transform canvas = attackProgressBar.transform.parent;  

        canvas.LookAt(
            canvas.position + mainCamera.transform.forward,
            mainCamera.transform.up
        );
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
        if (_detection.Player == null)
        {
            ResetAttackProgressBar();
            return;
        }

        if (_detection.CanSeePlayer(out Vector3 currentPos))
        {
            _loseTargetTimer = 0f;
            _lastKnownPlayerPosition = currentPos;

            float distance = Vector3.Distance(transform.position, currentPos);

            if (distance <= attackRange)
            {
                _agent.isStopped = true;

                // Commence la préparation de l'attaque
                isPreparingAttack = true;

                if (attackProgressBar != null)
                {
                    attackProgressBar.gameObject.SetActive(true);
                }

                attackTimer += Time.deltaTime;

                UpdateAttackProgressBar();

                // Attaque lorsque la barre est pleine
                if (attackTimer >= attackAnimDuration)
                {
                    PlayAttackAnimation();
                    DealDamageToPlayer();

                    ResetAttackProgressBar();
                }
            }
            else
            {
                _agent.isStopped = false;

                ResetAttackProgressBar();

                _agent.SetDestination(currentPos);
            }

            return;
        }

        bool playerHidden = PlayerStealth.Instance != null &&
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

    private void PlayAttackAnimation()
    {
        if (animator != null)
            animator.SetTrigger(AttackTrigger);
    }
    private void DealDamageToPlayer()
    {
        if (_detection.Player == null)
            return;

        S_HealthBar playerHealth =
            _detection.Player.GetComponentInParent<S_HealthBar>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage);

            Debug.Log("Enemy attacks player : -" + attackDamage);
        }
        else
        {
            Debug.LogWarning("S_HealthBar introuvable sur le Player.");
        }
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
    private void UpdateDetectionLight(EnemyState state)
    {
        if (detectionLight == null)
            return;

        detectionLight.intensity = lightIntensity;

        switch (state)
        {
            case EnemyState.Chasing:
                detectionLight.enabled = true;
                detectionLight.color = chasingLightColor;
                break;

            case EnemyState.Surveillance:
                detectionLight.enabled = true;
                detectionLight.color = surveillanceLightColor;
                break;

            case EnemyState.Roaming:
                detectionLight.enabled = true;
                detectionLight.color = roamingLightColor;
                break;

            default:
                detectionLight.enabled = false;
                break;
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
    private void UpdateAttackProgressBar()
    {
        if (attackProgressBar == null)
            return;

        attackProgressBar.value = attackTimer / attackAnimDuration;
    }
    private void ResetAttackProgressBar()
    {
        attackTimer = 0f;
        isPreparingAttack = false;

        if (attackProgressBar != null)
        {
            attackProgressBar.value = 0f;
            attackProgressBar.gameObject.SetActive(false);
        }
    }

    // ------------------------------------------------------------------
    // UTILITAIRES
    // ------------------------------------------------------------------

    private void SetState(EnemyState newState)
    {
        _state = newState;

        UpdateDetectionLight(newState);
    }
}
