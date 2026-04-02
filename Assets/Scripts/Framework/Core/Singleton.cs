using System;

/// <summary>
/// 普通单例泛型基类
/// </summary>
/// <typeparam name="T"></typeparam>
public class Singleton<T> where T : class
{
    private static T instance;
    private static readonly object lockObj = new object();
    public static T Instance
    {
        get
        {
            // 双检锁
            // 外层 if：实例已创建时跳过加锁，避免每次都等锁
            // 内层 if：保证多线程下只创建一个实例
            if (instance == null)
            {
                lock (lockObj)
                {
                    // 第二次检测 是为了防止多线程同时进入时会生成多个实例
                    if (instance == null)
                    {
                        instance = Activator.CreateInstance<T>();
                    }
                }
            }
            return instance;
        }
    }
}