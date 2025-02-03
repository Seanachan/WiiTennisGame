using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // The object to follow
    public Vector3 offset = new Vector3(0, 5, -10); // Offset from the target
    public float smoothSpeed = 0.125f; // Smoothing factor for camera movement

    private void LateUpdate()
    {
        if (target == null) return;

        // Desired position based on the target's position and offset
        Vector3 desiredPosition = target.position + offset;

        // Smoothly move the camera to the desired position
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;

        // Optionally look at the target
        transform.LookAt(target);
    }
}
