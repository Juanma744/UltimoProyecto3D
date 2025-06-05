using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public enum ZombieType { Walker, Crawler }

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class NPCMovement : MonoBehaviour
{
    [Header("Referencias")]
    public Transform target;

    [Header("Configuración de Ataque")]
    public float attackRange = 2.0f;
    public bool isCrawler = false;

    [Header("Tipo y Vida")]
    public ZombieType tipo = ZombieType.Walker;
    public float health = 100f;

    // Estados internos
    private NavMeshAgent nav;
    private Animator animator;
    private PlayerHealth playerHealth;
    private bool isPerformingAction = false;
    private bool hasStoodUp = false;
    private bool isBiting = false;
    private bool isDead = false;
    private bool isAwake = true;

    // Progresión y personalización
    private float baseSpeed = 1.0f;
    private float baseAttackRange = 2.0f;

    void Awake()
    {
        nav = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        isCrawler = tipo == ZombieType.Crawler;
        animator.SetBool("Crawler", isCrawler);
        animator.SetBool("Crawl", isCrawler);
        animator.SetBool("Eat", false);
        animator.SetBool("Bite", false);
        animator.SetBool("Run", false);
        animator.SetBool("Walk", !isCrawler);

        // Si es Walker, está de pie desde el inicio
        if (tipo == ZombieType.Walker)
            hasStoodUp = true;

        if (target != null && target.CompareTag("Player"))
            playerHealth = target.GetComponent<PlayerHealth>();
    }

    void Update()
    {
        if (!isAwake || isDead || isPerformingAction || target == null)
        {
            if (nav != null && nav.hasPath)
                nav.ResetPath();
            if (animator != null)
                animator.SetFloat("Speed", 0);
            return;
        }

        float distance = Vector3.Distance(transform.position, target.position);
        float gameTime = Time.timeSinceLevelLoad;

        // Progresión de dificultad
        nav.speed = baseSpeed + Mathf.Min(gameTime / 60f, 2.0f);
        attackRange = baseAttackRange + Mathf.Min(gameTime / 120f, 2.0f);

        // Sincroniza la animación de caminar con la velocidad real
        if (animator != null)
            animator.SetFloat("Speed", nav.velocity.magnitude);

        // Movimiento y lógica de objetivo
        if (!isPerformingAction)
        {
            nav.SetDestination(target.position);
        }

        if (target.CompareTag("Food"))
        {
            if (distance <= attackRange && !isPerformingAction)
                StartCoroutine(PerformEat());
        }
        else if (target.CompareTag("Player"))
        {
            if (isCrawler && !hasStoodUp && distance <= attackRange && !isPerformingAction)
            {
                StartCoroutine(StandUpAndAttack());
            }
            else if (!isCrawler || hasStoodUp)
            {
                HandleAttackOrBite(distance <= attackRange);
            }

            // Animaciones de caminar/correr
            if (!isPerformingAction && (!isCrawler || hasStoodUp))
            {
                bool shouldRun = distance <= 6f && distance > attackRange;
                animator.SetBool("Run", shouldRun);
                animator.SetBool("Walk", !shouldRun);
            }
        }
    }

    // --- ACCIONES ---

    IEnumerator PerformEat()
    {
        isPerformingAction = true;
        nav.isStopped = true;
        animator.SetBool("Eat", true);

        while (target != null && Vector3.Distance(transform.position, target.position) <= attackRange)
            yield return null;

        animator.SetBool("Eat", false);
        nav.isStopped = false;
        isPerformingAction = false;
    }

    IEnumerator StandUpAndAttack()
    {
        isPerformingAction = true;
        nav.isStopped = true;
        animator.SetTrigger("Wake");

        yield return new WaitForSeconds(2.5f);

        isCrawler = false;
        hasStoodUp = true;
        animator.SetBool("Crawler", false);
        animator.SetBool("Crawl", false);
        animator.SetBool("Walk", true);
        nav.isStopped = false;
        isPerformingAction = false;

        HandleAttackOrBite(true);
    }

    IEnumerator PerformAttack()
    {
        isPerformingAction = true;
        nav.isStopped = true;
        animator.SetTrigger("Attack");

        // Daño al jugador
        if (playerHealth != null)
            playerHealth.TakeDamage(10); // Ajusta el valor según tu juego

        yield return new WaitForSeconds(1f);

        nav.isStopped = false;
        isPerformingAction = false;
        StopBiting();
    }

    IEnumerator PerformBite()
    {
        isPerformingAction = true;
        nav.isStopped = true;
        animator.SetBool("Bite", true);
        isBiting = true;

        // Daño al jugador
        if (playerHealth != null)
            playerHealth.TakeDamage(20); // Ajusta el valor según tu juego

        yield return new WaitForSeconds(2.5f);

        nav.isStopped = false;
        isPerformingAction = false;
        StopBiting();
    }

    IEnumerator PerformHit()
    {
        isPerformingAction = true;
        nav.isStopped = true;
        animator.SetTrigger("Hit");

        yield return new WaitForSeconds(0.8f);

        nav.isStopped = false;
        isPerformingAction = false;
    }

    // --- LÓGICA DE ATAQUE ---

    void HandleAttackOrBite(bool inRange)
    {
        if (inRange && !isPerformingAction)
        {
            if (Random.value < 0.5f)
            {
                StartCoroutine(PerformAttack());
                StopBiting();
            }
            else
            {
                StartCoroutine(PerformBite());
            }
        }
        else if (!inRange)
        {
            StopBiting();
        }
    }

    public void StopBiting()
    {
        isBiting = false;
        animator.SetBool("Bite", false);
    }

    // --- RECIBIR DAÑO Y MUERTE ---

    public void Hit(string body)
    {
        if (isDead || isPerformingAction) return;
        if (isBiting)
            StopBiting();

        float damage = 0;
        switch (body)
        {
            case "Head":
                Die();
                return;
            case "Spine2":
                damage = 20;
                break;
            default:
                damage = 10;
                break;
        }
        health -= damage;

        if (health <= 0)
            Die();
        else
            StartCoroutine(PerformHit());
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        nav.isStopped = true;
        nav.enabled = false;

        animator.SetTrigger(Random.value < .5f ? "Death1" : "Death2");
        animator.SetBool("Crawl", false);
        animator.SetBool("Walk", false);
        animator.SetBool("Eat", false);
        animator.SetBool("Bite", false);

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    // --- PERSONALIZACIÓN DESDE EL SPAWNER ---

    public void SetCustomStats(float speed, float attackRange)
    {
        baseSpeed = speed;
        baseAttackRange = attackRange;
    }
}
