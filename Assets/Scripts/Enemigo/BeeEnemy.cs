using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Enemigo abeja para 3D y 2D.
/// - En 3D usa NavMeshAgent para moverse
/// - En 2D patrulla en un eje y persigue al jugador en el mismo carril
/// - Ataque: empuje al jugador
/// - Muerte: el jugador salta encima
/// </summary>
[RequireComponent(typeof(Animator))]
public class BeeEnemy : MonoBehaviour
{
    // ── Estados ───────────────────────────────────────────────────────────
    private enum State { Idle, Patrol, Chase, Attack, Dead }
    private State currentState = State.Idle;

    // ── Configuración ─────────────────────────────────────────────────────
    [Header("Detección")]
    [SerializeField] private float detectionRadius = 6f;   // radio para detectar al jugador
    [SerializeField] private float attackRadius = 1.5f; // radio para atacar
    [SerializeField] private LayerMask playerLayer;

    [Header("Movimiento 3D")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float patrolRadius = 4f;   // radio de patrulla aleatoria
    [SerializeField] private float patrolWaitTime = 2f;   // espera entre puntos de patrulla

    [Header("Movimiento 2D")]
    [SerializeField] private float moveSpeed2D = 2.5f;
    [SerializeField] private float patrolDistance2D = 3f;   // distancia de patrulla en 2D

    [Header("Ataque")]
    [SerializeField] private float pushForce = 8f;   // fuerza del empuje al jugador
    [SerializeField] private float pushUpForce = 3f;   // componente vertical del empuje
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float attackWindup = 0.8f; // tiempo de animación antes del empuje

    [Header("Muerte por salto")]
    [SerializeField] private Transform topTrigger;          // trigger en la parte superior
    [SerializeField] private float bounceForce = 6f;   // rebote que recibe el jugador al matarla

    // ── Referencias ───────────────────────────────────────────────────────
    private Animator animator;
    private NavMeshAgent agent;
    private Transform player;
    private Rigidbody playerRb;

    // ── Estado interno ────────────────────────────────────────────────────
    private bool isDead = false;
    private bool isIn2DMode = false;
    private float attackTimer = 0f;
    private float patrolTimer = 0f;
    private Vector3 patrolTarget = Vector3.zero;

    // ── Parámetros del Animator (los creamos nosotros) ────────────────────
    private static readonly int AnimSpeed = Animator.StringToHash("Speed");
    private static readonly int AnimAttack = Animator.StringToHash("Attack");
    private static readonly int AnimDamage = Animator.StringToHash("Damage");
    private static readonly int AnimDeath = Animator.StringToHash("Death");

    // ── 2D ───────────────────────────────────────────────────────────────
    private Vector3 patrolOrigin2D;
    private float patrolDir2D = 1f;
    private Vector3 moveAxis2D;

    // -----------------------------------------------------------------------
    void Start()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();

        // Buscar al jugador por tag
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerRb = playerObj.GetComponent<Rigidbody>();
        }

        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.stoppingDistance = attackRadius * 0.8f;
        }

        patrolOrigin2D = transform.position;
        SetState(State.Patrol);
    }

    void Update()
    {
        if (isDead) return;

        attackTimer -= Time.deltaTime;
        patrolTimer -= Time.deltaTime;

        if (isIn2DMode)
            UpdateAI2D();
        else
            UpdateAI3D();
    }

    // ── IA 3D ─────────────────────────────────────────────────────────────
    void UpdateAI3D()
    {
        if (player == null) return;

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case State.Patrol:
                Patrol3D();
                if (distToPlayer < detectionRadius)
                    SetState(State.Chase);
                break;

            case State.Chase:
                Chase3D();
                if (distToPlayer <= attackRadius && attackTimer <= 0f)
                    SetState(State.Attack);
                else if (distToPlayer > detectionRadius * 1.5f)
                    SetState(State.Patrol);
                break;

            case State.Attack:
                // El ataque lo maneja la coroutine
                break;
        }

        // Velocidad para el animator
        float speed = agent != null ? agent.velocity.magnitude : 0f;
        animator.SetFloat(AnimSpeed, speed);
    }

    void Patrol3D()
    {
        if (agent == null) return;

        if (patrolTimer <= 0f || agent.remainingDistance < 0.5f)
        {
            // Buscar un nuevo punto de patrulla aleatorio en el NavMesh
            Vector3 randomDir = Random.insideUnitSphere * patrolRadius;
            randomDir += transform.position;
            randomDir.y = transform.position.y;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDir, out hit, patrolRadius, NavMesh.AllAreas))
                agent.SetDestination(hit.position);

            patrolTimer = patrolWaitTime + Random.Range(0f, 1f);
        }
    }

    void Chase3D()
    {
        if (agent != null && player != null)
            agent.SetDestination(player.position);
    }

    // ── IA 2D ─────────────────────────────────────────────────────────────
    void UpdateAI2D()
    {
        if (player == null) return;

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        // Desactivar NavMesh en 2D
        if (agent != null && agent.enabled)
            agent.enabled = false;

        switch (currentState)
        {
            case State.Patrol:
                Patrol2D();
                if (distToPlayer < detectionRadius)
                    SetState(State.Chase);
                break;

            case State.Chase:
                Chase2D();
                if (distToPlayer <= attackRadius && attackTimer <= 0f)
                    SetState(State.Attack);
                else if (distToPlayer > detectionRadius * 1.5f)
                    SetState(State.Patrol);
                break;

            case State.Attack:
                break;
        }

        float speed = new Vector2(
            GetComponent<Rigidbody>() ? GetComponent<Rigidbody>().linearVelocity.x : 0f,
            GetComponent<Rigidbody>() ? GetComponent<Rigidbody>().linearVelocity.z : 0f
        ).magnitude;
        animator.SetFloat(AnimSpeed, speed);
    }

    void Patrol2D()
    {
        // Moverse de un lado al otro en el eje 2D
        Vector3 target = patrolOrigin2D + moveAxis2D * (patrolDir2D * patrolDistance2D);
        MoveTowards2D(target);

        if (Vector3.Distance(transform.position, target) < 0.3f)
            patrolDir2D *= -1f; // invertir dirección
    }

    void Chase2D()
    {
        if (player == null) return;
        MoveTowards2D(player.position);
    }

    void MoveTowards2D(Vector3 target)
    {
        Vector3 dir = (target - transform.position);

        // Solo moverse en el eje permitido
        float projected = Vector3.Dot(dir, moveAxis2D);
        Vector3 moveDir = moveAxis2D * Mathf.Sign(projected);

        transform.position += moveDir * moveSpeed2D * Time.deltaTime;

        // Rotar hacia donde se mueve
        if (Mathf.Abs(projected) > 0.1f)
            transform.rotation = Quaternion.LookRotation(moveDir);
    }

    // ── Ataque ────────────────────────────────────────────────────────────
    void SetState(State newState)
    {
        currentState = newState;

        if (agent != null && !isIn2DMode)
        {
            agent.isStopped = (newState == State.Attack || newState == State.Idle);
        }

        if (newState == State.Attack)
            StartCoroutine(AttackCoroutine());
    }

    IEnumerator AttackCoroutine()
    {
        // Parar movimiento
        if (agent != null) agent.isStopped = true;

        // Animación de ataque
        animator.SetTrigger(AnimAttack);

        // Esperar el windup (la animación de preparación)
        yield return new WaitForSeconds(attackWindup);

        // Aplicar empuje al jugador si sigue cerca
        if (player != null && playerRb != null)
        {
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist <= attackRadius * 1.5f)
            {
                Vector3 pushDir = (player.position - transform.position).normalized;
                pushDir.y = 0f;
                pushDir.y += pushUpForce / pushForce; // pequeño componente vertical

                playerRb.linearVelocity = Vector3.zero;
                playerRb.AddForce(pushDir.normalized * pushForce, ForceMode.Impulse);
            }
        }

        attackTimer = attackCooldown;

        yield return new WaitForSeconds(0.5f);

        // Volver a perseguir
        if (!isDead)
        {
            if (player != null && Vector3.Distance(transform.position, player.position) < detectionRadius)
                SetState(State.Chase);
            else
                SetState(State.Patrol);
        }
    }

    // ── Muerte por salto ──────────────────────────────────────────────────
    /// <summary>
    /// Llamado desde BeeTopTrigger cuando el jugador salta encima.
    /// </summary>
    public void GetStompedBy(Rigidbody attackerRb)
    {
        if (isDead) return;

        isDead = true;

        // Rebotar al jugador hacia arriba
        if (attackerRb != null)
        {
            attackerRb.linearVelocity = new Vector3(
                attackerRb.linearVelocity.x, 0f, attackerRb.linearVelocity.z
            );
            attackerRb.AddForce(Vector3.up * bounceForce, ForceMode.Impulse);
        }

        // Animación de muerte
        animator.SetTrigger(AnimDeath);

        // Desactivar agente y colisiones
        if (agent != null) agent.enabled = false;
        foreach (Collider col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        StartCoroutine(DestroyAfterDeath());
    }

    IEnumerator DestroyAfterDeath()
    {
        yield return new WaitForSeconds(2f);
        Destroy(gameObject);
    }

    // ── Modo 2D ───────────────────────────────────────────────────────────
    /// <summary>
    /// Llamado desde Zone2D cuando el jugador entra/sale del modo 2D.
    /// </summary>
    public void SetMode2D(bool active, Vector3 axis)
    {
        isIn2DMode = active;
        moveAxis2D = axis.normalized;
        patrolOrigin2D = transform.position;

        if (!active && agent != null)
        {
            agent.enabled = true;
            agent.speed = moveSpeed;
        }
    }

    // ── Gizmos ────────────────────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }
}