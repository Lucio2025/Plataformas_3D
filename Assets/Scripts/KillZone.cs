using UnityEngine;

/// <summary>
/// Colocalo en un GameObject grande debajo del mapa con Collider (Is Trigger = true).
/// Cuando el jugador cae y lo toca, hace fade negro y respawnea.
/// </summary>
public class KillZone : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        CheckpointManager.Instance.RespawnPlayer(other.gameObject);
    }
}