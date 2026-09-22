using UnityEngine;
using UnityEngine.UI;

public class S_Exit_Door : MonoBehaviour
{
    public Image image;

    void Start()
    {
        image.gameObject.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            image.gameObject.SetActive(true);

            Debug.Log("Sorti");
        }
    }
}
