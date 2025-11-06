using UnityEngine;

/// <summary>
/// Level platformu - Değdiğinde puan ve hız artışı verir
/// </summary>
public class LevelPlatform : MonoBehaviour
{
    [Header("Ödüller")]
    [SerializeField] private int scoreReward = 10; // Hoop değince +10 puan
    [SerializeField] private float speedBoost = 0.5f; // Hıza eklenecek değer
    
    [Header("Görsel Efekt")]
    [SerializeField] private bool destroyOnCollect = true;
    [SerializeField] private GameObject collectEffect; // Opsiyonel parçalanma efekti
    
    private bool collected = false;

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"🔍 [LevelPlatform] OnTriggerEnter called! Other tag: {other.tag}, Name: {other.name}");

        if (!other.CompareTag("Hoop"))
        {
            // Bullet veya başka bir şeyse görmezden gel
            return;
        }

        var hoopCtrl = other.GetComponent<HoopController>();
        DoCollect(hoopCtrl);
    }

    // Hoop yakından tespit edildiğinde dışarıdan çağrılabilir
    public void Collect(Transform collector)
    {
        var hoopCtrl = collector != null ? collector.GetComponent<HoopController>() : null;
        DoCollect(hoopCtrl);
    }

    void DoCollect(HoopController hoop)
    {
        if (collected) return;
        collected = true;

        // Puan ekle
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.Add(scoreReward);
            Debug.Log($"🎯 Level Platform! +{scoreReward} puan! New score: {ScoreManager.Instance.Score}");
        }
        else
        {
            Debug.LogError("[LevelPlatform] ScoreManager.Instance is NULL!");
        }

        // Hız artışı
        if (hoop != null)
        {
            hoop.BoostSpeed(speedBoost);
            Debug.Log($"⚡ Hız artışı! +{speedBoost}");
        }

        // Efekt varsa göster
        if (collectEffect != null)
        {
            Instantiate(collectEffect, transform.position, Quaternion.identity);
        }

        // Platformu yok et veya gizle
        if (destroyOnCollect)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
