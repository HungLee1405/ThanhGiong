# Implementation Plan - Hoàn thiện game Thánh Gióng: Nuôi Lớn Anh Hùng

Tài liệu này chi tiết hóa cách thức xây dựng và tích hợp các tính năng phục vụ cho bản game hoàn chỉnh dài 7 ngày chơi.

## User Review Required

> [!IMPORTANT]
> 1. **Kiểm tra biên dịch**: Dự án hiện tại đang biên dịch tốt trong Unity Editor do các thư viện uGUI/TMPro được giải quyết nội bộ, nhưng MSBuild ngoài Command Line báo thiếu thư viện vì các reference tuyệt đối. Chúng ta sẽ viết mã nguồn độc lập đảm bảo độ chính xác cao và không làm lỗi compile của Editor.
> 2. **Tương thích Input System**: Hệ thống điều khiển sử dụng `UnityEngine.InputSystem` phiên bản mới (`Keyboard.current`). Chúng ta tuyệt đối không dùng `Input.GetKeyDown`.
> 3. **Phần thưởng nhiệm vụ**: Để bảo toàn dữ liệu Inspector, chúng ta bổ sung các trường phần thưởng vào `QuestStep` mà không xóa hay đổi tên các trường cũ.

---

## Proposed Changes

Chúng ta sẽ thực hiện chỉnh sửa và tạo mới các file mã nguồn theo thứ tự ưu tiên tích hợp chặt chẽ.

### 1. Hệ thống cầm vật phẩm & Chọn Slot

#### [MODIFY] [PlayerHandController.cs](file:///f:/GameDev/Project/ThanhGiong-2026-06-17-07-56-00/ThanhGiong/Assets/Script/PlayerHandController.cs)
- Bổ sung các phương thức:
  - `public void SelectSlot(int index)` (Cập nhật highlight và visual).
  - `public void ClearSelectedSlot()` (Gọi `DeselectSlot()`).
  - `public void RefreshHeldItem()` (Gọi `RefreshSelectedItem()`).
  - `public InventoryItem GetHeldItem()` (Trả về `selectedItem`).
  - `public bool HasHeldItem(string itemId)` (Kiểm tra xem tay có đang cầm item đó không).
  - `public bool TryConsumeHeldItem(int amount)` (Trừ số lượng item ở slot đang chọn và cập nhật visual).
- Cập nhật hàm `HandleHotkeys()` để bỏ qua việc đổi slot nếu slot hiện tại chứa gà (`chicken`).

---

### 2. Hệ thống nhận phần thưởng theo Quest

#### [NEW] [RewardTiming.cs](file:///f:/GameDev/Project/ThanhGiong-2026-06-17-07-56-00/ThanhGiong/Assets/Script/RewardTiming.cs)
- Khai báo enum xác định thời điểm nhận quà: `None`, `StartOfStep`, `TalkToNPC`, `CompletionOfStep`.

#### [MODIFY] [QuestStep.cs](file:///f:/GameDev/Project/ThanhGiong-2026-06-17-07-56-00/ThanhGiong/Assets/Script/QuestStep.cs)
- Thêm các trường phần thưởng:
  - `public ItemData rewardItem;`
  - `public int rewardAmount;`
  - `public RewardTiming rewardTiming;`
  - `public bool rewardReceived;`
  - `public string rewardMessage;`
  - `public bool requireInventorySpace;`

#### [MODIFY] [QuestManager.cs](file:///f:/GameDev/Project/ThanhGiong-2026-06-17-07-56-00/ThanhGiong/Assets/Script/QuestManager.cs)
- Thêm phương thức `TryGrantReward(QuestStep step, RewardTiming timing)`:
  - Kiểm tra nếu `step.rewardItem == null` hoặc `step.rewardReceived == true` hoặc `step.rewardTiming != timing` thì bỏ qua.
  - Tìm `PlayerInventory` cục bộ. Nếu đầy và `requireInventorySpace` là true, hiển thị thông báo "Túi đầy" qua hội thoại hoặc UI và trả về `false`.
  - Thêm item vào inventory, đánh dấu `rewardReceived = true` và hiển thị thông báo.
- Gọi `TryGrantReward` tại:
  - `LoadDay` và `CompleteCurrentStep` (khi đổi sang bước mới) với timing `StartOfStep`.
  - `CompleteTalkToNPC` trước khi gọi `AddProgress` với timing `TalkToNPC` (nếu không nhận được, chặn không cho hoàn thành bước nói chuyện).
  - `CompleteCurrentStep` trước khi tăng index với timing `CompletionOfStep` (nếu không nhận được, chặn không cho chuyển bước).

