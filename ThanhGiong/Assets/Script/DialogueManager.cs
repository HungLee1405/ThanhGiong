using UnityEngine;
using TMPro;
using System;

public class DialogueManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject dialoguePanel;
    public TMP_Text dialogueNpcNameText;
    public TMP_Text dialogueText;

    private string[] lines;
    private int index;
    private bool isTalking;
    private Action onDialogueEnd;

    private void Start()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
    }

    public void StartDialogue(string npcName, string[] newLines, Action onEnd = null)
    {
        if (newLines == null || newLines.Length == 0)
        {
            Debug.LogWarning("Không có lời thoại.");
            return;
        }

        lines = newLines;
        index = 0;
        isTalking = true;
        onDialogueEnd = onEnd;

        dialoguePanel.SetActive(true);

        if (dialogueNpcNameText != null)
        {
            dialogueNpcNameText.text = npcName;
        }

        dialogueText.text = lines[index];
    }

    public void NextLine()
    {
        if (!isTalking) return;

        index++;

        if (index < lines.Length)
        {
            dialogueText.text = lines[index];
        }
        else
        {
            EndDialogue();
        }
    }

    private void EndDialogue()
    {
        isTalking = false;

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        onDialogueEnd?.Invoke();
    }

    public bool IsTalking()
    {
        return isTalking;
    }
}