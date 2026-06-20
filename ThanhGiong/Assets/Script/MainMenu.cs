using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Kéo object FadePanel vào đây")]
    public CanvasGroup fadeGroup;

    [Header("Thời gian đen màn hình (giây)")]
    public float fadeDuration = 0.5f;

    // Hàm này được gọi khi bấm nút Bắt đầu
    public void PlayGame()
    {
        StartCoroutine(FadeAndLoadScene());
    }

    private IEnumerator FadeAndLoadScene()
    {
        // 1. Bật chặn chuột để người chơi không bấm đúp loạn xạ trong lúc đang chuyển cảnh
        // 1. Khóa chuột không cho người chơi bấm linh tinh
        fadeGroup.blocksRaycasts = true;

        // 2. Tăng dần độ mờ (Alpha) từ 0 lên 1
        // 2. GỌI LỆNH TẮT NHẠC (thời gian tắt nhạc bằng đúng thời gian đen màn hình)
        if (PersistentBGM.instance != null)
        {
            PersistentBGM.instance.FadeOutMusic(fadeDuration);
        }

        // 3. Vòng lặp màn hình đen dần
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            fadeGroup.alpha = elapsedTime / fadeDuration;
            yield return null; // Đợi frame tiếp theo để tạo độ mượt
            yield return null;
        }

        // Đảm bảo màn hình đen kịt 100%
        fadeGroup.alpha = 1f;

        // 3. Chuyển Scene sau khi màn hình đã đen
        // 4. Chuyển sang màn chơi Game
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    // Hàm này được gọi khi bấm nút Thoát
    public void QuitGame()
    {
        StartCoroutine(FadeAndQuit());
    }

    private IEnumerator FadeAndQuit()
    {
        fadeGroup.blocksRaycasts = true;

        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            fadeGroup.alpha = elapsedTime / fadeDuration;
            yield return null;
        }

        fadeGroup.alpha = 1f;
        Debug.Log("Đã thoát game!");
        Application.Quit();
    }

    // Hàm này được gọi khi bấm nút Options
    public void OpenOptions()
    {
        // Dùng tên Scene để nhảy chính xác đến Scene Options
        SceneManager.LoadScene("OptionsScene");
    }
}