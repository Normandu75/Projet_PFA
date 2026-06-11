using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

public class S_Random_Movement : MonoBehaviour
{
    public NavMeshAgent agent;

    public float range; 

    public Transform Origin;
    public Transform Player;

    public LayerMask objectMask;

    public bool isInLight;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        Origin = GetComponent<Transform>();
        Player = GameObject.Find("Character").transform;
    }

    // Update is called once per frame
    void Update()
    {
        if (isInLight == false)
        {
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                Vector3 point;

                if (RandomPoint(Origin.position, range, out point))
                {
                    Debug.DrawRay(point, Vector3.up, Color.red, 1.0f);

                    agent.SetDestination(point);
                }

            }
        }
        else
        {
            agent.SetDestination(Player.position);
        }
    }

    bool RandomPoint(Vector3 center, float range, out Vector3 result)
    {
        Vector3 randomPoint = center + Random.insideUnitSphere * range;

        NavMeshHit hit;

        if (NavMesh.SamplePosition(randomPoint, out hit, 1.0f, NavMesh.AllAreas))
        {
            result = hit.position;
            
            return true;
        }

        result = Vector3.zero;

        return false;
    }

    public void NearestHide()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, 50f, objectMask);

        foreach (Collider col in colliders)
        {
            if (col.CompareTag("Hide"))
            {
                agent.SetDestination(col.transform.position);

                Debug.Log("Direction cachette");
            }
        }
    }
}
