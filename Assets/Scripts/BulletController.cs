using UnityEngine;

/// <summary>
/// Basit mermi hareketi - Mermi prefab'ına eklenecek
/// </summary>
public class BulletController : MonoBehaviour
{
    [Header("Hareket")]
    [SerializeField] private float speed = 20f;
    [SerializeField] private Vector3 direction = Vector3.up; // Hangi yönde gidecek
    
    [Header("Hasar")]
    [SerializeField] private int damage = 1; // Mermi hasarı
    
    [Header("Ömür")]
    [SerializeField] private float lifetime = 3f;

    void Start()
    {
        // Belirli süre sonra kendini yok et
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // Yukarı doğru hareket et (dünya koordinatlarında)
        transform.position += direction.normalized * speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        // Obstacle'a çarptığında
        if (other.CompareTag("Obstacle"))
        {
            // Can sistemi var mı kontrol et
            ObstacleHealth health = other.GetComponent<ObstacleHealth>();
            
            if (health != null)
            {
                // Can sistemi varsa hasar ver
                health.TakeDamage(damage);
                Debug.Log($"💥 Mermi Obstacle'a {damage} hasar verdi!");
            }
            else
            {
                // Can sistemi yoksa direkt yok et (eski sistem)
                Debug.Log($"💥 Mermi Obstacle'ı yok etti: {other.name}");
                Destroy(other.gameObject);
            }
            
            // Mermiyi yok et
            Destroy(gameObject);
        }
        // LevelPlatform'a çarptığında (isteğe bağlı)
        else if (other.CompareTag("LevelPlatform"))
        {
            Debug.Log("Mermi LevelPlatform'a çarptı (görmezden gelindi)");
            // LevelPlatform'a zarar verme, sadece mermiyi yok et
            Destroy(gameObject);
        }
    }
}
