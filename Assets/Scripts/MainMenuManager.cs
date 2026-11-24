
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.XR;

public class MainMenuManager : MonoBehaviour
{
    [Header("Menu References")]
    public GameObject menuCanvas;
    public Button startGameButton;
    public TMP_Text highScoreText;

    [Header("Game References")]
    public ZombieSpawner zombieSpawner;

    private const string HighScoreKey = "ZombieSiegeVR_HighScore";

    // Tracks whether this scene session has started.
    private static bool gameSessionStarted;

    // Set by RetryGame before the scene reloads.
    private static bool retryingGame;

    private UnityEngine.XR.InputDevice rightController;

    // Reset session state when entering Play mode or launching the app.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        gameSessionStarted = false;
        retryingGame = false;
    }

    private void Start()
    {
        if (menuCanvas == null)
            menuCanvas = gameObject;

        if (zombieSpawner == null)
            zombieSpawner = FindAnyObjectByType<ZombieSpawner>();

        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveListener(StartGame);
            startGameButton.onClick.AddListener(StartGame);
        }

        UpdateHighScoreDisplay();

        if (retryingGame)
        {
            // Retry reload: skip the menu and resume gameplay.
            gameSessionStarted = true;
            menuCanvas.SetActive(false);

            if (zombieSpawner != null)
                zombieSpawner.StartGame();
            else
                Debug.LogError("MainMenuManager: ZombieSpawner was not found.");
        }
        else
        {
            // Fresh launch or return from Game Over: show menu.
            gameSessionStarted = false;
            menuCanvas.SetActive(true);

            if (startGameButton != null)
                startGameButton.interactable = true;
        }

        retryingGame = false;
        FindRightController();
    }

    private void Update()
    {
        if (gameSessionStarted)
            return;

        // Keyboard R starts from the main menu.
        if (Keyboard.current != null &&
            Keyboard.current.rKey.wasPressedThisFrame)
        {
            StartGame();
            return;
        }

        // Right controller B starts from the main menu.
        if (!rightController.isValid)
            FindRightController();

        if (rightController.isValid &&
            rightController.TryGetFeatureValue(
                UnityEngine.XR.CommonUsages.secondaryButton,
                out bool pressed) &&
            pressed)
        {
            StartGame();
        }
    }

    private void FindRightController()
    {
        rightController =
            UnityEngine.XR.InputDevices.GetDeviceAtXRNode(
                UnityEngine.XR.XRNode.RightHand
            );
    }

    public void StartGame()
    {
        if (gameSessionStarted)
            return;

        if (zombieSpawner == null)
            zombieSpawner = FindAnyObjectByType<ZombieSpawner>();

        if (zombieSpawner == null)
        {
            Debug.LogError(
                "MainMenuManager: ZombieSpawner was not found."
            );
            return;
        }

        gameSessionStarted = true;

        if (startGameButton != null)
            startGameButton.interactable = false;

        menuCanvas.SetActive(false);
        zombieSpawner.StartGame();

        Debug.Log("MainMenuManager: Game started.");
    }

    public static void ResetGameSession()
    {
        gameSessionStarted = false;
        retryingGame = false;
    }

    public static void PrepareForRetry()
    {
        gameSessionStarted = true;
        retryingGame = true;
    }

    private void UpdateHighScoreDisplay()
    {
        if (highScoreText == null)
            return;

        int highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        highScoreText.text = "HIGH SCORE: " + highScore;
    }

    private void OnDestroy()
    {
        if (startGameButton != null)
            startGameButton.onClick.RemoveListener(StartGame);
    }
}
