using UnityEngine;

public class RotateAroundPlatform : MonoBehaviour
{
    [Header("Dönme Ayarları")]
    public Transform platformCenter;
    public float rotationSpeed = 30f; // derece/saniye
    public bool clockwise = true;

    [Header("Rastgele Varyasyon")]
    public bool randomizeSpeed = true;
    public float speedVariation = 0.3f; // %30 varyasyon

    private float actualSpeed;

    void Start()
    {
        // Platform merkezini bul
        if (platformCenter == null)
        {
            GameObject platform = GameObject.Find("Platform");
            if (platform != null)
                platformCenter = platform.transform;
        }

        // Rastgele hız varyasyonu
        if (randomizeSpeed)
        {
            float variation = Random.Range(-speedVariation, speedVariation);
            actualSpeed = rotationSpeed * (1f + variation);
        }
        else
        {
            actualSpeed = rotationSpeed;
        }

        // Yön artık ObstacleSpawner tarafından belirleniyor (rastgele seçim kaldırıldı)
    }

    void Update()
    {
        if (platformCenter == null) return;

        // Dönme yönü
        float direction = clockwise ? -1f : 1f;
        
        // Merkez etrafında döndür
        transform.RotateAround(
            platformCenter.position,
            Vector3.up,
            actualSpeed * direction * Time.deltaTime
        );

        // Objenin kendisi merkeze baksın
        Vector3 lookDirection = (platformCenter.position - transform.position).normalized;
        lookDirection.y = 0;
        if (lookDirection != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(lookDirection);
    }
}
