using Unity.Netcode;
using UnityEngine;

public class NetworkLocalPlayerSetup : NetworkBehaviour
{
    [Header("Local Only")]
    public Camera playerCamera;
    public AudioListener audioListener;
    public Canvas[] localCanvases;

    [Header("Camera Pose")]
    [SerializeField] private Vector3 localCameraPosition = new Vector3(-0.008f, 2.762f, 0.536f);
    [SerializeField] private Vector3 localCameraEulerAngles = Vector3.zero;

    private bool ownsLocalView;

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>(true);
        }

        if (audioListener == null && playerCamera != null)
        {
            audioListener = playerCamera.GetComponent<AudioListener>();
        }
    }

    public override void OnNetworkSpawn()
    {
        ApplyOwnershipState();
    }

    private void Start()
    {
        NetworkManager networkManager = NetworkManager.Singleton;

        if (networkManager == null || !networkManager.IsListening)
        {
            SetLocalObjectsActive(true);
        }
    }

    private void LateUpdate()
    {
        if (!ownsLocalView || playerCamera == null)
            return;

        Transform cameraTransform = playerCamera.transform;
        if ((cameraTransform.localPosition - localCameraPosition).sqrMagnitude > 0.000001f)
        {
            cameraTransform.localPosition = localCameraPosition;
        }
    }

    private void ApplyOwnershipState()
    {
        SetLocalObjectsActive(IsOwner);
    }

    private void SetLocalObjectsActive(bool isLocalPlayer)
    {
        ownsLocalView = isLocalPlayer;

        if (playerCamera != null)
        {
            if (isLocalPlayer)
            {
                ApplyLocalCameraPose();
            }

            playerCamera.enabled = isLocalPlayer;
        }

        if (audioListener != null)
        {
            audioListener.enabled = isLocalPlayer;
        }

        if (localCanvases == null)
            return;

        for (int i = 0; i < localCanvases.Length; i++)
        {
            if (localCanvases[i] != null)
            {
                localCanvases[i].enabled = isLocalPlayer;
            }
        }
    }

    private void ApplyLocalCameraPose()
    {
        Transform cameraTransform = playerCamera.transform;
        cameraTransform.localPosition = localCameraPosition;
        cameraTransform.localRotation = Quaternion.Euler(localCameraEulerAngles);
    }
}
