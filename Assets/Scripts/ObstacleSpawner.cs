using UnityEngine;
using System.Collections.Generic;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Engel Ayarları")]
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private GameObject hoopPlusPrefab;
    [SerializeField] private Transform platformCenter;
    [SerializeField] private Transform hoop;
    [SerializeField] private Transform spawnTarget;
    [SerializeField] private float obstacleRadius = 3f;
    [SerializeField] private int obstacleCount = 3;
    [SerializeField] private float minAngleGap = 90f;

    [Header("HoopPlus Ayarları")]
    [SerializeField] private float hoopPlusAngleOffset = 0f;
    [SerializeField] private Vector2 hoopPlusYOffsetRange = new Vector2(5f, 5f); // HoopPlus her zaman 5f yukarıda

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
    private readonly List<GameObject> activeCollectibles = new List<GameObject>();
    private float lastSpawnTime = 0f;
    private float currentSpawnHeight = 0f;

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
        if (hoopPlusPrefab == null) Debug.LogError("[Spawner] hoopPlusPrefab assigned? (NULL)");
        if (platformCenter == null) Debug.LogWarning("[Spawner] platformCenter not set! Will try to Find('Platform') in Start.");
        if (spawnTarget == null) Debug.LogWarning("[Spawner] spawnTarget not set! This may prevent spawning at expected heights.");
        if (minAngleGap > 120f) Debug.LogWarning($"[Spawner] minAngleGap is very large ({minAngleGap}). Try smaller (30-60) for testing.");
    }

    void Update()
    {
        if (hoop == null || platformCenter == null || spawnTarget == null) return;

        if (spawnTarget.position.y >= currentSpawnHeight && Time.time >= lastSpawnTime + spawnInterval)
        {
            SpawnWave();
            
            float randomHeight = Random.Range(8f, 15f);
            currentSpawnHeight = spawnTarget.position.y + randomHeight;
            lastSpawnTime = Time.time;
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

        int hoopPlusCountThisWave = 1;
        int actualObstacleCount = Mathf.Max(0, obstacleCount - hoopPlusCountThisWave);

        Debug.Log($"Spawn Wave: {actualObstacleCount} Obstacle + {hoopPlusCountThisWave} HoopPlus (Total: {obstacleCount})");

        if (obstaclePrefab == null && hoopPlusPrefab == null)
        {
            Debug.LogError("[Spawner] Both obstaclePrefab and hoopPlusPrefab are NULL. Assign in Inspector.");
            return;
        }

        List<float> usedAngles = new List<float>();

        // ENGELLER
        if (obstaclePrefab != null && actualObstacleCount > 0)
        {
            for (int i = 0; i < actualObstacleCount; i++)
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
                    Vector3 look = (platformCenter.position - pos).normalized;
                    look.y = 0f;
                    Quaternion rot = look != Vector3.zero ? Quaternion.LookRotation(look) : Quaternion.identity;

                    // Opsiyonel overlap kontrolu
                    if (WillOverlap(obstaclePrefab, pos, rot, overlapMask))
                    {
                        Debug.Log($"[Spawner][Obstacle] angle {cand:F2} would overlap other colliders, skipping.");
                        continue;
                    }

                    GameObject obstacle = Instantiate(obstaclePrefab, pos, rot);
                    obstacle.tag = "Obstacle";
                    activeObstacles.Add(obstacle);
                    usedAngles.Add(cand);
                    placed = true;

                    Debug.Log($"[Spawner][Obstacle] Placed at angle {cand:F2}, pos {pos}, rot {rot.eulerAngles}");
                    if (enableRotation) AddRotationComponent(obstacle, waveClockwise);
                }

                if (!placed) Debug.LogWarning($"[Spawner][Obstacle] Could not place obstacle #{i} after 50 attempts.");
            }
        }

        // HOOPPLUS (1 tane)
        if (hoopPlusPrefab != null)
        {
            bool placed = false;

            for (int attempt = 0; attempt < 100 && !placed; attempt++)
            {
                float cand = Random.Range(-90f, 90f);

                // HoopPlus için aynı gap kullan (obstacle ile aynı kurallar)
                if (HasConflictWithUsedAngles(cand, usedAngles, minAngleGap))
                {
                    continue;
                }

                // Yükseklik aralığından rastgele al
                float randomYOffset = Random.Range(hoopPlusYOffsetRange.x, hoopPlusYOffsetRange.y);
                float spawnY = spawnTarget.position.y + randomYOffset;

                Vector3 p = PolarOnPlatform(cand, obstacleRadius, spawnY);
                Vector3 look2 = (platformCenter.position - p).normalized;
                look2.y = 0f;
                Quaternion r = look2 != Vector3.zero ? Quaternion.LookRotation(look2) : Quaternion.identity;

                GameObject hoopPlus = Instantiate(hoopPlusPrefab, p, r);
                hoopPlus.tag = "HoopPlus";
                activeCollectibles.Add(hoopPlus);
                usedAngles.Add(cand);
                placed = true;

                Debug.Log($"[Spawner][HoopPlus] Placed at angle {cand:F2}, pos {p}, heightOffset {randomYOffset:F2}");
                if (enableRotation) AddRotationComponent(hoopPlus, waveClockwise);
            }

            if (!placed)
                Debug.LogWarning("[Spawner][HoopPlus] Could not place HoopPlus after 100 attempts!");
        }
        else
        {
            Debug.LogError("[Spawner] hoopPlusPrefab is NOT assigned in Inspector!");
        }
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

        for (int i = activeCollectibles.Count - 1; i >= 0; i--)
        {
            GameObject c = activeCollectibles[i];
            if (c == null || c.transform.position.y < cutoffY)
            {
                if (c != null) Debug.Log($"[Spawner] Destroying collectible at y={c.transform.position.y:F2} < cutoff {cutoffY:F2}");
                if (c != null) Destroy(c);
                activeCollectibles.RemoveAt(i);
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
