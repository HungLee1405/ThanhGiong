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
    public GameObject pauseMenuPanel;     // Cả cái bảng đen mờ lớn
    public GameObject mainPauseButtons;   // Nhóm chứa 4 nút chính
    public GameObject inGameOptionsPanel; // Nhóm chứa các thanh slider âm thanh

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
        // 1. Khởi tạo trạng thái ban đầu của UI
        pauseMenuPanel.SetActive(false);
        mainPauseButtons.SetActive(true);
        inGameOptionsPanel.SetActive(false);

        // 2. Tải giá trị âm lượng đã lưu từ máy
        float savedMusic = PlayerPrefs.GetFloat("MusicVol", 1f);
        float savedSFX = PlayerPrefs.GetFloat("SFXVol", 1f);
        musicSlider.value = savedMusic;
        sfxSlider.value = savedSFX;

        // 3. Lắng nghe sự kiện người chơi kéo thanh trượt (chạy mượt khi thời gian đóng băng)
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);

        isMuted = PlayerPrefs.GetInt("IsMuted", 0) == 1;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused)
            {
                // Nếu đang ở bảng Cài đặt mà nhấn ESC, quay lại menu Pause chính chứ không tắt menu luôn
                if (inGameOptionsPanel.activeSelf)
                {
                    CloseOptions();
                }
                else
                {
                    ResumeGame();
                }
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void ResumeGame()
    {
        pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void PauseGame()
    {
        pauseMenuPanel.SetActive(true);
        mainPauseButtons.SetActive(true);
        inGameOptionsPanel.SetActive(false); // Luôn mở bảng 4 nút chính trước
        NetworkManager manager = NetworkManager.Singleton;
        Time.timeScale = manager != null && manager.IsListening ? 1f : 0f;
        isPaused = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        UpdateMuteUI();
    }

    // Hàm gắn vào nút CÀI ĐẶT
    public void OpenOptions()
    {
        mainPauseButtons.SetActive(false);
        inGameOptionsPanel.SetActive(true);
    }

    // Hàm gắn vào nút QUAY LẠI trong bảng cài đặt
    public void CloseOptions()
    {
        inGameOptionsPanel.SetActive(false);
        mainPauseButtons.SetActive(true);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitToMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene("StartScene");
    }

    // --- CÁC HÀM XỬ LÝ ÂM THANH TRONG GAME ---
    public void SetMusicVolume(float value)
    {
        float dB = Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f;
        mainMixer.SetFloat("MusicVol", dB);
        PlayerPrefs.SetFloat("MusicVol", value);
    }

    public void SetSFXVolume(float value)
    {
        float dB = Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f;
        mainMixer.SetFloat("SFXVol", dB);
        PlayerPrefs.SetFloat("SFXVol", value);
    }

    public void ToggleMute()
    {
        isMuted = !isMuted;
        PlayerPrefs.SetInt("IsMuted", isMuted ? 1 : 0);
        UpdateMuteUI();
    }

    private void UpdateMuteUI()
    {
        // Đóng/mở loa tổng
        AudioListener.volume = isMuted ? 0f : 1f;

        // Đổi hình icon
        if (muteButtonImage != null)
        {
            muteButtonImage.sprite = isMuted ? soundOffIcon : soundOnIcon;
        }

        // THÊM ĐOẠN NÀY ĐỂ ĐỔI CHỮ VÀ ĐỔI MÀU
        if (muteText != null)
        {
            // Nếu đang tắt (isMuted = true) thì in chữ Tắt, ngược lại in chữ Mở
            muteText.text = isMuted ? "TẮT ÂM THANH" : "MỞ ÂM THANH";

            // Đổi màu chữ: Tắt thì màu Xám, Mở thì màu Trắng cho trực quan
            muteText.color = isMuted ? Color.gray : Color.white;
        }
    }
}
