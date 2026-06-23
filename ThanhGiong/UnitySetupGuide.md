# Hướng dẫn thiết lập Inspector trong Unity

Sau khi cập nhật code, hãy thực hiện các bước thiết lập sau trong Unity Editor:

## 1. ChickenQuestSpawner
* Tạo một GameObject trống mới trong Scene (ví dụ: đặt tên là `ChickenQuestSpawner`).
* Gắn component `ChickenQuestSpawner` vào GameObject vừa tạo.
* Kéo thả Component `QuestManager` trong Scene vào trường **Quest Manager** của Spawner.
* Tại mảng **Chickens**, nhấn nút khóa (Lock) Inspector, quét chọn tất cả các GameObject gà có component `ChickenController` trong Scene và kéo thả chúng vào mảng này.

## 2. ChickenCoop (Chuồng Gà)
* Chọn GameObject chuồng gà (`ChickenCoop`) trong Scene.
* Kiểm tra collider kích hoạt trigger: Đảm bảo **Is Trigger** được tích chọn trên Collider của Chuồng gà.
* Thiết lập các trường trong Inspector của `ChickenCoop`:
  * **Accepted Item Id**: Điền `chicken` (trùng khớp với ID của gà trong ItemData).
  * **Interaction UI**: Kéo thả GameObject chứa component `InteractionUI` (thường là một Canvas World Space hiển thị phía trên chuồng gà) vào đây.
  * **Quest Manager**: Kéo thả Component `QuestManager` vào đây.
  * **Required Chicken Count**: Điền số lượng gà cần thiết lập (mặc định là `3`, dùng làm fallback nếu không tìm thấy QuestStep CatchChicken).
  * **Chicken Visuals**: Kéo thả các mô hình visual của gà con ở trong chuồng để chúng tự động bật/tắt khi có gà được đưa vào.

## 3. PlayerHubUI
* Chọn GameObject UI chính (`PlayerHubUI`) dưới Canvas.
* Nếu muốn hiển thị tách biệt nhiệm vụ chính và phụ:
  * Tạo thêm một component `TMP_Text` cho nhiệm vụ phụ.
  * Kéo thả thành phần text cũ vào **Main Quest Text** và thành phần text mới vào **Side Quest Text**.
  * Nếu không gán hai trường tùy chọn này, hệ thống sẽ tự động ghép chung nội dung mô tả nhiệm vụ chính và nhiệm vụ phụ hiển thị xuống dòng trên trường **Quest Text** hiện có.
