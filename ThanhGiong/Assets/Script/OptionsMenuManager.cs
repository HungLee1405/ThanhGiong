using UnityEngine;
using UnityEngine.SceneManagement;

public class OptionsMenuManager : MonoBehaviour
{
    // Hàm này gắn vào nút Quay Lại
    public void GoBackToMenu()
    {
        // Thay chữ "StartScene" bằng đúng cái tên Scene Menu chính của bạn
        SceneManager.LoadScene("StartScene");
    }
}