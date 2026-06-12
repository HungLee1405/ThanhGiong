using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHandController : MonoBehaviour
{
    [Header("References")]
    public PlayerInventory playerInventory;
    public PlayerInventoryUI playerInventoryUI;

    [Header("Hand Settings")]
    public Transform handPoint;

    [Header("Runtime")]
    public int selectedSlotIndex = -1;
    public InventoryItem selectedItem;

    private GameObject currentHandObject;
    private IItemReceiver currentReceiver;

    private void Start()
    {
        if (playerInventory == null)
        {
            playerInventory = GetComponent<PlayerInventory>();
        }

        if (playerInventoryUI == null)
        {
            playerInventoryUI = FindFirstObjectByType<PlayerInventoryUI>();
        }

        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged += OnInventoryChanged;
        }

        DeselectSlot();
    }

    private void OnDestroy()
    {
        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged -= OnInventoryChanged;
        }
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        HandleHotkeys();
        HandlePutItem();
    }

    private void HandleHotkeys()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            ToggleSlot(0);
        }
        else if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            ToggleSlot(1);
        }
        else if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            ToggleSlot(2);
        }
        else if (Keyboard.current.digit4Key.wasPressedThisFrame)
        {
            ToggleSlot(3);
        }
        else if (Keyboard.current.digit5Key.wasPressedThisFrame)
        {
            ToggleSlot(4);
        }
        else if (Keyboard.current.digit6Key.wasPressedThisFrame)
        {
            ToggleSlot(5);
        }
        else if (Keyboard.current.digit7Key.wasPressedThisFrame)
        {
            ToggleSlot(6);
        }
        else if (Keyboard.current.digit8Key.wasPressedThisFrame)
        {
            ToggleSlot(7);
        }
    }

    private void ToggleSlot(int slotIndex)
    {
        if (selectedSlotIndex == slotIndex)
        {
            DeselectSlot();
        }
        else
        {
            SelectSlot(slotIndex);
        }
    }

    public void SelectSlot(int slotIndex)
    {
        if (playerInventory == null) return;

        selectedSlotIndex = slotIndex;
        RefreshSelectedItem();

        if (playerInventoryUI != null)
        {
            playerInventoryUI.HighlightSlot(slotIndex);
        }

        if (selectedItem != null && selectedItem.itemData != null)
        {
            Debug.Log("Đang cầm: " + selectedItem.itemData.itemName);
        }
        else
        {
            Debug.Log("Đã chọn slot " + (slotIndex + 1) + ", nhưng slot đang trống.");
        }
    }

    public void DeselectSlot()
    {
        selectedSlotIndex = -1;
        selectedItem = null;

        RefreshHandVisual();

        if (playerInventoryUI != null)
        {
            playerInventoryUI.ClearHighlight();
        }

        Debug.Log("Đã bỏ chọn vật phẩm.");
    }

    private void HandlePutItem()
    {
        if (!Keyboard.current.rKey.wasPressedThisFrame) return;

        RefreshSelectedItem();

        if (selectedSlotIndex < 0)
        {
            Debug.Log("Chưa chọn slot nào.");
            return;
        }

        if (selectedItem == null || selectedItem.itemData == null)
        {
            Debug.Log("Slot đang chọn không có vật phẩm.");
            return;
        }

        if (currentReceiver == null)
        {
            Debug.Log("Không có nơi nào để bỏ vật phẩm.");
            return;
        }

        ItemData itemToPut = selectedItem.itemData;

        bool accepted = currentReceiver.ReceiveItem(itemToPut, 1);

        if (!accepted)
        {
            Debug.Log("Nơi này không nhận vật phẩm: " + itemToPut.itemName);
            return;
        }

        bool removed = playerInventory.RemoveItemAtSlot(selectedSlotIndex, 1);

        if (!removed)
        {
            Debug.LogWarning("Không xóa được item khỏi inventory.");
            return;
        }

        Debug.Log("Đã bỏ xuống: " + itemToPut.itemName);

        // Không bỏ highlight.
        // Vẫn giữ slot đang chọn, chỉ cập nhật lại item trong slot đó.
        RefreshSelectedItem();

        if (playerInventoryUI != null)
        {
            playerInventoryUI.HighlightSlot(selectedSlotIndex);
        }
    }

    private void OnInventoryChanged()
    {
        if (selectedSlotIndex < 0) return;

        RefreshSelectedItem();

        if (playerInventoryUI != null)
        {
            playerInventoryUI.HighlightSlot(selectedSlotIndex);
        }
    }

    private void RefreshSelectedItem()
    {
        if (playerInventory == null || selectedSlotIndex < 0)
        {
            selectedItem = null;
            RefreshHandVisual();
            return;
        }

        selectedItem = playerInventory.GetItemAtSlot(selectedSlotIndex);
        RefreshHandVisual();
    }

    private void RefreshHandVisual()
    {
        if (currentHandObject != null)
        {
            Destroy(currentHandObject);
        }

        currentHandObject = null;

        if (selectedItem == null) return;
        if (selectedItem.itemData == null) return;
        if (selectedItem.itemData.handPrefab == null) return;
        if (handPoint == null) return;

        currentHandObject = Instantiate(
            selectedItem.itemData.handPrefab,
            handPoint.position,
            handPoint.rotation,
            handPoint
        );

        currentHandObject.transform.localPosition = Vector3.zero;
        currentHandObject.transform.localRotation = Quaternion.identity;
    }

    public void SetCurrentReceiver(IItemReceiver receiver)
    {
        currentReceiver = receiver;
    }

    public void ClearCurrentReceiver(IItemReceiver receiver)
    {
        if (currentReceiver == receiver)
        {
            currentReceiver = null;
        }
    }

    public ItemData GetHeldItemData()
    {
        if (selectedItem == null) return null;
        if (selectedItem.itemData == null) return null;

        return selectedItem.itemData;
    }

    public bool IsHoldingItem(string itemId)
    {
        ItemData heldItem = GetHeldItemData();

        if (heldItem == null) return false;

        return heldItem.itemId == itemId;
    }
}