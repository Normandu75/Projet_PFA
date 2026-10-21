
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class S_Field_Of_View : MonoBehaviour
{
    public static S_Field_Of_View instance;

    [Header("Lampe torche")]
    public float viewRadius = 40f;

    [Range(0, 360)]
    public float viewAngle;

    public Transform viewDirection;
    public bool isActive = true;

    public Vector3 ViewDirection =>
        viewDirection != null
            ? viewDirection.forward
            : transform.forward;

    [Header("Détection")]
    public LayerMask targetMask;

    [Tooltip("Layers qui bloquent la lumière.")]
    public LayerMask obstacleMask;

    [SerializeField, Min(0.02f)]
    private float detectionInterval = 0.1f;

    public List<Transform> visibleTargets =
        new List<Transform>();

    private readonly HashSet<EnemyAI> _visibleEnemies =
        new HashSet<EnemyAI>();

    private Coroutine detectionRoutine;
    private Light flashlightLight;
    private Transform observer;

    private void Awake()
    {
        if (instance == null)
            instance = this;

        S_Robot_Controller robotController =
            GetComponentInParent<S_Robot_Controller>();

        S_Control_Ally allyController =
            GetComponentInParent<S_Control_Ally>();

        observer = robotController != null
            ? robotController.transform
            : allyController != null
                ? allyController.transform
                : transform.root;

        GameObject lightObject =
            GameObject.Find("FlashLight");

        if (lightObject != null)
            flashlightLight = lightObject.GetComponent<Light>();
    }

    private void OnEnable()
    {
        detectionRoutine =
            StartCoroutine(FindTargetsWithDelay());
    }

    private void OnDisable()
    {
        if (detectionRoutine != null)
        {
            StopCoroutine(detectionRoutine);
            detectionRoutine = null;
        }

        ClearDetectedEnemies();

        if (instance == this)
            instance = null;
    }

    private void Start()
    {
        SetFlashlightActive(isActive);
    }

    private IEnumerator FindTargetsWithDelay()
    {
        while (true)
        {
            FindVisibleTargets();

            yield return new WaitForSeconds(
                detectionInterval
            );
        }
    }

    private void FindVisibleTargets()
    {
        if (!isActive)
        {
            ClearDetectedEnemies();
            return;
        }

        visibleTargets.Clear();

        HashSet<EnemyAI> currentlyVisibleEnemies =
            new HashSet<EnemyAI>();

        Collider[] targetsInRange =
            Physics.OverlapSphere(
                transform.position,
                viewRadius,
                targetMask
            );

        foreach (Collider target in targetsInRange)
        {
            Transform targetTransform = target.transform;

            if (!IsInFieldOfView(targetTransform))
                continue;

            Vector3 directionToTarget =
                targetTransform.position - transform.position;

            float distanceToTarget =
                directionToTarget.magnitude;

            if (distanceToTarget > 0f &&
                Physics.Raycast(
                    transform.position,
                    directionToTarget / distanceToTarget,
                    distanceToTarget,
                    obstacleMask))
            {
                continue;
            }

            if (!visibleTargets.Contains(targetTransform))
                visibleTargets.Add(targetTransform);

            EnemyAI enemy =
                targetTransform.GetComponentInParent<EnemyAI>();

            if (enemy != null)
                currentlyVisibleEnemies.Add(enemy);
        }

        foreach (EnemyAI enemy in currentlyVisibleEnemies)
        {
            if (enemy != null &&
                !_visibleEnemies.Contains(enemy))
            {
                enemy.SetObservedBy(observer, true);
            }
        }

        foreach (EnemyAI enemy in _visibleEnemies)
        {
            if (enemy != null &&
                !currentlyVisibleEnemies.Contains(enemy))
            {
                enemy.SetObservedBy(observer, false);
            }
        }

        _visibleEnemies.Clear();
        _visibleEnemies.UnionWith(currentlyVisibleEnemies);
    }

    private void ClearDetectedEnemies()
    {
        foreach (EnemyAI enemy in _visibleEnemies)
        {
            if (enemy != null)
                enemy.SetObservedBy(observer, false);
        }

        _visibleEnemies.Clear();
        visibleTargets.Clear();
    }

    private bool IsInFieldOfView(Transform target)
    {
        Vector3 directionToTarget =
            target.position - transform.position;

        return Vector3.Angle(
            ViewDirection,
            directionToTarget
        ) < viewAngle / 2f;
    }

    private void SetFlashlightActive(bool active)
    {
        isActive = active;

        if (flashlightLight != null)
            flashlightLight.enabled = isActive;

        if (!isActive)
            ClearDetectedEnemies();
    }

    public void TriggerFlashLight()
    {
        if (Gamepad.current == null)
            return;

        if (Gamepad.current.rightStickButton.wasPressedThisFrame)
        {
            SetFlashlightActive(!isActive);
        }
    }
}
