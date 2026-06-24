using UnityEngine;

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
