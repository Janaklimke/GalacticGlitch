using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class spawner : MonoBehaviour
{
    public enum ObjectGroup
    {
        Default,
        BigAsteroid,
        SmallAsteroid,
        Special,
        Collectable
    }

    [System.Serializable]
    public class SpawnableObject
    {
        public string name = "Spawn Object";
        public GameObject prefab;

        [Header("Gruppe & Spawn-Chance")]
        public ObjectGroup group = ObjectGroup.Default;
        
        [Tooltip("Höher = wahrscheinlicher relativ zu anderen Objekten.")]
        public float spawnChance = 1f;

        [Tooltip("Wie viele Spawnpunkte dieses Objekt blockiert (1 = normal, 2 = groß, etc.)")]
        public int sizeInSlots = 1;

        [Tooltip("Maximal gleichzeitig aktive Instanzen dieses Prefabs in der Szene insgesamt")]
        public int maxActiveCount = 4;

        [Header("Spezifische Spawnpunkte")]
        public bool useSpecificSpawnPoints = false;
        [Tooltip("Erlaubte Start-Spawnpunkte für dieses Objekt.")]
        public Transform[] allowedSpawnPoints;

        [System.NonSerialized]
        public List<GameObject> activeInstances = new List<GameObject>();
    }

    [System.Serializable]
    public class GroupWaveRule
    {
        public ObjectGroup group;
        [Tooltip("Mindestanzahl dieser Gruppe pro Spawn-Line/Wave")]
        public int minPerWave = 0;
        [Tooltip("Maximalanzahl dieser Gruppe pro Spawn-Line/Wave")]
        public int maxPerWave = 1;
    }

    [Header("Spawnable Prefabs")]
    public List<SpawnableObject> spawnableObjects = new List<SpawnableObject>();

    [Header("Gruppen-Regeln pro Spawn-Line")]
    public List<GroupWaveRule> groupRules = new List<GroupWaveRule>();

    [Header("Spawn Line Limits")]
    [Tooltip("Maximale Gesamtzahl an Objekten (egal welcher Typ) pro Spawn Line")]
    public int maxTotalPerWave = 4;

    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    [Header("Timing")]
    public float minSpawnInterval = 1.5f;
    public float maxSpawnInterval = 3f;

    [Header("Multi-Spawn Settings")]
    [Range(0f, 1f)]
    public float multiSpawnChance = 0.2f;

    [Header("Object Speed Settings")]
    public float startSpeed = 3f;
    public float maxSpeed = 8f;
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
        CurrentDifficultySpeed = currentSpeed;
        SpawnerActive = true;
        SetNextSpawnTime();
    }

    void OnDestroy()
    {
        SpawnerActive = false;
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

        CleanupActiveInstances();

        bool[] occupied = new bool[spawnPoints.Length];
        
        // Zufällige Anzahl an Versuchen für diese Line (maximal begrenzt durch maxTotalPerWave)
        int attemptsThisWave = Random.Range(1, maxTotalPerWave + 1);

        Dictionary<ObjectGroup, int> spawnedGroupCounts = new Dictionary<ObjectGroup, int>();
        int totalSpawnedInWave = 0;

        for (int attempt = 0; attempt < attemptsThisWave; attempt++)
        {
            // Ab dem 2. Objekt entscheidet die Multi-Spawn-Chance, ob noch eins hinzugefügt wird
            if (Random.value > multiSpawnChance && attempt > 0) break;

            // Gesamtes Limit pro Line erreicht?
            if (totalSpawnedInWave >= maxTotalPerWave) break;

            // 1. Objekt basierend auf Chancen und Gruppen-Limits wählen
            SpawnableObject chosen = GetRandomWeightedObject(spawnedGroupCounts);
            if (chosen == null) continue;

            // 2. Erlaubte Slots ermitteln
            int[] allowedIndices = chosen.useSpecificSpawnPoints
                ? GetAllowedIndices(chosen)
                : null;

            int startIndex = FindFreeSlotRange(occupied, chosen.sizeInSlots, allowedIndices);
            if (startIndex == -1) continue; // Kein freier Platz frei

            // Slots testen
            for (int i = startIndex; i < startIndex + chosen.sizeInSlots; i++)
            {
                if (i < occupied.Length) occupied[i] = true;
            }

            // Verhindern, dass die gesamte Line dicht gemacht wird (mind. 1 Ausweichplatz)
            if (IsEntireLineBlocked(occupied))
            {
                for (int i = startIndex; i < startIndex + chosen.sizeInSlots; i++)
                {
                    if (i < occupied.Length) occupied[i] = false;
                }
                continue;
            }

            // Objekt spawnen
            Vector3 spawnPos = spawnPoints[startIndex].position;
            if (chosen.sizeInSlots > 1)
            {
                int lastSlot = Mathf.Min(startIndex + chosen.sizeInSlots - 1, spawnPoints.Length - 1);
                Vector3 posA = spawnPoints[startIndex].position;
                Vector3 posB = spawnPoints[lastSlot].position;
                spawnPos = (posA + posB) / 2f;
            }

            GameObject spawned = Instantiate(chosen.prefab, spawnPos, Quaternion.identity);
            activeObjects.Add(spawned);
            chosen.activeInstances.Add(spawned);

            // Zähler aktualisieren
            if (!spawnedGroupCounts.ContainsKey(chosen.group))
                spawnedGroupCounts[chosen.group] = 0;

            spawnedGroupCounts[chosen.group]++;
            totalSpawnedInWave++;
        }
    }

    bool IsEntireLineBlocked(bool[] occupied)
    {
        for (int i = 0; i < occupied.Length; i++)
        {
            if (!occupied[i]) return false;
        }
        return true;
    }

    int[] GetAllowedIndices(SpawnableObject obj)
    {
        if (obj.allowedSpawnPoints == null || obj.allowedSpawnPoints.Length == 0)
            return new int[0];

        List<int> indices = new List<int>();
        foreach (var t in obj.allowedSpawnPoints)
        {
            if (t == null) continue;
            int idx = System.Array.IndexOf(spawnPoints, t);
            if (idx != -1) indices.Add(idx);
        }
        return indices.ToArray();
    }

    void CleanupActiveInstances()
    {
        foreach (var obj in spawnableObjects)
        {
            obj.activeInstances.RemoveAll(instance => instance == null);
        }
    }

    int FindFreeSlotRange(bool[] occupied, int slotsNeeded, int[] allowedIndices = null)
    {
        List<int> validStarts = GetValidStartIndices(occupied, slotsNeeded, allowedIndices);
        if (validStarts.Count == 0) return -1;

        return validStarts[Random.Range(0, validStarts.Count)];
    }

    List<int> GetValidStartIndices(bool[] occupied, int slotsNeeded, int[] allowedIndices = null)
    {
        List<int> validStarts = new List<int>();

        for (int start = 0; start <= occupied.Length - slotsNeeded; start++)
        {
            if (allowedIndices != null && !allowedIndices.Contains(start))
                continue;

            bool allFree = true;
            for (int i = start; i < start + slotsNeeded; i++)
            {
                if (occupied[i]) { allFree = false; break; }
            }
            if (allFree) validStarts.Add(start);
        }

        return validStarts;
    }

    SpawnableObject GetRandomWeightedObject(Dictionary<ObjectGroup, int> spawnedGroupCounts)
    {
        List<SpawnableObject> available = spawnableObjects.FindAll(obj =>
        {
            // Max Active Check in der Szene
            if (obj.activeInstances.Count >= obj.maxActiveCount) return false;

            // Gruppen-Regel Check pro Wave
            GroupWaveRule rule = groupRules.Find(r => r.group == obj.group);
            if (rule != null)
            {
                int currentInWave = spawnedGroupCounts.ContainsKey(obj.group) ? spawnedGroupCounts[obj.group] : 0;
                if (currentInWave >= rule.maxPerWave) return false;
            }

            return true;
        });

        if (available.Count == 0) return null;

        // Priorisierung bei minPerWave
        List<SpawnableObject> priorityList = available.FindAll(obj =>
        {
            GroupWaveRule rule = groupRules.Find(r => r.group == obj.group);
            if (rule != null && rule.minPerWave > 0)
            {
                int currentInWave = spawnedGroupCounts.ContainsKey(obj.group) ? spawnedGroupCounts[obj.group] : 0;
                return currentInWave < rule.minPerWave;
            }
            return false;
        });

        List<SpawnableObject> selectionPool = priorityList.Count > 0 ? priorityList : available;

        float totalWeight = 0f;
        foreach (var obj in selectionPool) totalWeight += obj.spawnChance;

        float randomValue = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var obj in selectionPool)
        {
            cumulative += obj.spawnChance;
            if (randomValue <= cumulative) return obj;
        }

        return null;
    }
}