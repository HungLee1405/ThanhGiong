using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.SceneManagement;
#if UNITY_NETCODE
using Unity.Netcode;
#endif

public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance { get; private set; }

    [Header("UI References")]
    public GameObject panel;
    public TMP_Text titleText;
    public TMP_Text descriptionText;
    public Button retryButton;
    public TMP_Text retryButtonText;
    public Button mainMenuButton;

    private Action onRetryAction;

    private void Awake()
    {
        // Simple Singleton pattern for easy access
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (panel != null) panel.SetActive(false);

        if (retryButton != null) retryButton.onClick.AddListener(OnRetryClicked);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenuClicked);
    }

    public void ShowGameOver(string title, string reason, bool isDeath, Action onRetry = null)
    {
        if (panel != null) panel.SetActive(true);

        if (titleText != null) titleText.text = title;
        if (descriptionText != null) descriptionText.text = reason;

        onRetryAction = onRetry;

        if (retryButtonText != null)
        {
            if (isDeath)
            {
                retryButtonText.text = "Hồi sinh";
            }
            else
            {
                retryButtonText.text = "Thử lại";
            }
        }
        
        // Unlock and show cursor so player can click UI buttons
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnRetryClicked()
    {
        if (panel != null) panel.SetActive(false);

        if (onRetryAction != null)
        {
            onRetryAction.Invoke();
        }
        else
        {
            // Default behavior if no action provided (e.g. for Day Failed)
            // If multiplayer, we might need to shutdown first before reloading scene
            if (Unity.Netcode.NetworkManager.Singleton != null)
            {
                Unity.Netcode.NetworkManager.Singleton.Shutdown();
            }
            // Reload current scene (Assuming offline retry or returning to lobby)
            // Mặc định nạp lại Scene 0 (thường là MainMenu) khi thất bại hoàn toàn để an toàn, 
            // hoặc nạp lại scene hiện tại nếu chơi đơn.
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void OnMainMenuClicked()
    {
        if (Unity.Netcode.NetworkManager.Singleton != null)
        {
            Unity.Netcode.NetworkManager.Singleton.Shutdown();
        }
        // Load Main Menu scene, assuming index 0 is MainMenu
        SceneManager.LoadScene(0); 
    }
}
