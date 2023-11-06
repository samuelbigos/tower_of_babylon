using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : class
{
    private static Singleton<T> _instance;
    public static bool Exists => _instance != null;
    public static T Instance => _instance as T;

    protected virtual void Awake()
    {
        Debug.Assert(Instance == null, $"Attempting to create multiple {typeof(T)} instances!");
        _instance = this;
    }

    protected virtual void OnDestroy()
    {
        _instance = null;
    }
}