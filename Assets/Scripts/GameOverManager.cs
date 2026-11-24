
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class GameOverManager : MonoBehaviour
{
    [Header("References")]
    public PlayerHealth playerHealth;
    public ZombieSpawner zombieSpawner;
    public GameManager gameManager;

    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public TMP_Text finalScoreText;
    public TMP_Text finalWaveText;
    public TMP_Text zombiesDefeatedText;
    public TMP_Text damageTakenText;
    public TMP_Text performanceRatingText;
    public Button retryButton;
    public Button mainMenuButton;

    [Header("Retry Controls")]
    public Key retryKeyboardKey = Key.R;

    [Header("Main Menu Controls")]
    public Key mainMenuKeyboardKey = Key.M;

    private const string HighScoreKey = "ZombieSiegeVR_HighScore";

    private bool gameOver;
    private bool isReloading;
    private bool previousControllerRetryPressed;
    private bool previousControllerMenuPressed;

    private float damageTaken;
    private int zombiesDefeated;

    private UnityEngine.XR.InputDevice rightController;

    public bool IsGameOver => gameOver;

    private void Start()
    {
        if (playerHealth == null)
            playerHealth = FindAnyObjectByType<PlayerHealth>();

        if (zombieSpawner == null)
            zombieSpawner = FindAnyObjectByType<ZombieSpawner>();

        if (gameManager == null)
            gameManager = FindAnyObjectByType<GameManager>();

        if (playerHealth != null)
            playerHealth.OnPlayerDied.AddListener(HandleGameOver);
        else
            Debug.LogError(
                "GameOverManager: PlayerHealth is not assigned."
            );

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        else
            Debug.LogError(
                "GameOverManager: Game Over Panel is not assigned."
            );

        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(RetryGame);
            retryButton.onClick.AddListener(RetryGame);
        }
        else
        {
            Debug.LogWarning(
                "GameOverManager: Retry Button is not assigned."
            );
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }
        else
        {
            Debug.LogWarning(
                "GameOverManager: Main Menu Button is not assigned."
            );
        }
    }

    private void Update()
    {
        if (!gameOver || isReloading)
            return;

        // Keyboard M returns to the main menu.
        if (Keyboard.current != null &&
            Keyboard.current[mainMenuKeyboardKey].wasPressedThisFrame)
        {
            ReturnToMainMenu();
            return;
        }

        // Keyboard R retries the game.
        if (Keyboard.current != null &&
            Keyboard.current[retryKeyboardKey].wasPressedThisFrame)
        {
            RetryGame();
            return;
        }

        // Find the right VR controller.
        if (!rightController.isValid)
        {
            rightController =
                UnityEngine.XR.InputDevices.GetDeviceAtXRNode(
                    UnityEngine.XR.XRNode.RightHand
                );
        }

        if (!rightController.isValid)
            return;

        // B button: Retry.
        bool retryPressed = false;

        rightController.TryGetFeatureValue(
            UnityEngine.XR.CommonUsages.secondaryButton,
            out retryPressed
        );

        if (retryPressed && !previousControllerRetryPressed)
        {
            previousControllerRetryPressed = true;
            RetryGame();
            return;
        }

        previousControllerRetryPressed = retryPressed;

        // A button: Return to Main Menu.
        bool menuPressed = false;

        rightController.TryGetFeatureValue(
            UnityEngine.XR.CommonUsages.primaryButton,
            out menuPressed
        );

        if (menuPressed && !previousControllerMenuPressed)
        {
            previousControllerMenuPressed = true;
            ReturnToMainMenu();
            return;
        }

        previousControllerMenuPressed = menuPressed;
    }

    public void RegisterDamageTaken(float damage)
    {
        if (!gameOver && damage > 0f)
            damageTaken += damage;
    }

    public void RegisterZombieDefeated()
    {
        if (!gameOver)
            zombiesDefeated++;
    }

    private void HandleGameOver()
    {
        if (gameOver)
            return;

        gameOver = true;

        if (zombieSpawner != null)
            zombieSpawner.StopSpawning();

        // Freeze active zombies.
          ZombieController[] zombies =
            FindObjectsByType<ZombieController>();

        foreach (ZombieController zombie in zombies)
        {
            if (zombie == null)
                continue;

            NavMeshAgent agent =
                zombie.GetComponent<NavMeshAgent>();

            if (agent != null && agent.enabled)
            {
                if (agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                    agent.ResetPath();
                }

                agent.enabled = false;
            }

            zombie.enabled = false;

            Animator animator =
                zombie.GetComponentInChildren<Animator>();

            if (animator != null)
                animator.enabled = false;
        }

        // Final score.
        int score = gameManager != null
            ? gameManager.Score
            : 0;

        if (finalScoreText != null)
            finalScoreText.text = "Final Score: " + score;

        // Save the high score if this run beats the previous record.
        int previousHighScore =
            PlayerPrefs.GetInt(HighScoreKey, 0);

        if (score > previousHighScore)
        {
            PlayerPrefs.SetInt(HighScoreKey, score);
            PlayerPrefs.Save();

            Debug.Log("New high score saved: " + score);
        }

        // Wave reached.
        int wave = zombieSpawner != null
            ? zombieSpawner.CurrentWave
            : 0;

        if (finalWaveText != null)
            finalWaveText.text = "Wave Reached: " + wave;

        // Zombies defeated.
        if (zombiesDefeatedText != null)
            zombiesDefeatedText.text =
                "Zombies Defeated: " + zombiesDefeated;

        // Damage received.
        if (damageTakenText != null)
            damageTakenText.text =
                "Damage Taken: " + damageTaken.ToString("F0");

        // Performance rating.
        string rating;

        if (zombiesDefeated >= 20)
            rating = "Elite";
        else if (zombiesDefeated >= 10)
            rating = "Skilled";
        else
            rating = "Novice";

        if (performanceRatingText != null)
            performanceRatingText.text =
                "Combat Rating: " + rating;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        Debug.Log(
            "Game Over: B = Retry, A = Main Menu, " +
            "R = Retry, M = Main Menu."
        );
    }

    public void RetryGame()
    {
        if (isReloading)
            return;

        int sceneIndex = SceneManager.GetActiveScene().buildIndex;

        if (!IsValidSceneIndex(sceneIndex))
            return;

        isReloading = true;

        // Retry should skip the main menu after reloading.
        MainMenuManager.PrepareForRetry();

        SceneManager.LoadScene(sceneIndex);
    }

    public void ReturnToMainMenu()
    {
        if (isReloading)
            return;

        int sceneIndex = SceneManager.GetActiveScene().buildIndex;

        if (!IsValidSceneIndex(sceneIndex))
            return;

        isReloading = true;

        // Show the main menu after reloading.
        MainMenuManager.ResetGameSession();

        SceneManager.LoadScene(sceneIndex);
    }

    private bool IsValidSceneIndex(int sceneIndex)
    {
        if (sceneIndex < 0 ||
            sceneIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError(
                "Scene reload failed: Add the active scene to " +
                "File > Build Profiles > Scene List."
            );

            return false;
        }

        return true;
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnPlayerDied.RemoveListener(HandleGameOver);

        if (retryButton != null)
            retryButton.onClick.RemoveListener(RetryGame);

        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
    }
}
