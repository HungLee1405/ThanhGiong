# Kiến trúc Game Hiện tại & Kế hoạch triển khai

Tài liệu này tóm tắt kết quả phân tích mã nguồn và tài nguyên trong project Unity hiện tại và đưa ra kế hoạch chi tiết hoàn thiện game trong 7 ngày chơi.

## 1. Hệ thống hiện có & Phân tích

Dựa trên việc quét thư mục `Assets/Script` và `Assets/Items`, dưới đây là các hệ thống đang hoạt động và quan hệ của chúng:

| Hệ thống | Script chính | Trạng thái hiện tại | Đánh giá & Định hướng mở rộng |
|---|---|---|---|
| **Quản lý nhiệm vụ** | `QuestManager.cs`<br>`QuestDatabase.cs`<br>`QuestStep.cs` | Đã có cơ chế cơ bản cho 7 ngày chơi. Hỗ trợ đối thoại NPC, thu thập nước/gạo, rèn và sinh tồn. | Cần bổ sung hệ thống phần thưởng vật phẩm theo bước nhiệm vụ, quản lý marker chỉ đường thế giới, và hướng dẫn thu thập. |
| **Hệ thống Inventory** | `PlayerInventory.cs`<br>`InventorySlot.cs`<br>`PlayerInventoryUI.cs`<br>`ItemSlotUI.cs` | Đã có inventory dạng slot (8 ô), hỗ trợ thêm/xóa vật phẩm. | Cần tối ưu hóa đồng bộ giữa slot được chọn và vật phẩm hiển thị trên tay người chơi. |
| **Điều khiển cầm đồ** | `PlayerHandController.cs` | Chọn slot qua hotkey (1-8), hiển thị prefab trên tay qua `ItemData.handPrefab`. Hỗ trợ đưa đồ vào `IItemReceiver`. | Cần bổ sung các public API: `SelectSlot`, `ClearSelectedSlot`, `RefreshHeldItem`, `GetHeldItem`, `HasHeldItem`, `TryConsumeHeldItem`. Khóa đổi item khi đang bế gà. |
| **Nấu ăn** | `CookingPot.cs` | Cơ chế nấu cơm trắng cơ bản (nhấn giữ E để trừ 1 gạo + 1 nước và nhận 1 cơm trắng). | Sẽ mở rộng thành hệ thống dữ liệu hóa nấu ăn bằng Menu nấu. Hỗ trợ ScriptableObject `CookingRecipe` cho cơm trắng, cơm gà, cơm lam. |
| **Thanh đói Gióng** | `GiongHunger.cs`<br>`FeedGiong.cs` | Tụt đói theo thời gian thực. Đưa cơm cho mẹ Gióng để hồi phục. | Cần điều chỉnh mức độ tụt đói và bổ sung điều kiện chiến thắng/thất bại dựa trên mức đói này ở cuối ngày. |
| **Vòng lặp ngày** | `GameDayManager.cs`<br>`DayTransitionUI.cs` | Chạy bộ đếm thời gian ngày. Hết ngày chuyển qua ngày tiếp theo. | Cần kiểm tra điều kiện sinh tồn/nhiệm vụ cuối ngày trước khi chuyển ngày. Bổ sung màn hình kết quả (DaySummary) và kết thúc game (Victory/Defeat). |
| **Tương tác tài nguyên** | `ResourcePickup.cs`<br>`InteractionUI.cs` | Hỗ trợ nhặt gạo, nước, sắt, tre (cần cuốc/rìu). | Thêm cơ chế gà chạy nhảy và chuồng gà (chicken coop). |

---

## 2. Các Script được sửa đổi & tạo mới

### Script sẽ được sửa đổi:
1. `PlayerHandController.cs`:
   - Thêm các public API hoàn chỉnh theo yêu cầu.
   - Thêm khóa đổi slot khi cầm gà.
2. `QuestStep.cs` & `QuestStepType.cs`:
   - Thêm các trường hỗ trợ phần thưởng vật phẩm (`rewardItem`, `rewardAmount`, `rewardTiming`, `rewardReceived`, `rewardMessage`, `requireInventorySpace`).
