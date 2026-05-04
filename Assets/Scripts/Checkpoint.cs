using UnityEngine;

/// <summary>
/// Colocalo en un GameObject con Collider (Is Trigger = true).
/// Cuando el jugador lo toca, registra el spawn y lanza el efecto de confeti.
/// </summary>
public class Checkpoint : MonoBehaviour
{
    [Header("Efecto")]
    [SerializeField] private ParticleSystem confettiEffect; // arrastrá tu prefab de confeti

    [Header("Visual (opcional)")]
    [SerializeField] private Renderer flagRenderer;         // si tenés un modelo de bandera
    [SerializeField] private Color activatedColor = Color.yellow;

    private bool activated = false;

    void OnTriggerEnter(Collider other)
    {
        if (activated) return;
        if (!other.CompareTag("Player")) return;

        activated = true;

        // Registrar posición de spawn
        CheckpointManager.Instance.SetSpawn(transform.position);

        // Lanzar confeti
        if (confettiEffect != null)
        {
            confettiEffect.gameObject.SetActive(true);
            confettiEffect.Play();
        }

        // Cambiar color de la bandera (opcional)
        if (flagRenderer != null)
            flagRenderer.material.color = activatedColor;

        Debug.Log($"Checkpoint activado: {gameObject.name}");
    }
}