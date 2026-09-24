using System.Collections;
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

    [Header("Random Event Canvas")]
    [Tooltip("Canvas, das in zufälligen Abständen aktiviert wird.")]
    public GameObject randomEventCanvas;
    [Tooltip("Minimale Wartezeit in Sekunden, bevor das Canvas aktiviert wird.")]
    public float minInterval = 5f;
    [Tooltip("Maximale Wartezeit in Sekunden, bevor das Canvas aktiviert wird.")]
    public float maxInterval = 15f;
    [Tooltip("Wie lange das Canvas sichtbar bleibt, bevor es wieder deaktiviert wird.")]
    public float displayDuration = 2f;

    private Coroutine randomEventRoutine;

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
        if (randomEventCanvas != null)
        {
            randomEventCanvas.SetActive(false);
            randomEventRoutine = StartCoroutine(RandomEventCanvasLoop());
        }
        PlayGameplayMusic();

    }

    public void GameOver()
    {
        CurrentState = GameState.GameOver;
        gameOverCanvas.SetActive(true);

        StopRandomEventCanvas();

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
        StopRandomEventCanvas();
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

    private IEnumerator RandomEventCanvasLoop()
    {
        while (CurrentState == GameState.Playing)
        {
            float wait = Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(wait);

            if (CurrentState != GameState.Playing) yield break;

            randomEventCanvas.SetActive(true);
            yield return new WaitForSeconds(displayDuration);

            if (randomEventCanvas != null)
                randomEventCanvas.SetActive(false);
        }
    }

    private void StopRandomEventCanvas()
    {
        if (randomEventRoutine != null)
        {
            StopCoroutine(randomEventRoutine);
            randomEventRoutine = null;
        }

        if (randomEventCanvas != null)
            randomEventCanvas.SetActive(false);
    }
}