using UnityEngine;

public class Coin : MonoBehaviour
{
    [Header("Rotación")]
    [SerializeField] private float rotationSpeed = 90f;

    [Header("Efecto al recoger")]
    [SerializeField] private ParticleSystem sparkleEffect;

    void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        CoinManager.Instance.AddCoin();

        if (sparkleEffect != null)
        {
            sparkleEffect.transform.SetParent(null);
            sparkleEffect.Play();
            Destroy(sparkleEffect.gameObject, sparkleEffect.main.duration);
        }

        Destroy(gameObject);
    }
}