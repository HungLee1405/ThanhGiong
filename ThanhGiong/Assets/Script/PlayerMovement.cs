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
    [SerializeField] private float groundedGraceTime = 0.12f;

    [Header("Footstep Audio")]
    public AudioSource footstepSource;
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

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private static readonly int JumpHash = Animator.StringToHash("Jump");

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        InitializeFootstepSource();
        InitializeAnimator();

        if (CanUseLocalInput() && !MultiplayerConnector.IsRoomMenuOpen)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Update()
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

        if (CookingMenuUI.IsMenuOpen)
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

    private void HandleMouseLook()
    {
        if (Mouse.current == null || playerCamera == null)
            return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        float mouseX = mouseDelta.x * mouseSensitivity * Time.deltaTime * 50f;
        float mouseY = mouseDelta.y * mouseSensitivity * Time.deltaTime * 50f;

        transform.Rotate(Vector3.up * mouseX);

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -maxLookAngle, maxLookAngle);

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
        bool wantsToRun = Keyboard.current != null
            && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);

        bool canJump = Time.time - lastGroundedTime <= groundedGraceTime;

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && canJump)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            lastGroundedTime = float.NegativeInfinity;
            TriggerJumpAnimation();
        }

        UpdateMovementAnimation(input, wantsToRun);

        Vector3 move = transform.right * input.x + transform.forward * input.y;
        controller.Move(move * moveSpeed * Time.deltaTime);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        UpdateFootstepAudio(input);
    }

    private Vector2 ReadMoveInput()
    {
        Vector2 input = Vector2.zero;

        if (Keyboard.current == null)
            return input;

        if (Keyboard.current.aKey.isPressed)
            input.x -= 1f;

        if (Keyboard.current.dKey.isPressed)
            input.x += 1f;

        if (Keyboard.current.wKey.isPressed)
            input.y += 1f;

        if (Keyboard.current.sKey.isPressed)
            input.y -= 1f;

        return input.normalized;
    }

    private bool IsGrounded()
    {
        bool controllerGrounded = controller != null && controller.isGrounded;

        if (groundCheck == null)
            return controllerGrounded;

        int collisionMask = groundMask.value != 0 ? groundMask.value : Physics.DefaultRaycastLayers;
        bool sphereGrounded = Physics.CheckSphere(
            groundCheck.position,
            Mathf.Max(groundDistance, 0.05f),
            collisionMask,
            QueryTriggerInteraction.Ignore);

        return controllerGrounded || sphereGrounded;
    }

    private void InitializeAnimator()
    {
        if (characterAnimator == null)
        {
            Transform defaultVisualRoot = transform.Find("Idle");
            if (defaultVisualRoot != null)
            {
                characterAnimator = defaultVisualRoot.GetComponentInChildren<Animator>(true);
            }
        }

        if (characterAnimator == null)
        {
            Transform defaultVisualRoot = transform.Find("Idle");
            SkinnedMeshRenderer skinnedMesh = defaultVisualRoot != null
                ? defaultVisualRoot.GetComponentInChildren<SkinnedMeshRenderer>(true)
                : GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (skinnedMesh != null)
            {
                Transform animatorRoot = FindAnimatorRoot(skinnedMesh);
                characterAnimator = animatorRoot.gameObject.AddComponent<Animator>();
            }
        }

        ConfigureAnimator(true);
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

    public void BindCharacterAnimator(Animator animator)
    {
        if (animator == null)
            return;

        characterAnimator = animator;
        ConfigureAnimator(false);
    }

    private void ConfigureAnimator(bool allowSerializedAvatar)
    {
        if (characterAnimator == null)
            return;

        if (animatorController != null)
        {
            characterAnimator.runtimeAnimatorController = animatorController;
        }

        if (avatar != null && allowSerializedAvatar)
        {
            characterAnimator.avatar = avatar;
        }

        if (characterAnimator.avatar == null)
        {
            Debug.LogWarning(
                $"Animator on '{characterAnimator.name}' has no Avatar. Assign the Avatar from the same character model, not IdleAvatar from another FBX.",
                characterAnimator);
        }

        characterAnimator.applyRootMotion = false;
        characterAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        characterAnimator.Rebind();
        characterAnimator.Update(0f);
    }

    private void TriggerJumpAnimation()
    {
        if (characterAnimator == null)
            return;

        characterAnimator.SetTrigger(JumpHash);
    }

    private void InitializeFootstepSource()
    {
        if (footstepSource == null)
        {
            footstepSource = GetComponent<AudioSource>();
        }

        if (footstepSource == null)
        {
            footstepSource = gameObject.AddComponent<AudioSource>();
        }

        footstepSource.playOnAwake = false;
        footstepSource.loop = true;
        footstepSource.volume = 0f;
    }

    private void UpdateFootstepAudio(Vector2 input)
    {
        if (footstepSource == null || footstepSource.clip == null)
            return;

        if (isGrounded && input.sqrMagnitude > 0.01f)
        {
            if (!footstepSource.isPlaying)
            {
                footstepSource.Play();
            }

            footstepSource.volume = Mathf.MoveTowards(footstepSource.volume, 1f, Time.deltaTime * fadeSpeed);
        }
        else
        {
            footstepSource.volume = Mathf.MoveTowards(footstepSource.volume, 0f, Time.deltaTime * fadeSpeed);

            if (footstepSource.volume <= 0f && footstepSource.isPlaying)
            {
                footstepSource.Stop();
            }
        }
    }

    public void ResetVelocity()
    {
        velocity = Vector3.zero;
    }
}
