using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Bắt buộc phải có để bắt sự kiện chuột

public class MultiImageButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Kéo các mảnh ghép (Image) vào đây")]
    public Image[] targetImages;

    [Header("Cài đặt màu (Nhớ chỉnh Alpha)")]
    public Color normalColor = new Color(1f, 1f, 1f, 0f);
    public Color hoverColor = new Color(1f, 0.9f, 0.4f, 0.4f); // Vàng mờ khi rê chuột
    public Color pressedColor = new Color(0f, 0f, 0f, 0.6f);   // Đen mờ khi bấm

    private void Start()
    {
        SetColor(normalColor);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetColor(hoverColor);
    }

    // Khi chuột đi ra khỏi nút
    public void OnPointerExit(PointerEventData eventData)
    {
        SetColor(normalColor);
    }

    // Khi click chuột xuống
    public void OnPointerDown(PointerEventData eventData)
    {
        SetColor(pressedColor);
    }

    // Khi nhả chuột ra
    public void OnPointerUp(PointerEventData eventData)
    {
        SetColor(hoverColor);
    }

    // Hàm thực hiện việc đổi màu hàng loạt
    private void SetColor(Color color)
    {
        foreach (Image img in targetImages)
        {
            if (img != null)
            {
                img.color = color;
            }
        }
    }
}