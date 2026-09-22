using UnityEngine;

public class S_Input_Iso : MonoBehaviour
{
    void Update()
    {
        S_Character_Controller.instance.CursorToCamera();
        S_Character_Controller.instance.SeeThroughWalls();
    }

    void FixedUpdate()
    {
        S_Character_Controller.instance.Movement();
    }
}