using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Collider))]
public class WaterKillZone : MonoBehaviour
{
    private void Start()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            NetworkManager manager = NetworkManager.Singleton;
            PlayerMovement movement = other.GetComponentInParent<PlayerMovement>();
            if (manager != null && manager.IsListening && movement != null && movement.IsSpawned && !movement.IsOwner)
                return;

            PlayerRespawnController respawn = other.GetComponent<PlayerRespawnController>();
            if (respawn == null)
            {
                respawn = other.GetComponentInParent<PlayerRespawnController>();
            }

            if (respawn != null)
            {
                respawn.Respawn();
            }
        }
    }
}
