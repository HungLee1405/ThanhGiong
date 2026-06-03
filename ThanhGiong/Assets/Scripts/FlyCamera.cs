using UnityEngine;
using UnityEngine.InputSystem;

public class FlyCamera : MonoBehaviour
{
    [Header("Movement Settings")]
    public float movementSpeed = 15f;
    public float fastSpeedMultiplier = 2.5f;
    public float lookSensitivity = 0.1f;

    private float rotationX = 0f;
    private float rotationY = 0f;

    void Start()
    {
        Vector3 rot = transform.localRotation.eulerAngles;
        rotationX = rot.y;
        rotationY = -rot.x;
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;

        if (keyboard == null || mouse == null) return;

        // Giữ chuột phải để xoay camera nhìn xung quanh
        if (mouse.rightButton.isPressed)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            Vector2 mouseDelta = mouse.delta.ReadValue();
            rotationX += mouseDelta.x * lookSensitivity;
            rotationY += mouseDelta.y * lookSensitivity;
            rotationY = Mathf.Clamp(rotationY, -90f, 90f);

            transform.localRotation = Quaternion.Euler(-rotationY, rotationX, 0f);
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Tốc độ di chuyển (nhấn Shift để bay nhanh hơn)
        float speed = movementSpeed;
        if (keyboard.leftShiftKey.isPressed)
        {
            speed *= fastSpeedMultiplier;
        }

        // Tính toán hướng di chuyển WASD + EQ
        float forwardInput = 0f;
        float rightInput = 0f;
        float upInput = 0f;

        if (keyboard.wKey.isPressed) forwardInput += 1f;
        if (keyboard.sKey.isPressed) forwardInput -= 1f;
        if (keyboard.dKey.isPressed) rightInput += 1f;
        if (keyboard.aKey.isPressed) rightInput -= 1f;
        if (keyboard.eKey.isPressed) upInput += 1f;
        if (keyboard.qKey.isPressed) upInput -= 1f;

        // Chuẩn hóa vector di chuyển để không bị đi nhanh hơn khi đi chéo
        Vector3 direction = new Vector3(rightInput, upInput, forwardInput);
        if (direction.sqrMagnitude > 0.01f)
        {
            direction.Normalize();
        }
        
        transform.Translate(direction * speed * Time.deltaTime);
    }
}
