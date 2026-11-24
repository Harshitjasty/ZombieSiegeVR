
using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;

    public UnityEvent<float> OnHealthChanged;
    public UnityEvent OnPlayerDied;

    private float currentHealth;
    private bool isDead;
    private GameOverManager gameOverManager;

    public float CurrentHealth => currentHealth;
    public bool IsDead => isDead;

    private void Awake()
    {
        ResetHealth();

        gameOverManager =
            FindAnyObjectByType<GameOverManager>();
    }

    public void TakeDamage(float damage)
    {
        if (isDead || damage <= 0f)
            return;

        // Record damage for the performance report.
        if (gameOverManager == null)
        {
            gameOverManager =
                FindAnyObjectByType<GameOverManager>();
        }

        if (gameOverManager != null)
        {
            gameOverManager.RegisterDamageTaken(damage);
        }

        // Update player health.
        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0f);

        OnHealthChanged?.Invoke(currentHealth);

        // End the game when health reaches zero.
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        OnPlayerDied?.Invoke();
    }

    public void ResetHealth()
    {
        isDead = false;
        currentHealth = maxHealth;

        OnHealthChanged?.Invoke(currentHealth);
    }
}
