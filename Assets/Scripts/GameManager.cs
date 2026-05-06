using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Singleton central del nivel.
/// Maneja el temporizador, las monedas finales y el panel de victoria.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Panel de Victoria")]
    [SerializeField] private GameObject victoryPanel;       // el panel completo
    [SerializeField] private TextMeshProUGUI timeText;      // "Tiempo: 1:23"
    [SerializeField] private TextMeshProUGUI coinsText;     // "Monedas: 12"

    [Header("Timer UI (opcional)")]
    [SerializeField] private TextMeshProUGUI timerDisplay;  // timer en pantalla durante el juego (puede ser null)

    private float elapsedTime = 0f;
    private bool gameActive = true;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (victoryPanel != null)
            victoryPanel.SetActive(false);
    }

    void Update()
    {
        if (!gameActive) return;

        elapsedTime += Time.deltaTime;

        // Mostrar timer en pantalla si hay un Text asignado
        if (timerDisplay != null)
            timerDisplay.text = FormatTime(elapsedTime);
    }

    /// <summary>Llamado por StarCollectible cuando el jugador agarra la estrella.</summary>
    public void OnStarCollected()
    {
        gameActive = false;
        Time.timeScale = 0f; // pausa el juego

        // Mostrar panel
        if (victoryPanel != null)
            victoryPanel.SetActive(true);

        // Tiempo final
        if (timeText != null)
            timeText.text = $"Tiempo: {FormatTime(elapsedTime)}";

        // Monedas recogidas
        if (coinsText != null && CoinManager.Instance != null)
            coinsText.text = $"Monedas: {CoinManager.Instance.GetCount()}";

        // Mostrar cursor para que pueda hacer clic en Reiniciar
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>Botón de reiniciar — arrastrá este método al onClick del botón.</summary>
    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    string FormatTime(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        return $"{m}:{s:00}";
    }
}