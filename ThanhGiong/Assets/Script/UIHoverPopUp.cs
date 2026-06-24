using UnityEngine;
using UnityEngine.EventSystems; // Bắt buộc phải có để nhận diện sự kiện chuột

public class UIHoverPopUp : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Cấu hình hiệu ứng")]
    [Tooltip("Kích thước nút sẽ phóng to lên khi di chuột vào (1.05 = to hơn 5%)")]
    public Vector3 hoverScale = new Vector3(1.05f, 1.05f, 1.05f);

    [Tooltip("Độ nẩy lên trên (tính bằng pixel)")]
    public float liftAmount = 8f;

    [Tooltip("Tốc độ chuyển động mượt mà")]
    public float smoothSpeed = 12f;

    // Các biến lưu vị trí và kích thước gốc
    private Vector3 originalScale;
    private Vector2 originalPosition;
    private RectTransform rectTransform;

    // Các biến mục tiêu cần hướng tới
    private Vector3 targetScale;
    private Vector2 targetPosition;

    void Start()
    {
        // Lấy component quản lý kích thước giao diện
        rectTransform = GetComponent<RectTransform>();

        // Lưu lại trạng thái ban đầu của nút để khi di chuột ra nó quay về đúng chỗ cũ
        originalScale = transform.localScale;
        originalPosition = rectTransform.anchoredPosition;

        // Đặt mục tiêu ban đầu chính là trạng thái gốc
        targetScale = originalScale;
        targetPosition = originalPosition;
    }

    void Update()
    {
        // Thay Time.deltaTime thành Time.unscaledDeltaTime
        // Điều này giúp hiệu ứng vẫn chạy mượt mà ngay cả khi game đang bị Pause (Time.timeScale = 0)
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * smoothSpeed);
        rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, targetPosition, Time.unscaledDeltaTime * smoothSpeed);
    }

    // Sự kiện tự động kích hoạt KHI DI CHUỘT VÀO NÚT
    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = hoverScale; // Đặt mục tiêu phóng to
        targetPosition = originalPosition + new Vector2(0, liftAmount); // Đặt mục tiêu dịch lên trên
    }

    // Sự kiện tự động kích hoạt KHI DI CHUỘT RA KHỎI NÚT
    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale; // Quay về kích thước cũ
        targetPosition = originalPosition; // Quay về vị trí cũ
    }
}