3. `QuestManager.cs`:
   - Thêm logic tự động/thủ công trao phần thưởng nhiệm vụ khi bắt đầu, nói chuyện NPC hoặc hoàn thành bước.
   - Thêm kiểm tra điều kiện chuyển ngày / thất bại dựa trên nhiệm vụ và thanh đói.
4. `QuestDatabase.cs`:
   - Cập nhật các bước nhiệm vụ ngày 3 (bắt gà mở khóa cơm gà), ngày 4 (nhận cuốc từ thợ rèn để đào quặng), ngày 5 (nhận rìu từ già làng để chặt tre).
5. `CookingPot.cs`:
   - Mở rộng để tương tác với UI Menu nấu ăn, hỗ trợ recipe động thay vì hardcode.
6. `PlayerMovement.cs` & `GiongHunger.cs`:
   - Cập nhật tốc độ di chuyển và mức độ tụt đói cho Gióng phù hợp theo từng ngày.
   - Gửi tham số tốc độ/vận tốc sang Animator.

### Script sẽ được tạo mới:
1. `CookingRecipe.cs` (ScriptableObject):
   - Định nghĩa công thức nấu ăn: Cơm trắng, Cơm gà, Cơm lam.
2. `CookingMenuUI.cs`:
   - Canvas UI cho phép chọn công thức, xem nguyên liệu cần/có, nhấn nút Nấu, đóng/mở và khóa/mở khóa.
3. `QuestMarker.cs` (World Marker):
   - Gắn vào các đối tượng thế giới (NPC, ruộng lúa, giếng, mỏ sắt, rừng tre) để hiện icon/mũi tên dựa trên bước nhiệm vụ hiện tại.
4. `ChickenController.cs`:
   - Quản lý AI gà đi lang thang, chạy trốn người chơi, hiển thị progress bar khi bắt và ẩn gà khi bị bắt.
5. `ChickenCoop.cs`:
   - Chuồng gà nhận gà từ tay người chơi (R), đếm số gà và cập nhật quest.
6. `ForgeManager.cs` & `ForgeUI.cs`:
   - Trực quan hóa cơ chế lò rèn ngày 7 (bơm sắt, nước, kéo bễ duy trì nhiệt, đập búa, làm nguội).
7. `WaterKillZone.cs` & `PlayerRespawnManager.cs`:
   - Xử lý hồi sinh người chơi khi rơi xuống nước mà không mất vật phẩm nhiệm vụ.
8. `VillageStorage.cs`:
   - Kho chứa tài nguyên của làng cho ngày 6.
9. `DevelopmentDebugPanel.cs`:
   - Bảng điều khiển debug chuyển ngày nhanh, thêm đồ, đặt thanh đói để phục vụ test game.

---

## 3. Thứ tự triển khai

Chúng ta sẽ bám sát 100% thứ tự trong quy trình bắt buộc để đảm bảo sự ổn định của project:

1. **Giai đoạn 1**: Hoàn thiện hệ thống cầm vật phẩm (`PlayerHandController.cs`).
2. **Giai đoạn 2**: Cơ chế nhận item theo quest (mở rộng `QuestStep` & `QuestManager`).
3. **Giai đoạn 3**: Chỉ đường thế giới & UI nhiệm vụ (`QuestMarker` & UI context).
4. **Giai đoạn 4**: Cảnh báo thu thập dư thừa.
5. **Giai đoạn 5**: AI gà (`ChickenController`) và Chuồng gà (`ChickenCoop`).
6. **Giai đoạn 6**: Menu nấu ăn dữ liệu hóa (`CookingRecipe` & `CookingMenuUI`).
7. **Giai đoạn 7**: Kho tài nguyên làng (`VillageStorage`).
8. **Giai đoạn 8**: Rèn mở rộng ngày 7 (`ForgeManager` & `ForgeUI`).
9. **Giai đoạn 9**: Water respawn (`WaterKillZone` & `PlayerRespawnManager`).
10. **Giai đoạn 10**: Animation và Âm thanh.
11. **Giai đoạn 11**: Màn hình tổng kết ngày (Day Summary) & Debug panel.
12. **Giai đoạn 12**: Polish & Kiểm thử liên tục từ ngày 1 đến 7.
