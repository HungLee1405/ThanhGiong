using UnityEngine;
using UnityEngine.EventSystems;

// Script này bắt buộc phải kế thừa từ 2 Interface để bắt sự kiện chuột
public class UIHoverPopUp : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Cấu hình Phóng To")]
    public Vector3 hoveredScale = new Vector3(1.1f, 1.1f, 1.1f); // Kích thước khi di chuột vào (phóng to 10%)
    public float speed = 12f;                                    // Tốc độ chuyển đổi (càng cao càng nảy nhanh)

    private Vector3 originalScale = Vector3.one;
    private Vector3 targetScale = Vector3.one;

    void Start()
    {
        // Lưu lại kích thước chuẩn ban đầu của nút (thường là 1, 1, 1)
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    void Update()
    {
        // Thay đổi scale mượt mà từ kích thước hiện tại sang kích thước mục tiêu
        transform.localScale = Vector3.MoveTowards(transform.localScale, targetScale, speed * Time.unscaledDeltaTime);
    }

    // Sự kiện xảy ra khi RÊ CHUỘT VÀO nút
    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = hoveredScale;
    }

    // Sự kiện xảy ra khi DI CHUỘT RA KHỎI nút
    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
    }

    // CỰC KỲ QUAN TRỌNG: Đảm bảo khi bảng menu bị đóng/ẩn đi đột ngột, 
    // nút sẽ tự động reset về kích thước chuẩn, không bị kẹt trạng thái phóng to.
    void OnDisable()
    {
        transform.localScale = originalScale;
        targetScale = originalScale;
    }
}