using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using TMPro;
using Unity.Netcode;

public class PauseMenuManager : MonoBehaviour
{
    public static bool isPaused = false;

    [Header("Quản lý Các Bảng UI")]
    public GameObject pauseMenuPanel;     // Bảng nền đen tổng
    public GameObject mainPauseButtons;   // Bảng chứa 4 nút chính
    public GameObject inGameOptionsPanel; // Bảng chứa thanh trượt âm thanh

    [Header("Âm thanh Menu")]
    public AudioSource uiSource;         // Loa phát âm thanh UI trên GameManager
    public AudioClip buttonClickClip;    // (Tùy chọn) File âm thanh khi click các nút khác

    [Header("Cấu hình Âm thanh Audio Mixer")]
    public AudioMixer mainMixer;
    public Slider musicSlider;
    public Slider sfxSlider;
    public Image muteButtonImage;
    public Sprite soundOnIcon;
    public Sprite soundOffIcon;
    public TextMeshProUGUI muteText;

    private bool isMuted = false;

    void Start()
    {
        // 1. Đảm bảo UI luôn tắt khi vừa vào game
        if (pauseMenuPanel) pauseMenuPanel.SetActive(false);
        if (mainPauseButtons) mainPauseButtons.SetActive(true);
        if (inGameOptionsPanel) inGameOptionsPanel.SetActive(false);

        // 2. Tải giá trị âm thanh
        float savedMusic = PlayerPrefs.GetFloat("MusicVol", 1f);
        float savedSFX = PlayerPrefs.GetFloat("SFXVol", 1f);
        if (musicSlider) musicSlider.value = savedMusic;
        if (sfxSlider) sfxSlider.value = savedSFX;

        if (musicSlider) musicSlider.onValueChanged.AddListener(SetMusicVolume);
        if (sfxSlider) sfxSlider.onValueChanged.AddListener(SetSFXVolume);

        isMuted = PlayerPrefs.GetInt("IsMuted", 0) == 1;
        UpdateMuteUI();
    }

    void Update()
    {
        // Bấm phím ESC để mở/tắt Pause
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused)
            {
                if (inGameOptionsPanel.activeSelf) CloseOptions();
                else ResumeGame();
            }
            else PauseGame();
        }
    }

    public void ResumeGame()
    {
        PlayClickSound();
        pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f; // Chạy lại thời gian
        isPaused = false;

        // Ẩn và khóa chuột lại để tiếp tục chơi
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void PauseGame()
    {
        pauseMenuPanel.SetActive(true);
        mainPauseButtons.SetActive(true);
        inGameOptionsPanel.SetActive(false); // Luôn mở bảng 4 nút chính trước
        NetworkManager manager = NetworkManager.Singleton;
        Time.timeScale = manager != null && manager.IsListening ? 1f : 0f;
        inGameOptionsPanel.SetActive(false);
        Time.timeScale = 0f; // Đóng băng thời gian
        isPaused = true;

        // Hiện và thả tự do chuột để click UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OpenOptions()
    {
        PlayClickSound();
        mainPauseButtons.SetActive(false);
        inGameOptionsPanel.SetActive(true);
    }

    public void CloseOptions()
    {
        PlayClickSound();
        inGameOptionsPanel.SetActive(false);
        mainPauseButtons.SetActive(true);
    }

    public void RestartGame()
    {
        PlayClickSound();
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitToMenu()
    {
        PlayClickSound();
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene("StartScene"); // Đổi đúng tên Scene menu của bạn
    }

    // --- XỬ LÝ ÂM THANH ---
    public void SetMusicVolume(float value)
    {
        float dB = Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f;
        if (mainMixer) mainMixer.SetFloat("MusicVol", dB);
        PlayerPrefs.SetFloat("MusicVol", value);
    }

    public void SetSFXVolume(float value)
    {
        float dB = Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f;
        if (mainMixer) mainMixer.SetFloat("SFXVol", dB);
        PlayerPrefs.SetFloat("SFXVol", value);
    }

    public void ToggleMute()
    {
        PlayClickSound();
        isMuted = !isMuted;
        PlayerPrefs.SetInt("IsMuted", isMuted ? 1 : 0);
        UpdateMuteUI();
    }

    private void UpdateMuteUI()
    {
        AudioListener.volume = isMuted ? 0f : 1f;
        if (muteButtonImage != null)
            muteButtonImage.sprite = isMuted ? soundOffIcon : soundOnIcon;

        if (muteText != null)
        {
            muteText.text = isMuted ? "TẮT ÂM THANH" : "MỞ ÂM THANH";
            muteText.color = isMuted ? Color.gray : Color.white;
        }
    }

    // Hàm phụ để tự động phát tiếng click nút
    private void PlayClickSound()
    {
        if (uiSource != null && buttonClickClip != null)
        {
            uiSource.PlayOneShot(buttonClickClip);
        }
    }
}
