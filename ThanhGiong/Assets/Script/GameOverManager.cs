using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    [Header("UI Thua Game")]
    public GameObject loseMenuPanel;

    [Header("Âm thanh (Tùy chọn)")]
    public AudioSource uiSource;
    public AudioClip loseSound;
    public AudioClip clickSound;

    void Start()
    {
        // Luôn giấu bảng thua game khi mới bắt đầu chơi
        if (loseMenuPanel != null)
        {
            loseMenuPanel.SetActive(false);
        }
    }

    // Hàm này sẽ được gọi khi nhân vật chết (hết máu, rơi xuống vực...)
    public void TriggerGameOver()
    {
        loseMenuPanel.SetActive(true);

        // Đóng băng game và hiện chuột
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Phát tiếng thua game (nếu có)
        if (uiSource != null && loseSound != null)
        {
            uiSource.PlayOneShot(loseSound);
        }
    }

    public void RestartGame()
    {
        PlayClick();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitToMenu()
    {
        PlayClick();
        Time.timeScale = 1f;
        SceneManager.LoadScene("StartScene");
    }

    private void PlayClick()
    {
        if (uiSource != null && clickSound != null)
        {
            uiSource.PlayOneShot(clickSound);
        }
    }
}