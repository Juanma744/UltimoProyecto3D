using UnityEngine;
using UnityEngine.AI;

public class WolfAI : MonoBehaviour
{
    // ------------------
    // 1) REFERENCIAS
    // ------------------

    [Header("References")]
    [Tooltip("El Animator del lobo. Asignar manualmente desde el Inspector.")]
    public Animator anim;

    [Tooltip("El NavMeshAgent del lobo. Debe estar agregado como componente en el mismo GameObject.")]
    public NavMeshAgent agent;

    [Tooltip("Transform del jugador (arrastrar el objeto con Tag 'Player' o asignarlo directamente).")]
    public Transform player;

    // ------------------
    // 2) PARÁMETROS DE IA
    // ------------------

    [Header("Rangos de detección")]
    [Tooltip("Radio en unidades dentro del cual el lobo 'huele' al jugador (entra en Chase).")]
    public float smellRadius = 15f;

    [Tooltip("Radio en unidades dentro del cual el lobo ataca cuerpo a cuerpo.")]
    public float attackRadius = 2f;

    [Tooltip("Tiempo (segundos) que debe pasar entre un mordisco y el siguiente.")]
    public float attackCooldown = 1.5f;

    [Header("Estadísticas del lobo")]
    [Tooltip("Vida máxima del lobo.")]
    public int maxHealth = 100;

    [Tooltip("Daño que inflige al jugador por ataque.")]
    public int damageToPlayer = 10;

    // ------------------
    // 3) VARIABLES PARA WANDER (PATRULLA)
    // ------------------

    [Header("Wander / Patrol Settings")]
    [Tooltip("Radio en el que el lobo selecciona un nuevo punto para patrullar.")]
    public float wanderRadius = 10f;

    [Tooltip("Tiempo mínimo en segundos que el lobo caminando antes de elegir otro waypoint.")]
    public float wanderTimerMin = 5f;

    [Tooltip("Tiempo máximo en segundos que el lobo caminando antes de elegir otro waypoint.")]
    public float wanderTimerMax = 10f;

    private float wanderTimer;
    private Vector3 currentWanderTarget;

    // ------------------
    // 4) VARIABLES INTERNAS (ESTADOS Y BUFFERS)
    // ------------------

    private int currentHealth;
    private float lastAttackTime = 0f;
    private bool isDead = false;
    private bool isChasing = false;
    private bool isAttacking = false;

    // Flags para el Animator
    private bool _isWalking = false;
    private bool _isCrouching = false;
    private bool _isSitting = false;
    private float _speedValue = 0f;

    // ------------------
    // 5) MÉTODOS UNITY
    // ------------------

    void Start()
    {
        currentHealth = maxHealth;

        if (anim == null)
            anim = GetComponent<Animator>();
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
        if (player == null)
        {
            GameObject pj = GameObject.FindGameObjectWithTag("Player");
            if (pj != null)
                player = pj.transform;
            else
                Debug.LogError("WolfAI: No hay ningún objeto con Tag 'Player' en la escena.");
        }

        wanderTimer = Random.Range(wanderTimerMin, wanderTimerMax);
        currentWanderTarget = RandomNavmeshLocation(transform.position, wanderRadius);

        if (agent != null)
            agent.updateRotation = true;
    }

    void Update()
    {
        if (isDead) return;
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // 1. Ataque
        if (distanceToPlayer <= attackRadius)
        {
            isChasing = false;
            agent.isStopped = true;
            agent.SetDestination(player.position);

            // Mirar al jugador
            Vector3 dir = (player.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);

            // Atacar con cooldown
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                lastAttackTime = Time.time;
                StartCoroutine(PerformAttack());
            }
            return;
        }

        // 2. Persecución
        if (distanceToPlayer <= smellRadius)
        {
            isChasing = true;
            isAttacking = false;
            agent.isStopped = false;
            agent.SetDestination(player.position);

            if (distanceToPlayer > (smellRadius * 0.5f))
            {
                // Caminar
                _speedValue = 0f;
                _isWalking = true;
                _isCrouching = false;
                _isSitting = false;
            }
            else
            {
                // Correr
                _speedValue = 1f;
                _isWalking = false;
                _isCrouching = false;
                _isSitting = false;
            }
            UpdateAnimatorParameters();
            return;
        }

        // 3. Patrulla
        isChasing = false;
        isAttacking = false;
        agent.isStopped = false;

        wanderTimer -= Time.deltaTime;
        float distanceToWanderTarget = Vector3.Distance(transform.position, currentWanderTarget);
        if (distanceToWanderTarget <= 1.5f || wanderTimer <= 0f)
        {
            Vector3 newTarget = RandomNavmeshLocation(transform.position, wanderRadius);
            if (newTarget != Vector3.zero)
            {
                currentWanderTarget = newTarget;
                agent.SetDestination(currentWanderTarget);
            }
            wanderTimer = Random.Range(wanderTimerMin, wanderTimerMax);
        }

        _speedValue = 0f;
        _isWalking = true;
        _isCrouching = false;
        _isSitting = false;
        UpdateAnimatorParameters();
    }

    // ------------------
    // 6) MÉTODO PARA WANDER: OBTENER PUNTO ALEATORIO EN EL NAVMESH
    // ------------------
    private Vector3 RandomNavmeshLocation(Vector3 origin, float radius)
    {
        for (int i = 0; i < 30; i++)
        {
            Vector3 randomDirection = Random.insideUnitSphere * radius;
            randomDirection += origin;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, 2.0f, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }
        return Vector3.zero;
    }

    // ------------------
    // 7) ACTUALIZAR PARÁMETROS DEL ANIMATOR
    // ------------------
    private void UpdateAnimatorParameters()
    {
        if (anim == null) return;
        anim.SetFloat("Speed", _speedValue);
        anim.SetBool("IsWalking", _isWalking);
        anim.SetBool("IsCrouching", _isCrouching);
        anim.SetBool("IsSitting", _isSitting);
    }

    // ------------------
    // 8) CORUTINA DE ATAQUE
    // ------------------
    private System.Collections.IEnumerator PerformAttack()
    {
        isAttacking = true;

        // Animación de ataque (puedes ajustar esto según tu animación)
        _speedValue = 0f;
        _isWalking = false;
        _isCrouching = true;
        _isSitting = false;
        UpdateAnimatorParameters();

        yield return new WaitForSeconds(0.3f);

        // Infligir daño al jugador
        if (player != null)
        {
            PlayerHealth ph = player.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(damageToPlayer);
            }
        }

        yield return new WaitForSeconds(0.2f);

        isAttacking = false;
    }

    // ------------------
    // 9) RECIBIR DAÑO
    // ------------------
    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // ------------------
    // 10) MUERTE
    // ------------------
    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (agent != null) agent.isStopped = true;
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        _speedValue = 0f;
        _isWalking = false;
        _isCrouching = false;
        _isSitting = true;
        UpdateAnimatorParameters();

        Destroy(gameObject, 3f);
    }

    // ------------------
    // 11) GIZMOS PARA DEBUG EN SCENE VIEW
    // ------------------
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, smellRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, wanderRadius);
    }
}
