
using System.Collections.Generic;
using UnityEngine;

public class Grenade : MonoBehaviour
{
    [Header("Destruction")]
    [Min(0f)]
    [SerializeField] private float destroyDelay = 5f;

    [Header("Leurre")]
    [SerializeField] private float distractionRadius = 8f;
    [SerializeField] private LayerMask enemyLayers = ~0;

    private readonly HashSet<EnemyAI> affectedEnemies = new();

    private void Start()
    {
        Destroy(gameObject, destroyDelay);
    }

    private void Update()
    {
        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            distractionRadius,
            enemyLayers,
            QueryTriggerInteraction.Collide
        );

        foreach (Collider col in colliders)
        {
            EnemyAI enemy = col.GetComponentInParent<EnemyAI>();

            if (enemy == null || !affectedEnemies.Add(enemy))
                continue;

            enemy.InvestigateDecoy(transform);
        }
    }

    private void OnDestroy()
    {
        foreach (EnemyAI enemy in affectedEnemies)
        {
            if (enemy != null)
                enemy.StopInvestigatingDecoy(transform);
        }

        affectedEnemies.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(
            transform.position,
            distractionRadius
        );
    }
}
