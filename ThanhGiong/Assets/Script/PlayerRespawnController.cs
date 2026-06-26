using UnityEngine;

public class PlayerRespawnController : MonoBehaviour
{
    [Header("Respawn Config")]
    public Vector3 respawnPoint;

    private CharacterController characterController;
    private bool isRespawning = false;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        UpdateCheckpoint(transform.position);
    }

    public void UpdateCheckpoint(Vector3 newCheckpoint)
    {
        respawnPoint = newCheckpoint;
    }

    public void Respawn()
    {
        if (isRespawning) return;
        isRespawning = true;

        if (GameOverUI.Instance != null)
        {
            GameOverUI.Instance.ShowGameOver("Bạn đã chết!", "Hãy cẩn thận bước chân của mình...", true, () => 
            {
                DoRespawnLogic();
            });
        }
        else
        {
            DoRespawnLogic();
        }
    }

    private void DoRespawnLogic()
    {
        if (characterController != null)
        {
            characterController.enabled = false;
            transform.position = respawnPoint;
            characterController.enabled = true;
        }
        else
        {
            transform.position = respawnPoint;
        }

        PlayerMovement pm = GetComponent<PlayerMovement>();
        if (pm != null)
        {
            pm.ResetVelocity();
        }

        // Nếu cầm gà thì trả gà về khu vực spawn
        PlayerHandController hand = GetComponent<PlayerHandController>();
        if (hand != null && hand.carriedChicken != null && !hand.carriedChicken.isDelivered)
        {
            hand.carriedChicken.ResetToSpawn();
            
            // Xóa item gà khỏi tay
            if (hand.TryConsumeHeldItem(1))
            {
                hand.carriedChicken = null;
                hand.RefreshHeldItem(); // Đảm bảo visual biến mất và mở khóa đổi slot
            }
        }
        
        Debug.Log("Player has been respawned at " + respawnPoint);
        isRespawning = false;
    }
}
