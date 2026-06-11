using Unity.VisualScripting;
using UnityEngine;

public class S_Hide : MonoBehaviour
{
    private S_Random_Movement enemy;
    public S_Controller control;

    public bool isHidden;
    public bool playerInside;

    public GameObject character;
    public Collider collision;
    public Rigidbody rb;

    void Start()
    {
        enemy = GameObject.Find("Target").GetComponent<S_Random_Movement>();
        character = GameObject.Find("Character");
        control = GameObject.Find("Character").GetComponent<S_Controller>();
        collision = GameObject.Find("Collider").GetComponent<Collider>();
        rb = GameObject.Find("Character").GetComponent<Rigidbody>();
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.F) && playerInside && !isHidden)
        {
            Debug.Log("Caché");

            collision.enabled = false;
            character.transform.position = transform.position;
            rb.isKinematic = true;
            control.canMove = false;
            isHidden = true;
        }
        else if (Input.GetKeyDown(KeyCode.F) && isHidden)
        {
            collision.enabled = true;
            character.transform.position = transform.position + transform.forward * 1f;
            control.canMove = true;
            isHidden = false;
        }
    }

    public void OnTriggerEnter(Collider collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInside = true;

            Debug.Log("Caché");
        }
    }

    public void OnTriggerExit(Collider collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInside = false;
        }
    }
}
