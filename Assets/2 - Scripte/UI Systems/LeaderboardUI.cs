using System.Text;
using TMPro;
using UnityEngine;

public class LeaderboardUI : MonoBehaviour
{
    public ScoreSaver highScores;
    public TMP_Text leaderboardText;
    public TMP_Text finalScoreText; 

    public void Refresh(int finalScore)
    {
        if (finalScoreText != null)
            finalScoreText.text = "This run: " + finalScore;

        var scores = highScores.GetTopScores();

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < scores.Count; i++)
        {
            sb.AppendLine((i + 1) + ". " + scores[i].playerName + " - " + scores[i].score);
        }

        if (scores.Count == 0)
            sb.Append("No scores yet");

        leaderboardText.text = sb.ToString();
    }
}