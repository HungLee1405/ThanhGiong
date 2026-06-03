using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;

    [Header("Look Settings")]
    public Transform cameraTransform;
    public float lookSensitivity = 0.15f;
    public float upperLookLimit = 80f;
    public float lowerLookLimit = -80f;

    private CharacterController characterController;
    private Vector3 velocity;
    private bool isGrounded;

    private float rotationX = 0f;
    private float rotationY = 0f;

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        // Khóa con trỏ chuột vào giữa màn hình để chơi game
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Nếu chưa kéo Camera vào, tự động tìm Camera chính
        if (cameraTransform == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                cameraTransform = mainCam.transform;
            }
        }

        // Khởi tạo hướng xoay ban đầu
        Vector3 rot = transform.localRotation.eulerAngles;
        rotationX = rot.y;
        
        if (cameraTransform != null)
        {
            rotationY = cameraTransform.localRotation.eulerAngles.x;
        }
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;

        if (keyboard == null || mouse == null) return;

        // --- Kiểm tra mặt đất ---
        isGrounded = characterController.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Giữ người chơi bám mặt đất ổn định
        }

        // --- Xoay camera & Nhân vật theo chuột ---
        Vector2 mouseDelta = mouse.delta.ReadValue();
        rotationX += mouseDelta.x * lookSensitivity;
        rotationY -= mouseDelta.y * lookSensitivity;
        rotationY = Mathf.Clamp(rotationY, lowerLookLimit, upperLookLimit);

        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(rotationY, 0f, 0f);
        }
        transform.rotation = Quaternion.Euler(0f, rotationX, 0f);

        // --- Di chuyển WASD ---
        float forwardInput = 0f;
        float rightInput = 0f;

        if (keyboard.wKey.isPressed) forwardInput += 1f;
        if (keyboard.sKey.isPressed) forwardInput -= 1f;
        if (keyboard.dKey.isPressed) rightInput += 1f;
        if (keyboard.aKey.isPressed) rightInput -= 1f;

        // Tính hướng di chuyển theo góc quay của nhân vật
        Vector3 moveDirection = (transform.forward * forwardInput + transform.right * rightInput);
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            moveDirection.Normalize();
        }

        // --- Nhảy (Jump) ---
        if (keyboard.spaceKey.wasPressedThisFrame && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // --- Trọng lực (Gravity) ---
        velocity.y += gravity * Time.deltaTime;

        // --- Di chuyển tổng hợp (Một lệnh Move duy nhất để tránh lỗi kẹt vật lý) ---
        Vector3 finalVelocity = moveDirection * moveSpeed;
        finalVelocity.y = velocity.y;

        characterController.Move(finalVelocity * Time.deltaTime);
    }
}
