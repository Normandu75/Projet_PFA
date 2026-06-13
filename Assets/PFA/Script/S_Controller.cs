using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class S_Controller : MonoBehaviour
{
    public float speed = 6f;

    Rigidbody rigidBody;
    Camera cam;

    public Image image;
    public Material mat;

    public bool FlashLightOn;
    public bool canMove;
    public bool canPress;

    public S_Field_Of_View fov;
    public S_FlashLight_Energy energy;

    Vector3 velocity;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rigidBody = GetComponent<Rigidbody>();
        cam = Camera.main;
        image = GameObject.Find("EnergyBar_Inside").GetComponent<Image>();
        mat = GameObject.Find("EnergyBar_Inside").GetComponent<Image>().material;
        fov = GetComponent<S_Field_Of_View>();
        energy = GetComponent<S_FlashLight_Energy>();
        canMove = true;
        canPress = true;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 mousePos = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, cam.transform.position.y));
        velocity = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized * speed;

        transform.LookAt(mousePos + Vector3.up * transform.position.y);

        if (Input.GetKeyDown(KeyCode.E) && image.fillAmount < 1f)
        {
            image.fillAmount += 0.5f;

            if (image.fillAmount == 1f)
            {
                StartCoroutine(StopDepletion());
            }
        }

        if (canPress)
        {
            if (Input.GetKeyDown(KeyCode.Q) && FlashLightOn == true)
            {
                fov.viewRadius = 8;
                fov.viewAngle = 90;

                FlashLightOn = false;
                energy.Depleting = false;
            }
            else if (Input.GetKeyDown(KeyCode.Q) && FlashLightOn == false)
            {
                fov.viewRadius = 20;
                fov.viewAngle = 50;

                FlashLightOn = true;
                energy.Depleting = true;
            }

            if (image.fillAmount > 0.5f)
            {
                mat.color = Color.Lerp(Color.white, Color.white, Mathf.PingPong(Time.time, 1));

                Debug.Log("Energy Resplenished");
            }
        }
    }

    void FixedUpdate()
    {
        if (canMove)
        {
            rigidBody.MovePosition(rigidBody.position + velocity * Time.fixedDeltaTime);
        }
    }

    IEnumerator StopDepletion()
    {
        energy.Depleting = false;

        yield return new WaitForSeconds(5f);

        if (FlashLightOn == true)
        {
            energy.Depleting = true;
        }
        else
        {
            energy.Depleting = false;          
        }
    }
}
