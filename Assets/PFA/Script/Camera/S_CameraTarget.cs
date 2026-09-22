using UnityEngine;

public class CameraTarget : MonoBehaviour
{
    public Transform player;
    public float cursorInfluence = 0.35f;
    public float maxDistance = 6f;
    public float smoothSpeed = 8f;

    void LateUpdate()
    {
        Vector3 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouse.z = player.position.z;

        Vector3 offset = mouse - player.position;

        offset = Vector3.ClampMagnitude(offset, maxDistance);

        Vector3 desired =
            player.position +
            offset * cursorInfluence;

        transform.position = Vector3.Lerp(
            transform.position,
            desired,
            Time.deltaTime * smoothSpeed
        );
    }
}