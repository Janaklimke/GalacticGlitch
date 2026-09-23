using System.Collections.Generic;
using System.IO;
using UnityEngine;

[CreateAssetMenu(fileName = "ScoreSaver", menuName = "Game/High Score Data")]
public class ScoreSaver : ScriptableObject
{
    const int MaxScores = 10;

    [System.Serializable]
    public class ScoreEntry
    {
        public string playerName;
        public int score;

        public ScoreEntry(string playerName, int score)
        {
            this.playerName = playerName;
            this.score = score;
        }
    }

    [System.Serializable]
    class ScoreList
    {
        public List<ScoreEntry> scores = new List<ScoreEntry>();
    }

    [Header("Runtime data (do not edit directly)")]
    public List<ScoreEntry> topScores = new List<ScoreEntry>();

    string SavePath => Path.Combine(Application.persistentDataPath, "highscores.json");

    void OnEnable()
    {
        Load();
    }
    public void AddScore(string name, int score)
    {
        if (string.IsNullOrEmpty(name) || name.Length != 4)
        {
            Debug.LogError("Score not saved: name must be exactly 4 letters, got \"" + name + "\"");
            return;
        }

        topScores.Add(new ScoreEntry(name.ToUpper(), score));
        topScores.Sort((a, b) => b.score.CompareTo(a.score));

        if (topScores.Count > MaxScores)
            topScores.RemoveRange(MaxScores, topScores.Count - MaxScores);

        Save();
    }

    public List<ScoreEntry> GetTopScores()
    {
        return topScores;
    }

    public int GetHighScore()
    {
        return topScores.Count > 0 ? topScores[0].score : 0;
    }

    // Wipe / for clearing out debug runs before release
    public void ResetScores()
    {
        topScores.Clear();
        Save();
    }

    void Save()
    {
        ScoreList wrapper = new ScoreList { scores = topScores };
        string json = JsonUtility.ToJson(wrapper, true);
        File.WriteAllText(SavePath, json);
    }

    void Load()
    {
        if (!File.Exists(SavePath))
        {
            topScores = new List<ScoreEntry>();
            return;
        }

        string json = File.ReadAllText(SavePath);
        ScoreList wrapper = JsonUtility.FromJson<ScoreList>(json);
        topScores = wrapper != null ? wrapper.scores : new List<ScoreEntry>();
    }
}