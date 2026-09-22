using UnityEngine;
using System.Collections.Generic;

public class spawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnableObject
    {
        public GameObject prefab;
        [Tooltip("Höher = wahrscheinlicher. Wert ist relativ zu den anderen Objekten in der Liste, kein festes Maximum. Z.B. 10 = häufig, 1 = selten")]
        public float spawnChance = 1f;
        [Tooltip("Wie viele Spawnpunkte dieses Objekt blockiert (1 = normal, 2 = groß, etc.)")]
        public int sizeInSlots = 1;
        [Tooltip("Maximal gleichzeitig aktive Instanzen dieses Prefabs in der Szene")]
        public int maxActiveCount = 4;

        [System.NonSerialized]
        public List<GameObject> activeInstances = new List<GameObject>();
    }

    [Header("Spawnable Prefabs")]
    public List<SpawnableObject> spawnableObjects = new List<SpawnableObject>();

    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    [Header("Timing")]
    public float minSpawnInterval = 1.5f;
    public float maxSpawnInterval = 3f;

    [Header("Multi-Spawn Settings")]
    [Range(0f, 1f)]
    public float multiSpawnChance = 0.2f;
    public int maxSimultaneousSpawns = 2;

    [Header("Object Speed Settings")]
    public float startSpeed = 3f;
    public float maxSpeed = 8f;
    [Tooltip("Wie viel die Speed pro Sekunde ansteigt, während gespielt wird")]
    public float speedIncreasePerSecond = 0.05f;

    private float currentSpeed;
    private float timer;
    private float nextSpawnTime;

    private List<GameObject> activeObjects = new List<GameObject>();

    public static float CurrentDifficultySpeed { get; private set; }
    public static bool SpawnerActive { get; private set; }

    void Start()
    {
        currentSpeed = startSpeed;
        CurrentDifficultySpeed = currentSpeed; // NEU
        SpawnerActive = true; // NEU
        SetNextSpawnTime();
    }

    void OnDestroy()
    {
        SpawnerActive = false; // NEU
    }

    void Update()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        if (currentSpeed < maxSpeed)
        {
            currentSpeed += speedIncreasePerSecond * Time.deltaTime;
            currentSpeed = Mathf.Min(currentSpeed, maxSpeed);
        }

        CurrentDifficultySpeed = currentSpeed;

        float dashMultiplier = GetDashMultiplier();

        MoveActiveObjects(dashMultiplier);

        timer += Time.deltaTime * dashMultiplier;
        if (timer >= nextSpawnTime)
        {
            timer = 0f;
            SpawnWave();
            SetNextSpawnTime();
        }
    }

    float GetDashMultiplier()
    {
        return (Glitchdash.BaseWorldSpeed > 0f)
            ? Glitchdash.WorldSpeed / Glitchdash.BaseWorldSpeed
            : 1f;
    }

    void MoveActiveObjects(float dashMultiplier)
    {
        float effectiveSpeed = currentSpeed * dashMultiplier;

        for (int i = activeObjects.Count - 1; i >= 0; i--)
        {
            GameObject obj = activeObjects[i];

            if (obj == null)
            {
                activeObjects.RemoveAt(i);
                continue;
            }

            obj.transform.Translate(Vector2.left * effectiveSpeed * Time.deltaTime);
        }
    }

    void SetNextSpawnTime()
    {
        nextSpawnTime = Random.Range(minSpawnInterval, maxSpawnInterval);
    }

    void SpawnWave()
    {
        if (spawnPoints.Length == 0 || spawnableObjects.Count == 0) return;

        // Zuerst alle "toten" (destroyed) Referenzen aus den per-Prefab-Listen aufräumen
        CleanupActiveInstances();

        bool[] occupied = new bool[spawnPoints.Length];
        int guaranteedGapIndex = Random.Range(0, spawnPoints.Length);
        occupied[guaranteedGapIndex] = true;

        int attemptsThisWave = Random.Range(1, maxSimultaneousSpawns + 1);

        for (int attempt = 0; attempt < attemptsThisWave; attempt++)
        {
            if (Random.value > multiSpawnChance && attempt > 0) break;

            SpawnableObject chosen = GetRandomWeightedObject();
            if (chosen == null) continue;

            // Limit-Check: ist von diesem Typ schon das Maximum aktiv?
            if (chosen.activeInstances.Count >= chosen.maxActiveCount) continue;

            int startIndex = FindFreeSlotRange(occupied, chosen.sizeInSlots);
            if (startIndex == -1) continue;

            for (int i = startIndex; i < startIndex + chosen.sizeInSlots; i++)
            {
                occupied[i] = true;
            }

            Vector3 spawnPos = spawnPoints[startIndex].position;
            if (chosen.sizeInSlots > 1)
            {
                Vector3 posA = spawnPoints[startIndex].position;
                Vector3 posB = spawnPoints[startIndex + chosen.sizeInSlots - 1].position;
                spawnPos = (posA + posB) / 2f;
            }

            GameObject spawned = Instantiate(chosen.prefab, spawnPos, Quaternion.identity);
            activeObjects.Add(spawned);
            chosen.activeInstances.Add(spawned);
        }
    }

    void CleanupActiveInstances()
    {
        foreach (var obj in spawnableObjects)
        {
            obj.activeInstances.RemoveAll(instance => instance == null);
        }
    }

    int FindFreeSlotRange(bool[] occupied, int slotsNeeded)
    {
        List<int> validStarts = new List<int>();

        for (int start = 0; start <= occupied.Length - slotsNeeded; start++)
        {
            bool allFree = true;
            for (int i = start; i < start + slotsNeeded; i++)
            {
                if (occupied[i]) { allFree = false; break; }
            }
            if (allFree) validStarts.Add(start);
        }

        if (validStarts.Count == 0) return -1;
        return validStarts[Random.Range(0, validStarts.Count)];
    }

    SpawnableObject GetRandomWeightedObject()
    {
        List<SpawnableObject> available = spawnableObjects.FindAll(
            obj => obj.activeInstances.Count < obj.maxActiveCount
        );

        if (available.Count == 0) return null;

        float totalWeight = 0f;
        foreach (var obj in available) totalWeight += obj.spawnChance;

        float randomValue = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var obj in available)
        {
            cumulative += obj.spawnChance;
            if (randomValue <= cumulative) return obj;
        }

        return null;
    }
}