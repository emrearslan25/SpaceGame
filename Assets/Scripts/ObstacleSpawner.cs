using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Engel Ayarları")]
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private GameObject levelPlatformPrefab;
    [SerializeField] private Transform platformCenter;
    [SerializeField] private Transform hoop;
    [SerializeField] private Transform spawnTarget;
    [SerializeField] private float obstacleRadius = 3f;
    [SerializeField] private int obstacleCount = 3;
    [SerializeField] private float minAngleGap = 90f;
    [SerializeField] private float obstacleInwardOffset = 0f;

    [Header("Level Platform Ayarları")]
    [SerializeField] private float levelPlatformInterval = 10f;
    [SerializeField] private float levelPlatformRadius = 3f;

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
    private float nextLevelPlatformTime = 10f;
    private float nextDebugTime = 5f;

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
    }

    void Update()
    {
        if (hoop == null || platformCenter == null || spawnTarget == null) return;

        if (Time.time >= nextDebugTime)
        {
            Debug.Log($"⏱️ Time: {Time.time:F0}");
            nextDebugTime += 5f;
        }

        if (spawnTarget.position.y >= currentSpawnHeight && Time.time >= lastSpawnTime + spawnInterval)
        {
            SpawnWave();

            float randomHeight = Random.Range(8f, 15f);
            currentSpawnHeight = spawnTarget.position.y + randomHeight;
            lastSpawnTime = Time.time;
        }

        if (Time.time >= nextLevelPlatformTime)
        {
            SpawnLevelPlatform();
            nextLevelPlatformTime += levelPlatformInterval;
        }

        CleanOldObjects();
    }

    void SpawnWave()
    {
        bool waveClockwise = Random.value < 0.5f;
        List<float> usedAngles = new List<float>();

        for (int i = 0; i < obstacleCount; i++)
        {
            bool placed = false;

            for (int attempt = 0; attempt < 50 && !placed; attempt++)
            {
                float cand = Random.Range(-90f, 90f);
                if (HasConflictWithUsedAngles(cand, usedAngles, minAngleGap))
                    continue;

                float spawnY = spawnTarget.position.y;
                Vector3 pos = PolarOnPlatform(cand, obstacleRadius, spawnY);

                // Merkeze doğru kaydır
                Vector3 look = (platformCenter.position - pos).normalized;
                look.y = 0f;
                Vector3 adjustedPos = pos + look * obstacleInwardOffset;

                // 🔹 Engel hem merkeze dönük hem de yan yatık (x ekseninde -90°)
                Quaternion lookRot = Quaternion.LookRotation(look);
                Quaternion finalRotation = lookRot * Quaternion.Euler(-90f, 0f, 0f);

                if (WillOverlap(obstaclePrefab, adjustedPos, finalRotation, overlapMask))
                    continue;

                GameObject obstacle = Instantiate(obstaclePrefab, adjustedPos, finalRotation);
                obstacle.tag = "Obstacle";
                activeObstacles.Add(obstacle);
                usedAngles.Add(cand);
                placed = true;

                if (enableRotation) AddRotationComponent(obstacle, waveClockwise);
            }

            if (!placed)
                Debug.LogWarning($"[Spawner] Engel #{i} yerleştirilemedi.");
        }
    }

    void SpawnLevelPlatform()
    {
        if (levelPlatformPrefab == null) return;

        float spawnHeight = spawnTarget != null ? spawnTarget.position.y : 0f;
        Vector3 spawnPos = new Vector3(platformCenter.position.x, spawnHeight, platformCenter.position.z);

        GameObject platform = Instantiate(levelPlatformPrefab, spawnPos, Quaternion.identity);
        activePlatforms.Add(platform);

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

    bool WillOverlap(GameObject prefab, Vector3 pos, Quaternion rot, LayerMask mask)
    {
        var col = prefab.GetComponentInChildren<Collider>();
        if (col == null) return false;
        Vector3 half = col.bounds.extents;
        return Physics.CheckBox(pos, half, rot, mask, QueryTriggerInteraction.Ignore);
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
                if (o != null) Destroy(o);
                activeObstacles.RemoveAt(i);
            }
        }

        for (int i = activePlatforms.Count - 1; i >= 0; i--)
        {
            GameObject p = activePlatforms[i];
            if (p == null || p.transform.position.y < cutoffY)
            {
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
