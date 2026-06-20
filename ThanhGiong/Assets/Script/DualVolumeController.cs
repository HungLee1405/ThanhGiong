using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class DualVolumeController : MonoBehaviour
{
    [Header("Cấu hình Thanh trượt")]
    public AudioMixer mainMixer;
    public Slider musicSlider;
    public Slider sfxSlider;

    [Header("Cấu hình Nút Mute (Tùy chọn)")]
    public Image muteButtonImage; // Nắm component Image của nút Mute thả vào đây
    public Sprite soundOnIcon;    // Ảnh cái loa đang bật
    public Sprite soundOffIcon;   // Ảnh cái loa bị gạch chéo (tắt)

    private bool isMuted = false;

    void Start()
    {
        // 1. Tải và áp dụng âm lượng thanh trượt
        float savedMusic = PlayerPrefs.GetFloat("MusicVol", 1f);
        float savedSFX = PlayerPrefs.GetFloat("SFXVol", 1f);

        musicSlider.value = savedMusic;
        sfxSlider.value = savedSFX;

        SetMusicVolume(savedMusic);
        SetSFXVolume(savedSFX);

        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);

        // 2. Tải và áp dụng trạng thái Mute
        isMuted = PlayerPrefs.GetInt("IsMuted", 0) == 1;
        UpdateMuteUI();
    }

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

    // Gắn hàm này vào sự kiện OnClick() của nút Mute
    public void ToggleMute()
    {
        isMuted = !isMuted; // Đảo ngược trạng thái (Đang tắt thì bật, đang bật thì tắt)
        PlayerPrefs.SetInt("IsMuted", isMuted ? 1 : 0); // Lưu vào máy

        UpdateMuteUI();
    }

    // Hàm cập nhật icon và âm lượng tổng
    private void UpdateMuteUI()
    {
        // Chặn/Mở loa tổng của game
        AudioListener.volume = isMuted ? 0f : 1f;

        // Đổi hình ảnh nếu bạn có cung cấp Icon
        if (muteButtonImage != null)
        {
            muteButtonImage.sprite = isMuted ? soundOffIcon : soundOnIcon;
        }
    }
}