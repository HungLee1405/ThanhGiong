using UnityEngine;
using TMPro;
using System;
using UnityEngine.InputSystem;
using System.Collections;

public class DialogueManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject dialoguePanel;
    public TMP_Text dialogueNpcNameText;
    public TMP_Text dialogueText;

    [Header("Voice Audio")]
    [Tooltip("AudioSource dùng để phát voice. Nếu để trống sẽ tự tạo.")]
    public AudioSource voiceAudioSource;

    [Header("BGM Ducking")]
    [Tooltip("Gắn trực tiếp AudioSource nhạc nền vào đây (ví dụ: Gameplay BGM). Nếu để trống sẽ tự tìm.")]
    public AudioSource bgmSourceOverride;

    [Tooltip("Âm lượng nhạc nền khi đang hội thoại (0 = tắt hẳn)")]
    [Range(0f, 1f)] public float duckedVolume = 0.0f;

    [Tooltip("Âm lượng nhạc nền bình thường (lúc không hội thoại)")]
    [Range(0f, 1f)] public float normalVolume = 0.15f;

    [Tooltip("Thời gian fade nhạc XUỐNG khi bắt đầu hội thoại (giây)")]
    [Range(0f, 3f)] public float bgmFadeInDuration = 0.5f;

    [Tooltip("Thời gian fade nhạc LÊN khi kết thúc hội thoại (giây)")]
    [Range(0f, 3f)] public float bgmFadeOutDuration = 1.0f;

    private string[] lines;
    private AudioClip[] voiceClips;
    private int index;
    private bool isTalking;
    private Action onDialogueEnd;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        // Tự tạo AudioSource nếu chưa gắn
        if (voiceAudioSource == null)
        {
            voiceAudioSource = gameObject.AddComponent<AudioSource>();
            voiceAudioSource.playOnAwake = false;
            voiceAudioSource.spatialBlend = 0f; // 2D audio
        }
        // Tiếng lồng luôn to nhất, ưu tiên cao nhất
        voiceAudioSource.volume   = 1f;
        voiceAudioSource.priority = 0;  // 0 = highest priority
    }

    private void Start()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        // Áp dụng volume bình thường ngay khi scene load
        AudioSource bgm = GetBGMSource();
        if (bgm != null) bgm.volume = normalVolume;
    }

    private void Update()
    {
        if (PauseMenuManager.isPaused) return;
        if (NetworkLobbyCoordinator.IsOnlineLobbyActive) return;
        if (!isTalking) return;
        if (Keyboard.current == null) return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            NextLine();
        }
    }

    /// <summary>
    /// Bắt đầu hội thoại với NPC.
    /// </summary>
    /// <param name="npcName">Tên NPC hiển thị.</param>
    /// <param name="newLines">Mảng các dòng hội thoại.</param>
    /// <param name="clips">Mảng AudioClip tương ứng 1-1 với newLines. Có thể null hoặc ngắn hơn.</param>
    /// <param name="onEnd">Callback khi hội thoại kết thúc.</param>
    public void StartDialogue(string npcName, string[] newLines, AudioClip[] clips = null, Action onEnd = null)
    {
        if (newLines == null || newLines.Length == 0)
        {
            Debug.LogWarning("Không có lời thoại.");
            return;
        }

        lines = newLines;
        voiceClips = clips;
        index = 0;
        isTalking = true;
        onDialogueEnd = onEnd;

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }

        if (dialogueNpcNameText != null)
        {
            dialogueNpcNameText.text = npcName;
        }
        else
        {
            Debug.LogWarning("Chưa gắn dialogueNpcNameText trong DialogueManager.");
        }

        if (dialogueText != null)
        {
            dialogueText.text = lines[index];
        }
        else
        {
            Debug.LogWarning("Chưa gắn dialogueText trong DialogueManager.");
        }

        // Phát voice clip cho dòng đầu tiên
        PlayVoiceClipAt(index);

        // Fade nhạc nền xuống nhẹ khi bắt đầu hội thoại
        DuckBGM(true);
    }

    private void NextLine()
    {
        if (!isTalking) return;

        index++;

        if (index < lines.Length)
        {
            if (dialogueText != null)
            {
                dialogueText.text = lines[index];
            }

            // Phát voice clip cho dòng tiếp theo
            PlayVoiceClipAt(index);
        }
        else
        {
            EndDialogue();
        }
    }

    private void EndDialogue()
    {
        isTalking = false;

        // Dừng voice đang phát
        if (voiceAudioSource != null && voiceAudioSource.isPlaying)
        {
            voiceAudioSource.Stop();
        }

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        // Fade nhạc nền trở lại mức bình thường
        DuckBGM(false);

        onDialogueEnd?.Invoke();
    }

    /// <summary>
    /// Tìm AudioSource nhạc nền theo thứ tự ưu tiên:
    /// 1. bgmSourceOverride (gán tay trong Inspector)
    /// 2. PersistentBGM.instance.bgmSource
    /// 3. AudioSource loop đầu tiên tìm thấy trong scene
    /// </summary>
    private AudioSource GetBGMSource()
    {
        if (bgmSourceOverride != null) return bgmSourceOverride;

        PersistentBGM persistBGM = PersistentBGM.instance;
        if (persistBGM != null && persistBGM.bgmSource != null) return persistBGM.bgmSource;

        // Fallback: tìm AudioSource đang loop trong scene (Gameplay BGM, v.v.)
        var all = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        foreach (var src in all)
        {
            if (src != voiceAudioSource && src.loop)
                return src;
        }
        return null;
    }

    /// <summary>Fade nhạc nền: duck=true → tắt/giảm, duck=false → khôi phục.</summary>
    private void DuckBGM(bool duck)
    {
        AudioSource bgmSrc = GetBGMSource();
        if (bgmSrc == null) return;

        float target   = duck ? duckedVolume     : normalVolume;
        float duration = duck ? bgmFadeInDuration : bgmFadeOutDuration;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

        if (duration <= 0f)
        {
            bgmSrc.volume = target;
            return;
        }

        fadeCoroutine = StartCoroutine(FadeVolume(bgmSrc, target, duration));
    }

    private IEnumerator FadeVolume(AudioSource source, float targetVolume, float duration)
    {
        float startVolume = source.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
            yield return null;
        }

        source.volume = targetVolume;
        fadeCoroutine = null;
    }

    /// <summary>Phát clip voice tại vị trí lineIndex nếu tồn tại.</summary>
    private void PlayVoiceClipAt(int lineIndex)
    {
        if (voiceAudioSource == null) return;
        if (voiceClips == null || lineIndex >= voiceClips.Length) return;

        AudioClip clip = voiceClips[lineIndex];
        if (clip == null) return;

        voiceAudioSource.Stop();
        voiceAudioSource.clip = clip;
        voiceAudioSource.Play();
    }

    public bool IsTalking()
    {
        return isTalking;
    }
}
