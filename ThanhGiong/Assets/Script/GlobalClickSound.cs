using UnityEngine;
using UnityEngine.InputSystem; // Thư viện của hệ thống mới

public class GlobalClickSound : MonoBehaviour
{
    [Header("Kéo Loa chứa tiếng Click vào đây")]
    public AudioSource clickAudio;

    void Update()
    {
        // Kiểm tra xem có chuột không và chuột trái có vừa được bấm không
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (clickAudio != null)
            {
                clickAudio.Play();
            }
        }
    }
}