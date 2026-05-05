using UnityEngine;

public class Coin : MonoBehaviour
{
    [Header("Rotación")]
    [SerializeField] private float rotationSpeed = 90f;

    void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        CoinManager.Instance.AddCoin();
        Destroy(gameObject);
    }
}