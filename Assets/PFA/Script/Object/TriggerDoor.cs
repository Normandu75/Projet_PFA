using UnityEngine;

public class TriggerDoor : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && S_Door_Controller.instance != null)
        {
            int doorIndex = S_Door_Controller.instance.doors.IndexOf(gameObject);

            if (doorIndex >= 0)
            {
                S_Door_Controller.instance.OpenDoor(doorIndex);
                S_Door_Controller.instance.ChangeLightColor(doorIndex, Color.green);
            }
        }
    }
}
