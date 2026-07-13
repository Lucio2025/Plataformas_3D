using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement2D : MonoBehaviour
{
    [Header("Velocidades 2D")]
    [SerializeField] private float walkSpeed2D = 5f;
    [SerializeField] private float runSpeed2D = 9f;
    [SerializeField] private float jumpForce2D = 8f;

    [Header("Dash 2D")]
    [SerializeField] private float dashForce2D = 20f;
    [SerializeField] private float dashDuration2D = 0.18f;
    [SerializeField] private float dashCooldown2D = 1.2f;

    // Ground layer se copia automáticamente del PlayerMovement — no hace falta asignarlo
    private LayerMask groundLayer;

    // ── Referencias ──────────────────────────────────────────────────────
    private Rigidbody rb;
    private Animator animator;
    private TrailRenderer trail;

    // ── Estado ───────────────────────────────────────────────────────────
    private Vector3 wallNormal;
    private Vector3 moveAxis;

    private bool isActive = false;
    private bool isGrounded = false;
    private bool isDashing = false;
    private float dashCooldownTimer = 0f;

    // ── Input ────────────────────────────────────────────────────────────
    private float inputAxis = 0f;
    private bool jumpPressed = false;
    private bool dashPressed = false;

    // ── Coyote time simple ───────────────────────────────────────────────
    private float coyoteTimer = 0f;
    private const float COYOTE_TIME = 0.15f;

    // -----------------------------------------------------------------------
    /// <summary>
    /// Llamado desde Zone2D. Recibe también el groundLayer del PlayerMovement.
    /// </summary>
    public void Activate(Vector3 wallNormal, Rigidbody rigidbody, LayerMask groundLayer)
    {
        this.wallNormal = wallNormal.normalized;
        this.rb = rigidbody;
        this.groundLayer = groundLayer; // copiado del PlayerMovement original

        moveAxis = Vector3.Cross(wallNormal, Vector3.up).normalized;

        animator = GetComponentInChildren<Animator>();
        trail = GetComponent<TrailRenderer>();

        rb.linearVelocity = Vector3.zero;
        transform.rotation = Quaternion.LookRotation(-wallNormal);

        dashCooldownTimer = 0f;
        coyoteTimer = 0f;
        isActive = true;
        enabled = true;
    }

    public void Deactivate()
    {
        isActive = false;
        isDashing = false;
        enabled = false;

        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
            animator.SetBool("IsDashing", false);
            animator.SetBool("IsSprinting", false);
            animator.SetBool("IsGrounded", true);
        }

        if (trail != null) trail.emitting = false;
    }

    // ── Input callbacks ──────────────────────────────────────────────────
    public void OnMove(InputValue value)
    {
        if (!isActive) return;
        inputAxis = value.Get<Vector2>().x;
    }

    public void OnJump(InputValue value)
    {
        if (!isActive) return;
        if (value.isPressed) jumpPressed = true;
    }

    public void OnDash(InputValue value)
    {
        if (!isActive) return;
        if (value.isPressed) dashPressed = true;
    }

    // -----------------------------------------------------------------------
    void Update()
    {
        if (!isActive) return;

        // Ground check — usa el mismo layer que el PlayerMovement original
        Vector3 bottom = transform.position + Vector3.down * 0.9f;
        bool groundedNow = Physics.CheckSphere(bottom, 0.25f, groundLayer);

        // Coyote time
        if (groundedNow)
            coyoteTimer = COYOTE_TIME;
        else
            coyoteTimer -= Time.deltaTime;

        isGrounded = groundedNow;

        // Cooldown dash
        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;

        // Iniciar dash
        if (dashPressed && dashCooldownTimer <= 0f && !isDashing)
            StartCoroutine(DashCoroutine());
        dashPressed = false;

        // Salto — respeta la gravedad invertida
        if (jumpPressed && isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

            // Si la gravedad está invertida, el salto va hacia abajo (que es el nuevo "arriba")
            GravityFlipManager flipManager = GetComponent<GravityFlipManager>();
            Vector3 jumpDir = (flipManager != null && flipManager.IsFlipped) ? Vector3.down : Vector3.up;

            rb.AddForce(jumpDir * jumpForce2D, ForceMode.Impulse);
            if (animator != null) animator.SetTrigger("Jump");
        }
        jumpPressed = false;

        UpdateAnimations();
    }

    void FixedUpdate()
    {
        if (!isActive || rb == null || isDashing) return;

        bool isSprinting = Keyboard.current.leftShiftKey.isPressed;
        float speed = Mathf.Abs(inputAxis) > 0.1f
            ? (isSprinting ? runSpeed2D : walkSpeed2D)
            : 0f;

        Vector3 targetVel = moveAxis * (inputAxis * speed);
        targetVel.y = rb.linearVelocity.y;

        // Cancelar deriva en el eje de la normal
        float drift = Vector3.Dot(rb.linearVelocity, wallNormal);
        targetVel -= wallNormal * drift;

        rb.linearVelocity = targetVel;

        // Rotar según dirección
        if (Mathf.Abs(inputAxis) > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveAxis * Mathf.Sign(inputAxis));
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 15f * Time.fixedDeltaTime);
        }
    }

    // -----------------------------------------------------------------------
    IEnumerator DashCoroutine()
    {
        isDashing = true;
        dashCooldownTimer = dashCooldown2D;

        float dir = Mathf.Abs(inputAxis) > 0.1f
            ? Mathf.Sign(inputAxis)
            : (Vector3.Dot(transform.forward, moveAxis) >= 0f ? 1f : -1f);

        rb.linearVelocity = Vector3.zero;
        rb.AddForce(moveAxis * dir * dashForce2D, ForceMode.Impulse);

        if (trail != null) trail.emitting = true;
        if (animator != null) animator.SetBool("IsDashing", true);

        yield return new WaitForSeconds(dashDuration2D);

        rb.linearVelocity = new Vector3(
            rb.linearVelocity.x * 0.3f,
            rb.linearVelocity.y,
            rb.linearVelocity.z * 0.3f
        );

        if (trail != null) trail.emitting = false;
        if (animator != null) animator.SetBool("IsDashing", false);

        isDashing = false;
    }

    // -----------------------------------------------------------------------
    void UpdateAnimations()
    {
        if (animator == null) return;

        float speed = Mathf.Abs(inputAxis) > 0.1f
            ? new Vector2(rb.linearVelocity.x, rb.linearVelocity.z).magnitude
            : 0f;

        bool isSprinting = Keyboard.current.leftShiftKey.isPressed && speed > 0.1f;

        animator.SetFloat("Speed", speed);
        animator.SetBool("IsSprinting", isSprinting);
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsDashing", isDashing);
    }

    // -----------------------------------------------------------------------
    void OnDrawGizmosSelected()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position + Vector3.down * 0.9f, 0.25f);
    }
}