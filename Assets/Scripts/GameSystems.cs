using UnityEngine;

public class GameSystems : MonoBehaviour
{
    private void Awake()
    {
        SetupZombieSpawner();
    }

    private void SetupZombieSpawner()
    {
        ZombieSpawner spawner = FindAnyObjectByType<ZombieSpawner>();

        if (spawner != null)
            return;

        GameObject spawnerObject = new GameObject("Zombie_Spawner");
        spawnerObject.transform.position = Vector3.zero;

        spawnerObject.AddComponent<ZombieSpawner>();
    }
}