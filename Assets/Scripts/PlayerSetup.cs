using UnityEngine;

public class PlayerSetup : MonoBehaviour
{
    [Header("Player Settings")]
    public float maxHealth = 100f;

    [Header("Defense Point")]
    public Vector3 defensePointPosition = Vector3.zero;

    private PlayerHealth playerHealth;

    private void Awake()
    {
        SetupHealth();
        SetupDefensePoint();
    }

    private void SetupHealth()
    {
        playerHealth = GetComponent<PlayerHealth>();

        if (playerHealth == null)
        {
            playerHealth = gameObject.AddComponent<PlayerHealth>();
        }

        playerHealth.maxHealth = maxHealth;
    }

    private void SetupDefensePoint()
    {
        GameObject defensePoint = GameObject.Find("Player_Defense_Point");

        if (defensePoint == null)
        {
            defensePoint = new GameObject("Player_Defense_Point");
        }

        defensePoint.transform.position = defensePointPosition;
    }
}