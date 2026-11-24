
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class ZombieController : MonoBehaviour
{
    [Header("Zombie Stats")]
    public float maxHealth = 100f;
    public float moveSpeed = 2f;
    public float animationSpeed = 0.8f;
    public float attackDistance = 1.5f;
    public float attackDamage = 10f;
    public float attackCooldown = 1.5f;

    [Header("Zombie Type Defaults")]
    public float walkerSpeed = 2f;
    public float runnerSpeed = 3.5f;

    [Header("Navigation Recovery")]
    public float destinationRefreshInterval = 0.5f;
    public float stuckCheckInterval = 2f;
    public float minimumMovementDistance = 0.15f;
    public float navMeshSearchRadius = 2f;

    [Header("Score")]
    public int pointsForKilling = 10;

    [Header("Damage Flash")]
    public Color hitFlashColor = Color.red;
    [Range(0.05f, 0.5f)]
    public float hitFlashDuration = 0.3f;
    [Range(1f, 8f)]
    public float emissionIntensity = 3f;

    [Header("Zombie Audio")]
    public AudioSource zombieAudioSource;
    public AudioClip walkerIdleClip;
    public AudioClip walkerAttackClip;
    public AudioClip walkerHurtClip;
    public AudioClip walkerDeathClip;
    public AudioClip runnerIdleClip;
    public AudioClip runnerAttackClip;
    public AudioClip runnerHurtClip;
    public AudioClip runnerDeathClip;
    public float minimumIdleSoundInterval = 5f;
    public float maximumIdleSoundInterval = 12f;
    public float audioMaximumDistance = 18f;

    private float currentHealth;
    private float nextAttackTime;
    private float nextDestinationRefresh;
    private float nextStuckCheck;

    private Vector3 lastPosition;
    private Transform defensePoint;
    private PlayerHealth playerHealth;
    private NavMeshAgent agent;
    private Animator animator;
    private bool isDead;

    private Renderer[] renderers;
    private Material[][] runtimeMaterials;
    private MaterialState[][] originalMaterialStates;
    private Coroutine hitFlashCoroutine;
    private float nextIdleSoundTime;

    private AudioClip idleClip;
    private AudioClip attackClip;
    private AudioClip hurtClip;
    private AudioClip deathClip;

    private static readonly int BaseColorId =
        Shader.PropertyToID("_BaseColor");

    private static readonly int ColorId =
        Shader.PropertyToID("_Color");

    private static readonly int EmissionColorId =
        Shader.PropertyToID("_EmissionColor");

    private class MaterialState
    {
        public Color baseColor;
        public Color emissionColor;
        public bool hasBaseColor;
        public bool hasColor;
        public bool hasEmission;
        public bool emissionWasEnabled;
    }

    public float CurrentHealth => currentHealth;
    public bool IsDead => isDead;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        renderers = GetComponentsInChildren<Renderer>(true);
        runtimeMaterials = new Material[renderers.Length][];
        originalMaterialStates =
            new MaterialState[renderers.Length][];

        CacheOriginalMaterials();
        SetupZombieAudio();

        if (CompareTag("Runner"))
        {
            moveSpeed = runnerSpeed;
            pointsForKilling = 25;
        }
        else if (CompareTag("Walker"))
        {
            moveSpeed = walkerSpeed;
            pointsForKilling = 10;
        }

        agent.speed = moveSpeed;
        agent.stoppingDistance = attackDistance;
        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.isStopped = false;

        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.speed = animationSpeed;
        }

        currentHealth = maxHealth;
        lastPosition = transform.position;
    }

    private void SetupZombieAudio()
    {
        if (CompareTag("Runner"))
        {
            idleClip = runnerIdleClip;
            attackClip = runnerAttackClip;
            hurtClip = runnerHurtClip;
            deathClip = runnerDeathClip;
        }
        else if (CompareTag("Walker"))
        {
            idleClip = walkerIdleClip;
            attackClip = walkerAttackClip;
            hurtClip = walkerHurtClip;
            deathClip = walkerDeathClip;
        }

        if (zombieAudioSource == null)
            zombieAudioSource = GetComponent<AudioSource>();

        if (zombieAudioSource == null)
            zombieAudioSource = gameObject.AddComponent<AudioSource>();

        zombieAudioSource.playOnAwake = false;
        zombieAudioSource.loop = false;
        zombieAudioSource.spatialBlend = 1f;
        zombieAudioSource.dopplerLevel = 0f;
        zombieAudioSource.minDistance = 1f;
        zombieAudioSource.maxDistance = audioMaximumDistance;
        zombieAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;

        ScheduleNextIdleSound();
    }

    private void ScheduleNextIdleSound()
    {
        float minimum = Mathf.Max(0.1f, minimumIdleSoundInterval);
        float maximum = Mathf.Max(minimum, maximumIdleSoundInterval);

        nextIdleSoundTime =
            Time.time + Random.Range(minimum, maximum);
    }

    private void TryPlayIdleSound()
    {
        if (idleClip == null ||
            zombieAudioSource == null ||
            zombieAudioSource.isPlaying ||
            Time.time < nextIdleSoundTime)
            return;

        zombieAudioSource.PlayOneShot(idleClip);
        ScheduleNextIdleSound();
    }

    private void PlayZombieSound(AudioClip clip)
    {
        if (clip == null || zombieAudioSource == null)
            return;

        zombieAudioSource.Stop();
        zombieAudioSource.PlayOneShot(clip);
    }

    private void CacheOriginalMaterials()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null)
                continue;

            // Create material instances for this zombie only.
            // This prevents the flash changing other zombies.
            runtimeMaterials[i] = renderer.materials;

            originalMaterialStates[i] =
                new MaterialState[runtimeMaterials[i].Length];

            for (int j = 0; j < runtimeMaterials[i].Length; j++)
            {
                Material material = runtimeMaterials[i][j];

                if (material == null)
                    continue;

                MaterialState state = new MaterialState();

                state.hasBaseColor =
                    material.HasProperty(BaseColorId);

                state.hasColor =
                    material.HasProperty(ColorId);

                state.hasEmission =
                    material.HasProperty(EmissionColorId);

                if (state.hasBaseColor)
                    state.baseColor =
                        material.GetColor(BaseColorId);
                else if (state.hasColor)
                    state.baseColor =
                        material.GetColor(ColorId);

                if (state.hasEmission)
                {
                    state.emissionColor =
                        material.GetColor(EmissionColorId);

                    state.emissionWasEnabled =
                        material.IsKeywordEnabled("_EMISSION");
                }

                originalMaterialStates[i][j] = state;
            }
        }
    }

    public void FlashRed()
    {
        if (isDead)
            return;

        if (hitFlashCoroutine != null)
            StopCoroutine(hitFlashCoroutine);

        hitFlashCoroutine = StartCoroutine(PlayHitFlash());
    }

    private IEnumerator PlayHitFlash()
    {
        for (int i = 0; i < runtimeMaterials.Length; i++)
        {
            if (runtimeMaterials[i] == null)
                continue;

            for (int j = 0; j < runtimeMaterials[i].Length; j++)
            {
                Material material = runtimeMaterials[i][j];

                if (material == null)
                    continue;

                MaterialState state =
                    originalMaterialStates[i][j];

                if (state == null)
                    continue;

                // Set the surface to vivid red.
                if (state.hasBaseColor)
                    material.SetColor(BaseColorId, hitFlashColor);

                if (state.hasColor)
                    material.SetColor(ColorId, hitFlashColor);

                // Add red emission where the shader supports it.
                if (state.hasEmission)
                {
                    material.EnableKeyword("_EMISSION");

                    material.SetColor(
                        EmissionColorId,
                        hitFlashColor * emissionIntensity
                    );
                }
            }
        }

        yield return new WaitForSeconds(hitFlashDuration);

        RestoreOriginalColors();
        hitFlashCoroutine = null;
    }

    private void RestoreOriginalColors()
    {
        for (int i = 0; i < runtimeMaterials.Length; i++)
        {
            if (runtimeMaterials[i] == null ||
                originalMaterialStates[i] == null)
                continue;

            for (int j = 0; j < runtimeMaterials[i].Length; j++)
            {
                Material material = runtimeMaterials[i][j];

                if (material == null)
                    continue;

                MaterialState state =
                    originalMaterialStates[i][j];

                if (state == null)
                    continue;

                if (state.hasBaseColor)
                    material.SetColor(
                        BaseColorId,
                        state.baseColor
                    );

                if (state.hasColor)
                    material.SetColor(
                        ColorId,
                        state.baseColor
                    );

                if (state.hasEmission)
                {
                    material.SetColor(
                        EmissionColorId,
                        state.emissionColor
                    );

                    if (state.emissionWasEnabled)
                        material.EnableKeyword("_EMISSION");
                    else
                        material.DisableKeyword("_EMISSION");
                }
            }
        }
    }

    private void Start()
    {
        GameObject defenseObject =
            GameObject.Find("Player_Defense_Point");

        if (defenseObject != null)
            defensePoint = defenseObject.transform;
        else
            Debug.LogWarning(
                "ZombieController: Player_Defense_Point was not found.",
                this
            );

        playerHealth = FindAnyObjectByType<PlayerHealth>();

        if (playerHealth == null)
            Debug.LogWarning(
                "ZombieController: PlayerHealth was not found.",
                this
            );

        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(
                transform.position,
                out NavMeshHit hit,
                navMeshSearchRadius,
                NavMesh.AllAreas))
            {
                if (!agent.Warp(hit.position))
                {
                    Debug.LogWarning(
                        "ZombieController: Could not place agent on NavMesh.",
                        this
                    );
                    return;
                }
            }
            else
            {
                Debug.LogWarning(
                    "ZombieController: No NavMesh found near spawn position.",
                    this
                );
                return;
            }
        }

        lastPosition = transform.position;
        RefreshDestination();
    }

    private void Update()
    {
        if (isDead || agent == null || !agent.enabled)
            return;

        if (defensePoint == null ||
            playerHealth == null ||
            playerHealth.IsDead)
        {
            agent.isStopped = true;
            return;
        }

        if (!agent.isOnNavMesh)
        {
            RecoverAgentPosition();
            return;
        }

        float distance = Vector3.Distance(
            transform.position,
            defensePoint.position
        );

        if (distance <= attackDistance)
        {
            agent.isStopped = true;
            AttackPlayer();
            return;
        }

        TryPlayIdleSound();

        agent.isStopped = false;

        if (Time.time >= nextDestinationRefresh)
            RefreshDestination();

        if (Time.time >= nextStuckCheck)
            CheckMovementProgress();
    }

    private void RefreshDestination()
    {
        nextDestinationRefresh =
            Time.time + destinationRefreshInterval;

        if (agent == null ||
            !agent.enabled ||
            !agent.isOnNavMesh ||
            defensePoint == null)
            return;

        NavMeshPath path = new NavMeshPath();

        bool foundPath = agent.CalculatePath(
            defensePoint.position,
            path
        );

        if (foundPath &&
            path.status == NavMeshPathStatus.PathComplete)
        {
            agent.SetPath(path);
        }
        else
        {
            Debug.LogWarning(
                "ZombieController: No complete path to defense point. " +
                "Check NavMesh coverage and obstacles.",
                this
            );
        }
    }

    private void CheckMovementProgress()
    {
        nextStuckCheck = Time.time + stuckCheckInterval;

        float movedDistance = Vector3.Distance(
            transform.position,
            lastPosition
        );

        lastPosition = transform.position;

        if (agent.velocity.magnitude > 0.05f &&
            movedDistance < minimumMovementDistance)
        {
            Debug.LogWarning(
                "ZombieController: Agent appears stuck. " +
                "Checking navigation path again.",
                this
            );

            RefreshDestination();
        }
    }

    private void RecoverAgentPosition()
    {
        if (NavMesh.SamplePosition(
            transform.position,
            out NavMeshHit hit,
            navMeshSearchRadius,
            NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            agent.isStopped = false;
            RefreshDestination();
        }
    }

    private void AttackPlayer()
    {
        if (Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + attackCooldown;

        PlayZombieSound(attackClip);
        playerHealth.TakeDamage(attackDamage);
    }

    public void TakeDamage(float damage)
    {
        if (isDead || damage <= 0f)
            return;

        FlashRed();

        currentHealth -= damage;

        if (currentHealth <= 0f)
            Die();
        else
            PlayZombieSound(hurtClip);
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        if (hitFlashCoroutine != null)
        {
            StopCoroutine(hitFlashCoroutine);
            hitFlashCoroutine = null;
        }

        RestoreOriginalColors();

        GameManager gameManager =
            FindAnyObjectByType<GameManager>();

        if (gameManager != null)
            gameManager.AddScore(pointsForKilling);

        ZombieSpawnerNotifier notifier =
            GetComponent<ZombieSpawnerNotifier>();

        if (notifier != null)
        {
            notifier.ReportZombieDefeated();
        }
        else
        {
            Debug.LogWarning(
                "ZombieController: ZombieSpawnerNotifier is missing.",
                this
            );
        }

        if (agent != null && agent.enabled)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            agent.enabled = false;
        }

        if (zombieAudioSource != null && deathClip != null)
        {
            zombieAudioSource.Stop();
            zombieAudioSource.PlayOneShot(deathClip);
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // Release the material instances created for this zombie.
        if (runtimeMaterials == null)
            return;

        foreach (Material[] materials in runtimeMaterials)
        {
            if (materials == null)
                continue;

            foreach (Material material in materials)
            {
                if (material != null)
                    Destroy(material);
            }
        }
    }
}
