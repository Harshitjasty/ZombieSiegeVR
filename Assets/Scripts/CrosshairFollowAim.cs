
using UnityEngine;

public class CrosshairFollowAim : MonoBehaviour
{
    [Header("Aim References")]
    public Transform aimOrigin;
    public Camera playerCamera;
    public RectTransform overlayCrosshair;

    [Header("Aim Settings")]
    public float aimRange = 50f;

    [Header("Visibility Settings")]
    public float surfaceOffset = 0.02f;

    private RectTransform canvasRect;

    private void Start()
    {
        if (overlayCrosshair != null)
        {
            canvasRect = overlayCrosshair.parent
                as RectTransform;
        }

        if (aimOrigin == null)
            Debug.LogError("Crosshair: Aim Origin is missing.");

        if (playerCamera == null)
            Debug.LogError("Crosshair: Player Camera is missing.");

        if (overlayCrosshair == null)
            Debug.LogError("Crosshair: Overlay Crosshair is missing.");

        if (canvasRect == null)
            Debug.LogError("Crosshair: Canvas RectTransform is missing.");
    }

    private void LateUpdate()
    {
        if (aimOrigin == null ||
            playerCamera == null ||
            overlayCrosshair == null ||
            canvasRect == null)
            return;

        // Use the exact direction of the gun.
        Ray aimRay = new Ray(
            aimOrigin.position,
            aimOrigin.forward
        );

        Vector3 aimPoint;

        // Find the surface the gun is pointing towards.
        if (Physics.Raycast(
            aimRay,
            out RaycastHit hit,
            aimRange))
        {
            aimPoint = hit.point +
                hit.normal * surfaceOffset;
        }
        else
        {
            aimPoint = aimRay.origin +
                aimRay.direction * aimRange;
        }

        // Convert the world aim point into headset view coordinates.
        Vector3 viewportPoint =
            playerCamera.WorldToViewportPoint(aimPoint);

        if (viewportPoint.z <= 0f)
        {
            overlayCrosshair.gameObject.SetActive(false);
            return;
        }

        float x =
            (viewportPoint.x - 0.5f) *
            canvasRect.rect.width;

        float y =
            (viewportPoint.y - 0.5f) *
            canvasRect.rect.height;

        overlayCrosshair.anchoredPosition =
            new Vector2(x, y);

        overlayCrosshair.gameObject.SetActive(true);
    }
}
