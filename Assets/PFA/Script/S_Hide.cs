using Unity.VisualScripting;
using UnityEngine;
using TMPro;

public class S_Hide : MonoBehaviour
{
    public S_Field_Of_View_Target fovTarget;
    public S_Field_Of_View fovCharacter;
    public S_Controller control;

    public bool isHidden;
    public bool playerInside;

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
        lightObj = transform.Find("Light");
        //circleObj = transform.Find("Circle");
        lumen = lightObj.GetComponent<MeshRenderer>();
        //lumenFloor = circleObj.GetComponent<SpriteRenderer>();

        lumen.enabled = false;
        //lumenFloor.enabled = false; 

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
            lumen.enabled = true;

            hideText.gameObject.SetActive(false);

            character.transform.position = transform.position;
            fovCharacter.viewRadius = 0f;

        }
        else if (Input.GetKeyDown(KeyCode.F) && isHidden)
        {
            collision.enabled = true;
            rb.isKinematic = false;
            control.canMove = true;
            control.canPress = true;
            isHidden = false;
            lumen.enabled = false;

            hideText.gameObject.SetActive(false);

            character.transform.position = transform.position + transform.forward * 2f;
            fovCharacter.viewRadius = 8f;
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
