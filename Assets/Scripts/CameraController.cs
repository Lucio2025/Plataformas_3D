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

    private float rotX = 0f;
    private float rotY = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        rotY = transform.eulerAngles.y;
    }

    void Update()
    {
        HandleRotation();
        FollowTarget();
    }

    void HandleRotation()
    {
        Vector2 mouse = Mouse.current.delta.ReadValue();
        rotY += mouse.x * sensitivityX;
        rotX -= mouse.y * sensitivityY;
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
}