using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra de cooldown del dash.
/// Aparece con fade cuando el jugador dashea, se drena y recarga,
/// y desaparece con fade cuando está lista.
/// </summary>
public class DashCooldownUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Image fillImage;       // la barra que se llena/vacía
    [SerializeField] private CanvasGroup canvasGroup; // para el fade

    [Header("Tiempos")]
    [SerializeField] private float fadeInTime = 0.15f;
    [SerializeField] private float fadeOutTime = 0.5f;
    [SerializeField] private float holdAfterFull = 0.6f; // cuánto espera antes de desaparecer

    [Header("Velocidad de barra")]
    [SerializeField] private float drainSpeed = 8f;   // qué tan rápido se vacía visualmente
    [SerializeField] private float refillSpeed = 1.2f; // qué tan rápido se recarga (igualar al cooldown)

    // Estado interno
    private float fillAmount = 1f;   // 1 = llena, 0 = vacía
    private bool isDraining = false;
    private bool isRecharging = false;
    private bool isVisible = false;

    // Referencia al PlayerMovement para leer el cooldown
    private PlayerMovement playerMovement;

    void Start()
    {
        playerMovement = FindAnyObjectByType<PlayerMovement>();

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        if (fillImage != null)
            fillImage.fillAmount = 1f;
    }

    void Update()
    {
        if (isDraining)
        {
            // Se drena rápido
            fillAmount = Mathf.MoveTowards(fillAmount, 0f, drainSpeed * Time.deltaTime);
            fillImage.fillAmount = fillAmount;

            if (fillAmount <= 0f)
            {
                isDraining = false;
                isRecharging = true;
            }
        }
        else if (isRecharging)
        {
            // Se recarga al ritmo del cooldown
            fillAmount = Mathf.MoveTowards(fillAmount, 1f, refillSpeed * Time.deltaTime);
            fillImage.fillAmount = fillAmount;

            if (fillAmount >= 1f)
            {
                isRecharging = false;
                StartCoroutine(HoldThenFadeOut());
            }
        }
    }

    /// <summary>Llamado desde PlayerMovement cuando se hace un dash.</summary>
    public void OnDashUsed(float cooldown)
    {
        StopAllCoroutines();

        fillAmount = 1f;
        fillImage.fillAmount = 1f;
        isDraining = false;
        isRecharging = false;

        // Ajustar velocidad de recarga al cooldown real
        refillSpeed = 1f / cooldown;

        StartCoroutine(ShowAndDrain());
    }

    IEnumerator ShowAndDrain()
    {
        // Fade in
        yield return StartCoroutine(Fade(0f, 1f, fadeInTime));
        isVisible = true;

        // Pequeña pausa para que se vea la barra llena
        yield return new WaitForSeconds(0.05f);

        // Empezar a drenar
        isDraining = true;
    }

    IEnumerator HoldThenFadeOut()
    {
        yield return new WaitForSeconds(holdAfterFull);

        // Fade out
        yield return StartCoroutine(Fade(1f, 0f, fadeOutTime));
        isVisible = false;
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}