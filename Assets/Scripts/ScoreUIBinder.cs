using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Basit UI baglayici: ScoreManager skorunu otomatik UI'ya yazar.
/// Bir GameObject'e ekleyin ve ya TMP_Text ya da Text atayin.
/// </summary>
public class ScoreUIBinder : MonoBehaviour
{
    [SerializeField] private string prefix = ""; // ornegin "Score: "
    [SerializeField] private TMP_Text tmpText;
    [SerializeField] private Text legacyText;

    void OnEnable()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged += HandleScoreChanged;
            // ilk deger
            HandleScoreChanged(ScoreManager.Instance.Score);
        }
    }

    void OnDisable()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged -= HandleScoreChanged;
        }
    }

    void HandleScoreChanged(int value)
    {
        string s = prefix + value.ToString();
        if (tmpText != null) tmpText.text = s;
        if (legacyText != null) legacyText.text = s;
    }
}
