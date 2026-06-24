# Multiplayer setup

Project da co nen tang multiplayer bang Netcode for GameObjects.

## Da setup

- `Assets/Resources/NetworkPlayer.prefab` la player prefab dung cho Netcode.
- `Assets/Scenes/GameScene.unity` da co `Network Manager`, `UnityTransport`, va `MultiplayerConnector`.
- Player offline trong scene se tu tat khi Host/Client bat dau de khong bi trung 2 player.

## Cach test

1. Mo `GameScene`.
2. Chay game:
   - `CREATE ROOM`: tao phong online bang Unity Relay
   - `JOIN ROOM`: nhap online code cua host de vao phong

Host se hien online code dang chu/so do Unity Relay tao. Gui code nay cho may khac, may do nhap code roi bam `Join Online`.

## Lobby

1. Moi nguoi nhap ten va chon mau nhan vat.
2. Moi nguoi bam `READY`.
3. Host xem danh sach va bam `PLAY` khi tat ca da san sang.
4. Trong tran, ten nguoi choi hien tren dau bang chu trang, nen den.

Trong lobby, di chuyen, hoi thoai, nhat do, nau an, dong ho ngay va thanh doi deu tam dung. Phong se khong nhan them nguoi sau khi host bat dau tran.

De dung online qua Internet, can link project voi Unity Cloud va bat Unity Relay trong Unity Dashboard. Moi player can co internet. Khong can mo port router.

## Ghi chu

`PlayerMovement` va `PlayerHandController` van hoat dong single-player nhu cu khi network chua start. Khi da start host/client, chi owner cua network player moi duoc doc input.
