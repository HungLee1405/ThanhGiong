using Unity.Netcode;
using UnityEngine;

public class OfflinePlayerDisabler : MonoBehaviour
{
    private bool subscribed;

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        if (subscribed && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= DisableOfflinePlayer;
            NetworkManager.Singleton.OnClientStarted -= DisableOfflinePlayer;
        }

        subscribed = false;
    }

    private void Start()
    {
        TrySubscribe();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            DisableOfflinePlayer();
        }
    }

    private void Update()
    {
        if (!subscribed)
        {
            TrySubscribe();
        }

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            DisableOfflinePlayer();
        }
    }

    private void TrySubscribe()
    {
        if (subscribed || NetworkManager.Singleton == null)
            return;

        NetworkManager.Singleton.OnServerStarted += DisableOfflinePlayer;
        NetworkManager.Singleton.OnClientStarted += DisableOfflinePlayer;
        subscribed = true;
    }

    private void DisableOfflinePlayer()
    {
        gameObject.SetActive(false);
    }
}
