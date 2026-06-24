using UnityEngine;
using UnityEngine.SceneManagement; // Thư viện để kiểm tra tên Scene
using UnityEngine.InputSystem;     // Thư viện Input System mới của bạn

public class GlobalClickSound : MonoBehaviour
{
    [Header("Kéo Audio Source chứa tiếng Click vào đây")]
    public AudioSource clickSource;

    void Update()
    {
        // Kiểm tra xem người chơi có nhấp chuột trái hay không (Dùng Input System mới)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            // Nếu đủ điều kiện thì mới phát tiếng
            if (ShouldPlaySound())
            {
                clickSource.Play();
            }
        }
    }

    // Hàm kiểm tra điều kiện phát âm thanh
    private bool ShouldPlaySound()
    {
        // 1. Lấy tên của Scene hiện tại
        string currentScene = SceneManager.GetActiveScene().name;

        // 2. NẾU ĐANG Ở TRONG MÀN CHƠI CHÍNH (Đổi "GameScene" thành đúng tên Scene game của bạn nếu khác nhé)
        if (currentScene == "GameScene")
        {
            // Kiểm tra xem game có đang bị Pause (đóng băng thời gian) hay không
            if (Time.timeScale == 0f)
            {
                return true; // Đang Pause -> Cho phép kêu
            }
            else
            {
                return false; // Đang chơi bình thường -> Khóa tiếng click
            }
        }

        // 3. NẾU Ở CÁC SCENE KHÁC (StartScene, OptionsScene...)
        // Mặc định luôn cho phép phát tiếng click
        return true;
    }
}