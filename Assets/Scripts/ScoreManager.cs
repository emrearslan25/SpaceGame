using UnityEngine;
using System;
// UI için:
using UnityEngine.UI;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Skor")]
    [SerializeField] private int score = 0; // backing field

    public int Score => score; // okunabilir property
    public event Action<int> OnScoreChanged;

    [Header("UI (istediðini baðla)")]
    [SerializeField] private string prefix = "Score: ";
    [SerializeField] private TMP_Text tmpText;     // TextMeshPro kullanýyorsan bunu baðla
    [SerializeField] private Text legacyText;      // Legacy UI Text kullanýyorsan bunu baðla

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        RefreshUI(); // baþta 0'ý yaz
    }

    public void Add(int amount = 1)
    {
        score += amount;
        OnScoreChanged?.Invoke(score);
        RefreshUI();
    }

    public void Subtract(int amount = 1)
    {
        score = Mathf.Max(0, score - amount);
        OnScoreChanged?.Invoke(score);
        RefreshUI();
    }

    public void ResetScore()
    {
        score = 0;
        OnScoreChanged?.Invoke(score);
        RefreshUI();
    }

    // ---- UI güncelleme ----
    void RefreshUI()
    {
        if (tmpText != null)
            tmpText.text = prefix + score.ToString();

        if (legacyText != null)
            legacyText.text = prefix + score.ToString();
    }
}
