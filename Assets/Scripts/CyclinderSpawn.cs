using UnityEngine;

public class CylinderSpawn : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private Transform hoop; // kýrmýzý halka
    [SerializeField] private Transform cylinder; // silindir mesh (child objen)

    [Header("Ayarlar")]
    [SerializeField] private float heightFactor = 1f; // hoop ne kadar yükselirse o kadar uzasýn

    private Vector3 startScale;
    private float startHoopY;

    void Start()
    {
        if (cylinder == null)
        {
            cylinder = transform.GetChild(0); // otomatik bulur (ilk child silindir)
        }

        startScale = cylinder.localScale;
        startHoopY = hoop.position.y;
    }

    void Update()
    {
        float deltaY = hoop.position.y - startHoopY;
        if (deltaY < 0f) deltaY = 0f;

        // Yeni yükseklik (pivot yukarýda olduðundan sadece Y pozitif yönde uzayacak)
        float newHeightY = startScale.y + deltaY * heightFactor;

        cylinder.localScale = new Vector3(startScale.x, newHeightY, startScale.z);
    }
}
