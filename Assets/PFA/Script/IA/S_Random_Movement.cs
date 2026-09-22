using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;
using System.Diagnostics.CodeAnalysis;

public class S_Random_Movement : MonoBehaviour
{
    public NavMeshAgent agent;

    public S_Controller controller;

    public float range; 

    public Transform Origin;
    public Transform Player;

    public LayerMask objectMask;

    public bool isInLight;
    public bool playerDetected;
    public bool isWaiting;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        Origin = GetComponent<Transform>();
        Player = GameObject.Find("Character").transform;
        controller = GameObject.Find("Character").GetComponent<S_Controller>();

        agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        agent.avoidancePriority = 0;
    }

    // Update is called once per frame
    void Update()
    {
        DestructionHide();

        /*agent.stoppingDistance = 1.0f;*/

        if (isInLight == false)
        {
            if (!isWaiting && (agent.remainingDistance <= agent.stoppingDistance))
            {
                StartCoroutine(WaitBeforeMoving());
            }
        }
        else
        {
            agent.SetDestination(Player.position);

            if (!playerDetected)
            {
                /*SoundManager.PlaySound(SoundType.Detect);*/

                playerDetected = true;
            }
        }
    }

        IEnumerator WaitBeforeMoving()
    {
        isWaiting = true;

        Debug.Log("Waiting before moving");

        yield return new WaitForSeconds(0.5f); // l’ennemi attend 0.5 secondes

        Vector3 point;

        if (RandomPoint(Origin.position, range, out point))
        {
            Debug.DrawRay(point, Vector3.up, Color.red, 1.0f);

            agent.SetDestination(point);
            
            playerDetected = false;
        }

        isWaiting = false;
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

        void DestructionHide()
    {
        S_Hide hideSpot = GetPlayerHideSpot();

        if (hideSpot == null) return; // le joueur n’est pas dans une cachette

        float dist = Vector3.Distance(transform.position, hideSpot.transform.position);

        if (dist <= 5f)
        {
            Debug.Log("Cachette détruite : " + hideSpot.name);

            if (controller.holdBreath)
            {
                isInLight = false;
            }
            else if (!controller.holdBreath)
            {
                
                hideSpot.collision.enabled = true;
                hideSpot.control.canMove = true;
                hideSpot.control.canPress = true;
                hideSpot.isHidden = false;
                hideSpot.inHiding = false;
                hideSpot.rb.isKinematic = false;

                hideSpot.fovCharacter.viewRadius = 8f;
                hideSpot.fovCharacter.circleRadius = 2f;

                Destroy(hideSpot.gameObject);

                isInLight = true;

                Debug.Log("Cachette détruite : " + hideSpot.name);
            }
        }
        else
        {
            return;
        }
    }

    S_Hide GetPlayerHideSpot()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, 50f, objectMask);

        foreach (Collider col in colliders)
        {
            if (col.CompareTag("Hide") && playerDetected)
            {
                S_Hide hide = col.GetComponent<S_Hide>();

                if (hide != null && hide.inHiding)
                {
                    return hide;
                }
            }
        }
        return null;
    }
}
