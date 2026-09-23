using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Menu, Playing, GameOver }
    public GameState CurrentState { get; private set; } = GameState.Menu;

    [Header("UI Canvases")]
    public GameObject startScreenCanvas;
    public GameObject gameOverCanvas;
    public ScoreSaver highScores;
    public NameEntryUI nameEntryUI;
    public LeaderboardUI leaderboardUI;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        ShowStartScreen();
    }

    public void StartGame()
    {
        CurrentState = GameState.Playing;
        startScreenCanvas.SetActive(false);
        gameOverCanvas.SetActive(false);
    }

    public void GameOver()
    {
        CurrentState = GameState.GameOver;
        gameOverCanvas.SetActive(true);

        int finalScore = PointManager.Instance.Points;
        nameEntryUI.Show(finalScore); 
    }

    public void SubmitScore(string name, int finalScore)
    {
        highScores.AddScore(name, finalScore);
        leaderboardUI.Refresh(finalScore);
    }

    void ShowStartScreen()
    {
        CurrentState = GameState.Menu;
        startScreenCanvas.SetActive(true);
        gameOverCanvas.SetActive(false);
    }

    public void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}