using System.Collections;
using TMPro;
using UnityEngine;

public class PointManager : MonoBehaviour
{
    public static PointManager Instance { get; private set; }

    public TMP_Text texts;

    [Header("Scaling difficulty/reward")]
    public float tickInterval = 1f;       // how often points are added
    public int basePointsPerTick = 1;     // points per tick at the start of a run
    public int rampIncrease = 1;          // how much pointsPerTick grows...
    public float rampInterval = 15f;      // ...every this many seconds of play

    public int Points { get; private set; } = 0;

    float playTime; // seconds spent actually in the Playing state this run

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        StartCoroutine(AddPoints());
    }

    void Update()
    {
        bool isPlaying = GameManager.Instance.CurrentState == GameManager.GameState.Playing;

        bool shouldShow = isPlaying;
        if (texts.gameObject.activeSelf != shouldShow)
            texts.gameObject.SetActive(shouldShow);

        if (isPlaying)
            playTime += Time.deltaTime;
    }

    IEnumerator AddPoints()
    {
        while (true)
        {
            yield return new WaitForSeconds(tickInterval);

            if (GameManager.Instance.CurrentState == GameManager.GameState.Playing)
            {
                int pointsPerTick = CurrentPointsPerTick();
                Points += pointsPerTick;
                texts.text = "Points: " + Points;
            }
        }
    }

    int CurrentPointsPerTick()
    {
        int steps = Mathf.FloorToInt(playTime / rampInterval);
        return basePointsPerTick + steps * rampIncrease;
    }

    public void ResetPoints()
    {
        Points = 0;
        playTime = 0f;
        texts.text = "Points: 0";
    }
}