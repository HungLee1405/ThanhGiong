using UnityEngine;
using System.Collections;

/// <summary>
/// Đảm bảo nhạc nền luôn phát trong GameScene dù bắt đầu từ scene nào.
/// Nếu PersistentBGM đã tồn tại (đến từ StartScene), sẽ không tạo thêm.
/// Nếu chưa có (chạy thẳng từ GameScene), sẽ tự tạo và phát nhạc nền.
/// </summary>
public class BGMAutoStart : MonoBehaviour
{
    [Header("Clip nhạc nền (kéo file nhạc vào đây)")]
    public AudioClip bgmClip;

    [Header("Âm lượng (0 - 1)")]
    [Range(0f, 1f)]
    public float volume = 0.5f;

    private AudioSource audioSource;

    private void Awake()
    {
        // Nếu đã có PersistentBGM từ StartScene đang chạy, không làm gì thêm
        if (PersistentBGM.instance != null && PersistentBGM.instance.bgmSource != null
            && PersistentBGM.instance.bgmSource.isPlaying)
        {
            return;
        }

        // Nếu PersistentBGM có nhưng đang dừng, play lại
        if (PersistentBGM.instance != null && PersistentBGM.instance.bgmSource != null)
        {
            var src = PersistentBGM.instance.bgmSource;
            if (!src.isPlaying && src.clip != null)
            {
                src.loop = true;
                src.volume = volume;
                src.Play();
            }
            return;
        }

        // Không có PersistentBGM (chạy thẳng từ GameScene) → tự phát nhạc
        if (bgmClip == null) return;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = bgmClip;
        audioSource.loop = true;
        audioSource.volume = volume;
        audioSource.playOnAwake = false;

        // Áp dụng Mute setting nếu có
        bool isMuted = PlayerPrefs.GetInt("IsMuted", 0) == 1;
        AudioListener.volume = isMuted ? 0f : 1f;

        audioSource.Play();
    }
}
