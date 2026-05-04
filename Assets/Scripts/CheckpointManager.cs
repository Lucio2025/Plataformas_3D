using UnityEngine;

/// <summary>
/// Singleton que guarda el último checkpoint activado.
/// Existe uno solo en la escena. Los Checkpoint y KillZone lo consultan.
/// </summary>
public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    [Header("Fade")]
    [SerializeField] private ScreenFade screenFade; // arrastrá el objeto con ScreenFade

    private Vector3 currentSpawnPoint;
    private bool hasSpawn = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>Registra un nuevo punto de spawn (lo llama Checkpoint).</summary>
    public void SetSpawn(Vector3 position)
    {
        currentSpawnPoint = position;
        hasSpawn = true;
    }

    /// <summary>Teletransporta al jugador al último spawn con fade.</summary>
    public void RespawnPlayer(GameObject player)
    {
        if (!hasSpawn)
        {
            Debug.LogWarning("CheckpointManager: no hay spawn registrado todavía.");
            return;
        }

        if (screenFade != null)
            StartCoroutine(screenFade.FadeAndRespawn(player, currentSpawnPoint));
        else
            TeleportPlayer(player, currentSpawnPoint);
    }

    public static void TeleportPlayer(GameObject player, Vector3 position)
    {
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        player.transform.position = position + Vector3.up * 0.1f;
    }
}