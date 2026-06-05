using UnityEngine;
using UnityEngine.UI;

public class S_FlashLight_Energy : MonoBehaviour
{
    public Image image;
    public Material mat;

    public S_Field_Of_View fov;
    public S_Controller controller;

    public bool Depleting;
    private float NumberWang;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        image = GameObject.Find("EnergyBar_Inside").GetComponent<Image>();
        mat = GameObject.Find("Character").GetComponent<MeshRenderer>().material;
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
                NumberWang = Random.Range(0f, 1f);

                image.fillAmount -= 0.1f * Time.deltaTime;

                if (NumberWang < 0.5f)
                {
                    Debug.Log("Wang");
                }
                else
                {
                    Debug.Log("Not Wang");
                }
            }
        }

    }
}
