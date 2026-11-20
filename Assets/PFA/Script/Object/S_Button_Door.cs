using UnityEngine;

public class S_Button_Door : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ally") && S_Door_Controller.instance != null)
        {
            int buttonIndex = S_Door_Controller.instance.buttonDoors.IndexOf(gameObject);
            S_Door_Controller.instance.UnlockDoor(buttonIndex);
            S_Door_Controller.instance.ChangeLightColor(buttonIndex, Color.green);

            Debug.Log("Ouverture possible");
        }
    }
}
