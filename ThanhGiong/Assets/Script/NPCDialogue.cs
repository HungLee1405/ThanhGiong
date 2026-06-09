using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class NPCDialogue : MonoBehaviour
{
    [Header("NPC Info")]
    public string npcId = "village_elder";
    public string npcName = "Già Làng";

    [Header("NPC World UI")]
    public TMP_Text npcNameText;
    public GameObject worldInteractionHint;

    [Header("References")]
    public DialogueManager dialogueManager;
    public QuestManager questManager;

    private bool playerInRange;

    private void Start()
    {
        if (npcNameText != null)
        {
            npcNameText.text = npcName;
        }

        if (worldInteractionHint != null)
        {
            worldInteractionHint.SetActive(false);
        }
    }

    private void Update()
    {
        if (!playerInRange) return;
        if (questManager == null) return;
        if (dialogueManager == null) return;

        if (!questManager.CanTalkToNPC(npcId)) return;

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (dialogueManager.IsTalking())
            {
                dialogueManager.NextLine();
            }
            else
            {
                string[] dialogueLines = questManager.GetCurrentDialogueForNPC(npcId);

                dialogueManager.StartDialogue(npcName, dialogueLines, OnDialogueFinished);
            }
        }
    }

    private void OnDialogueFinished()
    {
        if (questManager != null)
        {
            questManager.CompleteTalkToNPC(npcId);
        }

        HideInteractionHint();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;

        if (questManager != null && questManager.CanTalkToNPC(npcId))
        {
            ShowInteractionHint();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        HideInteractionHint();
    }

    private void ShowInteractionHint()
    {
        if (worldInteractionHint != null)
        {
            worldInteractionHint.SetActive(true);
        }
    }

    private void HideInteractionHint()
    {
        if (worldInteractionHint != null)
        {
            worldInteractionHint.SetActive(false);
        }
    }
}