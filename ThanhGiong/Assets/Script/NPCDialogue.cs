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
    public DialogueManager dialogueManager;

    [Header("UI References")]
    public GameObject interactionTextObject;
    public TMP_Text interactionText;

    [Header("World Name")]
    public TMP_Text worldNameText;

    private bool playerInRange = false;

    private void Start()
    {
        if (questManager == null)
        {
            questManager = FindFirstObjectByType<QuestManager>();
        }

        if (dialogueManager == null)
        {
            dialogueManager = FindFirstObjectByType<DialogueManager>();
        }

        if (worldNameText != null)
        {
            worldNameText.text = npcName;
            worldNameText.gameObject.SetActive(true);
        }

        HideInteractionText();
    }

    private void Update()
    {
        if (Keyboard.current == null) return;
        if (!playerInRange) return;
        if (dialogueManager != null && dialogueManager.IsTalking()) return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            TryOpenDialogue();
        }
    }

    private void TryOpenDialogue()
    {
        if (questManager == null)
        {
            Debug.LogWarning("NPCDialogue chưa tìm thấy QuestManager.");
            return;
        }

        if (dialogueManager == null)
        {
            Debug.LogWarning("NPCDialogue chưa tìm thấy DialogueManager.");
            return;
        }

        if (!questManager.CanTalkToNPC(npcId))
        {
            Debug.Log("NPC này không phải mục tiêu nhiệm vụ hiện tại: " + npcId);
            return;
        }

        string[] dialogueLines = questManager.GetCurrentDialogueForNPC(npcId);

        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            Debug.LogWarning("NPC không có lời thoại cho nhiệm vụ hiện tại: " + npcId);
            return;
        }

        HideInteractionText();

        dialogueManager.StartDialogue(npcName, dialogueLines, () =>
        {
            questManager.CompleteTalkToNPC(npcId);

            if (playerInRange && questManager.CanTalkToNPC(npcId))
            {
                ShowInteractionText();
            }
            else
            {
                HideInteractionText();
            }
        });
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
        HideInteractionText();
    }
}