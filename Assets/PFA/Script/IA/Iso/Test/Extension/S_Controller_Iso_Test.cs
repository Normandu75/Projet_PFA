using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class S_Controller_Iso_Test : MonoBehaviour
{
    protected Vector2 stickL;
    protected Vector2 stickR;

    public Vector2 StickL { get => stickL; }
    public Vector2 StickR { get => stickR; }

    public Vector3 StickL3 => new Vector3(StickL.x, 0, StickL.y);
    public Vector3 StickR3 => new Vector3(StickR.x, 0, StickR.y);

    void Update()
    {
        stickL = Gamepad.current?.leftStick.ReadValue() ?? Vector2.zero;
        stickR = Gamepad.current?.rightStick.ReadValue() ?? Vector2.zero;
    }

    public Button A = new Button();
    public Button B = new Button();
    public Button X = new Button();
    public Button Y = new Button();

    public class Button
    {
        bool isPressed = false;
        public UnityEvent OnPressDown = new UnityEvent();
        public UnityEvent OnPressUp = new UnityEvent();


        public bool IsPressed
        {
            get => isPressed;

            set
            {
                if (isPressed == value)
                    return;

                if (!isPressed && value)
                    OnPressDown.Invoke();

                if (isPressed && !value)
                    OnPressUp.Invoke();

                isPressed = value;
            }
        }
    }
}