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
    [Tooltip("Maximale Gesamtzahl an belegten Slots pro Spawn Line (z.B. max 2 von 4 Slots belegen)")]
    public int maxOccupiedSlotsPerWave = 2;

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
        Dictionary<ObjectGroup, int> spawnedGroupCounts = new Dictionary<ObjectGroup, int>();
        
        int occupiedSlotsInWave = 0;

        // 1. ZUERST MINDEST-ANFORDERUNGEN DER GRUPPEN SPANWEN (minPerWave)
        foreach (var rule in groupRules)
        {
            if (rule.minPerWave <= 0) continue;

            for (int i = 0; i < rule.minPerWave; i++)
            {
                if (occupiedSlotsInWave >= maxOccupiedSlotsPerWave) break;

                SpawnableObject chosen = GetRandomObjectFromGroup(rule.group, spawnedGroupCounts, occupied, out int startIndex);
                if (chosen != null && startIndex != -1)
                {
                    ExecuteSpawn(chosen, startIndex, occupied, spawnedGroupCounts, ref occupiedSlotsInWave);
                }
            }
        }

        // 2. OPTIONALE WEITERE OBJEKTE PER CHANCE HINZUFÜGEN
        // Entscheidet per multiSpawnChance, ob überhaupt mehr als das Minimum gespawnt werden soll
        int maxAttempts = spawnPoints.Length;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Ab dem 1. optionalen Objekt bestimmt die multiSpawnChance das Abbrechen
            if (attempt > 0 && Random.value > multiSpawnChance) break;

            if (occupiedSlotsInWave >= maxOccupiedSlotsPerWave) break;

            SpawnableObject chosen = GetRandomWeightedObject(spawnedGroupCounts, occupied, out int startIndex);
            if (chosen == null || startIndex == -1) break;

            ExecuteSpawn(chosen, startIndex, occupied, spawnedGroupCounts, ref occupiedSlotsInWave);
        }
    }

    void ExecuteSpawn(SpawnableObject chosen, int startIndex, bool[] occupied, Dictionary<ObjectGroup, int> spawnedGroupCounts, ref int occupiedSlots)
    {
        // Slots im Array als belegt markieren
        for (int i = startIndex; i < startIndex + chosen.sizeInSlots; i++)
        {
            if (i < occupied.Length) occupied[i] = true;
        }

        // Position berechnen
        Vector3 spawnPos = spawnPoints[startIndex].position;
        if (chosen.sizeInSlots > 1)
        {
            int lastSlot = Mathf.Min(startIndex + chosen.sizeInSlots - 1, spawnPoints.Length - 1);
            Vector3 posA = spawnPoints[startIndex].position;
            Vector3 posB = spawnPoints[lastSlot].position;
            spawnPos = (posA + posB) / 2f;
        }

        // Instanziieren
        GameObject spawned = Instantiate(chosen.prefab, spawnPos, Quaternion.identity);
        activeObjects.Add(spawned);
        chosen.activeInstances.Add(spawned);

        // Zähler nachführen
        if (!spawnedGroupCounts.ContainsKey(chosen.group))
            spawnedGroupCounts[chosen.group] = 0;

        spawnedGroupCounts[chosen.group]++;
        occupiedSlots += chosen.sizeInSlots;
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

    SpawnableObject GetRandomObjectFromGroup(ObjectGroup group, Dictionary<ObjectGroup, int> spawnedGroupCounts, bool[] occupied, out int startIndex)
    {
        startIndex = -1;
        var groupObjects = spawnableObjects.Where(o => o.group == group).ToList();
        return SelectObjectFromCandidateList(groupObjects, spawnedGroupCounts, occupied, out startIndex);
    }

    SpawnableObject GetRandomWeightedObject(Dictionary<ObjectGroup, int> spawnedGroupCounts, bool[] occupied, out int startIndex)
    {
        startIndex = -1;
        return SelectObjectFromCandidateList(spawnableObjects, spawnedGroupCounts, occupied, out startIndex);
    }

    SpawnableObject SelectObjectFromCandidateList(List<SpawnableObject> candidates, Dictionary<ObjectGroup, int> spawnedGroupCounts, bool[] occupied, out int startIndex)
    {
        startIndex = -1;
        List<(SpawnableObject obj, List<int> validStarts)> available = new List<(SpawnableObject, List<int>)>();

        foreach (var obj in candidates)
        {
            if (obj.spawnChance <= 0f) continue;
            if (obj.activeInstances.Count >= obj.maxActiveCount) continue;

            // Gruppen-Regel Max-Per-Wave prüfen
            GroupWaveRule rule = groupRules.Find(r => r.group == obj.group);
            if (rule != null)
            {
                int currentInWave = spawnedGroupCounts.ContainsKey(obj.group) ? spawnedGroupCounts[obj.group] : 0;
                if (currentInWave >= rule.maxPerWave) continue;
            }

            int[] allowedIndices = obj.useSpecificSpawnPoints ? GetAllowedIndices(obj) : null;
            List<int> validStarts = GetValidStartIndices(occupied, obj.sizeInSlots, allowedIndices);
            if (validStarts.Count == 0) continue;

            available.Add((obj, validStarts));
        }

        if (available.Count == 0) return null;

        float totalWeight = 0f;
        foreach (var entry in available) totalWeight += entry.obj.spawnChance;

        float randomValue = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var entry in available)
        {
            cumulative += entry.obj.spawnChance;
            if (randomValue <= cumulative)
            {
                startIndex = entry.validStarts[Random.Range(0, entry.validStarts.Count)];
                return entry.obj;
            }
        }

        return null;
    }
}