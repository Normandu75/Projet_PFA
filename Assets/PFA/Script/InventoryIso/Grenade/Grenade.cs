using System;
using UnityEngine;
using UnityEngine.AI;

public class Grenade : MonoBehaviour
{
    [Header("Explosion Prefab")]
    [SerializeField] private float explosionDelay = 3f; 
    // [SerializeField] private float explosionForce = 700f;
    // [SerializeField] private float explosionRadius = 5f;
    private float countDown;
    private bool hasExploded = false;
    private bool enemyInRange = false;
    public Transform target;

    public NavMeshAgent agent;
    
    void Awake()
    {
        agent = GameObject.Find("Enemy").GetComponent<NavMeshAgent>();
    }
    void Start()
    {
        countDown = explosionDelay;
    }

    // Update is called once per frame
    void Update()
    {
        if (!hasExploded)
        {
            countDown -= Time.deltaTime;
            if(countDown <= 0f)
            {
                Explode();
                hasExploded = true;
            }
        }
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Enemy"))
        {
            Debug.Log(@"Triste");
            enemyInRange = true;

        }
    }
    void Explode()
    {
        if(enemyInRange == true)
        {

        NavMeshHit hit;

        if (NavMesh.SamplePosition(
            target.position,
            out hit,
            5f,
            NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }

        }
    }
}
