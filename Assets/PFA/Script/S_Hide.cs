using Unity.VisualScripting;
using UnityEngine;
using TMPro;

public class S_Hide : MonoBehaviour
{
    public S_Field_Of_View_Target fovTarget;
    public S_Field_Of_View fovCharacter;
    public S_Controller control;
    public S_FlashLight_Energy flashLight;

    public bool isHidden;
    public bool playerInside;
    public bool inHiding;

    [SerializeField]
    private TMP_Text hideText;

    public GameObject character;
    public Collider collision;
    public Rigidbody rb;
    public MeshRenderer lumen;
    //public SpriteRenderer lumenFloor;
    public Transform lightObj;
    //public Transform circleObj;

    void Start()
    {
        fovTarget = GameObject.Find("Target").GetComponent<S_Field_Of_View_Target>();
        fovCharacter = GameObject.Find("Character").GetComponent<S_Field_Of_View>();
        character = GameObject.Find("Character");
        control = GameObject.Find("Character").GetComponent<S_Controller>();
        collision = GameObject.Find("Character").GetComponent<Collider>();
        rb = GameObject.Find("Character").GetComponent<Rigidbody>();
        flashLight = GameObject.Find("Character").GetComponent<S_FlashLight_Energy>();

        lightObj = transform.Find("Light");

        lumen = lightObj.GetComponent<MeshRenderer>();

        lumen.enabled = false;

        hideText.gameObject.SetActive(false);
    }

    void Update()
    {
        Debug.Log("control.canMove");

        if(Input.GetKeyDown(KeyCode.F) && playerInside && !isHidden)
        {
            Debug.Log("Caché");

            /*if (fovTarget.isInSight == false)
            {
                character.layer = 0;
            }*/

            collision.enabled = false;
            rb.isKinematic = true;
            control.canMove = false;
            control.FlashLightOn = false;
            control.canPress = false;
            isHidden = true;
            inHiding = true;
            lumen.enabled = true;
            flashLight.Depleting = false;

            hideText.gameObject.SetActive(false);

            character.transform.position = transform.position;
            fovCharacter.viewRadius = 0f;
            fovCharacter.circleRadius = 0f;

        }
        else if (Input.GetKeyDown(KeyCode.F) && isHidden)
        {
            collision.enabled = true;
            rb.isKinematic = false;
            control.canMove = true;
            control.canPress = true;
            isHidden = false;
            inHiding = false;
            lumen.enabled = false;

            hideText.gameObject.SetActive(false);

            character.transform.position = transform.position + transform.forward * 2f;
            fovCharacter.viewRadius = 8f;
            fovCharacter.circleRadius = 2f;
        }
    }

    public void OnTriggerEnter(Collider collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInside = true;
            //lumenFloor.enabled = true;

            hideText.gameObject.SetActive(true);

            Debug.Log("Caché");
        }
    }

    public void OnTriggerExit(Collider collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInside = false;
            //lumenFloor.enabled = true;

            hideText.gameObject.SetActive(false);
        }
    }
}