#### [MODIFY] [QuestDatabase.cs](file:///f:/GameDev/Project/ThanhGiong-2026-06-17-07-56-00/ThanhGiong/Assets/Script/QuestDatabase.cs)
- Gắn phần thưởng rìu, cuốc và các vật phẩm hướng dẫn tương ứng vào cơ sở dữ liệu các ngày 3, 4, 5.

---

### 3. Hệ thống chỉ đường nhiệm vụ (Quest Markers)

#### [NEW] [QuestMarker.cs](file:///f:/GameDev/Project/ThanhGiong-2026-06-17-07-56-00/ThanhGiong/Assets/NEW_FILE_QuestMarker.cs) (Ví dụ vị trí tạo mới)
- Tạo component `QuestMarker` gắn vào NPC hoặc tài nguyên.
- Kiểm tra loại nhiệm vụ hiện tại từ `QuestManager`. Nếu `targetNPCId` hoặc `targetItemId` trùng khớp thì bật hiệu ứng vòng sáng/mũi tên chỉ đường, ngược lại tự động ẩn đi.

---

### 4. Hệ thống cảnh báo thu thập dư thừa

#### [MODIFY] [ResourcePickup.cs](file:///f:/GameDev/Project/ThanhGiong-2026-06-17-07-56-00/ThanhGiong/Assets/Script/ResourcePickup.cs)
- Khi người chơi đứng gần, kiểm tra `QuestManager` xem số lượng tài nguyên hiện tại trong inventory/kho đã đủ cho bước nhiệm vụ chưa.
- Nếu đã đủ, hiển thị gợi ý context hint phù hợp thay vì hiển thị nút thu thập mặc định, ngăn thu thập thừa thãi không cần thiết (nhưng không chặn hoàn toàn nếu inventory còn chỗ trống).

---

### 5. Hệ thống bắt gà AI và chuồng gà

#### [NEW] [ChickenController.cs](file:///f:/GameDev/Project/ThanhGiong-2026-06-17-07-56-00/ThanhGiong/Assets/NEW_FILE_ChickenController.cs)
- Xây dựng AI đơn giản cho gà chạy nhảy tự do (`Wander`) hoặc chạy trốn người chơi (`Flee`).
- Cho phép người chơi đứng gần giữ `E` để bắt gà. Khi giữ `E`, hiển thị thanh tiến độ bằng `InteractionUI`.
- Bắt thành công: Ẩn gà ngoài thế giới, thêm item `chicken` vào túi đồ của người chơi, đồng thời tự chọn slot này và khóa không cho đổi slot để giữ gà trên tay.

#### [NEW] [ChickenCoop.cs](file:///f:/GameDev/Project/ThanhGiong-2026-06-17-07-56-00/ThanhGiong/Assets/NEW_FILE_ChickenCoop.cs)
- Kế thừa `IItemReceiver`, chỉ chấp nhận vật phẩm `chicken`.
- Khi người chơi thả gà vào (nhấn R), xóa item khỏi inventory, mở khóa slot cầm đồ, tăng đếm số gà trong chuồng, hiển thị visual gà xuất hiện trong chuồng và gọi `QuestManager.AddProgress` tương ứng.

---

### 6. Hệ thống Nấu ăn bằng Menu

#### [NEW] [CookingRecipe.cs](file:///f:/GameDev/Project/ThanhGiong-2026-06-17-07-56-00/ThanhGiong/Assets/NEW_FILE_CookingRecipe.cs)
- Lớp ScriptableObject chứa: ID công thức, tên hiển thị, mô tả, nguyên liệu cần (`ItemData` + số lượng), đầu ra, thời gian nấu, mức hồi thanh đói, ngày mở khóa, v.v.

#### [NEW] [CookingMenuUI.cs](file:///f:/GameDev/Project/ThanhGiong-2026-06-17-07-56-00/ThanhGiong/Assets/NEW_FILE_CookingMenuUI.cs)
- Canvas điều khiển: hiển thị danh sách công thức, hiển thị số lượng nguyên liệu trong túi người chơi, nút "Nấu" hoạt động khi đủ nguyên liệu, thanh chạy tiến độ nấu, và đóng menu bằng phím Escape hoặc nút đóng.
- Tích hợp hướng dẫn nấu ăn bước đầu (cơm trắng) và hiển thị thông báo mở khóa cơm gà/cơm lam.

#### [MODIFY] [CookingPot.cs](file:///f:/GameDev/Project/ThanhGiong-2026-06-17-07-56-00/ThanhGiong/Assets/Script/CookingPot.cs)
- Sửa đổi để khi tương tác (E), mở `CookingMenuUI` thay vì nấu trực tiếp. Quản lý trạng thái nấu, lưu trữ món ăn đã nấu xong khi inventory của người chơi đầy để người chơi lấy sau.

