# Hướng dẫn Setup trong Unity Inspector

Sau khi cập nhật Script từ Giai đoạn 1 đến Giai đoạn 7, bạn cần thực hiện các thao tác sau trong Unity Editor:

## 1. Cấu hình QuestDatabase
- Mở GameObject chứa `QuestDatabase` (thường là Manager chung).
- Kéo ItemData của **Cuốc chim** vào trường `pickaxeItem`.
- Kéo ItemData của **Rìu** vào trường `axeItem`.

## 2. Cấu hình QuestMarker
- Tạo GameObject làm hiệu ứng chỉ đường (VD: mũi tên, vòng sáng) và đặt làm child của NPC hoặc Resource.
- Gắn script `QuestMarker` vào GameObject đó.
- Gán `markerVisual` là chính GameObject có mesh/particle mũi tên (hoặc object chứa hiệu ứng).
- Chọn `Marker Type` là `NPC` hoặc `Item` hoặc `Any`.
- Điền `Target Id` tương ứng với ID của NPC hoặc Item (VD: `blacksmith`, `village_elder`, `water`, `iron_ore`).

## 3. Cấu hình ChickenController (Gà)
- Gắn `ChickenController` vào GameObject Gà.
- Đảm bảo Gà có thành phần `NavMeshAgent`.
- Đảm bảo Gà có thành phần `Collider` (`Is Trigger = true`) để phát hiện người chơi.
- Kéo UI Interaction và ItemData `chicken` vào Inspector của `ChickenController`.
- **BAKE lại NavMesh** trên cửa sổ Navigation để Gà có thể di chuyển (nếu chưa bake, AI sẽ bị kẹt hoặc báo lỗi `isOnNavMesh`).

## 4. Cấu hình ChickenCoop (Chuồng Gà)
- Gắn `ChickenCoop` vào GameObject Chuồng Gà.
- Đảm bảo GameObject này có gắn `Collider`.
- Điền `"chicken"` vào `acceptedItemId`.
- Kéo các object hiển thị gà (nằm trong chuồng) vào mảng `chickenVisuals`. Số lượng mảng này tương ứng sức chứa tối đa mà visual có thể hiện.

## 5. Cấu hình KillZone / Water
- Tạo một vùng nước và gắn một BoxCollider có `Is Trigger = true`.
- Gắn script `WaterKillZone` vào vùng nước đó.
- Chọn Tag `Player` cho GameObject người chơi. Gắn script `PlayerRespawnController` vào người chơi.
- Trong `PlayerRespawnController`, có thể cài đặt `respawnPoint` mặc định hoặc để script tự lấy vị trí lúc Start.

## 6. Cấu hình Nấu Ăn (Giai đoạn 6)
- Chuột phải ở cửa sổ Project `Create > Cooking > Recipe` để tạo dữ liệu món ăn (VD: Cơm trắng, Cơm gà, Cơm lam).
- Cấu hình từng Recipe: Kéo các `ItemData` vào list Ingredients và Output. Chỉnh thời gian nấu (`cookTime`) và `unlockDay`.
- Gắn `CookingMenuUI` vào Canvas. Kéo Panel menu vào trường `menuPanel` và các Recipe vào `availableRecipes`. Cấu hình UI nút bấm sẽ gọi hàm `StartCooking(recipe)` nếu cần.
- Ở GameObject `CookingPot` hiện tại, bật cờ `Use Data Driven Menu` = `true` và kéo object chứa `CookingMenuUI` vào.

## 7. Cấu hình Kho Làng (Giai đoạn 7)
- Tạo một GameObject Kho làng, gắn BoxCollider (để bắt TriggerEnter).
- Gắn script `VillageStorage`.
- Kéo `InteractionUI` chung vào biến tương ứng.
- Khi người chơi cầm gạo/sắt/tre và nhấn R gần Kho, dữ liệu sẽ lưu thẳng vào biến `...Amount` trong Runtime và gửi thông báo quest.
