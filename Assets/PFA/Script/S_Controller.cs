using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class S_Controller : MonoBehaviour
{
    public float speed = 6f;

    Rigidbody rigidBody;
    Camera cam;

    public Image image;
    public S_Field_Of_View fov;
    public S_FlashLight_Energy energy;

    Vector3 velocity;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rigidBody = GetComponent<Rigidbody>();
        cam = Camera.main;
        image = GameObject.Find("EnergyBar_Inside").GetComponent<Image>();
        fov = GetComponent<S_Field_Of_View>();
        energy = GetComponent<S_FlashLight_Energy>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 mousePos = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, cam.transform.position.y));
        velocity = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized * speed;

        transform.LookAt(mousePos + Vector3.up * transform.position.y);

        if (Input.GetKeyDown(KeyCode.E) && fov.viewRadius < 20)
        {
            image.fillAmount += 0.5f;
            fov.viewRadius += 5f;

            if (fov.viewRadius > 20)
            {
                fov.viewRadius = 20;

                StartCoroutine(StopDepletion());
            }
        }
    }

    void FixedUpdate()
    {
        rigidBody.MovePosition(rigidBody.position + velocity * Time.fixedDeltaTime);
    }

    IEnumerator StopDepletion()
    {
        energy.Depleting = false;

        Debug.Log(energy.Depleting);

        yield return new WaitForSeconds(5f);

        energy.Depleting = true;
    }
}
