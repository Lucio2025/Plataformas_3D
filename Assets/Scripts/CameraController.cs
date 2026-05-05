using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target; // arrastrá el Player

    [Header("Sensitivity")]
    [SerializeField] private float sensitivityX = 3f;
    [SerializeField] private float sensitivityY = 2f;

    [Header("Vertical Clamp")]
    [SerializeField] private float minY = -20f;
    [SerializeField] private float maxY = 60f;

    [Header("Distance")]
    [SerializeField] private float followSpeed = 10f;

    [Header("Colisión de cámara")]
    [SerializeField] private float minDistance = 1f;
    [SerializeField] private float maxDistance = 6f;
    [SerializeField] private LayerMask wallLayers;
    [SerializeField] private float collisionSmooth = 10f;

    private float currentDistance;

    private float rotX = 0f;
    private float rotY = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        rotY = transform.eulerAngles.y;

        currentDistance = maxDistance;
    }

    void Update()
    {
        HandleRotation();
        FollowTarget();
        HandleCameraCollision();
    }

    void HandleRotation()
    {
        Vector2 mouse = Mouse.current.delta.ReadValue();
        rotY += mouse.x * sensitivityX * 0.1f;
        rotX -= mouse.y * sensitivityY * 0.1f;
        rotX = Mathf.Clamp(rotX, minY, maxY);
        transform.rotation = Quaternion.Euler(rotX, rotY, 0f);
    }

    void FollowTarget()
    {
        if (target == null) return;
        transform.position = Vector3.Lerp(
            transform.position,
            target.position,
            followSpeed * Time.deltaTime
        );
    }

    void HandleCameraCollision()
    {
        // Punto desde donde sale el rayo (posición del jugador)
        Vector3 origin = target.position + Vector3.up * 1.5f;

        // Dirección hacia donde estaría la cámara sin colisión
        Vector3 camDir = transform.rotation * Vector3.back;

        float desiredDistance = maxDistance;

        if (Physics.SphereCast(origin, 0.2f, camDir, out RaycastHit hit, maxDistance, wallLayers))
        {
            // Si hay algo en el medio, acortamos la distancia
            desiredDistance = Mathf.Clamp(hit.distance - 0.1f, minDistance, maxDistance);
        }

        currentDistance = Mathf.Lerp(currentDistance, desiredDistance, collisionSmooth * Time.deltaTime);

        // Reposicionamos la CinemachineCamera hija
        Transform cam = transform.GetChild(0);
        if (cam != null)
            cam.localPosition = new Vector3(0f, 0f, -currentDistance);
    }
}