using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class CameraController : MonoBehaviour
{
    [Header("Sensitivity")]
    [SerializeField] private float sensitivityX = 3f;
    [SerializeField] private float sensitivityY = 1.5f;

    [Header("Vertical Clamp")]
    [SerializeField] private float minY = -20f;
    [SerializeField] private float maxY = 60f;

    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private float followSpeed = 10f;

    [Header("Colisión de cámara")]
    [SerializeField] private float minDistance = 1f;
    [SerializeField] private float maxDistance = 6f;
    [SerializeField] private LayerMask wallLayers;
    [SerializeField] private float collisionSmooth = 10f;

    // ── Estado 3D ────────────────────────────────────────────────────────
    private float rotX = 0f;
    private float rotY = 0f;
    private float currentDistance;
    private bool is2DMode = false;

    // ── Estado 2D ────────────────────────────────────────────────────────
    private Vector3 saved3DRotation;
    private Coroutine transitionCoroutine;

    // -----------------------------------------------------------------------
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        currentDistance = maxDistance;
        rotY = transform.eulerAngles.y;
    }

    void Update()
    {
        if (is2DMode)
        {
            // En modo 2D solo seguir al jugador verticalmente
            FollowTarget();
            return;
        }

        HandleRotation();
        FollowTarget();
        HandleCameraCollision();
    }

    // ── Rotación 3D normal ───────────────────────────────────────────────
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
            transform.position, target.position, followSpeed * Time.deltaTime
        );
    }

    // ── Colisión de cámara ───────────────────────────────────────────────
    void HandleCameraCollision()
    {
        Vector3 origin = target != null
            ? target.position + Vector3.up * 1.5f
            : transform.position;

        Vector3 camDir = transform.rotation * Vector3.back;
        float desiredDistance = maxDistance;

        if (Physics.SphereCast(origin, 0.2f, camDir, out RaycastHit hit, maxDistance, wallLayers))
            desiredDistance = Mathf.Clamp(hit.distance - 0.1f, minDistance, maxDistance);

        currentDistance = Mathf.Lerp(currentDistance, desiredDistance, collisionSmooth * Time.deltaTime);

        Transform cam = transform.childCount > 0 ? transform.GetChild(0) : null;
        if (cam != null)
            cam.localPosition = new Vector3(0f, 0f, -currentDistance);
    }

    // ── Modo 2D ──────────────────────────────────────────────────────────
    public void EnterMode2D(Vector3 wallNormal, float distance, float speed)
    {
        if (is2DMode) return;
        is2DMode = true;

        // Guardar rotación actual para restaurarla después
        saved3DRotation = new Vector3(rotX, rotY, 0f);

        // La cámara en modo 2D se posiciona perpendicular a la pared
        // mirando hacia la pared desde el lado
        Vector3 camDirection = -wallNormal; // la cámara mira hacia la pared
        Quaternion target2DRot = Quaternion.LookRotation(camDirection);

        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(TransitionCamera(target2DRot, distance, speed));
    }

    public void ExitMode2D()
    {
        if (!is2DMode) return;
        is2DMode = false;

        // Restaurar rotación 3D guardada
        rotX = saved3DRotation.x;
        rotY = saved3DRotation.y;

        Quaternion restoredRot = Quaternion.Euler(rotX, rotY, 0f);

        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(TransitionCamera(restoredRot, maxDistance, 3f));
    }

    IEnumerator TransitionCamera(Quaternion targetRot, float targetDist, float speed)
    {
        Transform cam = transform.childCount > 0 ? transform.GetChild(0) : null;

        while (Quaternion.Angle(transform.rotation, targetRot) > 0.5f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, speed * Time.deltaTime);

            if (cam != null)
            {
                Vector3 targetPos = new Vector3(0f, 0f, -targetDist);
                cam.localPosition = Vector3.Lerp(cam.localPosition, targetPos, speed * Time.deltaTime);
            }

            yield return null;
        }

        transform.rotation = targetRot;
        if (cam != null)
            cam.localPosition = new Vector3(0f, 0f, -targetDist);
    }
}