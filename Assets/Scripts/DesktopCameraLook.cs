
using UnityEngine;
using UnityEngine.InputSystem;

public class DesktopCameraLook : MonoBehaviour
{
    public float sensitivity = 0.15f;

    private float pitch;
    private float yaw;

    private void Start()
    {
        Vector3 angles = transform.localEulerAngles;
        pitch = angles.x;
        yaw = angles.y;
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        if (Mouse.current.rightButton.wasPressedThisFrame)
            Cursor.lockState = CursorLockMode.Locked;

        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
        }

        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        yaw += mouseDelta.x * sensitivity;
        pitch -= mouseDelta.y * sensitivity;
        pitch = Mathf.Clamp(pitch, -80f, 80f);

        transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
    }
}
