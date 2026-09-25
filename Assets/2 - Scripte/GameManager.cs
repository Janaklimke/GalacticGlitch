using UnityEngine;

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

    [Header("Music")]
    public AudioSource menuMusicSource;   // menu + game over music, Loop checked, Play On Awake off
    public AudioSource gameplayMusicSource; // in-game music, Loop checked, Play On Awake off

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

        PlayGameplayMusic();
    }

    public void GameOver()
    {
        CurrentState = GameState.GameOver;
        gameOverCanvas.SetActive(true);

        int finalScore = PointManager.Instance.Points;
        nameEntryUI.Show(finalScore);

        PlayMenuMusic();
    }

    public void SubmitScore(string name, int finalScore)
    {
        ScoreSaver.ScoreEntry entry = highScores.AddScore(name, finalScore);
        leaderboardUI.Refresh(entry, finalScore);
    }

    void ShowStartScreen()
    {
        CurrentState = GameState.Menu;
        startScreenCanvas.SetActive(true);
        gameOverCanvas.SetActive(false);

        PlayMenuMusic();
    }

    void PlayMenuMusic()
    {
        if (gameplayMusicSource != null) gameplayMusicSource.Stop();
        if (menuMusicSource != null && !menuMusicSource.isPlaying) menuMusicSource.Play();
    }

    void PlayGameplayMusic()
    {
        if (menuMusicSource != null) menuMusicSource.Stop();
        if (gameplayMusicSource != null && !gameplayMusicSource.isPlaying) gameplayMusicSource.Play();
    }

    public void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}