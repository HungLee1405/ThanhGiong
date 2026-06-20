using UnityEngine;
using System.Collections; // Bắt buộc phải có để dùng vòng lặp thời gian

public class PersistentBGM : MonoBehaviour
{
    public static PersistentBGM instance;

    [Header("Kéo Loa Nhạc Nền vào đây")]
    public AudioSource bgmSource;

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
        }
    }

    [Header("Kéo file MainMixer vào đây")]
    public UnityEngine.Audio.AudioMixer mainMixer;

    void Start()
    {
        // 1. Tự động kiểm tra xem trong máy người chơi có dữ liệu âm lượng cũ không
        // Nếu có thì lấy ra, nếu chưa có (chơi lần đầu) thì mặc định là 1f (to nhất)
        float savedMusic = PlayerPrefs.GetFloat("MusicVol", 1f);
        float savedSFX = PlayerPrefs.GetFloat("SFXVol", 1f);

        // 2. Ép Audio Mixer phải nhỏ xuống ngay lập tức bằng công thức Decibel
        if (mainMixer != null)
        {
            float musicdB = Mathf.Log10(Mathf.Max(savedMusic, 0.0001f)) * 20f;
            mainMixer.SetFloat("MusicVol", musicdB);

            float sfxdB = Mathf.Log10(Mathf.Max(savedSFX, 0.0001f)) * 20f;
            mainMixer.SetFloat("SFXVol", sfxdB);
        }

        // Tải trạng thái Mute (0 là không Mute, 1 là đang Mute)
        bool isMuted = PlayerPrefs.GetInt("IsMuted", 0) == 1;

        // Bật/tắt loa tổng toàn game ngay khi vừa mở lên
        AudioListener.volume = isMuted ? 0f : 1f;
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
            yield return null; // Đợi frame tiếp theo để tạo độ mượt
        }

        // Tắt hẳn nhạc nền
        bgmSource.volume = 0f;
        bgmSource.Stop();

        // LƯU Ý: Lần này chúng ta KHÔNG dùng lệnh Destroy(gameObject) nữa. 
        // Nhờ vậy, cái loa tiếng Click chuột vẫn sẽ sống sót và hoạt động trong màn chơi chính!
    }
}