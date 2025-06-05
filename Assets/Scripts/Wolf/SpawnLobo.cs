using UnityEngine;

public class WolfSpawner : MonoBehaviour
{
    public GameObject wolfPrefab;
    public Transform[] spawnPoints;
    public Transform player;
    public float spawnDistance = 20f;

    public bool autoSpawn = false;
    public float spawnInterval = 5f;

    private int wolvesSpawned = 0;
    private float spawnTimer = 0f;

    void Update()
    {
        if (autoSpawn)
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnInterval)
            {
                SpawnWolf(true);
                spawnTimer = 0f;
            }
        }
    }

    public void SpawnWolf(bool spawnNearPlayer = false)
    {
        Vector3 spawnPos;
        Quaternion spawnRot;

        if (spawnNearPlayer && player != null)
        {
            Vector2 circle = Random.insideUnitCircle.normalized * spawnDistance;
            spawnPos = player.position + new Vector3(circle.x, 0, circle.y);
            spawnPos.y = player.position.y;
            spawnRot = Quaternion.identity;
        }
        else if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            spawnPos = spawnPoint.position;
            spawnRot = spawnPoint.rotation;
        }
        else
        {
            Debug.LogWarning("WolfSpawner: No hay puntos de spawn ni jugador asignado.");
            return;
        }

        GameObject wolfObj = Instantiate(wolfPrefab, spawnPos, spawnRot);
        WolfAI wolf = wolfObj.GetComponent<WolfAI>();
        // Puedes agregar lógica de dificultad aquí si lo deseas

        wolvesSpawned++;
    }
}
