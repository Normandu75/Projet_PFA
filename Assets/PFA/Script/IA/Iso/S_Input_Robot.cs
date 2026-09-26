using UnityEngine;
using UnityEngine.InputSystem;

public class S_Input_Robot : MonoBehaviour
{
    void Update()
    {
        if (S_Camera_Controller.instance == null || !S_Camera_Controller.instance.robotMode)
        {
            S_Robot_Controller.instance.SetMoveInput(Vector2.zero);
            S_CamController_Robot.instance.SetLookInput(Vector2.zero, Vector2.zero);
            return;
        }

        S_Robot_Controller.instance.SetMoveInput(ReadMove());
        S_CamController_Robot.instance.SetLookInput(ReadGamepadLook(), ReadMouseLook());
        S_CamController_Robot.instance.MoveCamera();
    }

    void FixedUpdate()
    {
        S_Robot_Controller.instance.Move();
    }

    Vector2 ReadMove()
    {
        Vector2 keyboardMove = ReadKeyboardMove();
        Vector2 gamepadMove = ReadGamepadMove();
        return gamepadMove.sqrMagnitude > keyboardMove.sqrMagnitude ? gamepadMove : keyboardMove;
    }

    Vector2 ReadKeyboardMove()
    {
        return new Vector2(
            (Keyboard.current?.dKey.isPressed == true ? 1f : 0f) - (Keyboard.current?.aKey.isPressed == true ? 1f : 0f),
            (Keyboard.current?.wKey.isPressed == true ? 1f : 0f) - (Keyboard.current?.sKey.isPressed == true ? 1f : 0f));
    }

    Vector2 ReadGamepadMove()
    {
        return Gamepad.current?.leftStick.ReadValue() ?? Vector2.zero;
    }

    Vector2 ReadGamepadLook()
    {
        return Gamepad.current?.rightStick.ReadValue() ?? Vector2.zero;
    }

    Vector2 ReadMouseLook()
    {
        return Mouse.current?.delta.ReadValue() ?? Vector2.zero;
    }
}
