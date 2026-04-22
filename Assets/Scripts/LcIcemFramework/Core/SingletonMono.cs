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
    /// <summary>
    /// 代码主动注册的实例（优先级最高，在 Awake 之前就可以注册）
    /// </summary>
    private static T _registeredInstance;

    public static T Instance
    {
        get
        {
            if (_applicationIsQuitting)
                return null;

            // 快速路径：实例存在且有效
            try
            {
                if (_instance != null)
                    return _instance;
            }
            catch (MissingReferenceException)
            {
                _instance = null;
            }

            lock (_lockObj)
            {
                // 代码注册的实例优先级最高
                if (_registeredInstance != null)
                {
                    _instance = _registeredInstance;
                    _registeredInstance = null; // 只使用一次
                    return _instance;
                }

                if (_instance == null)
                {
                    // 场景中查找有效实例
                    foreach (var obj in FindObjectsByType<T>(FindObjectsSortMode.None))
                    {
                        try
                        {
                            _ = obj.name;
                            _instance = obj;
                            return _instance;
                        }
                        catch (MissingReferenceException) { }
                    }

                    // 没有找到有效实例，创建一个新的（仅作为兜底）
                    GameObject go = new GameObject($"[Singleton]{typeof(T).Name}");
                    _instance = go.AddComponent<T>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// 主动注册实例（在 Awake 之前调用，优先级最高）
    /// 用于代码控制初始化顺序
    /// </summary>
    public static void RegisterInstance(T instance)
    {
        if (_instance != null && _instance != instance)
        {
            Debug.LogWarning($"[SingletonMono] {typeof(T).Name} RegisterInstance: 已有实例，跳过注册");
            return;
        }
        _registeredInstance = instance;
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
        // 如果已有有效实例，当前实例作为普通组件存在
        try
        {
            if (_instance != null && _instance != this)
                return;
        }
        catch (MissingReferenceException)
        {
            _instance = null;
        }

        // 如果代码已经注册了实例（_registeredInstance），使用它
        if (_registeredInstance != null)
        {
            _instance = _registeredInstance;
            _registeredInstance = null;
        }
        else
        {
            _instance = this as T;
            DontDestroyOnLoad(gameObject);
        }

        Init();
    }

    /// <summary>
    /// 只有真正退出应用程序时才设置退出标志
    /// </summary>
    protected virtual void OnDestroy()
    {
        if (!Application.isPlaying)
            _applicationIsQuitting = true;
    }
}
}
