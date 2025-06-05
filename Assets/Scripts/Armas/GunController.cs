using UnityEngine;

public class GunController : MonoBehaviour
{
    public float fireRate = 0.5f; // Tiempo entre disparos
    private float nextFireTime = 0f;
    public Camera playerCamera; // Cámara del jugador
    public float range = 100f; // Alcance del arma
    private AudioSource audio;
    public AudioClip shootAudio;
    public AudioClip reloadAudio;
    public AudioClip emptyAudio;
    public ParticleSystem muzzleFlash; // Efecto de fogonazo
    public Transform barrel;
    public ParticleSystem impactFX; // Efecto de impacto

    void Start()
    {
        audio = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            audio.PlayOneShot(shootAudio);
            Shoot();
        }
    }

    void Shoot()
    {
        // Fogonazo
        if (muzzleFlash != null && barrel != null)
        {
            ParticleSystem muzzle = Instantiate(muzzleFlash, barrel.position, barrel.rotation);
            Destroy(muzzle.gameObject, 1f);
        }

        Vector3 direction = playerCamera.transform.forward;
        if (Physics.Raycast(playerCamera.transform.position, direction, out RaycastHit hit, range))
        {
            Debug.Log("Hit: " + hit.collider.name);

            // Efecto de impacto (solo una vez)
            if (impactFX != null)
            {
                Instantiate(impactFX, hit.point, Quaternion.LookRotation(hit.normal));
            }

            // Dañar zombie
            NPCMovement zombie = hit.collider.GetComponentInParent<NPCMovement>();
            if (zombie != null)
            {
                zombie.Hit(hit.collider.name);
            }

            // Dañar lobo
            WolfAI wolf = hit.collider.GetComponentInParent<WolfAI>();
            if (wolf != null)
            {
                wolf.TakeDamage(50); // Ajusta el daño según lo que desees
            }
        }
    }
}
