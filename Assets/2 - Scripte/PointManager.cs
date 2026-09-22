using System.Collections;
using TMPro;
using UnityEngine;

public class PointManager : MonoBehaviour
{
    public TMP_Text texts;

    private int points = 0;

    void Start()
    {
        StartCoroutine(AddPoints());
    }

    IEnumerator AddPoints()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);

            points++;
            texts.text = "Points: " + points;
        }
    }
}
