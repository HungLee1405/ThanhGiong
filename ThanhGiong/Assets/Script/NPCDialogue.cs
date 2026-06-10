using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class NPCDialogue : MonoBehaviour
{
    [Header("NPC Info")]
    public string npcId = "village_elder";
    public string npcName = "Già Làng";

    [Header("Interaction")]
    public string interactionMessage = "[E] Tương tác";

    [Header("References")]
    public QuestManager questManager;

    [Header("UI References")]
    public GameObject interactionTextObject;
    public TextMeshProUGUI interactionText;

    public GameObject dialoguePanel;
    public TextMeshProUGUI npcNameText;
    public TextMeshProUGUI dialogueContentText;

    private bool playerInRange = false;
    private bool dialogueOpen = false;

    private string[] currentDialogueLines;
    private int currentLineIndex = 0;

    private void Start()
    {
        if (questManager == null)
        {
            questManager = FindFirstObjectByType<QuestManager>();
        }

        HideInteractionText();
        HideDialogue();
    }

    private void Update()
    {
        if (Keyboard.current == null) return;
        if (!playerInRange) return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (!dialogueOpen)
            {
                TryOpenDialogue();
            }
            else
            {
                NextDialogueLine();
            }
        }
    }

    private void TryOpenDialogue()
    {
        if (questManager == null)
        {
            Debug.LogWarning("NPCDialogue chưa tìm thấy QuestManager.");
            return;
        }

        if (!questManager.CanTalkToNPC(npcId))
        {
            Debug.Log("NPC này không phải mục tiêu nhiệm vụ hiện tại: " + npcId);
            return;
        }

        currentDialogueLines = questManager.GetCurrentDialogueForNPC(npcId);

        if (currentDialogueLines == null || currentDialogueLines.Length == 0)
        {
            Debug.LogWarning("NPC không có lời thoại cho nhiệm vụ hiện tại: " + npcId);
            return;
        }

        dialogueOpen = true;
        currentLineIndex = 0;

        HideInteractionText();
        ShowDialogueLine();
    }

    private void ShowDialogueLine()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }

        if (npcNameText != null)
        {
            npcNameText.text = npcName;
        }

        if (dialogueContentText != null)
        {
            dialogueContentText.text = currentDialogueLines[currentLineIndex];
        }
    }

    private void NextDialogueLine()
    {
        currentLineIndex++;

        if (currentDialogueLines != null && currentLineIndex < currentDialogueLines.Length)
        {
            ShowDialogueLine();
            return;
        }

        FinishDialogue();
    }

    private void FinishDialogue()
    {
        dialogueOpen = false;
        HideDialogue();

        if (questManager != null)
        {
            questManager.CompleteTalkToNPC(npcId);
        }

        if (playerInRange && questManager != null && questManager.CanTalkToNPC(npcId))
        {
            ShowInteractionText();
        }
        else
        {
            HideInteractionText();
        }
    }

    private void ShowInteractionText()
    {
        if (interactionTextObject != null)
        {
            interactionTextObject.SetActive(true);
        }

        if (interactionText != null)
        {
            interactionText.text = interactionMessage;
        }
    }

    private void HideInteractionText()
    {
        if (interactionTextObject != null)
        {
            interactionTextObject.SetActive(false);
        }
    }

    private void HideDialogue()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;

        if (questManager != null && questManager.CanTalkToNPC(npcId))
        {
            ShowInteractionText();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        dialogueOpen = false;

        HideInteractionText();
        HideDialogue();
    }
}