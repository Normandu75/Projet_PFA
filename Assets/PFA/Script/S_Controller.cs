using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    public bool hasFlashLight;

    public S_Field_Of_View fov;
    public S_FlashLight_Energy energy;
    [SerializeField] 
    private Image lightOff;
    [SerializeField] 
    private Image lightOn;
     [SerializeField] 
    private Image reload;

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
        lightOff.gameObject.SetActive(true);
        lightOn.gameObject.SetActive(false);
        reload.gameObject.SetActive(false);
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
            SoundManager.PlaySound(SoundType.BtrCharge);
            if (FlashLightOn)
            {
                lightOn.gameObject.SetActive(true);
            }
            else
            {
                lightOff.gameObject.SetActive(true);
            }

            if (image.fillAmount == 1f)
            {
                StartCoroutine(StopDepletion());
            }
        }

        if (canPress && hasFlashLight)
        {
            if (Input.GetKeyDown(KeyCode.Q) && FlashLightOn == true)
            {
                SoundManager.PlaySound(SoundType.FlashOn);
                fov.viewRadius = 8;
                fov.viewAngle = 90;

                FlashLightOn = false;
                energy.Depleting = false;
                lightOff.gameObject.SetActive(true);
                lightOn.gameObject.SetActive(false);
            }
            else if (Input.GetKeyDown(KeyCode.Q) && FlashLightOn == false)
            {
                SoundManager.PlaySound(SoundType.FlashOn);
                fov.viewRadius = 20;
                fov.viewAngle = 50;
                lightOff.gameObject.SetActive(false);
                lightOn.gameObject.SetActive(true);
                FlashLightOn = true;
                energy.Depleting = true;
               
            }

            if (image.fillAmount >= 0.1f)
            {
                //mat.color = Color.Lerp(Color.white, Color.white, Mathf.PingPong(Time.time, 1));
                reload.gameObject.SetActive(false);
                // -----------------------------------------
                // UTILISE LE SON ICI => LUMIERE RECHARGER
                // -----------------------------------------

                Debug.Log("Energy Resplenished");
            }
            if(image.fillAmount == 1f)
            {
                // -----------------------------------------
                // UTILISE LE SON ICI => LUMIERE RECHARGER ENTIEREMENT 
                // -----------------------------------------
            }
            if(image.fillAmount < 0.1)
            {
                // -----------------------------------------
                // UTILISE LE SON ICI => LUMIERE N'A PLUS DE BATTERIE
                // -----------------------------------------
                lightOff.gameObject.SetActive(false);
                lightOn.gameObject.SetActive(false);
                reload.gameObject.SetActive(true);
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
