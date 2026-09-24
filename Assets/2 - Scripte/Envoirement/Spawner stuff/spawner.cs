using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class spawner : MonoBehaviour
{
    public enum ObjectGroupType
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

        [Tooltip("Höher = wahrscheinlichere Auswahl innerhalb dieser Gruppe.")]
        public float spawnChance = 1f;

        [Tooltip("Wie viele Spawnpunkte dieses Objekt blockiert (1 = normal, 2 = groß, etc.)")]
        public int sizeInSlots = 1;

        [Header("Spezifische Spawnpunkte")]
        public bool useSpecificSpawnPoints = false;
        [Tooltip("Erlaubte Start-Spawnpunkte für dieses Objekt.")]
        public Transform[] allowedSpawnPoints;

        [System.NonSerialized]
        public ObjectGroupType parentGroupType;
    }

    [System.Serializable]
    public class ObjectGroupConfig
    {
        public string groupName = "New Group";
        public ObjectGroupType groupType = ObjectGroupType.Default;

        [Header("Gruppen-Regeln pro Wave")]
        [Tooltip("Mindestanzahl dieser Gruppe pro Spawn-Line/Wave")]
        public int minPerWave = 0;
        [Tooltip("Maximalanzahl dieser Gruppe pro Spawn-Line/Wave")]
        public int maxPerWave = 1;

        [Tooltip("Gewicht dieser GRUPPE beim Auffüllen (Schritt 2). Höher = die Gruppe wird insgesamt öfter gewählt, unabhängig davon wie viele Prefabs sie enthält.")]
        public float groupWeight = 1f;

        [Header("Prefabs in dieser Gruppe")]
        public List<SpawnableObject> spawnableObjects = new List<SpawnableObject>();
    }

    [Header("Gruppen-Konfigurationen")]
    public List<ObjectGroupConfig> groupConfigs = new List<ObjectGroupConfig>();

    [Header("Objekt-Anzahl pro Wave")]
    [Tooltip("Minimale Anzahl an Objekten (NICHT Slots!) pro Wave")]
    public int minObjectsPerWave = 1;
    [Tooltip("Maximale Anzahl an Objekten (NICHT Slots!) pro Wave")]
    public int maxObjectsPerWave = 3;

    [Header("Spawn Line Limits")]
    [Tooltip("Maximale Gesamtzahl an belegten Slots pro Spawn Line (z.B. max 2 von 4 Slots belegen)")]
    public int maxOccupiedSlotsPerWave = 2;

    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    [Header("Timing")]
    public float minSpawnInterval = 1.5f;
    public float maxSpawnInterval = 3f;

    [Header("Object Speed Settings")]
    public float startSpeed = 3f;
    public float maxSpeed = 8f;
    public float speedIncreasePerSecond = 0.05f;

    [Header("Collectable Settings")]
    [Tooltip("Wird an gespawnte Collectables (z.B. Worms) weitergegeben, da Prefabs selbst keine Szenen-Objekte referenzieren können.")]
    public GameObject collectCanvas;

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

        if (collectCanvas != null)
        {
            collectCanvas.SetActive(false);
        }

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
        if (spawnPoints.Length == 0 || groupConfigs.Count == 0) return;

        bool[] occupied = new bool[spawnPoints.Length];
        Dictionary<ObjectGroupType, int> spawnedGroupCounts = new Dictionary<ObjectGroupType, int>();

        int occupiedSlotsInWave = 0;
        int spawnedObjectsInWave = 0;

        int targetObjectCount = Random.Range(minObjectsPerWave, maxObjectsPerWave + 1);

        foreach (var groupConfig in groupConfigs)
        {
            if (groupConfig.minPerWave <= 0) continue;

            for (int i = 0; i < groupConfig.minPerWave; i++)
            {
                if (spawnedObjectsInWave >= targetObjectCount) break;
                if (occupiedSlotsInWave >= maxOccupiedSlotsPerWave) break;

                SpawnableObject chosen = GetRandomObjectFromGroup(groupConfig, spawnedGroupCounts, occupied, out int startIndex);
                if (chosen != null && startIndex != -1)
                {
                    ExecuteSpawn(chosen, groupConfig.groupType, startIndex, occupied, spawnedGroupCounts, ref occupiedSlotsInWave);
                    spawnedObjectsInWave++;
                }
            }
        }

        int maxAttempts = spawnPoints.Length;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            if (spawnedObjectsInWave >= targetObjectCount) break;
            if (occupiedSlotsInWave >= maxOccupiedSlotsPerWave) break;

            SpawnableObject chosen = GetRandomWeightedObject(spawnedGroupCounts, occupied, out int startIndex);
            if (chosen == null || startIndex == -1) break;

            ExecuteSpawn(chosen, chosen.parentGroupType, startIndex, occupied, spawnedGroupCounts, ref occupiedSlotsInWave);
            spawnedObjectsInWave++;
        }
    }

    void ExecuteSpawn(SpawnableObject chosen, ObjectGroupType groupType, int startIndex, bool[] occupied, Dictionary<ObjectGroupType, int> spawnedGroupCounts, ref int occupiedSlots)
    {
        for (int i = startIndex; i < startIndex + chosen.sizeInSlots; i++)
        {
            if (i < occupied.Length) occupied[i] = true;
        }

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

        WormshootablleCollectible collectable = spawned.GetComponent<WormshootablleCollectible>();
        if (collectable != null)
        {
            collectable.Init(collectCanvas);
        }

        // Zähler nachführen
        if (!spawnedGroupCounts.ContainsKey(groupType))
            spawnedGroupCounts[groupType] = 0;

        spawnedGroupCounts[groupType]++;
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

    SpawnableObject GetRandomObjectFromGroup(ObjectGroupConfig groupConfig, Dictionary<ObjectGroupType, int> spawnedGroupCounts, bool[] occupied, out int startIndex)
    {
        startIndex = -1;
        return SelectObjectFromCandidateList(groupConfig.spawnableObjects, groupConfig, spawnedGroupCounts, occupied, out startIndex);
    }


    SpawnableObject GetRandomWeightedObject(Dictionary<ObjectGroupType, int> spawnedGroupCounts, bool[] occupied, out int startIndex)
    {
        startIndex = -1;

        List<(ObjectGroupConfig group, List<(SpawnableObject obj, List<int> validStarts)> validObjs)> eligibleGroups
            = new List<(ObjectGroupConfig, List<(SpawnableObject, List<int>)>)>();

        foreach (var groupConfig in groupConfigs)
        {
            if (groupConfig.groupWeight <= 0f) continue;

            int currentInWave = spawnedGroupCounts.ContainsKey(groupConfig.groupType) ? spawnedGroupCounts[groupConfig.groupType] : 0;
            if (currentInWave >= groupConfig.maxPerWave) continue;

            List<(SpawnableObject obj, List<int> validStarts)> validObjs = new List<(SpawnableObject, List<int>)>();

            foreach (var obj in groupConfig.spawnableObjects)
            {
                if (obj.spawnChance <= 0f) continue;
                obj.parentGroupType = groupConfig.groupType;

                int[] allowedIndices = obj.useSpecificSpawnPoints ? GetAllowedIndices(obj) : null;
                List<int> validStarts = GetValidStartIndices(occupied, obj.sizeInSlots, allowedIndices);
                if (validStarts.Count == 0) continue;

                validObjs.Add((obj, validStarts));
            }

            if (validObjs.Count > 0)
                eligibleGroups.Add((groupConfig, validObjs));
        }

        if (eligibleGroups.Count == 0) return null;

        float totalGroupWeight = 0f;
        foreach (var entry in eligibleGroups) totalGroupWeight += entry.group.groupWeight;

        float groupRandom = Random.Range(0f, totalGroupWeight);
        float groupCumulative = 0f;

        List<(SpawnableObject obj, List<int> validStarts)> chosenGroupObjects = null;

        foreach (var entry in eligibleGroups)
        {
            groupCumulative += entry.group.groupWeight;
            if (groupRandom <= groupCumulative)
            {
                chosenGroupObjects = entry.validObjs;
                break;
            }
        }

        if (chosenGroupObjects == null) return null;

        float totalObjWeight = 0f;
        foreach (var c in chosenGroupObjects) totalObjWeight += c.obj.spawnChance;

        float objRandom = Random.Range(0f, totalObjWeight);
        float objCumulative = 0f;

        foreach (var c in chosenGroupObjects)
        {
            objCumulative += c.obj.spawnChance;
            if (objRandom <= objCumulative)
            {
                startIndex = c.validStarts[Random.Range(0, c.validStarts.Count)];
                return c.obj;
            }
        }

        return null;
    }

    SpawnableObject SelectObjectFromCandidateList(List<SpawnableObject> candidates, ObjectGroupConfig singleGroupConfig, Dictionary<ObjectGroupType, int> spawnedGroupCounts, bool[] occupied, out int startIndex)
    {
        startIndex = -1;
        List<(SpawnableObject obj, List<int> validStarts)> available = new List<(SpawnableObject, List<int>)>();

        foreach (var obj in candidates)
        {
            if (obj.spawnChance <= 0f) continue;

            // Gruppen-Regel Max-Per-Wave prüfen
            ObjectGroupConfig groupConfig = singleGroupConfig ?? groupConfigs.Find(g => g.spawnableObjects.Contains(obj));
            if (groupConfig != null)
            {
                obj.parentGroupType = groupConfig.groupType;
                int currentInWave = spawnedGroupCounts.ContainsKey(groupConfig.groupType) ? spawnedGroupCounts[groupConfig.groupType] : 0;
                if (currentInWave >= groupConfig.maxPerWave) continue;
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