using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpHeight = 1.5f;
    public float gravity = -25f;

    [Header("Mouse Look")]
    public Transform playerCamera;
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 80f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.3f;
    public LayerMask groundMask;

    [Header("Footstep Audio (Vòng lặp)")]
    public AudioSource footstepSource;    // Nguồn phát tiếng bước chân (đã bật Loop)
    public AudioClip footstepClip;
    public float fadeSpeed = 10f;         // Tốc độ tăng/giảm âm lượng để tiếng ngắt mượt mà

    [Header("Landing Audio")]
    public AudioClip landingClip;         // File âm thanh tiếng tiếp đất
    private bool wasGrounded;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    private float cameraPitch = 0f;
    private Vector3 lastObservedPosition;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (GetComponent<PlayerRespawnController>() == null)
        {
            gameObject.AddComponent<PlayerRespawnController>();
        }

        // Prefer a dedicated footstep source when a clip is configured. This also
        // avoids accidentally reusing the player's music or UI AudioSource.
        if (footstepSource == null && footstepClip != null)
        {
            GameObject audioChild = new GameObject("FootstepAudio");
            audioChild.transform.SetParent(transform, false);
            footstepSource = audioChild.AddComponent<AudioSource>();
            footstepSource.spatialBlend = 1f;
            footstepSource.volume = 0f;
        }

        // Tự tìm AudioSource nếu chưa được gán trong Inspector
        // Ưu tiên AudioSource đã có clip (footstep loop)
        if (footstepSource == null)
        {
            AudioSource[] sources = GetComponentsInChildren<AudioSource>(true);
            foreach (AudioSource src in sources)
            {
                if (src.loop || src.clip != null)
                {
                    footstepSource = src;
                    break;
                }
            }

            // Nếu không có loop/clip, lấy cái đầu tiên
            if (footstepSource == null && sources.Length > 0)
            {
                footstepSource = sources[0];
            }
        }

        // Nếu vẫn không có (NetworkPlayer prefab chưa có AudioSource),
        // tự tạo một AudioSource mới
        if (footstepSource == null)
        {
            GameObject audioChild = new GameObject("FootstepAudio");
            audioChild.transform.SetParent(transform, false);
            footstepSource = audioChild.AddComponent<AudioSource>();
            footstepSource.loop = true;
            footstepSource.spatialBlend = 1f; // 3D sound
            footstepSource.volume = 0f;
            footstepSource.playOnAwake = false;
        }

        // NetworkPlayer is generated separately from the offline player. Keep the
        // clip here as a fallback so a runtime AudioSource is never left empty.
        if (footstepSource.clip == null && footstepClip != null)
        {
            footstepSource.clip = footstepClip;
        }

        footstepSource.loop = true;
        footstepSource.playOnAwake = false;
        lastObservedPosition = transform.position;

        if (CanUseLocalInput() && !MultiplayerConnector.IsRoomMenuOpen)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Update()
    {
        if (PauseMenuManager.isPaused)
            return;

        if (!CanUseLocalInput())
        {
            HandleRemoteFootsteps();
            return;
        }

        if (MultiplayerConnector.IsRoomMenuOpen)
            return;

        if (NetworkPlayerAppearance.IsLocalSelectionOpen)
            return;

        if (CookingMenuUI.IsMenuOpen)
            return;

        HandleMouseLook();
        HandleMovement();
    }

    private void HandleRemoteFootsteps()
    {
        Vector3 displacement = transform.position - lastObservedPosition;
        displacement.y = 0f;
        lastObservedPosition = transform.position;

        bool remoteGrounded = groundCheck != null && Physics.CheckSphere(
            groundCheck.position,
            groundDistance,
            groundMask);

        UpdateFootstepAudio(remoteGrounded && displacement.sqrMagnitude > 0.000001f);
    }

    private void UpdateFootstepAudio(bool shouldPlay)
    {
        if (footstepSource == null) return;

        float targetVolume = shouldPlay ? 1f : 0f;

        if (shouldPlay && !footstepSource.isPlaying)
        {
            footstepSource.Play();
        }

        footstepSource.volume = Mathf.MoveTowards(
            footstepSource.volume,
            targetVolume,
            Time.deltaTime * fadeSpeed);

        if (!shouldPlay && footstepSource.volume <= 0f && footstepSource.isPlaying)
        {
            footstepSource.Stop();
        }
    }

    private bool CanUseLocalInput()
    {
        NetworkManager networkManager = NetworkManager.Singleton;

        if (networkManager == null || !networkManager.IsListening)
            return true;

        return !IsSpawned || IsOwner;
    }

    void HandleMouseLook()
    {
        if (Mouse.current == null || playerCamera == null)
            return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        float mouseX = mouseDelta.x * mouseSensitivity * Time.deltaTime * 50f;
        float mouseY = mouseDelta.y * mouseSensitivity * Time.deltaTime * 50f;

        // Xoay thân Player trái/phải
        transform.Rotate(Vector3.up * mouseX);

        // Xoay Camera lên/xuống
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -maxLookAngle, maxLookAngle);

        playerCamera.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    void HandleMovement()
    {
        isGrounded = Physics.CheckSphere(
            groundCheck.position,
            groundDistance,
            groundMask
        );

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        Vector2 input = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed)
                input.x -= 1f;

            if (Keyboard.current.dKey.isPressed)
                input.x += 1f;

            if (Keyboard.current.wKey.isPressed)
                input.y += 1f;

            if (Keyboard.current.sKey.isPressed)
                input.y -= 1f;

            input = input.normalized;

            // Nhảy bằng phím Space
            if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }

        Vector3 move = transform.right * input.x + transform.forward * input.y;
        controller.Move(move * moveSpeed * Time.deltaTime);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        UpdateFootstepAudio(isGrounded && input.sqrMagnitude > 0.01f);
        lastObservedPosition = transform.position;
    }

    public void ResetVelocity()
    {
        velocity = Vector3.zero;
    }
}
