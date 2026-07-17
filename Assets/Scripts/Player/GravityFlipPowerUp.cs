using System.Collections;
using UnityEngine;

/// <summary>
/// Colocalo en un GameObject con:
/// - Box Collider → Is Trigger = true
/// - Este script
/// - En "Visual Mesh" arrastrá el modelo hijo del powerup
/// 
/// Al agarrarlo, invierte la gravedad del jugador.
/// Reaparece después del cooldown.
/// Si el jugador ya tenía la gravedad invertida, la restaura (toggle).
/// </summary>
public class GravityFlipPowerUp : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private float respawnCooldown = 7f;

    [Header("Visual del PowerUp")]
    [SerializeField] private GameObject visualMesh;  // el modelo del powerup
    [SerializeField] private float floatAmplitude = 0.2f;
    [SerializeField] private float floatSpeed = 1.5f;
    [SerializeField] private float spinSpeed = 60f;

    private Vector3 startPosition;
    private bool isAvailable = true;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        if (!isAvailable) return;

        // Flotación y giro decorativo
        float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isAvailable) return;
        if (!other.CompareTag("Player")) return;

        // Buscar o agregar el GravityFlipManager en el jugador
        GravityFlipManager flipManager = other.GetComponent<GravityFlipManager>();
        if (flipManager == null)
            flipManager = other.gameObject.AddComponent<GravityFlipManager>();

        // Toggle: si ya está invertida la restaura, si no la invierte
        if (flipManager.IsFlipped)
            flipManager.DeactivateFlip();
        else
            flipManager.ActivateFlip();

        StartCoroutine(RespawnCooldown());
    }

    IEnumerator RespawnCooldown()
    {
        isAvailable = false;

        if (visualMesh != null)
            visualMesh.SetActive(false);

        yield return new WaitForSeconds(respawnCooldown);

        isAvailable = true;

        if (visualMesh != null)
            visualMesh.SetActive(true);

        // Resetear posición y rotación
        transform.position = startPosition;
        transform.rotation = Quaternion.identity;
    }
}