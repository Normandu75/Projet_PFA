using UnityEngine;

public class WorldSpaceUIBillboard : MonoBehaviour
{
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;

            if (mainCamera == null)
                return;
        }

        // Oriente l'UI vers la caméra
        transform.rotation = Quaternion.LookRotation(
            transform.position - mainCamera.transform.position
        );
    }
}
