using UnityEngine;

/// <summary>
/// Obstacle için can sistemi
/// </summary>
public class ObstacleHealth : MonoBehaviour
{
    [Header("Can Ayarları")]
    [SerializeField] private int maxHealth = 5;
    private int currentHealth;

    [Header("Görsel Feedback")]
    [SerializeField] private bool changeColorOnDamage = true;
    [SerializeField] private Color damageColor = Color.yellow;
    [SerializeField] private float colorChangeDuration = 0.1f;

    private Renderer objectRenderer;
    private Color originalColor;

    void Start()
    {
        currentHealth = maxHealth;
        objectRenderer = GetComponent<Renderer>();
        
        if (objectRenderer != null && objectRenderer.material != null)
        {
            originalColor = objectRenderer.material.color;
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"[{gameObject.name}] Hasar aldı! Kalan can: {currentHealth}/{maxHealth}");

        // Görsel feedback
        if (changeColorOnDamage && objectRenderer != null)
        {
            StartCoroutine(FlashColor());
        }

        // Can bittiyse yok et
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log($"[{gameObject.name}] Yok oldu!");
        
        // Dağılma efekti varsa tetikle
        var breakable = GetComponent<BreakableTweenScatter>();
        if (breakable != null)
        {
            // BreakableTweenScatter'ın ExplodeInternal metodunu tetikle
            // Reflection kullanarak private metodu çağırıyoruz
            var method = breakable.GetType().GetMethod("ExplodeInternal", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(breakable, null);
                Debug.Log("Dağılma efekti tetiklendi!");
                return; // ExplodeInternal zaten objeyi yok eder
            }
        }
        
        // Breakable yoksa normal yok et
        Destroy(gameObject);
    }

    System.Collections.IEnumerator FlashColor()
    {
        if (objectRenderer != null && objectRenderer.material != null)
        {
            objectRenderer.material.color = damageColor;
            yield return new WaitForSeconds(colorChangeDuration);
            objectRenderer.material.color = originalColor;
        }
    }

    // Dışarıdan can kontrolü için
    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public bool IsAlive() => currentHealth > 0;
}
