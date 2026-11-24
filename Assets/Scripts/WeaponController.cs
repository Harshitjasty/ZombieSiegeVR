using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

public class WeaponController : MonoBehaviour
{
    [System.Serializable]
    public class WeaponSettings
    {
        public string weaponName;
        public float damage;
        public float range;
        public float fireRate;
        public int magazineSize;
        public int startingReserveAmmo;

        public WeaponSettings(
            string name,
            float weaponDamage,
            float weaponRange,
            float weaponFireRate,
            int magazine,
            int reserve)
        {
            weaponName = name;
            damage = weaponDamage;
            range = weaponRange;
            fireRate = weaponFireRate;
            magazineSize = magazine;
            startingReserveAmmo = reserve;
        }
    }

    [Header("Weapon Configurations")]
    public WeaponSettings[] weapons =
    {
        new WeaponSettings("Pistol", 25f, 50f, 0.25f, 12, 48),
        new WeaponSettings("SMG", 15f, 40f, 0.08f, 30, 90),
        new WeaponSettings("Pulse Rifle", 35f, 60f, 0.35f, 20, 60)
    };

    [Header("Weapon Models")]
    [Tooltip("Order: Pistol, SMG, Pulse Rifle")]
    public GameObject[] weaponModels = new GameObject[3];

    [Header("Shooting")]
    public Camera playerCamera;
    public Transform aimOrigin;
    public InputActionProperty shootAction;

    [Header("Pistol Muzzle Flash")]
    public ParticleSystem pistolMuzzleParticles;
    public Light pistolMuzzleLight;
    public float pistolLightDuration = 0.06f;

    private Coroutine pistolLightCoroutine;

    [Header("SMG Muzzle Flash")]
    public Transform smgAimPoint;
    public ParticleSystem smgMuzzleParticles;
    public Light smgMuzzleLight;
    public float smgLightDuration = 0.04f;

    private Coroutine smgLightCoroutine;

    [Header("Pulse Rifle Projectile")]
    public Transform pulseRifleAimPoint;
    public GameObject pulseProjectilePrefab;

    [Header("Pulse Rifle Muzzle Light")]
    public Light pulseRifleMuzzleLight;
    public float pulseRifleLightDuration = 0.06f;

    private Coroutine pulseRifleLightCoroutine;

    [Header("VR Trigger Settings")]
    [Range(0.1f, 1f)]
    public float triggerPressThreshold = 0.75f;

    [Header("Weapon Haptic Feedback")]
    [Range(0f, 1f)]
    public float pistolHapticIntensity = 0.35f;
    public float pistolHapticDuration = 0.06f;

    [Range(0f, 1f)]
    public float smgHapticIntensity = 0.20f;
    public float smgHapticDuration = 0.04f;

    [Range(0f, 1f)]
    public float pulseRifleHapticIntensity = 0.65f;
    public float pulseRifleHapticDuration = 0.12f;

    [Header("Weapon Audio")]
    public AudioSource pistolAudioSource;
    public AudioClip pistolFireClip;
    public AudioClip pistolReloadClip;

    public AudioSource smgAudioSource;
    public AudioClip smgFireLoopClip;
    public AudioClip smgReloadClip;

    public AudioSource pulseRifleAudioSource;
    public AudioClip pulseRifleFireClip;
    public AudioClip pulseRifleReloadClip;

    private int selectedWeaponIndex;
    private int[] magazineAmmo;
    private int[] reserveAmmo;
    private float nextFireTime;
    private VRHUD hud;

    private UnityEngine.XR.InputDevice rightController;
    private UnityEngine.XR.InputDevice leftController;

    private bool previousA;
    private bool previousB;
    private bool previousX;
    private bool previousTrigger;

    public int CurrentAmmo =>
        magazineAmmo != null &&
        magazineAmmo.Length > selectedWeaponIndex
            ? magazineAmmo[selectedWeaponIndex]
            : 0;

    public int ReserveAmmo =>
        reserveAmmo != null &&
        reserveAmmo.Length > selectedWeaponIndex
            ? reserveAmmo[selectedWeaponIndex]
            : 0;

    private WeaponSettings CurrentWeapon =>
        weapons[selectedWeaponIndex];

