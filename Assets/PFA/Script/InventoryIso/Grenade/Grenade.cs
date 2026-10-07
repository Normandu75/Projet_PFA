using System.Collections.Generic;
using UnityEngine;

public class Grenade : MonoBehaviour
{
    [Header("Leurre")]
    [SerializeField] private float explosionDelay = 3f;
    [SerializeField] private float distractionRadius = 8f;
    [SerializeField] private LayerMask enemyLayers = ~0;
    [Tooltip("Temps pendant lequel l'ennemi reste sur le leurre.")]
    [Min(0f)]
    [SerializeField] private float investigationTime = 3f;

    private float countdown;
    private bool hasExploded;

    private void Start()
    {
        countdown = explosionDelay;
    }

    private void Update()
    {
        if (hasExploded)
            return;

        countdown -= Time.deltaTime;

        if (countdown <= 0f)
            Explode();
    }

    private void Explode()
    {
        if (hasExploded)
            return;

        hasExploded = true;

        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            distractionRadius,
            enemyLayers,
            QueryTriggerInteraction.Collide
        );

        HashSet<EnemyAI> affectedEnemies = new HashSet<EnemyAI>();

        foreach (Collider col in colliders)
        {
            if (col == null)
                continue;

            EnemyAI enemy = col.GetComponentInParent<EnemyAI>();

            if (enemy == null || !affectedEnemies.Add(enemy))
                continue;

            enemy.InvestigateDecoy(
                transform.position,
                investigationTime
            );
        }

        //Destroy(gameObject);
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
