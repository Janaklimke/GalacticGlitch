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
        public string date;

        public ScoreEntry(string playerName, int score, string date)
        {
            this.playerName = playerName;
            this.score = score;
            this.date = date;
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
    public ScoreEntry AddScore(string name, int score)
    {
        if (string.IsNullOrEmpty(name) || name.Length != 4)
        {
            Debug.LogError("Score not saved: name must be exactly 4 letters, got \"" + name + "\"");
            return null;
        }

        name = name.ToUpper();
        string now = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");

        ScoreEntry existing = topScores.Find(e => e.playerName == name);

        if (existing != null)
        {
            if (score <= existing.score)
            {
                return null;
            }

            existing.score = score;
            existing.date = now;
            topScores.Sort((a, b) => b.score.CompareTo(a.score));
            Save();
            return existing;
        }

        ScoreEntry entry = new ScoreEntry(name, score, now);
        topScores.Add(entry);
        topScores.Sort((a, b) => b.score.CompareTo(a.score));

        if (topScores.Count > MaxScores)
            topScores.RemoveRange(MaxScores, topScores.Count - MaxScores);

        Save();
        return topScores.Contains(entry) ? entry : null; // null if not top 10
    }

    public List<ScoreEntry> GetTopScores()
    {
        return topScores;
    }

    public int GetHighScore()
    {
        return topScores.Count > 0 ? topScores[0].score : 0;
    }

    // Wipe for clearing out debug runs before release
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
