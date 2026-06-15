using UnityEngine;
using UnityEngine.UI;

public class S_FlashLight_Energy : MonoBehaviour
{
    public Image image;
    public Material mat;

    public S_Field_Of_View fov;
    public S_Controller controller;

    public bool Depleting;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        image = GameObject.Find("EnergyBar_Inside").GetComponent<Image>();
        mat = GameObject.Find("View_Visualisation").GetComponent<MeshRenderer>().material;
        fov = GetComponent<S_Field_Of_View>();
        controller = GetComponent<S_Controller>();
        Depleting = true;
    }

    // Update is called once per frame
    void Update()
    {
        EnergyDepletion();
    }

    void EnergyDepletion()
    {
        if (Depleting == true)
        {
            if (image.fillAmount != 0)
            {
                image.fillAmount -= 0.05f * Time.deltaTime;

                if(image.fillAmount <= 0.5f)
                {
                    mat.color = Color.Lerp(Color.white, Color.red, Mathf.PingPong(Time.time, 1));

                    Debug.Log("Energy_Low");
                }
            }
            else
            {
                fov.viewRadius = 5;
                
                controller.FlashLightOn = false;
                Depleting = false;
            }
        }

    }
}
