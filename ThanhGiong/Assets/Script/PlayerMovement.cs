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
    public float fadeSpeed = 10f;         // Tốc độ tăng/giảm âm lượng để tiếng ngắt mượt mà

    [Header("Landing Audio")]
    public AudioClip landingClip;         // File âm thanh tiếng tiếp đất
    private bool wasGrounded;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    private float cameraPitch = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (CanUseLocalInput() && !MultiplayerConnector.IsRoomMenuOpen)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Update()
    {
        if (!CanUseLocalInput())
            return;

        if (MultiplayerConnector.IsRoomMenuOpen)
            return;

        if (NetworkPlayerAppearance.IsLocalSelectionOpen)
            return;

        if (CookingMenuUI.IsMenuOpen)
            return;

        HandleMouseLook();
        HandleMovement();
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

        if (footstepSource != null)
        {
            if (isGrounded && input.sqrMagnitude > 0.01f)
            {
                // Nếu loa đang bị tắt (hoặc game vừa mở), cho loa phát lại
                if (!footstepSource.isPlaying)
                {
                    footstepSource.Play();
                }

                // Tăng dần âm lượng lên 1 (To tối đa) một cách mượt mà
                footstepSource.volume = Mathf.MoveTowards(footstepSource.volume, 1f, Time.deltaTime * fadeSpeed);
            }
            else
            {
                // Nếu đứng im hoặc đang trên không: Giảm dần âm lượng về 0
                footstepSource.volume = Mathf.MoveTowards(footstepSource.volume, 0f, Time.deltaTime * fadeSpeed);

                // Khi âm lượng đã về hẳn bằng 0 thì tạm dừng loa để tiết kiệm tài nguyên
                if (footstepSource.volume <= 0f && footstepSource.isPlaying)
                {
                    footstepSource.Stop();
                }
            }
        }
    }

    public void ResetVelocity()
    {
        velocity = Vector3.zero;
    }
}
