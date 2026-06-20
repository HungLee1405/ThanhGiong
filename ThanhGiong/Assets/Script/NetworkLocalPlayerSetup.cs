using Unity.Netcode;
using UnityEngine;

public class NetworkLocalPlayerSetup : NetworkBehaviour
{
    [Header("Local Only")]
    public Camera playerCamera;
    public AudioListener audioListener;
    public Canvas[] localCanvases;

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

    private void ApplyOwnershipState()
    {
        SetLocalObjectsActive(IsOwner);
    }

    private void SetLocalObjectsActive(bool isLocalPlayer)
    {
        if (playerCamera != null)
        {
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
}
