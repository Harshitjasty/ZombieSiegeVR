
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class ZombieSpawner : MonoBehaviour
{
    [Header("Zombie Prefabs")]
    public GameObject walkerPrefab;
    public GameObject runnerPrefab;

    [Header("Wave Settings")]
    public int startingWalkers = 4;
    public int runnersPerWave = 1;
    public float timeBetweenSpawns = 1.5f;
    public float timeBetweenWaves = 5f;

    [Header("Spawn Area")]
    public float spawnDistance = 15f;

    private int currentWave = 0;
    private int zombiesAlive = 0;
    private bool spawningWave;
    private bool gameOver;
    private bool gameStarted;
    private Coroutine waveCoroutine;
    private VRHUD hud;

    public int CurrentWave => currentWave;
    public int ZombiesAlive => zombiesAlive;
    public bool IsGameOver => gameOver;

    private void Start()
    {
        hud = FindAnyObjectByType<VRHUD>();
        UpdateWaveHUD();

        // Waves now wait for the Start Game button.
    }

    public void StartGame()
    {
        if (gameStarted || gameOver)
            return;

        gameStarted = true;
        waveCoroutine = StartCoroutine(StartNextWave());

        Debug.Log("ZombieSpawner: Game started.");
    }

    private IEnumerator StartNextWave()
    {
        if (!gameStarted || spawningWave || gameOver)
            yield break;

        spawningWave = true;
        currentWave++;

        UpdateWaveHUD();

        int walkerCount =
            startingWalkers + ((currentWave - 1) * 2);

        int runnerCount =
            currentWave * runnersPerWave;

        for (int i = 0; i < walkerCount; i++)
        {
            if (gameOver)
                break;

            if (SpawnZombie(walkerPrefab))
                zombiesAlive++;

            UpdateWaveHUD();

            yield return new WaitForSeconds(timeBetweenSpawns);
        }

        for (int i = 0; i < runnerCount; i++)
        {
            if (gameOver)
                break;

            if (SpawnZombie(runnerPrefab))
                zombiesAlive++;

            UpdateWaveHUD();

            yield return new WaitForSeconds(timeBetweenSpawns);
        }

        spawningWave = false;
        waveCoroutine = null;

        if (gameStarted && !gameOver && zombiesAlive <= 0)
            waveCoroutine = StartCoroutine(WaitAndStartNextWave());
    }

    private bool SpawnZombie(GameObject prefab)
    {
        if (gameOver || prefab == null)
            return false;

        Vector3 spawnPosition = GetSpawnPosition();

        if (!IsValidPosition(spawnPosition))
        {
            Debug.LogWarning(
                "ZombieSpawner: Could not find a valid spawn position."
            );
            return false;
        }

        GameObject zombie = Instantiate(
            prefab,
            spawnPosition,
            Quaternion.identity
        );

        ZombieController controller =
            zombie.GetComponent<ZombieController>();

        if (controller == null)
            controller = zombie.AddComponent<ZombieController>();

        ZombieSpawnerNotifier notifier =
            zombie.GetComponent<ZombieSpawnerNotifier>();

        if (notifier == null)
            notifier = zombie.AddComponent<ZombieSpawnerNotifier>();

        notifier.SetSpawner(this);

        return true;
    }

    private Vector3 GetSpawnPosition()
    {
        GameObject defenseObject =
            GameObject.Find("Player_Defense_Point");

        Vector3 destination = defenseObject != null
            ? defenseObject.transform.position
            : transform.position;

        bool hasDestination = NavMesh.SamplePosition(
            destination,
            out NavMeshHit destinationHit,
            3f,
            NavMesh.AllAreas
        );

        if (!hasDestination)
            return Vector3.positiveInfinity;

        for (int attempt = 0; attempt < 30; attempt++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);

            Vector3 direction = new Vector3(
                Mathf.Cos(angle),
                0f,
                Mathf.Sin(angle)
            );

            Vector3 candidate =
                transform.position + direction * spawnDistance;

            if (!NavMesh.SamplePosition(
                candidate,
                out NavMeshHit spawnHit,
                3f,
                NavMesh.AllAreas))
            {
                continue;
            }

            NavMeshPath path = new NavMeshPath();

            bool pathFound = NavMesh.CalculatePath(
                spawnHit.position,
                destinationHit.position,
                NavMesh.AllAreas,
                path
            );

            if (pathFound &&
                path.status == NavMeshPathStatus.PathComplete)
            {
                return spawnHit.position;
            }
        }

        return Vector3.positiveInfinity;
    }

    private bool IsValidPosition(Vector3 position)
    {
        return
            float.IsFinite(position.x) &&
            float.IsFinite(position.y) &&
            float.IsFinite(position.z);
    }

    public void StopSpawning()
    {
        if (gameOver)
            return;

        gameOver = true;

        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
            waveCoroutine = null;
        }

        spawningWave = false;

        UpdateWaveHUD();

        Debug.Log("ZombieSpawner: Spawning stopped because the game ended.");
    }

    public void ZombieDied()
    {
        zombiesAlive = Mathf.Max(0, zombiesAlive - 1);

        UpdateWaveHUD();

        if (gameStarted && !gameOver &&
            zombiesAlive <= 0 && !spawningWave)
        {
            waveCoroutine = StartCoroutine(WaitAndStartNextWave());
        }
    }

    private IEnumerator WaitAndStartNextWave()
    {
        yield return new WaitForSeconds(timeBetweenWaves);

        if (gameStarted && !gameOver &&
            zombiesAlive <= 0 && !spawningWave)
        {
            waveCoroutine = StartCoroutine(StartNextWave());
        }
    }

    private void UpdateWaveHUD()
    {
        if (hud == null)
            hud = FindAnyObjectByType<VRHUD>();

        if (hud != null)
            hud.UpdateWave(currentWave, zombiesAlive);
    }
}
