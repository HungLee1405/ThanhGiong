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

    [Header("Animation")]
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private RuntimeAnimatorController animatorController;
    [SerializeField] private Avatar avatar;
    [SerializeField] private float runSpeedThreshold = 0.75f;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    private float cameraPitch = 0f;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private static readonly int JumpHash = Animator.StringToHash("Jump");

    void Start()
    {
        controller = GetComponent<CharacterController>();
        InitializeAnimator();

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
        {
            UpdateMovementAnimation(Vector2.zero, false);
            return;
        }

        if (NetworkPlayerAppearance.IsLocalSelectionOpen)
        {
            UpdateMovementAnimation(Vector2.zero, false);
            return;
        }

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

            bool wantsToRun = Keyboard.current.leftShiftKey.isPressed
                || Keyboard.current.rightShiftKey.isPressed;
            UpdateMovementAnimation(input, wantsToRun);

            // Nhảy bằng phím Space
            if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                TriggerJumpAnimation();
            }
        }
        else
        {
            UpdateMovementAnimation(Vector2.zero, false);
        }

        Vector3 move = transform.right * input.x + transform.forward * input.y;
        controller.Move(move * moveSpeed * Time.deltaTime);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void InitializeAnimator()
    {
        if (characterAnimator == null)
        {
            characterAnimator = GetComponentInChildren<Animator>(true);
        }

        if (characterAnimator == null)
        {
            SkinnedMeshRenderer skinnedMesh = GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (skinnedMesh != null)
            {
                Transform animatorRoot = FindAnimatorRoot(skinnedMesh);
                characterAnimator = animatorRoot.gameObject.AddComponent<Animator>();
            }
        }

        if (characterAnimator == null)
            return;

        if (animatorController != null)
        {
            characterAnimator.runtimeAnimatorController = animatorController;
        }

        if (avatar != null)
        {
            characterAnimator.avatar = avatar;
        }

        characterAnimator.applyRootMotion = false;
        characterAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
    }

    private Transform FindAnimatorRoot(SkinnedMeshRenderer skinnedMesh)
    {
        Transform animatorRoot = skinnedMesh.rootBone != null ? skinnedMesh.rootBone : skinnedMesh.transform;

        while (animatorRoot.parent != null && animatorRoot.parent != transform)
        {
            animatorRoot = animatorRoot.parent;
        }

        return animatorRoot;
    }

    private void UpdateMovementAnimation(Vector2 input, bool wantsToRun)
    {
        if (characterAnimator == null)
            return;

        float speed = input.magnitude;
        bool isRunning = speed >= runSpeedThreshold && wantsToRun;

        characterAnimator.SetFloat(SpeedHash, speed);
        characterAnimator.SetBool(IsRunningHash, isRunning);
    }

    private void TriggerJumpAnimation()
    {
        if (characterAnimator == null)
            return;

        characterAnimator.SetTrigger(JumpHash);
    }
}
