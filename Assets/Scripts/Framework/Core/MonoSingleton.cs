using UnityEngine;

/// <summary>
/// Mono单例泛型基类
/// </summary>
/// <typeparam name="T"></typeparam>
public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    private static readonly object lockObj = new object();

    public static T Instance
    {
        get
        {
            if (instance != null)
                return instance;

            lock (lockObj)
            {
                if (instance == null)
                {
                    GameObject go = new GameObject($"[Singleton]{typeof(T)}");
                    instance = go.AddComponent<T>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }
}