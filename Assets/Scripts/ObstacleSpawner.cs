using UnityEngine;
using System.Collections.Generic;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Engel Ayarları")]
    [SerializeField] private GameObject obstaclePrefab;   // engel prefab'ı (Tag: Obstacle)
    [SerializeField] private GameObject hoopPlusPrefab;   // toplanabilir prefab (Tag: HoopPlus)
    [SerializeField] private Transform platformCenter;    // platform merkezi
    [SerializeField] private Transform hoop;              // hoop referansı
    [SerializeField] private Transform spawnTarget;       // spawn yüksekliği referansı (hoop'un içindeki obje)
    [SerializeField] private float obstacleRadius = 2f;   // platform merkezinden uzaklık
    [SerializeField] private int obstacleCount = 3;       // kaç engel spawn olacak
    [SerializeField] private float minAngleGap = 45f;     // fallback boşluk

    [Header("HoopPlus Ayarları")]
    [SerializeField] private float hoopPlusAngleOffset = 0f;

    [Header("Spawn Timing")]
    [SerializeField] private float spawnInterval = 2f;

    [Header("Çakışma Ayarları")]
    [SerializeField] private float extraAngleMarginDeg = 5f;
    [SerializeField] private LayerMask overlapMask = ~0;

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
    }

    void Update()
    {
        if (hoop == null || platformCenter == null || spawnTarget == null) return;

        if (spawnTarget.position.y >= currentSpawnHeight && Time.time >= lastSpawnTime + spawnInterval)
        {
            SpawnWave();
            currentSpawnHeight = spawnTarget.position.y + 10f;
            lastSpawnTime = Time.time;
        }

        CleanOldObjects();
    }

    // === YENİ GEOMETRİK VE FİZİKSEL KONTROLLER ===
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

        float wObstacle = GetTangentWidth(obstaclePrefab);
        float wPlus = GetTangentWidth(hoopPlusPrefab);
        float gapObsDeg = AngleGapForWidths(wObstacle, wObstacle, obstacleRadius) + extraAngleMarginDeg;
        float gapPlusVsObsDeg = AngleGapForWidths(wPlus, wObstacle, obstacleRadius) + extraAngleMarginDeg;

        // === ENGELLER ===
        if (obstaclePrefab != null)
        {
            List<float> usedAngles = new List<float>();

            for (int i = 0; i < obstacleCount; i++)
            {
                bool placed = false;

                for (int attempt = 0; attempt < 40 && !placed; attempt++)
                {
                    float cand = Random.Range(-90f, 90f);
                    bool conflict = Mathf.Abs(Mathf.DeltaAngle(cand, hoopAngle)) < gapPlusVsObsDeg ||
                                    HasConflictWithUsedAngles(cand, usedAngles, gapObsDeg);
                    if (conflict) continue;

                    Vector3 pos = PolarOnPlatform(cand, obstacleRadius, spawnTarget.position.y);
                    Vector3 look = (platformCenter.position - pos).normalized;
                    look.y = 0f;
                    Quaternion rot = look != Vector3.zero ? Quaternion.LookRotation(look) : Quaternion.identity;

                    if (WillOverlap(obstaclePrefab, pos, rot, overlapMask)) continue;

                    GameObject obstacle = Instantiate(obstaclePrefab, pos, rot);
                    obstacle.tag = "Obstacle";
                    activeObstacles.Add(obstacle);
                    usedAngles.Add(cand);
                    placed = true;
                }
            }
        }

        // === HOOPPLUS ===
        if (hoopPlusPrefab != null)
        {
            float desired = Mathf.Clamp(hoopAngle + hoopPlusAngleOffset, -90f, 90f);
            float finalAngle = desired;

            for (int attempts = 0; attempts < 20; attempts++)
            {
                bool ok = true;
                for (int i = 0; i < activeObstacles.Count; i++)
                {
                    var obj = activeObstacles[i];
                    if (obj == null) continue;
                    float ang = WorldToAngle(obj.transform.position);
                    if (Mathf.Abs(Mathf.DeltaAngle(finalAngle, ang)) < gapPlusVsObsDeg)
                    {
                        ok = false; break;
                    }
                }
                if (ok) break;
                finalAngle = (attempts % 2 == 0)
                    ? desired + (attempts + 1) * gapPlusVsObsDeg * 0.5f
                    : desired - (attempts + 1) * gapPlusVsObsDeg * 0.5f;
                finalAngle = Mathf.Clamp(finalAngle, -90f, 90f);
            }

            Vector3 p = PolarOnPlatform(finalAngle, obstacleRadius, spawnTarget.position.y);
            Vector3 look2 = (platformCenter.position - p).normalized; look2.y = 0f;
            Quaternion r = look2 != Vector3.zero ? Quaternion.LookRotation(look2) : Quaternion.identity;

            if (!WillOverlap(hoopPlusPrefab, p, r, overlapMask))
            {
                GameObject hoopPlus = Instantiate(hoopPlusPrefab, p, r);
                hoopPlus.tag = "HoopPlus";
                activeCollectibles.Add(hoopPlus);
            }
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
                if (o != null) Destroy(o);
                activeObstacles.RemoveAt(i);
            }
        }

        for (int i = activeCollectibles.Count - 1; i >= 0; i--)
        {
            GameObject c = activeCollectibles[i];
            if (c == null || c.transform.position.y < cutoffY)
            {
                if (c != null) Destroy(c);
                activeCollectibles.RemoveAt(i);
            }
        }
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