    private void Awake()
    {
        if (weapons == null || weapons.Length == 0)
        {
            Debug.LogError(
                "WeaponController: No weapons configured.");

            enabled = false;
            return;
        }

        magazineAmmo = new int[weapons.Length];
        reserveAmmo = new int[weapons.Length];

        for (int i = 0; i < weapons.Length; i++)
        {
            magazineAmmo[i] = weapons[i].magazineSize;
            reserveAmmo[i] = weapons[i].startingReserveAmmo;
        }

        selectedWeaponIndex = 0;
    }

    private void Start()
    {
        hud = FindAnyObjectByType<VRHUD>();

        if (aimOrigin == null && transform.parent != null)
            aimOrigin = transform.parent;

        UpdateWeaponModels();
        UpdateAmmoHUD();

        FindControllers();

        if (aimOrigin == null)
            Debug.LogWarning(
                "WeaponController: Aim Origin is not assigned.");

        if (playerCamera == null)
            Debug.LogWarning(
                "WeaponController: Player Camera is not assigned.");

        if (pistolMuzzleParticles != null)
            pistolMuzzleParticles.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear);

        if (pistolMuzzleLight != null)
            pistolMuzzleLight.enabled = false;

        if (smgMuzzleParticles != null)
            smgMuzzleParticles.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear);

        if (smgMuzzleLight != null)
            smgMuzzleLight.enabled = false;

        if (pulseRifleMuzzleLight != null)
            pulseRifleMuzzleLight.enabled = false;

        if (smgAimPoint == null)
            Debug.LogWarning(
                "WeaponController: SMG Aim Point is not assigned.");

        if (pulseRifleAimPoint == null)
            Debug.LogWarning(
                "WeaponController: Pulse Rifle Aim Point is not assigned.");

        if (pulseProjectilePrefab == null)
            Debug.LogWarning(
                "WeaponController: Pulse Projectile Prefab is not assigned.");

        if (pulseRifleMuzzleLight == null)
            Debug.LogWarning(
                "WeaponController: Pulse Rifle Muzzle Light is not assigned.");
    }

    private void FindControllers()
    {
        rightController =
            UnityEngine.XR.InputDevices.GetDeviceAtXRNode(
                UnityEngine.XR.XRNode.RightHand);

        leftController =
            UnityEngine.XR.InputDevices.GetDeviceAtXRNode(
                UnityEngine.XR.XRNode.LeftHand);
    }

    // ==========================================
    // WEAPON AUDIO
    // ==========================================

    private void PlayPistolFireAudio()
    {
        if (pistolAudioSource != null && pistolFireClip != null)
            pistolAudioSource.PlayOneShot(pistolFireClip);
    }

    private void PlayPulseRifleFireAudio()
    {
        if (pulseRifleAudioSource != null && pulseRifleFireClip != null)
            pulseRifleAudioSource.PlayOneShot(pulseRifleFireClip);
    }

    private void PlaySMGFireAudio()
    {
        if (smgAudioSource != null && smgFireLoopClip != null)
            smgAudioSource.PlayOneShot(smgFireLoopClip);
    }

    private void PlayReloadAudio()
    {
        switch (selectedWeaponIndex)
        {
            case 0:
                if (pistolAudioSource != null && pistolReloadClip != null)
                    pistolAudioSource.PlayOneShot(pistolReloadClip);
                break;

            case 1:
                if (smgAudioSource != null && smgReloadClip != null)
                    smgAudioSource.PlayOneShot(smgReloadClip);
                break;

            case 2:
                if (pulseRifleAudioSource != null && pulseRifleReloadClip != null)
                    pulseRifleAudioSource.PlayOneShot(pulseRifleReloadClip);
                break;
        }
    }

    // ==========================================
    // HAPTIC FEEDBACK
    // ==========================================

    private void PlayWeaponHaptics(bool desktopTest)
    {
        if (desktopTest)
            return;

        if (!rightController.isValid)
            FindControllers();

        if (!rightController.isValid)
            return;

        float intensity;
        float duration;

        switch (selectedWeaponIndex)
        {
            case 0:
                intensity = pistolHapticIntensity;
                duration = pistolHapticDuration;
                break;

            case 1:
                intensity = smgHapticIntensity;
                duration = smgHapticDuration;
                break;

            case 2:
                intensity = pulseRifleHapticIntensity;
                duration = pulseRifleHapticDuration;
                break;

            default:
                return;
        }

        rightController.SendHapticImpulse(0, intensity, duration);
    }

    private void Update()
    {
        HandleDesktopInputs();
        HandleVRControllerInputs();
        HandleVRTriggerShooting();

        // Desktop mouse shooting.
        if (Mouse.current != null)
        {
            bool mouseHeld =
                Mouse.current.leftButton.isPressed;

            bool mousePressed =
                Mouse.current.leftButton.wasPressedThisFrame;

            bool mouseReleased =
                Mouse.current.leftButton.wasReleasedThisFrame;

            if (mouseHeld || mousePressed || mouseReleased)
                HandleShooting(mouseHeld, mousePressed, true);
        }
    }

    private void HandleDesktopInputs()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            SelectWeapon(0);

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
            SelectWeapon(1);

        if (Keyboard.current.digit3Key.wasPressedThisFrame)
            SelectWeapon(2);

        if (Keyboard.current.rKey.wasPressedThisFrame)
            Reload();
    }

    private void HandleVRControllerInputs()
    {
        if (!rightController.isValid || !leftController.isValid)
            FindControllers();

        // Right controller A: next weapon.
        bool aPressed = false;

        if (rightController.isValid)
        {
            rightController.TryGetFeatureValue(
                UnityEngine.XR.CommonUsages.primaryButton,
                out aPressed);
        }

        if (aPressed && !previousA)
            SelectNextWeapon();

        previousA = aPressed;

        // Right controller B: previous weapon.
        bool bPressed = false;

        if (rightController.isValid)
        {
            rightController.TryGetFeatureValue(
                UnityEngine.XR.CommonUsages.secondaryButton,
                out bPressed);
        }

        if (bPressed && !previousB)
            SelectPreviousWeapon();

        previousB = bPressed;

        // Left controller X: reload.
        bool xPressed = false;

        if (leftController.isValid)
        {
            leftController.TryGetFeatureValue(
                UnityEngine.XR.CommonUsages.primaryButton,
                out xPressed);
        }

        if (xPressed && !previousX)
            Reload();

        previousX = xPressed;
    }

    private void HandleVRTriggerShooting()
    {
        if (!rightController.isValid)
            FindControllers();

        if (!rightController.isValid)
            return;

        // Read the analog right controller trigger.
        bool triggerHeld = false;

        if (rightController.TryGetFeatureValue(
            UnityEngine.XR.CommonUsages.trigger,
            out float triggerValue))
        {
            triggerHeld =
                triggerValue >= triggerPressThreshold;
        }

        bool triggerPressed =
            triggerHeld && !previousTrigger;

        previousTrigger = triggerHeld;

        HandleShooting(
            triggerHeld,
            triggerPressed,
            false);
    }

    public void SelectNextWeapon()
    {
        if (weapons == null || weapons.Length == 0)
            return;

        int nextIndex =
            (selectedWeaponIndex + 1) % weapons.Length;

        SelectWeapon(nextIndex);
    }

    public void SelectPreviousWeapon()
    {
        if (weapons == null || weapons.Length == 0)
            return;

        int previousIndex =
            (selectedWeaponIndex - 1 + weapons.Length)
            % weapons.Length;

        SelectWeapon(previousIndex);
    }

    private void HandleShooting(
        bool isHeld,
        bool wasPressed,
        bool desktopTest)
    {
        if (weapons == null ||
            weapons.Length == 0 ||
            selectedWeaponIndex >= weapons.Length)
            return;

        // SMG: automatic fire while holding trigger.
        if (selectedWeaponIndex == 1)
        {
            if (isHeld && CurrentAmmo > 0)
                Shoot(desktopTest);
        }
        // Pistol and Pulse Rifle: one shot per press.
        else if (wasPressed)
        {
            Shoot(desktopTest);
        }
    }

    public void SelectWeapon(int weaponIndex)
    {
        if (weapons == null ||
            weaponIndex < 0 ||
            weaponIndex >= weapons.Length)
        {
            Debug.LogWarning(
                "WeaponController: Invalid weapon index " +
                weaponIndex);

            return;
        }

        selectedWeaponIndex = weaponIndex;
        nextFireTime = 0f;

        UpdateWeaponModels();
        UpdateAmmoHUD();

        Debug.Log(
            "Selected weapon: " + CurrentWeapon.weaponName);
    }

    private void UpdateWeaponModels()
    {
        if (weaponModels == null)
            return;

        for (int i = 0; i < weaponModels.Length; i++)
        {
            if (weaponModels[i] != null)
            {
                weaponModels[i].SetActive(
                    i == selectedWeaponIndex);
            }
        }
    }

    // ==========================================
    // PISTOL MUZZLE FLASH
    // ==========================================

    private void PlayPistolMuzzleFlash()
    {
        if (aimOrigin == null)
        {
            Debug.LogError(
                "Muzzle Flash: Aim Origin is missing!");

            return;
        }

        if (pistolMuzzleParticles != null)
        {
            pistolMuzzleParticles.transform.SetPositionAndRotation(
                aimOrigin.position,
                aimOrigin.rotation
            );

            pistolMuzzleParticles.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );

            pistolMuzzleParticles.Play(true);

            Debug.Log(
                "Pistol muzzle particle position: " +
                pistolMuzzleParticles.transform.position
            );
        }

        if (pistolMuzzleLight != null)
        {
            pistolMuzzleLight.transform.SetPositionAndRotation(
                aimOrigin.position,
                aimOrigin.rotation
            );

            pistolMuzzleLight.enabled = true;

            if (pistolLightCoroutine != null)
                StopCoroutine(pistolLightCoroutine);

            pistolLightCoroutine =
                StartCoroutine(DisablePistolLight());
        }
    }

    private IEnumerator DisablePistolLight()
    {
        yield return new WaitForSeconds(pistolLightDuration);

        if (pistolMuzzleLight != null)
            pistolMuzzleLight.enabled = false;

        pistolLightCoroutine = null;
    }

    // ==========================================
    // SMG MUZZLE FLASH
    // ==========================================

    private void PlaySMGMuzzleFlash()
    {
        if (smgAimPoint == null)
        {
            Debug.LogError(
                "SMG Muzzle Flash: SMG Aim Point is missing!");

            return;
        }

        if (smgMuzzleParticles != null)
        {
            smgMuzzleParticles.transform.SetPositionAndRotation(
                smgAimPoint.position,
                smgAimPoint.rotation
            );

            smgMuzzleParticles.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );

            smgMuzzleParticles.Play(true);
        }

        if (smgMuzzleLight != null)
        {
            smgMuzzleLight.transform.SetPositionAndRotation(
                smgAimPoint.position,
                smgAimPoint.rotation
            );

            smgMuzzleLight.enabled = true;

            if (smgLightCoroutine != null)
                StopCoroutine(smgLightCoroutine);

            smgLightCoroutine =
                StartCoroutine(DisableSMGLight());
        }
    }

    private IEnumerator DisableSMGLight()
    {
        yield return new WaitForSeconds(smgLightDuration);

        if (smgMuzzleLight != null)
            smgMuzzleLight.enabled = false;

        smgLightCoroutine = null;
    }

    // ==========================================
    // PULSE RIFLE MUZZLE LIGHT
    // ==========================================

    private void PlayPulseRifleMuzzleLight()
    {
        if (pulseRifleMuzzleLight == null)
        {
            Debug.LogWarning(
                "Pulse Rifle: Muzzle Light is not assigned.");

            return;
        }

        if (pulseRifleAimPoint != null)
        {
            pulseRifleMuzzleLight.transform.SetPositionAndRotation(
                pulseRifleAimPoint.position,
                pulseRifleAimPoint.rotation
            );
        }

        pulseRifleMuzzleLight.enabled = true;

        if (pulseRifleLightCoroutine != null)
            StopCoroutine(pulseRifleLightCoroutine);

        pulseRifleLightCoroutine =
            StartCoroutine(DisablePulseRifleMuzzleLight());
    }

    private IEnumerator DisablePulseRifleMuzzleLight()
    {
        yield return new WaitForSeconds(pulseRifleLightDuration);

        if (pulseRifleMuzzleLight != null)
            pulseRifleMuzzleLight.enabled = false;

        pulseRifleLightCoroutine = null;
    }

    // ==========================================
    // PULSE RIFLE PROJECTILE
    // ==========================================

    private void PlayPulseProjectile(Vector3 direction)
    {
        if (pulseRifleAimPoint == null)
        {
            Debug.LogError(
                "Pulse Rifle: Aim Point is missing!");

            return;
        }

        if (pulseProjectilePrefab == null)
        {
            Debug.LogError(
                "Pulse Rifle: Projectile Prefab is missing!");

            return;
        }

        GameObject projectile = Instantiate(
            pulseProjectilePrefab,
            pulseRifleAimPoint.position,
            pulseRifleAimPoint.rotation
        );

        PulseProjectile projectileScript =
            projectile.GetComponent<PulseProjectile>();

        if (projectileScript == null)
        {
            Debug.LogError(
                "Pulse Rifle: PulseProjectile component is missing from the prefab!");

            Destroy(projectile);
            return;
        }

        projectileScript.Launch(direction, CurrentWeapon.damage);
    }

    // ==========================================
    // SHOOTING
    // ==========================================

    private void Shoot(bool desktopTest)
    {
        if (Time.time < nextFireTime)
            return;

        if (CurrentAmmo <= 0)
        {
            Debug.Log(
                CurrentWeapon.weaponName +
                " magazine empty. Press reload.");

            return;
        }

        Vector3 rayOrigin;
        Vector3 rayDirection;

        if (desktopTest && playerCamera != null)
        {
            rayOrigin = playerCamera.transform.position;
            rayDirection = playerCamera.transform.forward;
        }
        else
        {
            if (aimOrigin == null)
            {
                Debug.LogWarning(
                    "WeaponController: Aim Origin is not assigned.");

                return;
            }

            rayOrigin = aimOrigin.position;
            rayDirection = aimOrigin.forward;
        }

        nextFireTime =
            Time.time + CurrentWeapon.fireRate;

        magazineAmmo[selectedWeaponIndex]--;
        UpdateAmmoHUD();

        // Trigger weapon-specific haptic feedback.
        PlayWeaponHaptics(desktopTest);

        // Weapon firing audio.
        if (selectedWeaponIndex == 0)
            PlayPistolFireAudio();
        else if (selectedWeaponIndex == 1)
            PlaySMGFireAudio();
        else if (selectedWeaponIndex == 2)
            PlayPulseRifleFireAudio();

        // Pistol muzzle flash.
        if (selectedWeaponIndex == 0)
            PlayPistolMuzzleFlash();

        // SMG muzzle flash.
        if (selectedWeaponIndex == 1)
            PlaySMGMuzzleFlash();

        // Pulse Rifle muzzle light and travelling projectile.
        if (selectedWeaponIndex == 2)
        {
            PlayPulseRifleMuzzleLight();
            PlayPulseProjectile(rayDirection);
        }

        Ray ray = new Ray(rayOrigin, rayDirection);

        Debug.DrawRay(
            ray.origin,
            ray.direction * CurrentWeapon.range,
            Color.red,
            1f);

        // Pistol and SMG use raycast damage.
        // Pulse Rifle uses projectile collision damage instead.
        if (selectedWeaponIndex != 2)
        {
            if (Physics.Raycast(
                ray,
                out RaycastHit hit,
                CurrentWeapon.range))
            {
                ZombieController zombie =
                    hit.collider.GetComponentInParent<ZombieController>();

                if (zombie != null)
                {
                    zombie.TakeDamage(CurrentWeapon.damage);

                    Debug.Log(
                        CurrentWeapon.weaponName +
                        " hit zombie! Damage: " +
                        CurrentWeapon.damage);
                }
                else
                {
                    Debug.Log(
                        "Shot hit: " + hit.collider.name);
                }
            }
            else
            {
                Debug.Log("Shot missed: no collider detected.");
            }
        }
    }

    // ==========================================
    // RELOADING
    // ==========================================

    public void Reload()
    {
        WeaponSettings weapon = CurrentWeapon;

        if (CurrentAmmo >= weapon.magazineSize)
            return;

        if (ReserveAmmo <= 0)
            return;

        int neededAmmo =
            weapon.magazineSize - CurrentAmmo;

        int ammoToLoad =
            Mathf.Min(neededAmmo, ReserveAmmo);

        magazineAmmo[selectedWeaponIndex] += ammoToLoad;
        reserveAmmo[selectedWeaponIndex] -= ammoToLoad;

        UpdateAmmoHUD();

        PlayReloadAudio();

        Debug.Log(
            "Reloaded " + weapon.weaponName +
            ". Ammo: " + CurrentAmmo +
            " / Reserve: " + ReserveAmmo);
    }

    private void UpdateAmmoHUD()
    {
        if (hud != null &&
            weapons != null &&
            weapons.Length > selectedWeaponIndex)
        {
            hud.UpdateAmmo(
                CurrentWeapon.weaponName,
                CurrentAmmo,
                ReserveAmmo);
        }
    }
}