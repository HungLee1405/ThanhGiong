# Multiplayer setup

Project da co nen tang multiplayer bang Netcode for GameObjects.

## Da setup

- `Assets/Resources/NetworkPlayer.prefab` la player prefab dung cho Netcode.
- `Assets/Scenes/GameScene.unity` da co `Network Manager`, `UnityTransport`, va `MultiplayerConnector`.
- Player offline trong scene se tu tat khi Host/Client bat dau de khong bi trung 2 player.

## Cach test

1. Mo `GameScene`.
2. Chay game:
   - `Host Online`: tao phong online bang Unity Relay
   - `Join Online`: nhap online code cua host de vao phong
   - `H`: Host nhanh
   - `K`: Shutdown

Host se hien online code dang chu/so do Unity Relay tao. Gui code nay cho may khac, may do nhap code roi bam `Join Online`.

De dung online qua Internet, can link project voi Unity Cloud va bat Unity Relay trong Unity Dashboard. Moi player can co internet. Khong can mo port router.

## Ghi chu

`PlayerMovement` va `PlayerHandController` van hoat dong single-player nhu cu khi network chua start. Khi da start host/client, chi owner cua network player moi duoc doc input.
