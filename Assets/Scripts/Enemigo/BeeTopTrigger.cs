using UnityEngine;

/// <summary>
/// Colocalo en un hijo de la abeja, en su parte superior.
/// Box Collider → Is Trigger = true
/// Detecta si el jugador cae encima con velocidad negativa en Y.
/// </summary>
public class BeeTopTrigger : MonoBehaviour
{
    [SerializeField] private float minFallSpeed = 0.5f; // velocidad mínima de caída para contar

    private BeeEnemy bee;

    void Start()
    {
        bee = GetComponentInParent<BeeEnemy>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (bee == null) return;

        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb == null) return;

        // Solo cuenta si el jugador está cayendo
        if (rb.linearVelocity.y < -minFallSpeed)
            bee.GetStompedBy(rb);
    }
}