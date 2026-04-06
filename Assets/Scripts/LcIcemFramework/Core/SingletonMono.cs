using UnityEngine;

namespace LcIcemFramework.Core
{
/// <summary>
/// Mono单例泛型基类
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class SingletonMono<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static readonly object _lockObj = new object();
    private static bool _applicationIsQuitting = false;

    public static T Instance
    {
        get
        {
            if (_applicationIsQuitting)
            {
                Debug.LogWarning($"[{typeof(T).Name}] Instance already destroyed on application quit. Returning null.");
                return null;
            }

            if (_instance != null)
                return _instance;

            lock (_lockObj)
            {
                if (_instance == null)
                {
                    T[] existing = FindObjectsOfType<T>();
                    if (existing.Length > 0)
                    {
                        _instance = existing[0];
                        return _instance;
                    }

                    GameObject go = new GameObject($"[Singleton]{typeof(T).Name}");
                    _instance = go.AddComponent<T>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// 在 Instance 首次被访问后、Awake 之前调用。
    /// 用于替代构造函数完成单例的初始化逻辑。
    /// 子类重写此方法以执行初始化代码。
    /// </summary>
    protected abstract void Init();

    /// <summary>
    /// Awake 中调用 Init()，确保派生类的初始化逻辑在正确的生命周期执行。
    /// </summary>
    protected virtual void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Init();
    }

    /// <summary>
    /// 防止场景切换时重复创建。
    /// </summary>
    protected virtual void OnDestroy()
    {
        _applicationIsQuitting = true;
    }
}
}
