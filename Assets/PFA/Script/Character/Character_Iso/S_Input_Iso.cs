using Unity.VisualScripting;
using UnityEngine;

public class S_Input_Iso : MonoBehaviour
{
    void Update()
    {
        if (S_Camera_Controller.instance != null && S_Camera_Controller.instance.robotMode)
        {
            return;
        }

        S_Character_Controller.instance.CursorToCamera();
        S_Character_Controller.instance.SeeThroughWalls();
        S_Character_Controller.instance.PickUpRobotAlly();
        S_Camera_Controller.instance.CameraRotation();
        S_Camera_Controller.instance.RemoveMouseCursor();
    }

    void FixedUpdate()
    {
        if (S_Camera_Controller.instance == null || !S_Camera_Controller.instance.robotMode)
        {
            S_Character_Controller.instance.Movement();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "Robot_Ally")
        {
            S_Character_Controller.instance.detected = true;
        }
    }
}