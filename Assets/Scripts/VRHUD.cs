
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VRHUD : MonoBehaviour
{
    [Header("Wrist HUD Settings")]
    public Transform leftHandAnchor;

    public Vector3 wristLocalPosition =
        new Vector3(0f, 0.12f, 0.08f);

    public Vector3 wristLocalRotation =
        new Vector3(45f, 0f, 0f);

    public float wristCanvasScale = 0.001f;

    [Header("HUD Appearance")]
    public Color panelColor =
        new Color(0.02f, 0.04f, 0.08f, 0.9f);

    public Color textColor = Color.white;

    public Color healthColor =
        new Color(0.2f, 0.9f, 0.35f);

    public Color ammoColor =
        new Color(1f, 0.8f, 0.2f);

    private TextMeshProUGUI healthText;
    private TextMeshProUGUI weaponText;
    private TextMeshProUGUI ammoText;
    private TextMeshProUGUI waveText;
    private TextMeshProUGUI scoreText;

    private Image healthBarFill;
    private RectTransform healthBarFillRect;
    private RectTransform healthBarBackgroundRect;

    private PlayerHealth playerHealth;

    private void Start()
    {
        Canvas canvas = GetComponent<Canvas>();

        if (canvas == null)
        {
            Debug.LogError("VRHUD must be attached to a Canvas.");
            enabled = false;
            return;
        }

        FindLeftHand();

        if (leftHandAnchor == null)
        {
            Debug.LogError(
                "VRHUD: Left Controller was not found. " +
                "Assign Left Hand Anchor in the Inspector.");

            enabled = false;
            return;
        }

        SetupWristCanvas(canvas);

        CreateHUD();

        playerHealth = FindAnyObjectByType<PlayerHealth>();

        if (playerHealth != null)
        {
            UpdateHealth(
                playerHealth.CurrentHealth,
                playerHealth.maxHealth
            );

            playerHealth.OnHealthChanged.AddListener(
                OnPlayerHealthChanged
            );
        }
        else
        {
            Debug.LogWarning(
                "VRHUD: PlayerHealth was not found.");
        }
    }

    private void FindLeftHand()
    {
        if (leftHandAnchor != null)
            return;

        GameObject leftController =
            GameObject.Find("Left Controller");

        if (leftController != null)
            leftHandAnchor = leftController.transform;
    }

    private void SetupWristCanvas(Canvas canvas)
    {
        // Convert the existing HUD Canvas to World Space.
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;

        // Attach the HUD to the left controller.
        transform.SetParent(leftHandAnchor, false);

        RectTransform canvasRect =
            GetComponent<RectTransform>();

        canvasRect.sizeDelta = new Vector2(600f, 300f);

        transform.localPosition = wristLocalPosition;
        transform.localRotation =
            Quaternion.Euler(wristLocalRotation);

        transform.localScale =
            Vector3.one * wristCanvasScale;
    }

    private void CreateHUD()
    {
        Transform oldPanel = transform.Find("HUD_Panel");

        if (oldPanel != null)
            Destroy(oldPanel.gameObject);

        GameObject panelObject = new GameObject(
            "HUD_Panel",
            typeof(RectTransform),
            typeof(Image)
        );

        panelObject.transform.SetParent(transform, false);

        RectTransform panelRect =
            panelObject.GetComponent<RectTransform>();

        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage =
            panelObject.GetComponent<Image>();

        panelImage.color = panelColor;
        panelImage.raycastTarget = false;

        healthText = CreateText(
            panelObject.transform,
            "Health_Text",
            "HEALTH 100 / 100",
            new Vector2(0.02f, 0.72f),
            new Vector2(0.48f, 0.98f),
            healthColor,
            32
        );

        weaponText = CreateText(
            panelObject.transform,
            "Weapon_Text",
            "WEAPON PISTOL",
            new Vector2(0.52f, 0.72f),
            new Vector2(0.98f, 0.98f),
            textColor,
            30
        );

        ammoText = CreateText(
            panelObject.transform,
            "Ammo_Text",
            "AMMO 12 / 48",
            new Vector2(0.52f, 0.36f),
            new Vector2(0.98f, 0.68f),
            ammoColor,
            34
        );

        waveText = CreateText(
            panelObject.transform,
            "Wave_Text",
            "WAVE 1 | ZOMBIES 0",
            new Vector2(0.02f, 0.36f),
            new Vector2(0.48f, 0.68f),
            textColor,
            27
        );

        scoreText = CreateText(
            panelObject.transform,
            "Score_Text",
            "SCORE 0",
            new Vector2(0.02f, 0.14f),
            new Vector2(0.98f, 0.32f),
            textColor,
            28
        );

        CreateHealthBar(panelObject.transform);
    }

    private TextMeshProUGUI CreateText(
        Transform parent,
        string objectName,
        string initialText,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Color color,
        float fontSize)
    {
        GameObject textObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );

        textObject.transform.SetParent(parent, false);

        RectTransform rect =
            textObject.GetComponent<RectTransform>();

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;

        rect.offsetMin = new Vector2(8f, 3f);
        rect.offsetMax = new Vector2(-8f, -3f);

        TextMeshProUGUI text =
            textObject.GetComponent<TextMeshProUGUI>();

        text.text = initialText;
        text.color = color;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;

        text.enableAutoSizing = true;
        text.fontSizeMin = 18;
        text.fontSizeMax = fontSize;

        text.raycastTarget = false;

        return text;
    }

    private void CreateHealthBar(Transform parent)
    {
        GameObject barObject = new GameObject(
            "Health_Bar",
            typeof(RectTransform),
            typeof(Image)
        );

        barObject.transform.SetParent(parent, false);

        healthBarBackgroundRect =
            barObject.GetComponent<RectTransform>();

        healthBarBackgroundRect.anchorMin =
            new Vector2(0.05f, 0.025f);

        healthBarBackgroundRect.anchorMax =
            new Vector2(0.95f, 0.11f);

        healthBarBackgroundRect.offsetMin = Vector2.zero;
        healthBarBackgroundRect.offsetMax = Vector2.zero;

        Image background =
            barObject.GetComponent<Image>();

        background.color = Color.black;
        background.raycastTarget = false;

        GameObject fillObject = new GameObject(
            "Health_Bar_Fill",
            typeof(RectTransform),
            typeof(Image)
        );

        fillObject.transform.SetParent(barObject.transform, false);

        healthBarFillRect =
            fillObject.GetComponent<RectTransform>();

        healthBarFillRect.anchorMin = new Vector2(0f, 0f);
        healthBarFillRect.anchorMax = new Vector2(0f, 1f);
        healthBarFillRect.pivot = new Vector2(0f, 0.5f);

        healthBarFillRect.anchoredPosition = Vector2.zero;
        healthBarFillRect.sizeDelta = Vector2.zero;

        healthBarFill =
            fillObject.GetComponent<Image>();

        healthBarFill.color = healthColor;
        healthBarFill.raycastTarget = false;

        SetHealthBarWidth(1f);
    }

    private void SetHealthBarWidth(float healthPercent)
    {
        if (healthBarFillRect == null ||
            healthBarBackgroundRect == null)
            return;

        healthPercent = Mathf.Clamp01(healthPercent);

        float fullWidth =
            healthBarBackgroundRect.rect.width;

        healthBarFillRect.sizeDelta = new Vector2(
            fullWidth * healthPercent,
            0f
        );
    }

    public void UpdateHealth(
        float currentHealth,
        float maxHealth)
    {
        float healthPercent = maxHealth > 0f
            ? Mathf.Clamp01(currentHealth / maxHealth)
            : 0f;

        if (healthText != null)
        {
            healthText.text =
                $"HEALTH {currentHealth:0} / {maxHealth:0}";
        }

        SetHealthBarWidth(healthPercent);
    }

    private void OnPlayerHealthChanged(float currentHealth)
    {
        if (playerHealth != null)
        {
            UpdateHealth(
                currentHealth,
                playerHealth.maxHealth
            );
        }
    }

    public void UpdateAmmo(
        string weaponName,
        int magazine,
        int reserve)
    {
        if (weaponText != null)
            weaponText.text = "WEAPON " + weaponName.ToUpper();

        if (ammoText != null)
            ammoText.text = $"AMMO {magazine} / {reserve}";
    }

    public void UpdateWave(int wave, int zombiesRemaining)
    {
        if (waveText != null)
        {
            waveText.text =
                $"WAVE {wave} | ZOMBIES {zombiesRemaining}";
        }
    }

    public void UpdateScore(int score)
    {
        if (scoreText != null)
            scoreText.text = "SCORE " + score;
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged.RemoveListener(
                OnPlayerHealthChanged
            );
        }
    }
}
