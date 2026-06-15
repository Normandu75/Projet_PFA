using UnityEngine;
using TMPro;

public class S_Pick_Up_FlashLight : MonoBehaviour
{
    public S_Controller control;
    public TMP_Text flashLightText;

    public bool inRange;

    void Start()
    {
        control = GameObject.Find("Character").GetComponent<S_Controller>();

        flashLightText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (inRange && Input.GetKeyDown(KeyCode.F))
        {
            control.hasFlashLight = true;

            flashLightText.gameObject.SetActive(false);

            Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            inRange = true;
            flashLightText.gameObject.SetActive(true);
        }
    }

    void OnCollisionExit(Collision collision)
    {
        flashLightText.gameObject.SetActive(false);
    }
}
