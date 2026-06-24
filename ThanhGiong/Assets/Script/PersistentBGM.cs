using UnityEngine;
using System.Collections;
using Unity.Netcode;

/// <summary>
/// Chạy nhạc nền liên tục xuyên suốt game.
/// - Tự động play khi start (nếu chưa play).
/// - Hoạt động cả trong offline lẫn online (multiplayer).
/// - Chỉ local owner player kích hoạt AudioListener → không bị nhiều listener conflict.
/// </summary>
public class PersistentBGM : MonoBehaviour
{
    public static PersistentBGM instance;

    [Header("Kéo Loa Nhạc Nền vào đây")]
    public AudioSource bgmSource;

    [Header("Kéo file MainMixer vào đây")]
    public UnityEngine.Audio.AudioMixer mainMixer;

    void Awake()
    {
        // Cơ chế giữ object bất tử khi chuyển Scene
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Tự tìm AudioSource nếu chưa gán
        if (bgmSource == null)
        {
            bgmSource = GetComponent<AudioSource>();
        }
        if (bgmSource == null)
        {
            bgmSource = GetComponentInChildren<AudioSource>();
        }
    }

    void Start()
    {
        // Áp dụng volume đã lưu
        float savedMusic = PlayerPrefs.GetFloat("MusicVol", 1f);
        float savedSFX = PlayerPrefs.GetFloat("SFXVol", 1f);

        if (mainMixer != null)
        {
            float musicdB = Mathf.Log10(Mathf.Max(savedMusic, 0.0001f)) * 20f;
            mainMixer.SetFloat("MusicVol", musicdB);

            float sfxdB = Mathf.Log10(Mathf.Max(savedSFX, 0.0001f)) * 20f;
            mainMixer.SetFloat("SFXVol", sfxdB);
        }

        // Áp dụng Mute
        bool isMuted = PlayerPrefs.GetInt("IsMuted", 0) == 1;
        AudioListener.volume = isMuted ? 0f : 1f;

        // Đảm bảo nhạc nền đang phát
        EnsureBGMPlaying();
    }

    private void Update()
    {
        // Nếu nhạc bị dừng vì lý do gì đó (scene reload, etc.), play lại
        EnsureBGMPlaying();
    }

    private void EnsureBGMPlaying()
    {
        if (bgmSource == null) return;
        if (bgmSource.clip == null) return;
        if (bgmSource.isPlaying) return;

        bgmSource.loop = true;
        bgmSource.Play();
    }

    // Hàm gọi lệnh làm mờ nhạc
    public void FadeOutMusic(float fadeDuration)
    {
        if (bgmSource != null)
        {
            StartCoroutine(FadeOutCoroutine(fadeDuration));
        }
    }

    // Vòng lặp từ từ vặn nhỏ âm lượng
    private IEnumerator FadeOutCoroutine(float fadeDuration)
    {
        float startVolume = bgmSource.volume;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsedTime / fadeDuration);
            yield return null;
        }

        bgmSource.volume = 0f;
        bgmSource.Stop();
    }

    public void FadeInMusic(float fadeDuration, float targetVolume = 1f)
    {
        if (bgmSource != null)
        {
            StartCoroutine(FadeInCoroutine(fadeDuration, targetVolume));
        }
    }

    private IEnumerator FadeInCoroutine(float fadeDuration, float targetVolume)
    {
        if (!bgmSource.isPlaying)
        {
            bgmSource.volume = 0f;
            bgmSource.Play();
        }

        float startVolume = bgmSource.volume;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsedTime / fadeDuration);
            yield return null;
        }

        bgmSource.volume = targetVolume;
    }
}