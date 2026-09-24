using TMPro;
using UnityEngine;

public class NameEntryUI : MonoBehaviour
{
    public TMP_InputField nameInput;
    public LeaderboardUI leaderboardUI; // hidden while typing, shown again after confirm

    int pendingScore;

    void Awake()
    {
        nameInput.characterLimit = 4;
        nameInput.onValueChanged.AddListener(OnInputChanged);
        nameInput.onSubmit.AddListener(OnSubmit); // fires when Enter is pressed in the field
    }

    // Call this when the game over screen appears
    public void Show(int finalScore)
    {
        pendingScore = finalScore;
        nameInput.text = "";

        if (leaderboardUI != null)
            leaderboardUI.SetVisible(false); // hide leaderboard while typing

        gameObject.SetActive(true);
        nameInput.Select();
        nameInput.ActivateInputField();
    }

    void OnInputChanged(string value)
    {
        // Strip anything that isn't a letter, force uppercase, keep cursor sane
        string filtered = "";
        foreach (char c in value)
        {
            if (char.IsLetter(c))
                filtered += char.ToUpper(c);
        }

        if (filtered != value)
        {
            nameInput.text = filtered;
            nameInput.caretPosition = filtered.Length;
        }
    }

    void OnSubmit(string value)
    {
        Confirm();
    }

    void Update()
    {
        if (nameInput.text.Length == 4 &&
            (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            Confirm();
        }
    }

    void Confirm()
    {
        string name = nameInput.text;
        if (name.Length != 4) return; 

        GameManager.Instance.SubmitScore(name, pendingScore);
        gameObject.SetActive(false);

        if (leaderboardUI != null)
            leaderboardUI.SetVisible(true); 
    }
}
