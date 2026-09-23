using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NameEntryUI : MonoBehaviour
{
    public TMP_InputField nameInput;
    public Button confirmButton;

    int pendingScore;

    void Awake()
    {
        nameInput.characterLimit = 4;
        nameInput.onValueChanged.AddListener(OnInputChanged);
        confirmButton.onClick.AddListener(Confirm);
    }

    // Call this when the game over screen appears
    public void Show(int finalScore)
    {
        pendingScore = finalScore;
        nameInput.text = "";

        gameObject.SetActive(true);
        nameInput.Select();
        nameInput.ActivateInputField();
    }

    void OnInputChanged(string value)
    {
        // Strip anything that isn't a letter, force uppercase
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

        confirmButton.interactable = (filtered.Length == 4);
    }

    void Confirm()
    {
        string name = nameInput.text;
        if (name.Length != 4) return; 

        GameManager.Instance.SubmitScore(name, pendingScore);
        gameObject.SetActive(false);
    }
}