---

### 7. Hệ thống Rèn mở rộng & Kho tài nguyên làng

#### [NEW] [VillageStorage.cs](file:///f:/GameDev/Project/ThanhGiong-2026-06-17-07-56-00/ThanhGiong/Assets/NEW_FILE_VillageStorage.cs)
- Kho cất sắt, tre, gạo, nước của làng. Người chơi mang tài nguyên đến và nhấn R để cất vào. Hiển thị số lượng. Phục vụ nhiệm vụ ngày 6 (tích trữ 10 sắt, 10 tre).

#### [NEW] [ForgeManager.cs](file:///f:/GameDev/Project/ThanhGiong-2026-06-17-07-56-00/ThanhGiong/Assets/NEW_FILE_ForgeManager.cs) & [ForgeUI.cs](file:///f:/GameDev/Project/ThanhGiong-2026-06-17-07-56-00/ThanhGiong/Assets/NEW_FILE_ForgeUI.cs)
- Quản lý mini-game rèn vũ khí ngày 7:
  - Cung cấp sắt và nước cho bể/lò.
  - Giữ E để kéo bễ nâng nhiệt độ lò rèn vào vùng tối ưu.
  - Giai đoạn đập búa (nhấn giữ E/nhấn theo nhịp để đập búa tăng tiến độ đúc).
  - Đổ nước làm nguội.
  - Hiển thị UI tiến độ rèn tổng, nhiệt độ lò và hướng dẫn hành động kế tiếp.

#### [MODIFY] [GameDayManager.cs](file:///f:/GameDev/Project/ThanhGiong-2026-06-17-07-56-00/ThanhGiong/Assets/Script/GameDayManager.cs)
- Cuối ngày 7, kiểm tra nếu `forgeProgress == 100%` và đói Gióng >= 80% thì thắng game. Nếu không, hoặc đói Gióng xuống 0%, kết thúc ngày thất bại và cho phép chơi lại ngày 7.
- Cuối các ngày 1-6, kiểm tra đói Gióng >= 80%. Nếu không đạt, báo thất bại và cho chơi lại ngày hiện tại.

---

### 8. Vực nước nguy hiểm & Hồi sinh (Water Kill Zone)

#### [NEW] [WaterKillZone.cs](file:///f:/GameDev/Project/ThanhGiong-2026-06-17-07-56-00/ThanhGiong/Assets/NEW_FILE_WaterKillZone.cs) & [PlayerRespawnManager.cs](file:///f:/GameDev/Project/ThanhGiong-2026-06-17-07-56-00/ThanhGiong/Assets/NEW_FILE_PlayerRespawnManager.cs)
- Khi người chơi rơi xuống sông/hồ, khóa di chuyển, màn hình tối lại (Fade Out), dịch chuyển người chơi về vị trí bắt đầu ngày (checkpoint gần nhất), khôi phục trạng thái và bật lại điều khiển. Đảm bảo gà đang cầm không bị biến mất vĩnh viễn.

---

### 9. Bảng Debug Nhà Phát Triển (Development Debug Panel)

#### [NEW] [DevelopmentDebugPanel.cs](file:///f:/GameDev/Project/ThanhGiong-2026-06-17-07-56-00/ThanhGiong/Assets/NEW_FILE_DevelopmentDebugPanel.cs)
- Bảng điều khiển (chỉ hiển thị trong Editor hoặc bản Development Build) giúp: nhảy nhanh đến ngày 1-7, thêm vật phẩm bất kỳ, dọn sạch hòm đồ, hoàn thành nhiệm vụ hiện tại, điều chỉnh thanh đói, spawn gà, đặt tiến trình rèn.

---

## Verification Plan

### Automated Tests
- Kiểm tra biên dịch thông qua Editor compile hoặc `dotnet build` nội bộ sau khi điều chỉnh reference.
- Rà soát toàn bộ lỗi cú pháp.

### Manual Verification
- Test vòng lặp ngày 1: nói chuyện trưởng làng -> mẹ Gióng -> múc nước -> lấy gạo -> nấu cơm -> đút Gióng -> sống sót qua ngày.
- Test ngày 3: nói chuyện lão Năm -> bắt 3 gà bỏ chuồng -> mở khóa cơm gà.
- Test ngày 4: nói blacksmith -> nhận cuốc -> đào quặng sắt.
- Test ngày 5: nhận rìu -> chặt tre -> mở khóa cơm lam.
- Test ngày 6: tích trữ 10 sắt, 10 tre vào kho làng.
- Test ngày 7: cho Gióng ăn + tham gia rèn vũ khí đạt 100% trước khi hết giờ.
- Test rơi xuống nước hồi sinh và bảng debug để nhảy ngày nhanh phục vụ test.
