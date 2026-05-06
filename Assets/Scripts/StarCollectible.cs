using UnityEngine;

/// <summary>
/// Colocalo en el prefab de la estrella.
/// Rota y flota en bucle. Al ser tocada por el jugador, activa el panel de victoria.
/// </summary>
public class StarCollectible : MonoBehaviour
{
    [Header("Rotación")]
    [SerializeField] private float rotationSpeed = 80f;

    [Header("Flotación")]
    [SerializeField] private float floatAmplitude = 0.3f;  // qué tan alto sube/baja
    [SerializeField] private float floatSpeed = 1.5f;  // qué tan rápido flota

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        // Rotación constante
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

        // Flotación suave en Y (seno en bucle)
        float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Avisar al GameManager
        GameManager.Instance.OnStarCollected();

        Destroy(gameObject);
    }
}