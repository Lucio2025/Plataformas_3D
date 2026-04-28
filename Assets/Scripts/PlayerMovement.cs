using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 9f;
    [SerializeField] private float acceleration = 15f;
    [SerializeField] private float deceleration = 20f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float gravityMultiplier = 2.5f;
    [SerializeField] private float coyoteTime = 0.15f;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Camera")]
    [SerializeField] private Transform cameraPivot; // arrastrá el CameraPivot

    private Rigidbody rb;
    private Vector2 moveInput;
    private bool jumpPressed;
    private bool isGrounded;
    private float coyoteTimer;
    private float currentSpeed;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }

    public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();
    public void OnJump(InputValue value) => jumpPressed = value.isPressed;

    void Update()
    {
        CheckGround();
        HandleCoyoteTime();
        HandleJump();
        ApplyExtraGravity();
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    void CheckGround()
    {
        isGrounded = Physics.Raycast(
            transform.position,
            Vector3.down,
            groundCheckDistance + 0.5f,
            groundLayer
        );
    }

    void HandleCoyoteTime()
    {
        if (isGrounded) coyoteTimer = coyoteTime;
        else coyoteTimer -= Time.deltaTime;
    }

    void HandleJump()
    {
        if (jumpPressed && coyoteTimer > 0f)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            coyoteTimer = 0f;
        }
        jumpPressed = false;
    }

    void ApplyExtraGravity()
    {
        if (!isGrounded && rb.linearVelocity.y < 0)
            rb.AddForce(Vector3.down * gravityMultiplier, ForceMode.Acceleration);
    }

    void HandleMovement()
    {
        if (cameraPivot == null) return;

        // Dirección relativa a donde mira la cámara (solo Y)
        Vector3 camForward = cameraPivot.forward;
        Vector3 camRight = cameraPivot.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDirection = camForward * moveInput.y + camRight * moveInput.x;
        bool isMoving = moveDirection.magnitude > 0.1f;

        float targetSpeed = isMoving ?
            (moveInput.magnitude > 0.8f ? runSpeed : walkSpeed) : 0f;

        float accel = isMoving ? acceleration : deceleration;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accel * Time.fixedDeltaTime);

        Vector3 targetVelocity = moveDirection.normalized * currentSpeed;
        targetVelocity.y = rb.linearVelocity.y;
        rb.linearVelocity = targetVelocity;

        // El jugador mira hacia donde se mueve
        if (moveDirection.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetRot, 15f * Time.fixedDeltaTime
            );
        }
    }
}