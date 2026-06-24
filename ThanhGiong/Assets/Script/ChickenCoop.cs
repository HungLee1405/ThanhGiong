using UnityEngine;
using Unity.Netcode;

public class ChickenCoop : MonoBehaviour, IItemReceiver
{
    [Tooltip("ID của vật phẩm được phép bỏ vào (vd: chicken)")]
    public string acceptedItemId = "chick";

    [Tooltip("Các object model gà sẽ được bật lên khi bỏ gà vào chuồng")]
    public GameObject[] chickenVisuals;

    [Header("Interaction UI")]
    [SerializeField] private InteractionUI interactionUI;
    [SerializeField] private string noChickenMessage = "Bạn cần bắt và cầm một con gà.";
    [SerializeField] private string putChickenMessage = "Nhấn R để bỏ gà vào chuồng.";
    [SerializeField] private string questInactiveMessage = "Hiện chưa có nhiệm vụ bắt gà.";
    [SerializeField] private string coopFullMessage = "Chuồng đã đủ gà.";

    [Header("Quest Settings")]
    [SerializeField] private QuestManager questManager;
    [SerializeField] private int requiredChickenCount = 3;
    
    private int chickenCount = 0;
    private PlayerHandController activePlayerHand;
    private string lastMessage = "";

    private void Start()
    {
        foreach (var visual in chickenVisuals)
        {
            if (visual != null) visual.SetActive(false);
        }
    }

    public bool ReceiveItem(ItemData itemData, int amount)
    {
        if (!CanReceiveItem(itemData, amount)) return false;

        chickenCount++;

        if (chickenCount <= chickenVisuals.Length)
        {
            if (chickenVisuals[chickenCount - 1] != null)
            {
                chickenVisuals[chickenCount - 1].SetActive(true);
            }
        }

        QuestManager questManager = FindFirstObjectByType<QuestManager>();
        if (questManager != null)
        {
            questManager.AddProgress(QuestStepType.CatchChicken, "chick", 1);
        }

        return true;
    }

    public bool CanReceiveItem(ItemData itemData, int amount)
    {
        if (questManager == null)
        {
            questManager = FindFirstObjectByType<QuestManager>();
        }

        if (questManager == null) return false;

        if (!questManager.IsStepActive(QuestStepType.CatchChicken))
            return false;

        if (itemData == null)
            return false;

        if (itemData.itemId != acceptedItemId)
            return false;

        int reqAmount = requiredChickenCount;
        QuestStep step = questManager.GetActiveStep(QuestStepType.CatchChicken);
        if (step != null)
        {
            reqAmount = step.requiredAmount;
        }

        if (chickenCount >= reqAmount)
            return false;

        return true;
    }

    public bool TryReceiveChicken(PlayerHandController hand)
    {
        if (hand == null || hand.carriedChicken == null) return false;
        ItemData heldItem = hand.GetHeldItemData();
        if (heldItem == null || !CanReceiveItem(heldItem, 1)) return false;

        hand.carriedChicken.isDelivered = true;
        if (hand.TryConsumeHeldItem(1))
        {
            hand.carriedChicken = null;
            ReceiveItem(heldItem, 1);
            return true;
        }
        else
        {
            hand.carriedChicken.isDelivered = false;
            return false;
        }
    }

    private void Update()
    {
        RefreshSharedChickenCount();

        if (activePlayerHand != null)
        {
            UpdateInteractionUI(activePlayerHand);
        }
    }

    private void RefreshSharedChickenCount()
    {
        if (questManager == null) questManager = FindFirstObjectByType<QuestManager>();
        QuestStep step = questManager != null ? questManager.GetActiveStep(QuestStepType.CatchChicken) : null;
        if (step == null || step.currentAmount <= chickenCount) return;

        chickenCount = step.currentAmount;
        if (chickenVisuals == null) return;
        for (int i = 0; i < chickenVisuals.Length; i++)
        {
            if (chickenVisuals[i] != null)
            {
                chickenVisuals[i].SetActive(i < chickenCount);
            }
        }
    }

    private void UpdateInteractionUI(PlayerHandController hand)
    {
        if (interactionUI == null) return;
        if (hand == null)
        {
            interactionUI.Hide();
            lastMessage = "";
            return;
        }

        if (questManager == null)
        {
            questManager = FindFirstObjectByType<QuestManager>();
        }

        string message = "";

        if (questManager == null || !questManager.IsStepActive(QuestStepType.CatchChicken))
        {
            message = questInactiveMessage;
        }
        else
        {
            int reqAmount = requiredChickenCount;
            QuestStep step = questManager.GetActiveStep(QuestStepType.CatchChicken);
            if (step != null)
            {
                reqAmount = step.requiredAmount;
            }

            if (chickenCount >= reqAmount)
            {
                message = coopFullMessage;
            }
            else if (hand.carriedChicken == null || !hand.IsHoldingItem(acceptedItemId))
            {
                message = noChickenMessage;
            }
            else
            {
                message = putChickenMessage;
            }
        }

        if (message != lastMessage)
        {
            lastMessage = message;
            interactionUI.Show(message);
        }
    }

    private PlayerHandController GetPlayerHandController(Collider other)
    {
        if (!other.CompareTag("Player")) return null;

        PlayerHandController hand = other.GetComponent<PlayerHandController>();
        if (hand == null) hand = other.GetComponentInParent<PlayerHandController>();
        if (hand == null) hand = other.GetComponentInChildren<PlayerHandController>();

        NetworkManager manager = NetworkManager.Singleton;
        if (manager != null && manager.IsListening && hand != null && hand.IsSpawned && !hand.IsOwner)
            return null;

        return hand;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerHandController hand = GetPlayerHandController(other);
        if (hand == null) return;

        activePlayerHand = hand;
        hand.SetCurrentReceiver(this);
        UpdateInteractionUI(hand);

        Debug.Log("Người chơi đã vào vùng chuồng gà.");
    }

    private void OnTriggerStay(Collider other)
    {
        if (activePlayerHand != null) return;

        PlayerHandController hand = GetPlayerHandController(other);
        if (hand == null) return;

        activePlayerHand = hand;
        hand.SetCurrentReceiver(this);
        UpdateInteractionUI(hand);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerHandController hand = GetPlayerHandController(other);
        if (hand == null) return;

        if (activePlayerHand == hand)
        {
            activePlayerHand = null;
        }
        hand.ClearCurrentReceiver(this);
        UpdateInteractionUI(null);

        Debug.Log("Người chơi đã rời vùng chuồng gà.");
    }
}
