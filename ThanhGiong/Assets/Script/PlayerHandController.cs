using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Unity.Collections;

public class PlayerHandController : NetworkBehaviour
{
    private const float HeldChickenScale = 0.18f;
    private static readonly Vector3 HeldChickenCenterOffset = new Vector3(0.02f, -0.14f, 0.02f);
    private static readonly Quaternion HeldChickenRotationOffset = Quaternion.Euler(0f, 180f, 0f);

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
    private Transform heldChickenAnchor;
    private bool isShowingHeldChicken;
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

    private void LateUpdate()
    {
        UpdateHeldChickenPose();
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
        EnsureLocalInventory();
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
        EnsureLocalInventory();
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
        EnsureLocalInventory();

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
        heldChickenAnchor = null;
        isShowingHeldChicken = false;

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

        if (itemData.itemId == "chick")
        {
            heldChickenAnchor = FindHandAnchor();
            isShowingHeldChicken = true;
            currentHandObject.transform.localScale = Vector3.one * HeldChickenScale;
            DisableHeldChickenGameplayComponents(currentHandObject);
            UpdateHeldChickenPose();
        }
    }

    private void UpdateHeldChickenPose()
    {
        if (!isShowingHeldChicken || currentHandObject == null)
            return;

        if (heldChickenAnchor == null || !heldChickenAnchor.gameObject.activeInHierarchy)
        {
            heldChickenAnchor = FindHandAnchor();
        }

        Transform anchor = heldChickenAnchor != null ? heldChickenAnchor : handPoint;
        if (anchor == null)
            return;

        Vector3 targetCenter =
            anchor.position +
            transform.right * HeldChickenCenterOffset.x +
            transform.up * HeldChickenCenterOffset.y +
            transform.forward * HeldChickenCenterOffset.z;

        currentHandObject.transform.rotation = transform.rotation * HeldChickenRotationOffset;

        if (TryGetHeldObjectRenderBounds(currentHandObject, out Bounds heldBounds))
        {
            currentHandObject.transform.position += targetCenter - heldBounds.center;
        }
        else
        {
            currentHandObject.transform.position = targetCenter;
        }
    }

    private Transform FindHandAnchor()
    {
        Animator[] animators = GetComponentsInChildren<Animator>(true);

        foreach (Animator animator in animators)
        {
            Transform rightHand = GetRightHandBone(animator, true);
            if (rightHand != null)
                return rightHand;
        }

        Transform namedRightHand = FindNamedRightHand(true);
        if (namedRightHand != null)
            return namedRightHand;

        foreach (Animator animator in animators)
        {
            Transform rightHand = GetRightHandBone(animator, false);
            if (rightHand != null)
                return rightHand;
        }

        namedRightHand = FindNamedRightHand(false);
        if (namedRightHand != null)
            return namedRightHand;

        return handPoint;
    }

    private static Transform GetRightHandBone(Animator animator, bool requireActive)
    {
        if (animator == null || !animator.isHuman)
            return null;

        if (requireActive && !animator.gameObject.activeInHierarchy)
            return null;

        return animator.GetBoneTransform(HumanBodyBones.RightHand);
    }

    private Transform FindNamedRightHand(bool requireActive)
    {
        Transform[] transforms = GetComponentsInChildren<Transform>(true);
        foreach (Transform candidate in transforms)
        {
            if (candidate == null)
                continue;

            if (requireActive && !candidate.gameObject.activeInHierarchy)
                continue;

            if (candidate.name == "mixamorig:RightHand" || candidate.name == "RightHand")
                return candidate;
        }

        return null;
    }

    private static bool TryGetHeldObjectRenderBounds(GameObject heldObject, out Bounds result)
    {
        result = default;

        if (heldObject == null)
            return false;

        Renderer[] renderers = heldObject.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;

        foreach (Renderer targetRenderer in renderers)
        {
            if (targetRenderer == null || targetRenderer is ParticleSystemRenderer)
                continue;

            if (!targetRenderer.enabled)
                continue;

            if (!hasBounds)
            {
                result = targetRenderer.bounds;
                hasBounds = true;
            }
            else
            {
                result.Encapsulate(targetRenderer.bounds);
            }
        }

        return hasBounds;
    }

    private static void DisableHeldChickenGameplayComponents(GameObject heldObject)
    {
        if (heldObject == null)
            return;

        ChickenController chickenController = heldObject.GetComponent<ChickenController>();
        if (chickenController != null)
        {
            chickenController.enabled = false;
        }

        UnityEngine.AI.NavMeshAgent navMeshAgent = heldObject.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (navMeshAgent != null)
        {
            navMeshAgent.enabled = false;
        }

        Collider[] colliders = heldObject.GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
        {
            if (collider != null)
            {
                collider.enabled = false;
            }
        }
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

        if (playerInventoryUI != null)
        {
            playerInventoryUI.RebindToLocalInventory();
        }
    }

    private void EnsureLocalInventory()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsListening)
        {
            if (playerInventory == null)
            {
                playerInventory = GetComponent<PlayerInventory>();
            }
            return;
        }

        if (IsSpawned)
        {
            if (!IsOwner)
                return;

            PlayerInventory ownInventory = GetComponent<PlayerInventory>();
            if (ownInventory == null)
            {
                ownInventory = GetComponentInChildren<PlayerInventory>();
            }

            if (ownInventory != null)
            {
                SetPlayerInventory(ownInventory);
                return;
            }
        }

        if (playerInventory != null && playerInventory.gameObject.activeInHierarchy)
        {
            PlayerMovement currentMovement = playerInventory.GetComponentInParent<PlayerMovement>();
            if (currentMovement != null && currentMovement.IsSpawned && currentMovement.IsOwner)
                return;
        }

        PlayerMovement[] movements = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        foreach (PlayerMovement movement in movements)
        {
            if (movement == null || !movement.IsSpawned || !movement.IsOwner)
                continue;

            PlayerInventory localInventory = movement.GetComponent<PlayerInventory>();
            if (localInventory == null)
            {
                localInventory = movement.GetComponentInChildren<PlayerInventory>();
            }

            if (localInventory == null)
                continue;

            SetPlayerInventory(localInventory);
            return;
        }
    }

    private void SetPlayerInventory(PlayerInventory inventory)
    {
        if (inventory == null || playerInventory == inventory)
            return;

        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged -= OnInventoryChanged;
        }

        playerInventory = inventory;
        playerInventory.OnInventoryChanged -= OnInventoryChanged;
        playerInventory.OnInventoryChanged += OnInventoryChanged;
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
