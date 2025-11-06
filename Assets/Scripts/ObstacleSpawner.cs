using UnityEngine;
using System.Collections.Generic;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Engel Ayarları")]
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private GameObject levelPlatformPrefab; // Eski HoopPlus yerine
    [SerializeField] private Transform platformCenter;
    [SerializeField] private Transform hoop;
    [SerializeField] private Transform spawnTarget;
    [SerializeField] private float obstacleRadius = 3f;
    [SerializeField] private int obstacleCount = 3;
    [SerializeField] private float minAngleGap = 90f;
    [SerializeField] private float obstacleInwardOffset = 0f; // Obstacle'ı merkeze doğru kaydırma miktarı

    [Header("Level Platform Ayarları")]
    [SerializeField] private float levelPlatformInterval = 10f; // Her 10 saniyede bir
    [SerializeField] private float levelPlatformRadius = 3f; // Platform'un yarıçapı

    [Header("Spawn Timing")]
    [SerializeField] private float spawnInterval = 5f;

    [Header("Çakışma Ayarları")]
    [SerializeField] private float extraAngleMarginDeg = 30f;
    [SerializeField] private LayerMask overlapMask = ~0;

    [Header("Dönme Ayarları")]
    [SerializeField] private bool enableRotation = true;
    [SerializeField] private float minRotationSpeed = 20f;
    [SerializeField] private float maxRotationSpeed = 40f;

    private readonly List<GameObject> activeObstacles = new List<GameObject>();
    private readonly List<GameObject> activePlatforms = new List<GameObject>();
    private float lastSpawnTime = 0f;
    private float currentSpawnHeight = 0f;
    private float nextLevelPlatformTime = 10f; // İlk platform 10 saniyede
    private float nextDebugTime = 5f; // Her 5 saniyede debug

    void Start()
    {
        if (platformCenter == null)
        {
            GameObject platform = GameObject.Find("Platform");
            if (platform != null) platformCenter = platform.transform;
        }
        if (hoop == null)
        {
            GameObject hoopObj = GameObject.Find("Hoop");
            if (hoopObj != null) hoop = hoopObj.transform;
        }

        if (spawnTarget != null)
            currentSpawnHeight = spawnTarget.position.y;
        else if (hoop != null)
            currentSpawnHeight = hoop.position.y + 10f;

        // Hızlı sanity check
        if (obstaclePrefab == null) Debug.LogError("[Spawner] obstaclePrefab assigned? (NULL)");
        if (levelPlatformPrefab == null) Debug.LogWarning("[Spawner] levelPlatformPrefab not assigned - level platforms won't spawn");
        if (platformCenter == null) Debug.LogWarning("[Spawner] platformCenter not set! Will try to Find('Platform') in Start.");
        if (spawnTarget == null) Debug.LogWarning("[Spawner] spawnTarget not set! This may prevent spawning at expected heights.");
        if (minAngleGap > 120f) Debug.LogWarning($"[Spawner] minAngleGap is very large ({minAngleGap}). Try smaller (30-60) for testing.");
    }

    void Update()
    {
        if (hoop == null || platformCenter == null || spawnTarget == null) return;

        // Debug timer - Her 5 saniyede bir
        if (Time.time >= nextDebugTime)
        {
            Debug.Log($"⏱️ Saniye: {Time.time:F0} - Next Platform: {nextLevelPlatformTime:F0}s - Will spawn? {Time.time >= nextLevelPlatformTime}");
            nextDebugTime += 5f;
        }

        // Normal obstacle spawn
        if (spawnTarget.position.y >= currentSpawnHeight && Time.time >= lastSpawnTime + spawnInterval)
        {
            SpawnWave();
            
            float randomHeight = Random.Range(8f, 15f);
            currentSpawnHeight = spawnTarget.position.y + randomHeight;
            lastSpawnTime = Time.time;
        }

        // Level platform spawn (her 10 saniyede bir)
        if (Time.time >= nextLevelPlatformTime)
        {
            Debug.Log($"🚨 [Spawner] LEVEL PLATFORM SPAWN TRIGGERED! Time: {Time.time:F2}, Next Platform Time: {nextLevelPlatformTime:F2}");
            SpawnLevelPlatform();
            nextLevelPlatformTime += levelPlatformInterval;
            Debug.Log($"[Spawner] Next platform scheduled at time: {nextLevelPlatformTime:F2}");
        }

        CleanOldObjects();
    }

    float GetTangentWidth(GameObject prefab)
    {
        if (prefab == null) return 0.5f;
        var r = prefab.GetComponentInChildren<Renderer>();
        var c = prefab.GetComponentInChildren<Collider>();
        Bounds b;
        if (c != null) b = c.bounds;
        else if (r != null) b = r.bounds;
        else return 0.5f;
        return b.size.x;
    }

    float AngleGapForWidths(float wA, float wB, float R)
    {
        float chord = (wA * 0.5f) + (wB * 0.5f);
        chord = Mathf.Max(0.0001f, chord);
        float angleRad = 2f * Mathf.Asin(Mathf.Min(1f, chord / R));
        return angleRad * Mathf.Rad2Deg;
    }

    bool WillOverlap(GameObject prefab, Vector3 pos, Quaternion rot, LayerMask mask)
    {
        var col = prefab.GetComponentInChildren<Collider>();
        if (col == null) return false;
        Vector3 half = col.bounds.extents;
        return Physics.CheckBox(pos, half, rot, mask, QueryTriggerInteraction.Ignore);
    }

    void SpawnWave()
    {
        float hoopAngle = 0f;
        var hoopController = hoop != null ? hoop.GetComponent<HoopController>() : null;
        if (hoopController != null) hoopAngle = hoopController.CurrentAngle;

        bool waveClockwise = Random.value < 0.5f;

        Debug.Log($"Spawn Wave: {obstacleCount} Obstacles");

        if (obstaclePrefab == null)
        {
            Debug.LogError("[Spawner] obstaclePrefab is NULL. Assign in Inspector.");
            return;
        }

        List<float> usedAngles = new List<float>();

        // ENGELLER
        if (obstaclePrefab != null && obstacleCount > 0)
        {
            for (int i = 0; i < obstacleCount; i++)
            {
                bool placed = false;

                for (int attempt = 0; attempt < 50 && !placed; attempt++)
                {
                    float cand = Random.Range(-90f, 90f);
                    Debug.Log($"[Spawner][Obstacle] Attempt {attempt} angle {cand:F2}");

                    if (HasConflictWithUsedAngles(cand, usedAngles, minAngleGap))
                    {
                        Debug.Log($"[Spawner][Obstacle] angle {cand:F2} conflicts with usedAngles (minGap {minAngleGap})");
                        continue;
                    }

                    float spawnY = spawnTarget.position.y;
                    Vector3 pos = PolarOnPlatform(cand, obstacleRadius, spawnY);
                    
                    // Pozisyonu merkeze doğru kaydır
                    Vector3 look = (platformCenter.position - pos).normalized;
                    look.y = 0f;
                    float totalOffset = obstacleInwardOffset;
                    Vector3 adjustedPos = pos + look * totalOffset;

                    // Basit rotasyon
                    Quaternion finalRotation = Quaternion.identity;

                    // Opsiyonel overlap kontrolu
                    if (WillOverlap(obstaclePrefab, adjustedPos, finalRotation, overlapMask))
                    {
                        Debug.Log($"[Spawner][Obstacle] angle {cand:F2} would overlap other colliders, skipping.");
                        continue;
                    }

                    GameObject obstacle = Instantiate(obstaclePrefab, adjustedPos, finalRotation);
                    obstacle.tag = "Obstacle";
                    activeObstacles.Add(obstacle);
                    usedAngles.Add(cand);
                    placed = true;

                    Debug.Log($"[Spawner][Obstacle] Placed at angle {cand:F2}, pos {adjustedPos}, rot {finalRotation.eulerAngles}");
                    if (enableRotation) AddRotationComponent(obstacle, waveClockwise);
                }

                if (!placed) Debug.LogWarning($"[Spawner][Obstacle] Could not place obstacle #{i} after 50 attempts.");
            }
        }
    }

    void SpawnLevelPlatform()
    {
        Debug.Log($"[Spawner] SpawnLevelPlatform called! Prefab null? {levelPlatformPrefab == null}");
        
        if (levelPlatformPrefab == null)
        {
            Debug.LogError("[Spawner] levelPlatformPrefab is NULL! Assign in Inspector!");
            return;
        }

        // Platform merkezde, mevcut yükseklikte spawn olacak
        float spawnHeight = spawnTarget != null ? spawnTarget.position.y : 0f;
        Vector3 spawnPos = new Vector3(
            platformCenter.position.x,
            spawnHeight,
            platformCenter.position.z
        );

        Debug.Log($"[Spawner] Spawning Level Platform at position: {spawnPos}");

        GameObject platform = Instantiate(levelPlatformPrefab, spawnPos, Quaternion.identity);
        // Tag yerine direkt name kullanabiliriz veya tag'i Unity'de manuel oluşturmalıyız
        activePlatforms.Add(platform);

        Debug.Log($"🎯 Level Platform spawned at height {spawnHeight:F2}, Active platforms: {activePlatforms.Count}");

        // Platform da dönsün
        bool clockwise = Random.value < 0.5f;
        if (enableRotation) AddRotationComponent(platform, clockwise);
    }

    Vector3 PolarOnPlatform(float angleDeg, float radius, float worldY)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return platformCenter.position + new Vector3(
            Mathf.Sin(rad) * radius,
            worldY - platformCenter.position.y,
            Mathf.Cos(rad) * radius
        );
    }

    bool HasConflictWithUsedAngles(float candidate, List<float> used, float gap)
    {
        for (int i = 0; i < used.Count; i++)
        {
            if (Mathf.Abs(Mathf.DeltaAngle(candidate, used[i])) < gap)
                return true;
        }
        return false;
    }

    float WorldToAngle(Vector3 worldPos)
    {
        Vector3 v = worldPos - platformCenter.position;
        v.y = 0f;
        if (v.sqrMagnitude < 0.0001f) return 0f;
        float deg = Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;
        return Mathf.Clamp(deg, -90f, 90f);
    }

    void CleanOldObjects()
    {
        if (hoop == null) return;
        float cutoffY = hoop.position.y - 20f;

        for (int i = activeObstacles.Count - 1; i >= 0; i--)
        {
            GameObject o = activeObstacles[i];
            if (o == null || o.transform.position.y < cutoffY)
            {
                if (o != null) Debug.Log($"[Spawner] Destroying obstacle at y={o.transform.position.y:F2} < cutoff {cutoffY:F2}");
                if (o != null) Destroy(o);
                activeObstacles.RemoveAt(i);
            }
        }

        for (int i = activePlatforms.Count - 1; i >= 0; i--)
        {
            GameObject p = activePlatforms[i];
            if (p == null || p.transform.position.y < cutoffY)
            {
                if (p != null) Debug.Log($"[Spawner] Destroying platform at y={p.transform.position.y:F2} < cutoff {cutoffY:F2}");
                if (p != null) Destroy(p);
                activePlatforms.RemoveAt(i);
            }
        }
    }

    void AddRotationComponent(GameObject obj, bool clockwise)
    {
        var rotator = obj.AddComponent<RotateAroundPlatform>();
        rotator.platformCenter = platformCenter;
        rotator.rotationSpeed = Random.Range(minRotationSpeed, maxRotationSpeed);
        rotator.clockwise = clockwise;
        rotator.randomizeSpeed = false;
    }

    void OnDrawGizmosSelected()
    {
        if (platformCenter != null && spawnTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(
                new Vector3(platformCenter.position.x, spawnTarget.position.y, platformCenter.position.z),
                obstacleRadius
            );
        }
    }
}
