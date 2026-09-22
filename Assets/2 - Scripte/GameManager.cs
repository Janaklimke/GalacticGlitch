using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Menu, Playing, GameOver }
    public GameState CurrentState { get; private set; } = GameState.Menu;

    [Header("UI Canvases")]
    public GameObject startScreenCanvas;
    public GameObject gameOverCanvas;

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