using System.Collections;
using TMPro;
using UnityEngine;

public class PointManager : MonoBehaviour
{
    public static PointManager Instance { get; private set; }

    public TMP_Text texts;

    public int Points { get; private set; } = 0;

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
        bool shouldShow = GameManager.Instance.CurrentState == GameManager.GameState.Playing;
        if (texts.gameObject.activeSelf != shouldShow)
            texts.gameObject.SetActive(shouldShow);
    }

    IEnumerator AddPoints()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);

            if (GameManager.Instance.CurrentState == GameManager.GameState.Playing)
            {
                Points++;
                texts.text = "Points: " + Points;
            }
        }
    }

    public void ResetPoints()
    {
        Points = 0;
        texts.text = "Points: 0";
    }
}