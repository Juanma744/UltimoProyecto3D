using Leguar.LowHealth;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("Salud")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Recuperación automática")]
    public float recoveryDelay = 3f;
    public float recoveryRate = 10f;

    [Header("Pantalla de muerte")]
    public GameObject deathScreen; // Asigna el panel desde el inspector

    private float lastDamageTime;
    private LowHealthController lowHealthController;
    
    // En PlayerHealth.cs
    private bool isDead = false;
    public bool IsDead => isDead;



    void Start()
    {
        currentHealth = maxHealth;
        lowHealthController = Camera.main.GetComponent<LowHealthController>();
        UpdateLowHealthEffect();
        if (deathScreen != null)
            deathScreen.SetActive(false);
    }

    void Update()
    {
        if (isDead) return;

        if (currentHealth < maxHealth && Time.time - lastDamageTime > recoveryDelay)
        {
            currentHealth += Mathf.CeilToInt(recoveryRate * Time.deltaTime);
            if (currentHealth > maxHealth) currentHealth = maxHealth;
            UpdateLowHealthEffect();
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        if (currentHealth < 0) currentHealth = 0;
        lastDamageTime = Time.time;
        UpdateLowHealthEffect();
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void UpdateLowHealthEffect()
    {
        if (lowHealthController != null)
        {
            float normalizedHealth = Mathf.Clamp01((float)currentHealth / maxHealth);
            lowHealthController.SetPlayerHealthInstantly(normalizedHealth);
        }
    }

    private void Die()
    {
        isDead = true;
        if (deathScreen != null)
            deathScreen.SetActive(true);

        // Desactivar movimiento
        var movement = GetComponent<FirstPersonMovement>();
        if (movement != null)
            movement.enabled = false;

        // Desactivar control de cámara
        var look = GetComponentInChildren<FirstPersonLook>();
        if (look != null)
            look.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Métodos para los botones de la pantalla de muerte
    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMenu()
    {
        SceneManager.LoadScene("Menu"); // Cambia por el nombre real de tu escena de menú
    }
}
