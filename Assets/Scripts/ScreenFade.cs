using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFade : MonoBehaviour
{
    [SerializeField] private Image fadeImage;       // Image negra del Canvas
    [SerializeField] private float fadeOutTime = 0.4f;  // negro
    [SerializeField] private float holdTime = 0.2f;  // espera en negro
    [SerializeField] private float fadeInTime = 0.5f;  // vuelve a transparente

    void Awake()
    {
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }
    }

    public IEnumerator FadeAndRespawn(GameObject player, Vector3 spawnPosition)
    {
        // Fade OUT (transparente → negro)
        yield return StartCoroutine(Fade(0f, 1f, fadeOutTime));

        // Teletransportar mientras la pantalla está negra
        CheckpointManager.TeleportPlayer(player, spawnPosition);

        // Esperar un momento en negro
        yield return new WaitForSeconds(holdTime);

        // Fade IN (negro → transparente)
        yield return StartCoroutine(Fade(1f, 0f, fadeInTime));
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        Color c = fadeImage.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, elapsed / duration);
            fadeImage.color = c;
            yield return null;
        }

        c.a = to;
        fadeImage.color = c;
    }
}