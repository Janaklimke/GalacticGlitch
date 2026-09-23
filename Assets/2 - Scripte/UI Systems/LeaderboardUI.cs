using System.Text;
using TMPro;
using UnityEngine;

public class LeaderboardUI : MonoBehaviour
{
    public ScoreSaver highScores;
    public TMP_Text leaderboardText;
    public TMP_Text finalScoreText; 
    public string highlightColor = "#FFD700"; // yellow

    public void Refresh(ScoreSaver.ScoreEntry highlightEntry, int finalScore)
    {
        if (finalScoreText != null)
            finalScoreText.text = "YOUR SCORE : " + finalScore;

        var scores = highScores.GetTopScores();

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < scores.Count; i++)
        {
            string line = (i + 1) + ". " + scores[i].playerName + " - " + scores[i].score;

            bool isThisRun = highlightEntry != null && ReferenceEquals(scores[i], highlightEntry);
            if (isThisRun)
                line = "<color=" + highlightColor + "><b>" + line + "</b></color>";

            sb.AppendLine(line);
        }

        if (scores.Count == 0)
            sb.Append("No scores yet");

        leaderboardText.text = sb.ToString();
    }
}