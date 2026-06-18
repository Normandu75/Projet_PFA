using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;
using System.Diagnostics.CodeAnalysis;

public class S_Random_Movement : MonoBehaviour
{
    public NavMeshAgent agent;

    public float range; 

    public Transform Origin;
    public Transform Player;

    public LayerMask objectMask;

    public bool isInLight;
    public bool playerDetected;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        Origin = GetComponent<Transform>();
        Player = GameObject.Find("Character").transform;

        agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        agent.avoidancePriority = 0;
    }

    // Update is called once per frame
    void Update()
    {
        agent.stoppingDistance = 1.0f;

        if (isInLight == false)
        {
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                Vector3 point;

                if (RandomPoint(Origin.position, range, out point))
                {
                    Debug.DrawRay(point, Vector3.up, Color.red, 1.0f);

                    agent.SetDestination(point);

                    playerDetected = false;
                }
            }
        }
        else
        {
            agent.SetDestination(Player.position);

            if (!playerDetected)
            {
                SoundManager.PlaySound(SoundType.Detect);

                playerDetected = true;
            }
        }
    }

    bool RandomPoint(Vector3 center, float range, out Vector3 result)
    {
        Vector3 randomPoint = center + Random.insideUnitSphere * range;

        NavMeshHit hit;

        if (NavMesh.SamplePosition(randomPoint, out hit, 1.0f, NavMesh.AllAreas))
        {
            NavMeshPath path = new NavMeshPath();

            if (hit.distance > 0.5f)
            {
                if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    result = hit.position;

                    return true;
                }
            }
        }

        result = Vector3.zero;

        return false;
    }

    public void NearestHide()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, 5f, objectMask);

        Transform nearest = null;
        float minDist = Mathf.Infinity;

        foreach (Collider col in colliders)
        {
            if (!col.CompareTag("Hide"))
            {
                continue;
            }

            float dist = Vector3.Distance(transform.position, col.transform.position);

            if (dist < minDist)
            {
                minDist = dist;
                nearest = col.transform;
            }
        }

        if (nearest != null)
        {
            agent.SetDestination(nearest.transform.position);

            playerDetected = false;

            Debug.Log(playerDetected);
        }
    }
}
