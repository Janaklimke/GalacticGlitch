using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[CustomEditor(typeof(ScoreSaver))]
public class HighScoreDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ScoreSaver data = (ScoreSaver)target;

        GUILayout.Space(10);
        if (GUILayout.Button("Reset Scores"))
        {
            if (EditorUtility.DisplayDialog(
                "Reset High Scores",
                "This clears all saved scores, including debug runs. This can't be undone. Continue?",
                "Reset",
                "Cancel"))
            {
                data.ResetScores();
            }
        }
    }
}