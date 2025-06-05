using UnityEngine;

public class ZombieSpawner : MonoBehaviour
{
    [Header("Prefabs y Spawn")]
    public GameObject zombiePrefab;
    public Transform[] spawnPoints; // Puntos fijos en el mapa
    public Transform player;        // Referencia al jugador (para spawn cerca)
    public float spawnDistance = 20f;

    [Header("Spawn Automático")]
    public bool autoSpawn = false;
    public float spawnInterval = 5f; // Segundos entre spawns automáticos

    private int zombiesSpawned = 0;
    private float spawnTimer = 0f;

    void Update()
    {
        if (autoSpawn)
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnInterval)
            {
                SpawnZombie(true);
                spawnTimer = 0f;
            }
        }
    }

    /// <summary>
    /// Spawnea un zombie en un punto fijo o cerca del jugador.
    /// </summary>
    /// <param name="spawnNearPlayer">Si es true, spawnea cerca del jugador; si es false, en un punto fijo.</param>
    public void SpawnZombie(bool spawnNearPlayer = false)
    {
        Vector3 spawnPos;
        Quaternion spawnRot;

        if (spawnNearPlayer && player != null)
        {
            // Spawn aleatorio alrededor del jugador
            Vector2 circle = Random.insideUnitCircle.normalized * spawnDistance;
            spawnPos = player.position + new Vector3(circle.x, 0, circle.y);
            spawnPos.y = player.position.y;
            spawnRot = Quaternion.identity;
        }
        else if (spawnPoints != null && spawnPoints.Length > 0)
        {
            // Spawn en un punto fijo
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            spawnPos = spawnPoint.position;
            spawnRot = spawnPoint.rotation;
        }
        else
        {
            Debug.LogWarning("ZombieSpawner: No hay puntos de spawn ni jugador asignado.");
            return;
        }

        GameObject zombieObj = Instantiate(zombiePrefab, spawnPos, spawnRot);
        var zombie = zombieObj.GetComponent<NPCMovement>();
        if (zombie == null)
        {
            Debug.LogError("ZombieSpawner: El prefab no tiene el componente NPCMovement.");
            Destroy(zombieObj);
            return;
        }

        // Asignar el objetivo al zombie (ajusta según tu script: target o player)
        zombie.target = player;


        // Configuración para los primeros zombies
        if (zombiesSpawned == 0)
        {
            zombie.health = 50;
            zombie.tipo = ZombieType.Walker;
            zombie.isCrawler = false;
            zombie.SetCustomStats(0.5f, 2.0f);
        }
        else if (zombiesSpawned == 1)
        {
            zombie.health = 80;
            zombie.tipo = ZombieType.Crawler;
            zombie.isCrawler = true;
            zombie.SetCustomStats(0.7f, 2.5f);
        }
        // Para los siguientes, aleatorio o progresivo
        else
        {
            float dificultad = 1f + zombiesSpawned * 0.1f;
            zombie.health = 100 * dificultad;
            zombie.tipo = (Random.value > 0.5f) ? ZombieType.Walker : ZombieType.Crawler;
            zombie.isCrawler = (zombie.tipo == ZombieType.Crawler);
            zombie.SetCustomStats(Random.Range(0.5f, 2.5f) * dificultad, Random.Range(2.0f, 4.0f));
        }

        zombiesSpawned++;
    }
}
