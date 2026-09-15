using UnityEngine;
using UnityEngine.AI;

public class S_Control_Ally : MonoBehaviour
{
    public NavMeshAgent agent;

    [Header ("Movement Settings")]
    public float speed = 10;

    [Header ("Input Settings")]
    public float sampleDistance = 0.5f;
    public bool objectClicked;

    public LayerMask groundLayer;
    public LayerMask objectLayer;

    private Collider targetCollider;

    public static event System.Action<Vector3> OnGroundTouch;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        agent.speed = speed;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer))
            {
                if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, sampleDistance, NavMesh.AllAreas))
                {
                    agent.SetDestination(navHit.position);

                    OnGroundTouch?.Invoke(navHit.position);
                }
            }
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, objectLayer))
            {
                Debug.Log("Objet touché : " + hit.collider.name);

                agent.SetDestination(hit.point);
                
                targetCollider = hit.collider;
                objectClicked = true;
            }
            else
            {
                Debug.Log("Aucun sol détecté");
            }
        }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (objectClicked && targetCollider.CompareTag("Hide"))
            {
                Debug.Log("Objet touché : " + targetCollider.name);

                Destroy(targetCollider.gameObject);
                /*Destroy(targetCollider.GetComponent<S_Hide>());*/
            }
            else
            {
                Debug.Log("Aucun objet détecté");
            }

            objectClicked = false;
            targetCollider = null;
        }
    }
}
