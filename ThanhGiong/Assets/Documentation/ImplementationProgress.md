# Báo cáo tiến độ triển khai

## Các hạng mục đã hoàn thành (Giai đoạn 1 - 5)
- **Giai đoạn 1**: Hoàn thiện `PlayerHandController.cs` (quản lý item trên tay, khóa đổi slot khi cầm gà). Đảm bảo prefab biến mất khi hết số lượng. (Static Review Passed)
- **Giai đoạn 2**: Cơ chế nhận phần thưởng (`RewardTiming`, `QuestManager`). Hệ thống đã hỗ trợ check slot inventory bằng `CanAddItem` (không yêu cầu rỗng túi, chỉ yêu cầu chứa được số lượng item). Ngăn qua bước nếu phần thưởng không trao được. Đã cập nhật `QuestDatabase` thêm phần thưởng Rìu và Cuốc chim. (Static Review Passed)
- **Giai đoạn 3**: Chỉ đường thế giới (`QuestMarker.cs` hỗ trợ chỉ đích theo Enum `QuestMarkerType` cho NPC/Item, chống NullReferenceException). Đã dùng event `OnQuestStepChanged`. (Static Review Passed)
- **Giai đoạn 4**: Cảnh báo dư thừa khi thu thập tài nguyên (`ResourcePickup.cs`). Cập nhật nhãn ngữ cảnh để người chơi biết cần thêm hay không. (Static Review Passed)
- **Giai đoạn 5**: Hệ thống AI gà lượn/chạy trốn, cho phép bắt và bỏ vào chuồng (`ChickenController.cs`, `ChickenCoop.cs`). (Static Review Passed)
- **Hệ thống WaterKillZone**: Hỗ trợ rơi xuống nước sẽ respawn người chơi và hồi gà về spawn. (Static Review Passed)

## Các bước tiếp theo (Chưa triển khai)
- Giai đoạn 6: Menu nấu ăn dữ liệu hóa. (Unity Compile Passed)
- Giai đoạn 7: Kho tài nguyên làng. (Unity Compile Passed)
- Giai đoạn 8: Rèn mở rộng ngày 7. (Not Run)
- Giai đoạn 9-12: Mở rộng môi trường, đánh trùm cuối, hoàn thiện trải nghiệm. (Not Run)
