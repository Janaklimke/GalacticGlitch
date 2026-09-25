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

        [Header("Exklusiv-Wave (z.B. für große Specials wie Rocket)")]
        [Tooltip("Wenn aktiv: diese Gruppe nimmt NICHT an normalen Waves teil. Stattdessen wird pro Wave gewürfelt, ob diese Gruppe die komplette Wave exklusiv für sich bekommt (nur 1 Objekt aus dieser Gruppe, sonst nichts).")]
        public bool isExclusiveGroup = false;

        [Tooltip("Wahrscheinlichkeit (0-1) pro Wave, dass diese Gruppe die Wave exklusiv übernimmt. Nur relevant wenn 'Is Exclusive Group' aktiv ist.")]
        [Range(0f, 1f)]
        public float exclusiveWaveChance = 0.15f;

        [Header("Prefabs in dieser Gruppe")]
        public List<SpawnableObject> spawnableObjects = new List<SpawnableObject>();
    }

    [Header("Gruppen-Konfigurationen")]
    public List<ObjectGroupConfig> groupConfigs = new List<ObjectGroupConfig>();

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

    [Header("Difficulty Ramp (spawn rate + wave size over time)")]
    [Tooltip("Nach dieser vielen Sekunden Spielzeit ist die Schwierigkeit voll aufgedreht (schnellste Intervalle, Kurven-Ende erreicht).")]
    public float difficultyRampDuration = 120f;
    [Tooltip("Spawn-Intervalle am Ende der Ramp (schneller = härter). Muss kleiner sein als min/maxSpawnInterval oben.")]
    public float minSpawnIntervalAtMaxDifficulty = 0.6f;
    public float maxSpawnIntervalAtMaxDifficulty = 1.2f;

    [Tooltip("Min. Objekte pro Wave über die Zeit. X-Achse = 0 (Start) bis 1 (voll aufgedreht nach Difficulty Ramp Duration). Y-Achse = Objektanzahl. Frei formbar - kann auch wieder absinken, z.B. 1 -> 1 -> 2 -> 2 -> 3.")]
    public AnimationCurve minObjectsPerWaveCurve = AnimationCurve.Linear(0f, 1f, 1f, 2f);
    [Tooltip("Max. Objekte pro Wave über die Zeit. Gleiche X/Y-Logik wie oben. Z.B. 2 am Anfang -> 4 in der Mitte -> 3 danach, ganz wie du willst.")]
    public AnimationCurve maxObjectsPerWaveCurve = AnimationCurve.Linear(0f, 2f, 1f, 5f);

    [Tooltip("Zusätzliche belegbare Slots pro Wave am Ende der Ramp.")]
    public int extraOccupiedSlotsAtMaxDifficulty = 2;

    [Header("Collectable Settings")]
    [Tooltip("Wird an gespawnte Collectables (z.B. Worms) weitergegeben, da Prefabs selbst keine Szenen-Objekte referenzieren können.")]
    public GameObject collectCanvas;

    private float currentSpeed;
    private float timer;
    private float nextSpawnTime;
    private float difficultyTime; // Sekunden, die der Spieler aktiv im Playing-State verbracht hat

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

        difficultyTime += Time.deltaTime;

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

    // 0 at the start of a run, 1 once difficultyRampDuration has passed
    float DifficultyProgress()
    {
        if (difficultyRampDuration <= 0f) return 1f;
        return Mathf.Clamp01(difficultyTime / difficultyRampDuration);
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
        float t = DifficultyProgress();

        float min = Mathf.Lerp(minSpawnInterval, minSpawnIntervalAtMaxDifficulty, t);
        float max = Mathf.Lerp(maxSpawnInterval, maxSpawnIntervalAtMaxDifficulty, t);

        nextSpawnTime = Random.Range(min, max);
    }

    void SpawnWave()
    {
        if (spawnPoints.Length == 0 || groupConfigs.Count == 0) return;

        float t = DifficultyProgress();

        int currentMinObjectsPerWave = Mathf.Max(0, Mathf.RoundToInt(minObjectsPerWaveCurve.Evaluate(t)));
        int currentMaxObjectsPerWave = Mathf.Max(currentMinObjectsPerWave, Mathf.RoundToInt(maxObjectsPerWaveCurve.Evaluate(t)));
        int currentMaxOccupiedSlots = maxOccupiedSlotsPerWave + Mathf.RoundToInt(extraOccupiedSlotsAtMaxDifficulty * t);

        // Zuerst prüfen, ob eine exklusive Gruppe diese Wave für sich beansprucht.
        // Reihenfolge = Reihenfolge in der Group-Configs-Liste; die erste Gruppe,
        // deren Würfelwurf trifft, gewinnt und die Wave endet danach sofort.
        foreach (var groupConfig in groupConfigs)
        {
            if (!groupConfig.isExclusiveGroup) continue;
            if (groupConfig.spawnableObjects.Count == 0) continue;

            if (Random.value <= groupConfig.exclusiveWaveChance)
            {
                SpawnExclusiveWave(groupConfig, currentMaxOccupiedSlots);
                return;
            }
        }

        bool[] occupied = new bool[spawnPoints.Length];
        Dictionary<ObjectGroupType, int> spawnedGroupCounts = new Dictionary<ObjectGroupType, int>();

        int occupiedSlotsInWave = 0;
        int spawnedObjectsInWave = 0;

        int targetObjectCount = Random.Range(currentMinObjectsPerWave, currentMaxObjectsPerWave + 1);

        foreach (var groupConfig in groupConfigs)
        {
            if (groupConfig.isExclusiveGroup) continue; // nimmt nur exklusiv teil, siehe oben
            if (groupConfig.minPerWave <= 0) continue;

            for (int i = 0; i < groupConfig.minPerWave; i++)
            {
                if (spawnedObjectsInWave >= targetObjectCount) break;

                // WICHTIG: verbleibendes Slot-Budget berechnen und Kandidaten
                // danach filtern lassen, statt erst nach dem Spawn zu merken
                // dass das Objekt gar nicht mehr reingepasst hätte.
                int remainingSlots = currentMaxOccupiedSlots - occupiedSlotsInWave;
                if (remainingSlots <= 0) break;

                SpawnableObject chosen = GetRandomObjectFromGroup(groupConfig, spawnedGroupCounts, occupied, remainingSlots, out int startIndex);
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

            int remainingSlots = currentMaxOccupiedSlots - occupiedSlotsInWave;
            if (remainingSlots <= 0) break;

            SpawnableObject chosen = GetRandomWeightedObject(spawnedGroupCounts, occupied, remainingSlots, out int startIndex);
            if (chosen == null || startIndex == -1) break;

            ExecuteSpawn(chosen, chosen.parentGroupType, startIndex, occupied, spawnedGroupCounts, ref occupiedSlotsInWave);
            spawnedObjectsInWave++;
        }
    }

    void SpawnExclusiveWave(ObjectGroupConfig groupConfig, int currentMaxOccupiedSlots)
    {
        bool[] occupied = new bool[spawnPoints.Length];
        Dictionary<ObjectGroupType, int> dummyCounts = new Dictionary<ObjectGroupType, int>();
        int occupiedSlotsInWave = 0;

        // Volles Slot-Budget steht zur Verfügung, da noch nichts anderes gespawnt wurde.
        SpawnableObject chosen = SelectObjectFromCandidateList(groupConfig.spawnableObjects, groupConfig, dummyCounts, occupied, currentMaxOccupiedSlots, out int startIndex);
        if (chosen != null && startIndex != -1)
        {
            ExecuteSpawn(chosen, groupConfig.groupType, startIndex, occupied, dummyCounts, ref occupiedSlotsInWave);
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

        WormshootablleCollectible collectable = spawned.GetComponentInChildren<WormshootablleCollectible>();
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

    SpawnableObject GetRandomObjectFromGroup(ObjectGroupConfig groupConfig, Dictionary<ObjectGroupType, int> spawnedGroupCounts, bool[] occupied, int remainingSlots, out int startIndex)
    {
        startIndex = -1;
        return SelectObjectFromCandidateList(groupConfig.spawnableObjects, groupConfig, spawnedGroupCounts, occupied, remainingSlots, out startIndex);
    }


    SpawnableObject GetRandomWeightedObject(Dictionary<ObjectGroupType, int> spawnedGroupCounts, bool[] occupied, int remainingSlots, out int startIndex)
    {
        startIndex = -1;

        List<(ObjectGroupConfig group, List<(SpawnableObject obj, List<int> validStarts)> validObjs)> eligibleGroups
            = new List<(ObjectGroupConfig, List<(SpawnableObject, List<int>)>)>();

        foreach (var groupConfig in groupConfigs)
        {
            if (groupConfig.isExclusiveGroup) continue; // nimmt nur exklusiv teil, siehe SpawnWave
            if (groupConfig.groupWeight <= 0f) continue;

            int currentInWave = spawnedGroupCounts.ContainsKey(groupConfig.groupType) ? spawnedGroupCounts[groupConfig.groupType] : 0;
            if (currentInWave >= groupConfig.maxPerWave) continue;

            List<(SpawnableObject obj, List<int> validStarts)> validObjs = new List<(SpawnableObject, List<int>)>();

            foreach (var obj in groupConfig.spawnableObjects)
            {
                if (obj.spawnChance <= 0f) continue;

                // NEU: Objekt überspringen, wenn es nicht mehr ins verbleibende
                // Slot-Budget der Wave passt.
                if (obj.sizeInSlots > remainingSlots) continue;

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

    SpawnableObject SelectObjectFromCandidateList(List<SpawnableObject> candidates, ObjectGroupConfig singleGroupConfig, Dictionary<ObjectGroupType, int> spawnedGroupCounts, bool[] occupied, int remainingSlots, out int startIndex)
    {
        startIndex = -1;
        List<(SpawnableObject obj, List<int> validStarts)> available = new List<(SpawnableObject, List<int>)>();

        foreach (var obj in candidates)
        {
            if (obj.spawnChance <= 0f) continue;

            // NEU: Objekt überspringen, wenn es nicht mehr ins verbleibende
            // Slot-Budget der Wave passt.
            if (obj.sizeInSlots > remainingSlots) continue;

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