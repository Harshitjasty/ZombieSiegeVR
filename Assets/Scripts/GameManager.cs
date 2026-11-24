
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Score")]
    [SerializeField] private int score = 0;

    private VRHUD hud;

    public int Score => score;

    private void Start()
    {
        hud = FindAnyObjectByType<VRHUD>();
        UpdateScoreHUD();
    }

    public void AddScore(int points)
    {
        if (points <= 0)
            return;

        score += points;
        UpdateScoreHUD();

        Debug.Log("Score: " + score);
    }

    public void ResetScore()
    {
        score = 0;
        UpdateScoreHUD();
    }

    private void UpdateScoreHUD()
    {
        if (hud == null)
            hud = FindAnyObjectByType<VRHUD>();

        if (hud != null)
            hud.UpdateScore(score);
    }
}
