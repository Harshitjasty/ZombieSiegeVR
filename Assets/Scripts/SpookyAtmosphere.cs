using UnityEngine;

public class SpookyAtmosphere : MonoBehaviour
{
    [Header("Moonlight")]
    public Light moonLight;
    public Color moonColor = new Color(0.56f, 0.65f, 0.78f);
    public float moonIntensity = 0.25f;

    [Header("Fog")]
    public Color fogColor = new Color(0.15f, 0.19f, 0.27f);
    public float fogStart = 5f;
    public float fogEnd = 28f;

    [Header("Mist")]
    public ParticleSystem groundMist;
    public float mistEmission = 4f;
    public float mistLifetime = 10f;
    public float mistSize = 0.5f;
    public float mistSpeed = 0.15f;

    [Header("Moonlight Flicker")]
    public bool enableFlicker = true;
    public float flickerAmount = 0.03f;
    public float flickerSpeed = 1.5f;

    private void Start()
    {
        SetupMoonlight();
        SetupFog();
        SetupMist();
    }

    private void Update()
    {
        if (enableFlicker && moonLight != null)
        {
            float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0f);
            moonLight.intensity = moonIntensity + (noise - 0.5f) * flickerAmount;
        }
    }

    private void SetupMoonlight()
    {
        if (moonLight == null)
            return;

        moonLight.color = moonColor;
        moonLight.intensity = moonIntensity;
    }

    private void SetupFog()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogStartDistance = fogStart;
        RenderSettings.fogEndDistance = fogEnd;
    }

    private void SetupMist()
    {
        if (groundMist == null)
            return;

        var main = groundMist.main;
        main.startLifetime = mistLifetime;
        main.startSpeed = 0f;
        main.startSize = mistSize;

        var emission = groundMist.emission;
        emission.rateOverTime = mistEmission;

        var velocity = groundMist.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = mistSpeed;
        velocity.y = 0f;
        velocity.z = mistSpeed * 0.3f;
    }
}