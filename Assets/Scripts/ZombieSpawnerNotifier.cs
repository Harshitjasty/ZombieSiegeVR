
using UnityEngine;

public class ZombieSpawnerNotifier : MonoBehaviour
{
    private ZombieSpawner spawner;
    private bool deathReported;

    public void SetSpawner(ZombieSpawner newSpawner)
    {
        spawner = newSpawner;
    }

    // Call this when the zombie is defeated by the player.
    public void ReportZombieDefeated()
    {
        if (deathReported)
            return;

        deathReported = true;

        GameOverManager gameOverManager =
            FindAnyObjectByType<GameOverManager>();

        if (gameOverManager != null)
            gameOverManager.RegisterZombieDefeated();

        if (spawner != null)
            spawner.ZombieDied();
    }

    private void OnDestroy()
    {
        // Prevent scene cleanup or other destruction from
        // being counted as a player defeat.
        if (!deathReported && spawner != null)
            spawner.ZombieDied();
    }
}
