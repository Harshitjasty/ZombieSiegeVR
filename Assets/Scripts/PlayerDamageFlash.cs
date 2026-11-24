
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerDamageFlash : MonoBehaviour
{
    [Header("References")]
    public PlayerHealth playerHealth;
    public Camera playerCamera;

    [Header("Flash Settings")]
    public Color flashColor = new Color(1f, 0f, 0f, 0.35f);

    [Range(0.05f, 0.5f)]
    public float flashDuration = 0.3f;

    [Header("VR Display Settings")]
    public float displayDistance = 0.8f;
    public float displayScale = 0.001f;

    private Image flashImage;
    private Coroutine flashCoroutine;
    private float previousHealth;

    private void Awake()
    {
        CreateFlashOverlay();
    }

    private void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerCamera == null)
        {
            Debug.LogError(
                "PlayerDamageFlash: Main Camera not found."
            );
            return;
        }

        // Attach the damage overlay directly to the VR camera.
        Transform canvasTransform =
            flashImage.transform.parent;

        canvasTransform.SetParent(
            playerCamera.transform,
            false
        );

        canvasTransform.localPosition =
            new Vector3(0f, 0f, displayDistance);

        canvasTransform.localRotation = Quaternion.identity;

        canvasTransform.localScale =
            Vector3.one * displayScale;

        Canvas canvas =
            canvasTransform.GetComponent<Canvas>();

        canvas.worldCamera = playerCamera;
    }

    private void OnEnable()
    {
        if (playerHealth == null)
            playerHealth = FindAnyObjectByType<PlayerHealth>();

        if (playerHealth != null)
        {
            previousHealth = playerHealth.CurrentHealth;

            playerHealth.OnHealthChanged.AddListener(
                OnHealthChanged
            );
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged.RemoveListener(
                OnHealthChanged
            );
        }

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }
    }

    private void CreateFlashOverlay()
    {
        GameObject canvasObject = new GameObject(
            "PlayerDamageFlashCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        canvasObject.transform.SetParent(transform, false);

        Canvas canvas =
            canvasObject.GetComponent<Canvas>();

        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;

        CanvasScaler scaler =
            canvasObject.GetComponent<CanvasScaler>();

        scaler.dynamicPixelsPerUnit = 10f;

        RectTransform canvasRect =
            canvasObject.GetComponent<RectTransform>();

        canvasRect.sizeDelta = new Vector2(1600f, 900f);

        GameObject imageObject = new GameObject(
            "RedDamageFlash",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        imageObject.transform.SetParent(
            canvasObject.transform,
            false
        );

        RectTransform rect =
            imageObject.GetComponent<RectTransform>();

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        flashImage =
            imageObject.GetComponent<Image>();

        flashImage.color = new Color(1f, 0f, 0f, 0f);
        flashImage.raycastTarget = false;
    }

    private void OnHealthChanged(float newHealth)
    {
        if (newHealth < previousHealth)
            TriggerFlash();

        previousHealth = newHealth;
    }

    private void TriggerFlash()
    {
        if (flashImage == null)
            return;

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        float elapsed = 0f;

        flashImage.color = flashColor;

        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;

            float alpha = Mathf.Lerp(
                flashColor.a,
                0f,
                elapsed / flashDuration
            );

            flashImage.color = new Color(
                flashColor.r,
                flashColor.g,
                flashColor.b,
                alpha
            );

            yield return null;
        }

        flashImage.color = new Color(
            flashColor.r,
            flashColor.g,
            flashColor.b,
            0f
        );

        flashCoroutine = null;
    }
}
