using UnityEngine;
using UnityEngine.UI;

public class S_FlashLight_Energy : MonoBehaviour
{
    public Image image;
    public S_Field_Of_View fov;

    public bool Depleting;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        image = GameObject.Find("EnergyBar_Inside").GetComponent<Image>();
        fov = GetComponent<S_Field_Of_View>();

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
            if (image.fillAmount != 0 && fov.viewRadius != 0)
            {
                image.fillAmount -= 0.1f * Time.deltaTime;
                fov.viewRadius -= 2f * Time.deltaTime;
            }
        }
    }
}
