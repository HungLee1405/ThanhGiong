using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Unity.Collections;

public class PlayerHandController : NetworkBehaviour
{
    [Header("References")]
    public PlayerInventory playerInventory;
    public PlayerInventoryUI playerInventoryUI;

    [Header("Hand Settings")]
    public Transform handPoint;

    [Header("Runtime")]
    public int selectedSlotIndex = -1;
    public InventoryItem selectedItem;
    public ChickenController carriedChicken;

    private GameObject currentHandObject;
    private IItemReceiver currentReceiver;
    private readonly NetworkVariable<FixedString64Bytes> networkHeldItemId = new NetworkVariable<FixedString64Bytes>(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        networkHeldItemId.OnValueChanged += OnNetworkHeldItemChanged;
        if (!IsOwner)
        {
            ApplyHandVisual(FindItemData(networkHeldItemId.Value.ToString()));
        }
    }

    public override void OnNetworkDespawn()
    {
        networkHeldItemId.OnValueChanged -= OnNetworkHeldItemChanged;
    }

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

    public override void OnDestroy()
    {
        base.OnDestroy();

        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged -= OnInventoryChanged;
        }
    }

    private void Update()
    {
        if (PauseMenuManager.isPaused) return;
        if (!CanUseLocalInput()) return;
        if (NetworkPlayerAppearance.IsLocalSelectionOpen) return;
        if (CookingMenuUI.IsMenuOpen) return;
        if (Keyboard.current == null) return;

        HandleHotkeys();
        HandlePutItem();
    }

    private bool CanUseLocalInput()
    {
        NetworkManager networkManager = NetworkManager.Singleton;

        if (networkManager == null || !networkManager.IsListening)
            return true;

        return !IsSpawned || IsOwner;
    }

    private void HandleHotkeys()
    {
        if (selectedItem != null && selectedItem.itemData != null && selectedItem.itemData.itemId == "chick")
        {
            return; // Khóa đổi slot nếu đang cầm gà
        }

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

        EnsureInventoryUI();
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

        if (currentReceiver is ChickenCoop coop && itemToPut.itemId == "chick")
        {
            bool success = coop.TryReceiveChicken(this);
            if (!success)
            {
                Debug.Log("Không thể đưa gà vào chuồng.");
            }
            else
            {
                Debug.Log("Đã bỏ gà vào chuồng.");
                RefreshSelectedItem();
            }
            return;
        }

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
        EnsureInventoryUI();

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
        ItemData itemData = selectedItem != null ? selectedItem.itemData : null;
        ApplyHandVisual(itemData);

        if (IsSpawned && IsOwner)
        {
            PublishHeldItemServerRpc(itemData != null ? itemData.itemId : "");
        }
    }

    private void ApplyHandVisual(ItemData itemData)
    {
        if (currentHandObject != null)
        {
            Destroy(currentHandObject);
        }

        currentHandObject = null;

        if (itemData == null) return;
        if (itemData.handPrefab == null) return;
        if (handPoint == null) return;

        currentHandObject = Instantiate(
            itemData.handPrefab,
            handPoint.position,
            handPoint.rotation,
            handPoint
        );

        currentHandObject.transform.localPosition = Vector3.zero;
        currentHandObject.transform.localRotation = Quaternion.identity;
    }

    [ServerRpc]
    private void PublishHeldItemServerRpc(string itemId)
    {
        networkHeldItemId.Value = itemId ?? "";
    }

    private void OnNetworkHeldItemChanged(FixedString64Bytes previous, FixedString64Bytes current)
    {
        if (IsOwner) return;
        ApplyHandVisual(FindItemData(current.ToString()));
    }

    private static ItemData FindItemData(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return null;

        ItemData[] items = Resources.FindObjectsOfTypeAll<ItemData>();
        foreach (ItemData item in items)
        {
            if (item != null && item.itemId == itemId) return item;
        }
        return null;
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

    public void ClearSelectedSlot()
    {
        DeselectSlot();
    }

    public void RefreshHeldItem()
    {
        RefreshSelectedItem();
    }

    private void EnsureInventoryUI()
    {
        if (playerInventoryUI == null)
        {
            playerInventoryUI = FindFirstObjectByType<PlayerInventoryUI>();
        }
    }

    public InventoryItem GetHeldItem()
    {
        return selectedItem;
    }

    public bool HasHeldItem(string itemId)
    {
        return IsHoldingItem(itemId);
    }

    public bool TryConsumeHeldItem(int amount)
    {
        if (selectedSlotIndex < 0 || playerInventory == null) return false;

        bool success = playerInventory.RemoveItemAtSlot(selectedSlotIndex, amount);
        if (success)
        {
            RefreshSelectedItem();
            
            if (playerInventoryUI != null)
            {
                playerInventoryUI.HighlightSlot(selectedSlotIndex);
            }
        }
        return success;
    }
}
