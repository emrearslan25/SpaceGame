using UnityEngine;

/// <summary>
/// Basit ateş mekaniği - Hoop'a eklenecek
/// </summary>
public class WeaponController : MonoBehaviour
{
    [Header("Mermi Ayarları")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 0.5f; // ateş aralığı (saniye)
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false; // log'ları kapattım

    private float nextFireTime = 0f;

    void Update()
    {
        // Sürekli ateş et
        if (Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    void Shoot()
    {
        // Prefab kontrolü
        if (bulletPrefab == null)
        {
            if (showDebugLogs)
                Debug.LogError("[WeaponController] Bullet Prefab atanmamış! Inspector'dan ata.");
            return;
        }

        // Fire point kontrolü
        if (firePoint == null)
        {
            if (showDebugLogs)
                Debug.LogError("[WeaponController] Fire Point atanmamış! Inspector'dan ata.");
            return;
        }

        // Mermiyi oluştur
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        
        if (showDebugLogs)
            Debug.Log($"🔫 Mermi ateşlendi! Pos: {firePoint.position}");
    }
}
