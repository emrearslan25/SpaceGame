using UnityEngine;

public class LevelPlatformScoreTrigger : MonoBehaviour
{
    [SerializeField] private int scoreToAdd = 10; // Temas başına eklenecek puan
    private bool hasTriggered = false; // Aynı platformdan birden fazla kez puan alınmasın

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        // Oyuncu veya top tag'ine sahip nesne temas ettiğinde
        if (other.CompareTag("Player") || other.CompareTag("Ball") || other.CompareTag("Hoop"))
        {
            hasTriggered = true;

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.Add(scoreToAdd);
                Debug.Log($"🏆 Level platform temas: +{scoreToAdd} puan eklendi! Toplam: {ScoreManager.Instance.Score}");
            }
            else
            {
                Debug.LogWarning("[LevelPlatformScoreTrigger] ScoreManager bulunamadı!");
            }
        }
    }
}
