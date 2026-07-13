using UnityEngine;
using System.Reflection;

public class Zone2D : MonoBehaviour
{
    [Header("Configuración de la pared")]
    [Tooltip("La dirección perpendicular a la pared. Ej: (0,0,1) si mira hacia Z, (-1,0,0) si mira hacia -X")]
    [SerializeField] private Vector3 wallNormal = Vector3.forward;

    [Header("Referencias")]
    [SerializeField] private CameraController cameraController;

    [Header("Cámara 2D")]
    [SerializeField] private float camera2DDistance = 10f;
    [SerializeField] private float cameraTransitionSpeed = 3f;

    private PlayerMovement playerMovement;
    private PlayerMovement2D playerMovement2D;
    private bool isIn2DMode = false;

    // -----------------------------------------------------------------------
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (isIn2DMode) return;

        playerMovement = other.GetComponent<PlayerMovement>();

        playerMovement2D = other.GetComponent<PlayerMovement2D>();
        if (playerMovement2D == null)
            playerMovement2D = other.gameObject.AddComponent<PlayerMovement2D>();

        Enter2DMode(other.gameObject);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (!isIn2DMode) return;
        Exit2DMode();
    }

    // -----------------------------------------------------------------------
    void Enter2DMode(GameObject player)
    {
        isIn2DMode = true;

        // Leer el groundLayer del PlayerMovement original via reflexión
        // (porque es SerializeField privado)
        LayerMask groundLayer = GetGroundLayer(playerMovement);

        // Desactivar movimiento 3D
        if (playerMovement != null)
            playerMovement.enabled = false;

        // Activar movimiento 2D pasándole el groundLayer
        playerMovement2D.Activate(wallNormal, player.GetComponent<Rigidbody>(), groundLayer);

        // Mover cámara
        if (cameraController != null)
            cameraController.EnterMode2D(wallNormal, camera2DDistance, cameraTransitionSpeed);
    }

    public void Exit2DMode()
    {
        isIn2DMode = false;

        if (playerMovement != null)
            playerMovement.enabled = true;

        if (playerMovement2D != null)
            playerMovement2D.Deactivate();

        if (cameraController != null)
            cameraController.ExitMode2D();
    }

    // -----------------------------------------------------------------------
    /// <summary>
    /// Lee el groundLayer del PlayerMovement usando reflexión
    /// (porque es un campo privado serializado).
    /// </summary>
    LayerMask GetGroundLayer(PlayerMovement pm)
    {
        if (pm == null) return ~0;

        FieldInfo field = typeof(PlayerMovement).GetField(
            "groundLayer",
            BindingFlags.NonPublic | BindingFlags.Instance
        );

        if (field != null)
            return (LayerMask)field.GetValue(pm);

        Debug.LogWarning("[Zone2D] No se pudo leer groundLayer de PlayerMovement. Usando ~0.");
        return ~0;
    }
}