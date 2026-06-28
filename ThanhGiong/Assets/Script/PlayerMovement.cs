using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float runSpeed = 15f;
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
    [SerializeField] private float groundedGraceTime = 0.12f;

    [Header("Footstep Audio")]
    public AudioSource footstepSource;
    public AudioClip footstepClip;
    public float fadeSpeed = 10f;

    [Header("Landing Audio")]
    public AudioClip landingClip;

    [Header("Animation")]
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private RuntimeAnimatorController animatorController;
    [SerializeField] private Avatar avatar;
    [SerializeField] private float runSpeedThreshold = 0.75f;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private float lastGroundedTime;
    private float cameraPitch;
    private Vector3 lastObservedPosition;
    private bool externallyBoundAnimator;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private static readonly int JumpHash = Animator.StringToHash("Jump");

    private void Start()
    {
        controller = GetComponent<CharacterController>();

        if (GetComponent<PlayerRespawnController>() == null)
        {
            gameObject.AddComponent<PlayerRespawnController>();
        }

        InitializeFootstepSource();
        InitializeAnimator();
        lastObservedPosition = transform.position;

        if (CanUseLocalInput() && !MultiplayerConnector.IsRoomMenuOpen)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Update()
    {
        if (PauseMenuManager.isPaused)
            return;

        if (!CanUseLocalInput())
        {
            HandleRemotePresentation();
            return;
        }

        if (MultiplayerConnector.IsRoomMenuOpen ||
            NetworkPlayerAppearance.IsLocalSelectionOpen ||
            CookingMenuUI.IsMenuOpen)
        {
            UpdateMovementAnimation(Vector2.zero, false);
            UpdateFootstepAudio(false);
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

    private void HandleMouseLook()
    {
        if (Mouse.current == null || playerCamera == null)
            return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        float mouseX = mouseDelta.x * mouseSensitivity * Time.deltaTime * 50f;
        float mouseY = mouseDelta.y * mouseSensitivity * Time.deltaTime * 50f;

        transform.Rotate(Vector3.up * mouseX);
        cameraPitch = Mathf.Clamp(cameraPitch - mouseY, -maxLookAngle, maxLookAngle);
        playerCamera.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    private void HandleMovement()
    {
        isGrounded = IsGrounded();

        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
            lastGroundedTime = Time.time;
        }

        Vector2 input = ReadMoveInput();
        bool wantsToRun = Keyboard.current != null &&
            (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame &&
            Time.time - lastGroundedTime <= groundedGraceTime)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            lastGroundedTime = float.NegativeInfinity;
            TriggerJumpAnimation();
            BroadcastJumpAnimation();
        }

        UpdateMovementAnimation(input, wantsToRun);

        Vector3 move = transform.right * input.x + transform.forward * input.y;
        float currentSpeed = wantsToRun && input.sqrMagnitude > 0.01f ? runSpeed : moveSpeed;
        controller.Move(move * currentSpeed * Time.deltaTime);
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        UpdateFootstepAudio(isGrounded && input.sqrMagnitude > 0.01f, wantsToRun);
        lastObservedPosition = transform.position;
    }

    private void HandleRemotePresentation()
    {
        Vector3 displacement = transform.position - lastObservedPosition;
        displacement.y = 0f;
        lastObservedPosition = transform.position;

        float speed = Time.deltaTime > 0f ? displacement.magnitude / Time.deltaTime : 0f;
        bool moving = speed > 0.05f;
        bool running = speed > (moveSpeed + runSpeed) * 0.5f * runSpeedThreshold;
        bool remoteGrounded = IsGrounded();

        UpdateMovementAnimation(moving ? Vector2.up : Vector2.zero, running);
        UpdateFootstepAudio(remoteGrounded && moving, running);
    }

    private Vector2 ReadMoveInput()
    {
        Vector2 input = Vector2.zero;
        if (Keyboard.current == null) return input;

        if (Keyboard.current.aKey.isPressed) input.x -= 1f;
        if (Keyboard.current.dKey.isPressed) input.x += 1f;
        if (Keyboard.current.wKey.isPressed) input.y += 1f;
        if (Keyboard.current.sKey.isPressed) input.y -= 1f;
        return input.normalized;
    }

    private bool IsGrounded()
    {
        bool controllerGrounded = controller != null && controller.isGrounded;
        if (groundCheck == null) return controllerGrounded;

        int collisionMask = groundMask.value != 0 ? groundMask.value : Physics.DefaultRaycastLayers;
        return controllerGrounded || Physics.CheckSphere(
            groundCheck.position,
            Mathf.Max(groundDistance, 0.05f),
            collisionMask,
            QueryTriggerInteraction.Ignore);
    }

    private void InitializeFootstepSource()
    {
        if (footstepSource == null && footstepClip != null)
        {
            GameObject audioChild = new GameObject("FootstepAudio");
            audioChild.transform.SetParent(transform, false);
            footstepSource = audioChild.AddComponent<AudioSource>();
        }

        if (footstepSource == null)
        {
            footstepSource = GetComponent<AudioSource>();
        }

        if (footstepSource == null)
        {
            footstepSource = gameObject.AddComponent<AudioSource>();
        }

        if (footstepSource.clip == null && footstepClip != null)
        {
            footstepSource.clip = footstepClip;
        }

        footstepSource.playOnAwake = false;
        footstepSource.loop = true;
        footstepSource.spatialBlend = 1f;
        footstepSource.volume = 0f;
    }

    private void UpdateFootstepAudio(bool shouldPlay, bool isRunning = false)
    {
        if (footstepSource == null || footstepSource.clip == null) return;

        if (shouldPlay && !footstepSource.isPlaying)
        {
            footstepSource.Play();
        }

        float targetVolume = shouldPlay ? (isRunning ? 1.0f : 0.7f) : 0f;
        float targetPitch = isRunning ? 1.35f : 1.0f;

        footstepSource.volume = Mathf.MoveTowards(
            footstepSource.volume,
            targetVolume,
            Time.deltaTime * fadeSpeed);

        footstepSource.pitch = Mathf.MoveTowards(
            footstepSource.pitch,
            targetPitch,
            Time.deltaTime * fadeSpeed);

        if (!shouldPlay && footstepSource.volume <= 0f && footstepSource.isPlaying)
        {
            footstepSource.Stop();
        }
    }

    private void InitializeAnimator()
    {
        if (characterAnimator == null)
        {
            characterAnimator = GetComponentInChildren<Animator>(true);
        }

        ConfigureAnimator(!externallyBoundAnimator);
    }

    public void BindCharacterAnimator(Animator animator)
    {
        if (animator == null) return;
        characterAnimator = animator;
        externallyBoundAnimator = true;
        ConfigureAnimator(false);
    }

    private void ConfigureAnimator(bool allowSerializedAvatar)
    {
        if (characterAnimator == null) return;

        if (animatorController != null)
            characterAnimator.runtimeAnimatorController = animatorController;

        if (avatar != null && allowSerializedAvatar)
            characterAnimator.avatar = avatar;

        characterAnimator.enabled = true;
        characterAnimator.applyRootMotion = false;
        characterAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        characterAnimator.Rebind();
        characterAnimator.Update(0f);
    }

    private void UpdateMovementAnimation(Vector2 input, bool wantsToRun)
    {
        if (characterAnimator == null) return;

        float speed = input.magnitude;
        characterAnimator.SetFloat(SpeedHash, speed);
        characterAnimator.SetBool(IsRunningHash, speed >= runSpeedThreshold && wantsToRun);
    }

    private void TriggerJumpAnimation()
    {
        if (characterAnimator != null)
            characterAnimator.SetTrigger(JumpHash);
    }

    private void BroadcastJumpAnimation()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsListening || !IsSpawned || !IsOwner)
            return;

        TriggerJumpAnimationServerRpc();
    }

    [ServerRpc]
    private void TriggerJumpAnimationServerRpc()
    {
        TriggerJumpAnimationClientRpc();
    }

    [ClientRpc]
    private void TriggerJumpAnimationClientRpc()
    {
        if (IsOwner)
            return;

        TriggerJumpAnimation();
    }

    public void ResetVelocity()
    {
        velocity = Vector3.zero;
    }
}
