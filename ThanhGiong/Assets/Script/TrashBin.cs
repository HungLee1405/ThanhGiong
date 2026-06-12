using UnityEngine;

public class TrashBin : MonoBehaviour, IItemReceiver
{
    [Header("UI")]
    public InteractionUI interactionUI;

    private PlayerHandController currentPlayerHand;

    public bool ReceiveItem(ItemData itemData, int amount)
    {
        if (itemData == null) return false;

        Debug.Log("Đã bỏ vào thùng rác: " + itemData.itemName);

        if (interactionUI != null)
        {
            interactionUI.Show("Đã bỏ " + itemData.itemName + " vào thùng rác");
        }

        return true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        BindPlayer(other);

        if (currentPlayerHand != null)
        {
            currentPlayerHand.SetCurrentReceiver(this);
        }

        if (interactionUI != null)
        {
            interactionUI.Show("Nhấn R để bỏ vật phẩm");
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Phòng trường hợp OnTriggerEnter bị miss hoặc chưa lấy được component.
        if (currentPlayerHand == null)
        {
            BindPlayer(other);
        }

        if (currentPlayerHand != null)
        {
            currentPlayerHand.SetCurrentReceiver(this);
        }

        if (interactionUI != null)
        {
            interactionUI.Show("Nhấn R để bỏ vật phẩm");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerHandController handController = GetPlayerHandController(other);

        if (handController != null)
        {
            handController.ClearCurrentReceiver(this);
        }

        if (currentPlayerHand == handController)
        {
            currentPlayerHand = null;
        }
        else
        {
            currentPlayerHand = null;
        }

        if (interactionUI != null)
        {
            interactionUI.Hide();
        }
    }

    private void BindPlayer(Collider other)
    {
        currentPlayerHand = GetPlayerHandController(other);
    }

    private PlayerHandController GetPlayerHandController(Collider other)
    {
        PlayerHandController handController = other.GetComponent<PlayerHandController>();

        if (handController == null)
        {
            handController = other.GetComponentInParent<PlayerHandController>();
        }

        if (handController == null)
        {
            handController = other.GetComponentInChildren<PlayerHandController>();
        }

        return handController;
    }
}