using System;

namespace LcIcemFramework.Core
{
/// <summary>
/// 普通单例泛型基类
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class Singleton<T> where T : class
{
    private static T _instance;
    private static readonly object _lockObj = new object();

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lockObj)
                {
                    if (_instance == null)
                    {
                        T instance = Activator.CreateInstance<T>();
                        // 首次创建后调用 Init() 方法完成初始化
                        if (instance is Singleton<T> s)
                        {
                            s.Init();
                        }
                        _instance = instance;
                    }
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// 在 Instance 首次被访问后调用，用于替代构造函数完成单例的初始化逻辑。
    /// 子类重写此方法以执行初始化代码。
    /// </summary>
    protected abstract void Init();
}
}
