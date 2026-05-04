using UnityEngine;
using TMPro;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject coinUI;      // el panel completo (arriba a la derecha)
    [SerializeField] private TextMeshProUGUI coinText;

    private int coinCount = 0;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (coinUI != null) coinUI.SetActive(false); // oculto al inicio
    }

    public void AddCoin()
    {
        coinCount++;

        if (coinUI != null && !coinUI.activeSelf)
            coinUI.SetActive(true); // aparece al recoger la primera

        if (coinText != null)
            coinText.text = $"{coinCount}";
    }

    public void ResetCoins()
    {
        coinCount = 0;
        if (coinText != null) coinText.text = "0";
        if (coinUI != null) coinUI.SetActive(false);
    }

    public int GetCount() => coinCount;
}