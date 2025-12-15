using System.Collections.Generic;
// Unity
using UnityEngine;

public class FloatingTextPool : MonoBehaviour
{
    public static FloatingTextPool Instance { get; private set; }

    [Header("Pool Settings")]
    [SerializeField] private FloatingText floatingTextPrefab;
    [SerializeField] private int initialPoolSize = 20;

    private Queue<FloatingText> _pool = new Queue<FloatingText>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePool();
        }
    }

    private void InitializePool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateAndAddToPool();
        }
    }

    private FloatingText CreateAndAddToPool()
    {
        FloatingText newText = Instantiate(floatingTextPrefab, transform);
        newText.gameObject.SetActive(false);
        _pool.Enqueue(newText);
        return newText;
    }

    public FloatingText Get()
    {
        if (_pool.Count == 0)
        {
            Debug.LogWarning("[FloatingTextPool] Pool empty, creating new instance on the fly.");
            CreateAndAddToPool();
        }

        FloatingText text = _pool.Dequeue();
        text.gameObject.SetActive(true);
        return text;
    }

    public void Return(FloatingText text)
    {
        text.gameObject.SetActive(false);
        _pool.Enqueue(text);
    }
}

