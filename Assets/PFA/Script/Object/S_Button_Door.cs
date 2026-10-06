using UnityEngine;

public class S_Button_Door : MonoBehaviour
{
    void OnTriggerEnter(Collider collision)
    {
        if (collision.CompareTag("Ally") && S_Door_Controller.instance != null)
        {
            int buttonIndex = S_Door_Controller.instance.buttonDoors.IndexOf(gameObject);

            if (buttonIndex >= 0)
            {
                S_Door_Controller.instance.OpenDoor(buttonIndex);
                S_Door_Controller.instance.ChangeLightColor(buttonIndex, Color.green);
                
                Destroy(gameObject);
            }
        }
    }
}
