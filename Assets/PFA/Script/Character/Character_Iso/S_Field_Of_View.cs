using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class S_Field_Of_View : MonoBehaviour
{
    public float viewRadius;
    [Range(0, 360)]
    public float viewAngle;
    public Transform viewDirection;

    public Vector3 ViewDirection => viewDirection != null ? viewDirection.forward : transform.forward;

    public LayerMask targetMask;
    [Tooltip("Layers that block line of sight to targets.")]
    public LayerMask obstacleMask;

    public List<Transform> visibleTargets = new List<Transform>();

    private readonly HashSet<EnemyAI> _visibleEnemies = new HashSet<EnemyAI>();

    void Start()
    {
        StartCoroutine(FindTargetsWithDelay(6f));
    }

    IEnumerator FindTargetsWithDelay(float delay)
    {
        while (true)
        {
            yield return new WaitForSeconds(delay);
            FindVisibleTargets();
        }
    }

    void FindVisibleTargets()
    {
        visibleTargets.Clear();

        HashSet<EnemyAI> currentlyVisibleEnemies = new HashSet<EnemyAI>();

        Collider[] targetsInRange = Physics.OverlapSphere(transform.position, viewRadius, targetMask);

        foreach (Collider target in targetsInRange)
        {
            Transform targetTransform = target.transform;

            if (visibleTargets.Contains(targetTransform) || !IsInFieldOfView(targetTransform))
            {
                continue;
            }

            Vector3 directionToTarget = targetTransform.position - transform.position;

            float distanceToTarget = directionToTarget.magnitude;

            if (distanceToTarget > 0f && Physics.Raycast(transform.position, directionToTarget / distanceToTarget, distanceToTarget, obstacleMask))
            {
                continue;
            }

            visibleTargets.Add(targetTransform);

            EnemyAI enemy = targetTransform.GetComponentInParent<EnemyAI>();

            if (enemy != null && currentlyVisibleEnemies.Add(enemy) && !_visibleEnemies.Contains(enemy))
            {
                S_Robot_Controller robotController =
                    GetComponentInParent<S_Robot_Controller>();
                S_Control_Ally allyController =
                    GetComponentInParent<S_Control_Ally>();
                Transform observer = robotController != null
                    ? robotController.transform
                    : allyController != null
                        ? allyController.transform
                        : transform.root;

                enemy.OnSpottedBy(observer);
            }
        }

        _visibleEnemies.Clear();
        _visibleEnemies.UnionWith(currentlyVisibleEnemies);
    }

    bool IsInFieldOfView(Transform target)
    {
        Vector3 directionToTarget = target.position - transform.position;

        return Vector3.Angle(ViewDirection, directionToTarget) < viewAngle / 2;
    }

}
