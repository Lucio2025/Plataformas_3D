using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    // ─────────────────────────────────────────
    //  MOVEMENT
    // ─────────────────────────────────────────
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 9f;
    [SerializeField] private float acceleration = 15f;
    [SerializeField] private float deceleration = 20f;

    // ─────────────────────────────────────────
    //  JUMP
    // ─────────────────────────────────────────
    [Header("Jump")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float gravityMultiplier = 3f;
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float airControl = 0.6f;   // 0 = sin control, 1 = control total

    // ─────────────────────────────────────────
    //  DASH
    // ─────────────────────────────────────────
    [Header("Dash")]
    [SerializeField] private float dashForce = 22f;
    [SerializeField] private float dashDuration = 0.18f;
    [SerializeField] private float dashCooldown = 1.2f;

    // ─────────────────────────────────────────
    //  TRAIL (estela)
    // ─────────────────────────────────────────
    [Header("Dash Trail")]
    [SerializeField] private float trailTime = 0.25f;   // duración de la estela
    [SerializeField] private float trailWidth = 0.3f;

    // ─────────────────────────────────────────
    //  GROUND CHECK
    // ─────────────────────────────────────────
    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;

    // ─────────────────────────────────────────
    //  CAMERA
    // ─────────────────────────────────────────
    [Header("Camera")]
    [SerializeField] private Transform cameraPivot;

    // ─────────────────────────────────────────
    //  PRIVADOS
    // ─────────────────────────────────────────
    private Rigidbody rb;
    private TrailRenderer trail;
    private Renderer meshRenderer;

    private Vector2 moveInput;
    private bool jumpPressed;
    private bool dashPressed;

    private bool isGrounded;
    private float coyoteTimer;
    private float currentSpeed;

    private bool isDashing;
    private float dashCooldownTimer;

    // Colores por estado
    private static readonly Color colorIdle = Color.green;
    private static readonly Color colorMove = Color.yellow;
    private static readonly Color colorSprint = Color.red;
    private static readonly Color colorDash = Color.red;
    private static readonly Color colorDefault = Color.green;

    // ─────────────────────────────────────────
    //  INIT
    // ─────────────────────────────────────────
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        // Buscamos el renderer en el hijo (la cápsula visual)
        meshRenderer = GetComponentInChildren<Renderer>();

        // Creamos el TrailRenderer por código
        SetupTrail();
    }

    void SetupTrail()
    {
        trail = gameObject.AddComponent<TrailRenderer>();
        trail.time = trailTime;
        trail.startWidth = trailWidth;
        trail.endWidth = 0f;
        trail.material = new Material(Shader.Find("Sprites/Default"));

        // Gradiente: rojo → transparente
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.red, 0f),
                new GradientColorKey(Color.yellow, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.8f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        trail.colorGradient = gradient;
        trail.emitting = false; // Solo emite al dashear
    }

    // ─────────────────────────────────────────
    //  INPUT CALLBACKS (Player Input - Send Messages)
    // ─────────────────────────────────────────
    public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();
    public void OnJump(InputValue value) => jumpPressed = value.isPressed;
    public void OnDash(InputValue value) => dashPressed = value.isPressed;

    // ─────────────────────────────────────────
    //  UPDATE
    // ─────────────────────────────────────────
    void Update()
    {
        if (isDashing) return;

        CheckGround();
        HandleCoyoteTime();
        HandleJump();
        ApplyExtraGravity();
        HandleDashInput();
        UpdateCooldown();
        UpdateColor();
    }

    void FixedUpdate()
    {
        if (isDashing) return;
        HandleMovement();
    }

    // ─────────────────────────────────────────
    //  GROUND
    // ─────────────────────────────────────────
    void CheckGround()
    {
        Vector3 bottom = transform.position + Vector3.down * 0.9f;
        isGrounded = Physics.CheckSphere(bottom, 0.2f, groundLayer);
    }

    void HandleCoyoteTime()
    {
        if (isGrounded) coyoteTimer = coyoteTime;
        else coyoteTimer -= Time.deltaTime;
    }

    // ─────────────────────────────────────────
    //  JUMP
    // ─────────────────────────────────────────
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

    // ─────────────────────────────────────────
    //  MOVEMENT
    // ─────────────────────────────────────────
    void HandleMovement()
    {
        if (cameraPivot == null) return;

        Vector3 camForward = cameraPivot.forward;
        Vector3 camRight = cameraPivot.right;
        camForward.y = 0f; camForward.Normalize();
        camRight.y = 0f; camRight.Normalize();

        Vector3 moveDir = camForward * moveInput.y + camRight * moveInput.x;
        bool isMoving = moveDir.magnitude > 0.1f;
        bool isRunning = isMoving && Keyboard.current.leftShiftKey.isPressed;

        float targetSpeed = isMoving ? (isRunning ? runSpeed : walkSpeed) : 0f;
        float accel = isMoving ? acceleration : deceleration;

        // Reducir control en el aire
        float controlFactor = isGrounded ? 1f : airControl;
        currentSpeed = Mathf.MoveTowards(
            currentSpeed, targetSpeed, accel * controlFactor * Time.fixedDeltaTime
        );

        Vector3 targetVel = moveDir.normalized * currentSpeed;
        targetVel.y = rb.linearVelocity.y;
        rb.linearVelocity = targetVel;

        // Rotar hacia donde se mueve
        if (moveDir.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetRot, 15f * Time.fixedDeltaTime
            );
        }
    }

    // ─────────────────────────────────────────
    //  DASH
    // ─────────────────────────────────────────
    void HandleDashInput()
    {
        if (dashPressed && dashCooldownTimer <= 0f)
        {
            StartCoroutine(DashCoroutine());
        }
        dashPressed = false;
    }

    void UpdateCooldown()
    {
        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;
    }

    IEnumerator DashCoroutine()
    {
        isDashing = true;
        dashCooldownTimer = dashCooldown;

        // Dirección del dash: hacia donde mira la cámara (ignorando Y)
        Vector3 dashDir = cameraPivot != null ? cameraPivot.forward : transform.forward;
        dashDir.y = 0f;
        dashDir.Normalize();

        // Aplicar velocidad del dash
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(dashDir * dashForce, ForceMode.Impulse);

        // Activar estela
        trail.emitting = true;
        SetColor(colorDash);

        yield return new WaitForSeconds(dashDuration);

        // Frenar suavemente al terminar
        rb.linearVelocity = new Vector3(
            rb.linearVelocity.x * 0.3f,
            rb.linearVelocity.y,
            rb.linearVelocity.z * 0.3f
        );

        trail.emitting = false;
        isDashing = false;
    }

    // ─────────────────────────────────────────
    //  COLORES
    // ─────────────────────────────────────────
    void UpdateColor()
    {
        if (meshRenderer == null || isDashing) return;

        bool isMoving = moveInput.magnitude > 0.1f;
        bool isSprinting = Keyboard.current.leftShiftKey.isPressed;

        if (!isMoving) SetColor(colorIdle);
        else if (isSprinting) SetColor(colorSprint);
        else SetColor(colorMove);
    }

    void SetColor(Color color)
    {
        if (meshRenderer != null)
            meshRenderer.material.color = color;
    }

    // ─────────────────────────────────────────
    //  DEBUG (opcional, podés comentar esto)
    // ─────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3 bottom = transform.position + Vector3.down * 0.9f;
        Gizmos.DrawWireSphere(bottom, 0.2f);
    }
}