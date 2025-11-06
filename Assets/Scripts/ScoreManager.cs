using System;
using UnityEngine;

/// <summary>
/// Basit, sahneler arası kalıcı ve olay tabanlı skor yönetimi.
/// - Otomatik bootstrap (scene load öncesi yaratılır)
/// - Her yerden erişilebilir (ScoreManager.Instance)
/// - UI'dan bağımsız; UI için ScoreUIBinder kullan
/// </summary>
public sealed class ScoreManager : MonoBehaviour
{
    private static ScoreManager _instance;

    public static ScoreManager Instance
    {
        get
        {
            if (_instance == null)
            {
                CreateSingleton();
            }
            return _instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance == null)
            CreateSingleton();
    }

    private static void CreateSingleton()
    {
        // Var olan bir ScoreManager sahnede varsa onu kullan
        var existing = GameObject.FindObjectOfType<ScoreManager>();
        if (existing != null)
        {
            _instance = existing;
            DontDestroyOnLoad(existing.gameObject);
            return;
        }

        var go = new GameObject("ScoreManager");
        _instance = go.AddComponent<ScoreManager>();
        DontDestroyOnLoad(go);
    }

    public event Action<int> OnScoreChanged;

    public int Score { get; private set; } = 0;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Set(int value)
    {
        if (value < 0) value = 0;
        if (Score == value) return;
        Score = value;
        Debug.Log($"[ScoreManager] Score set to {Score}");
        OnScoreChanged?.Invoke(Score);
    }

    public void Add(int amount = 1)
    {
        if (amount == 0) return;
        Set(Score + amount);
    }

    public void Subtract(int amount = 1)
    {
        if (amount == 0) return;
        Set(Score - amount);
    }

    public void ResetScore()
    {
        Set(0);
    }
}
