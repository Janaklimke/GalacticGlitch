using System.Collections;
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
    }

    public void GameOver()
    {
        CurrentState = GameState.GameOver;
        gameOverCanvas.SetActive(true);

        StopRandomEventCanvas();

        int finalScore = PointManager.Instance.Points;
        nameEntryUI.Show(finalScore);
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