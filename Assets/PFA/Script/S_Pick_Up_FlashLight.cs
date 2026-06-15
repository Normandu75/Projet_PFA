using UnityEngine;
using TMPro;

public class S_Pick_Up_FlashLight : MonoBehaviour
{
    public S_Controller control;
    public TMP_Text flashLightText;

    void Start()
    {
        control = GameObject.Find("Character").GetComponent<S_Controller>();

        flashLightText.gameObject.SetActive(false);
    }

    void OnCollisionEnter(Collision collision)
    {
        flashLightText.gameObject.SetActive(true);

        Debug.Log("Je détecte");

        if (collision.gameObject.tag == "Player")
        {
            control.hasFlashLight = true;
            flashLightText.gameObject.SetActive(false);

            Destroy(gameObject);
        }
    }

    void OnCollisionExit(Collision collision)
    {
        flashLightText.gameObject.SetActive(false);
    }
}
