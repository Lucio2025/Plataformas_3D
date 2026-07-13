using UnityEngine;
using System.Reflection;

/// <summary>
/// Maneja la inversión de gravedad del jugador.
/// - Desactiva la gravedad progresiva del PlayerMovement
/// - Mueve el CapsuleCollider para que coincida con el modelo invertido
/// - Mueve el ground check hacia arriba (para detectar el techo)
/// </summary>
public class GravityFlipManager : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private float rotationSpeed = 8f;

    private Rigidbody rb;
    private CapsuleCollider capsule;
    private Transform modelTransform;
    private PlayerMovement playerMovement;

    private bool isFlipped = false;

    // Gravedad
    private Vector3 normalGravity = new Vector3(0, -9.81f, 0);
    private Vector3 flippedGravity = new Vector3(0, 9.81f, 0);

    // Valores originales del PlayerMovement (leídos por reflexión)
    private float originalJumpForce;
    private float originalMaxGravity;
    private float originalGravityBuildSpeed;

    // Valores originales del CapsuleCollider
    private Vector3 originalColliderCenter;

    // Rotación objetivo del modelo
    private Quaternion targetModelRotation = Quaternion.identity;

    // -----------------------------------------------------------------------
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        playerMovement = GetComponent<PlayerMovement>();

        // Buscar el modelo hijo
        Transform found = transform.Find("HumanM_Model");
        modelTransform = found != null ? found : transform;

        // Guardar el centro original del collider
        if (capsule != null)
            originalColliderCenter = capsule.center;

        // Leer y guardar los valores de gravedad progresiva del PlayerMovement
        ReadGravityValues();
    }

    void Update()
    {
        // Rotar el modelo suavemente
        if (modelTransform != null)
        {
            modelTransform.localRotation = Quaternion.Slerp(
                modelTransform.localRotation,
                targetModelRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }

    // ── Leer valores privados del PlayerMovement via reflexión ───────────
    void ReadGravityValues()
    {
        if (playerMovement == null) return;

        FieldInfo maxField = typeof(PlayerMovement).GetField(
            "maxGravityMultiplier",
            BindingFlags.NonPublic | BindingFlags.Instance
        );
        FieldInfo speedField = typeof(PlayerMovement).GetField(
            "gravityBuildSpeed",
            BindingFlags.NonPublic | BindingFlags.Instance
        );
        FieldInfo jumpField = typeof(PlayerMovement).GetField(
            "jumpForce",
            BindingFlags.NonPublic | BindingFlags.Instance
        );

        if (maxField != null) originalMaxGravity = (float)maxField.GetValue(playerMovement);
        if (speedField != null) originalGravityBuildSpeed = (float)speedField.GetValue(playerMovement);
        if (jumpField != null) originalJumpForce = (float)jumpField.GetValue(playerMovement);

        Debug.Log($"[GravityFlip] Valores leídos: maxGravity={originalMaxGravity}, buildSpeed={originalGravityBuildSpeed}");
    }

    void SetGravityValues(float maxGrav, float buildSpeed, float jumpForce)
    {
        if (playerMovement == null) return;

        FieldInfo maxField = typeof(PlayerMovement).GetField(
            "maxGravityMultiplier",
            BindingFlags.NonPublic | BindingFlags.Instance
        );
        FieldInfo speedField = typeof(PlayerMovement).GetField(
            "gravityBuildSpeed",
            BindingFlags.NonPublic | BindingFlags.Instance
        );
        FieldInfo jumpField = typeof(PlayerMovement).GetField(
            "jumpForce",
            BindingFlags.NonPublic | BindingFlags.Instance
        );

        maxField?.SetValue(playerMovement, maxGrav);
        speedField?.SetValue(playerMovement, buildSpeed);
        jumpField?.SetValue(playerMovement, jumpForce);
    }

    // ── Activar ──────────────────────────────────────────────────────────
    public void ActivateFlip()
    {
        isFlipped = true;

        // 1 — Cambiar gravedad global
        Physics.gravity = flippedGravity;

        // 2 — Desactivar gravedad progresiva del PlayerMovement
        SetGravityValues(0f, 0f, -9f);

        // 3 — Resetear velocidad vertical
        if (rb != null)
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        // 4 — Mover el CapsuleCollider hacia abajo (en espacio local)
        //     El modelo se invierte, así que el collider debe bajar para seguirlo
        if (capsule != null)
        {
            // El centro original estaba en Y=0. Al invertir, lo movemos a -Y
            capsule.center = new Vector3(
                originalColliderCenter.x,
                -originalColliderCenter.y - capsule.height * 0.95f + 0.1f,
                originalColliderCenter.z
            );
        }

        // 5 — Rotar el modelo 180° en Z (boca abajo)
        targetModelRotation = Quaternion.Euler(0f, 0f, 180f);

        Debug.Log("[GravityFlip] Activado");
    }

    // ── Desactivar ───────────────────────────────────────────────────────
    public void DeactivateFlip()
    {
        isFlipped = false;

        // 1 — Restaurar gravedad normal
        Physics.gravity = normalGravity;

        // 2 — Restaurar gravedad progresiva
        SetGravityValues(originalMaxGravity, originalGravityBuildSpeed, originalJumpForce);

        // 3 — Resetear velocidad vertical
        if (rb != null)
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        // 4 — Restaurar centro del collider
        if (capsule != null)
            capsule.center = originalColliderCenter;

        // 5 — Volver modelo a normal
        targetModelRotation = Quaternion.identity;

        Debug.Log("[GravityFlip] Desactivado");
    }

    // ── Al morir ─────────────────────────────────────────────────────────
    public void OnPlayerDied()
    {
        if (isFlipped)
            DeactivateFlip();
    }

    public bool IsFlipped => isFlipped;
}