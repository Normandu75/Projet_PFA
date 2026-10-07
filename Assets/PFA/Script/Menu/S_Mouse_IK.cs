using UnityEngine;

public class S_Mouse_IK : MonoBehaviour
{
    [SerializeField] private Transform ikTarget;
    [SerializeField] private Camera targetCamera;

    private Plane movementPlane;

    private void Awake()
    {
        if (ikTarget == null)
        {
            ikTarget = transform;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            Debug.LogError("S_Mouse_IK requires a camera to project the mouse position.", this);
            enabled = false;
            return;
        }

        movementPlane = new Plane(targetCamera.transform.forward, ikTarget.position);
    }

    private void Update()
    {
        Ray mouseRay = targetCamera.ScreenPointToRay(Input.mousePosition);
        if (movementPlane.Raycast(mouseRay, out float distance) && distance >= 0f)
        {
            ikTarget.position = mouseRay.GetPoint(distance);
        }
    }
}
