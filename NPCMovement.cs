using UnityEngine;
using UnityEngine.AI;

public enum ZombieType
{
    Walker,
    Crawler
}

public class NPCMovement : MonoBehaviour
{
    // ------------------
    // 1) REFERENCIAS
    // ------------------

    [Header("Referencias")]
    public NavMeshAgent agent;
    public Animator anim;
    public Transform player;
    public Transform[] waypoints;

    // ------------------
    // 2) PARÁMETROS DE IA Y ESTADÍSTICAS
    // ------------------

    [Header("Estadísticas y tipo")]
    public ZombieType tipo = ZombieType.Walker;
    public float health = 100;
    public int damageToPlayer = 15;

    [Header("IA")]
    public float detectionRadius = 12f;
    public float attackRadius = 1.8f;
    public float attackCooldown = 1.7f;

    [Header("Patrulla")]
    public float waitTime = 2f;
    public float waypointThreshold = 0.5f;

    // ------------------
    // 3) VARIABLES INTERNAS
    // ------------------

    private int currentWaypoint = 0;
    private float waitTimer = 0f;
    private float lastAttackTime = -10f;
    private bool isDead = false;
    private bool isChasing = false;
    private bool isAttacking = false;

    // Animator flags
    private float _speedValue = 0f;
    private bool _isWalking = false;
    private bool _isAttackingAnim = false;

    // ------------------
    // 4) MÉTODOS UNITY
    // ------------------

    void Start()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
        if (anim == null)
            anim = GetComponent<Animator>();
        if (player == null)
        {
            GameObject pj = GameObject.FindGameObjectWithTag("Player");
            if (pj != null)
                player = pj.transform;
        }

        if (waypoints != null && waypoints.Length > 0)
            agent.SetDestination(waypoints[0].position);
    }

    void Update()
    {
        if (isDead) return;
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // 1. Ataque (mordida)
        if (distanceToPlayer <= attackRadius)
        {
            isChasing = false;
            agent.isStopped = true;
            agent.SetDestination(player.position);

            // Mirar al jugador
            Vector3 dir = (player.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                lastAttackTime = Time.time;
                StartCoroutine(PerformBite());
            }
            return;
        }

        // 2. Persecución
        if (distanceToPlayer <= detectionRadius)
        {
            isChasing = true;
            isAttacking = false;
            agent.isStopped = false;
            agent.SetDestination(player.position);

            _speedValue = 1f;
            _isWalking = true;
            _isAttackingAnim = false;
            UpdateAnimator();
            return;
        }

        // 3. Patrulla
        isChasing = false;
        isAttacking = false;
        agent.isStopped = false;

        if (waypoints == null || waypoints.Length == 0) return;

        if (!agent.pathPending && agent.remainingDistance < waypointThreshold)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTime)
            {
                currentWaypoint = (currentWaypoint + 1) % waypoints.Length;
                agent.SetDestination(waypoints[currentWaypoint].position);
                waitTimer = 0f;
            }
        }

        _speedValue = agent.velocity.magnitude > 0.1f ? 0.5f : 0f;
        _isWalking = agent.velocity.magnitude > 0.1f;
        _isAttackingAnim = false;
        UpdateAnimator();
    }

    // ------------------
    // 5) ANIMACIÓN
    // ------------------
    private void UpdateAnimator()
    {
        if (anim == null) return;
        anim.SetFloat("Speed", _speedValue);
        anim.SetBool("IsWalking", _isWalking);
        anim.SetBool("IsAttacking", _isAttackingAnim);
    }

    // ------------------
    // 6) CORUTINA DE ATAQUE (MORDIDA)
    // ------------------
    private System.Collections.IEnumerator PerformBite()
    {
        isAttacking = true;
        _isAttackingAnim = true;
        UpdateAnimator();

        // Espera para sincronizar con la animación de mordida
        yield return new WaitForSeconds(0.4f);

        // Infligir daño al jugador
        if (player != null)
        {
            PlayerHealth ph = player.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(damageToPlayer);
            }
        }

        yield return new WaitForSeconds(0.3f);

        isAttacking = false;
        _isAttackingAnim = false;
        UpdateAnimator();
    }

    // ------------------
    // 7) RECIBIR DAÑO DESDE EL EXTERIOR
    // ------------------
    public void Hit(string colliderName)
    {
        Debug.Log($"NPC hit by: {colliderName}");
        health -= 10; // Puedes ajustar el daño recibido
        if (health <= 0)
        {
            Die();
        }
    }

    // ------------------
    // 8) MUERTE
    // ------------------
    private void Die()
    {
        Debug.Log("NPC has died.");
        isDead = true;
        agent.isStopped = true;
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        // Puedes poner animación de muerte aquí si tienes
        Destroy(gameObject, 3f);
    }

    // ------------------
    // 9) CONFIGURACIÓN DESDE EL SPAWNER
    // ------------------
    public void SetCustomStats(float speed, float damage)
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
        agent.speed = speed;
        damageToPlayer = (int)damage;
    }

    // ------------------
    // 10) GIZMOS PARA WAYPOINTS Y RADIOS
    // ------------------
    private void OnDrawGizmosSelected()
    {
        if (waypoints != null && waypoints.Length > 0)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] != null)
                    Gizmos.DrawSphere(waypoints[i].position, 0.2f);
                if (i < waypoints.Length - 1 && waypoints[i] != null && waypoints[i + 1] != null)
                    Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }
        }
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
