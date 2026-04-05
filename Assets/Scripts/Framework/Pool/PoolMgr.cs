using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 对象池管理器
/// <list type="bullet">
///     <item>统一管理所有可复用对象</item>
/// </list>
/// </summary>
public class PoolMgr : MonoSingleton<PoolMgr>
{
    #region 字段
    // 对象池字典  键：预设体名  值：ObjectPool对象池
    private Dictionary<string, ObjectPool<GameObject>> _pools = new Dictionary<string, ObjectPool<GameObject>>();
    // 父对象字典  键：预设体名  值：父对象的Transform  目的是为了便于在Hierarchy中方便查看各对象池
    private Dictionary<string, Transform> _parents = new Dictionary<string, Transform>();
    // 池根对象 过场景时不销毁 作为所有 pool 父对象的父级
    private static Transform _poolRoot;
    #endregion

    #region 对象池操作
    /// <summary>
    /// 注册一个预设体到对象池
    /// </summary>
    /// <param name="prefabName">预设体名</param>
    /// <param name="prefab">预设体</param>
    /// <param name="initialCount">预创建数量</param>
    public void Register(string prefabName, GameObject prefab, int initialCount = 10, int maxSize = Constants.MAX_POOL_SIZE)
    {
        // 如果该预设体已经创建过对象池了 直接返回
        if (_pools.ContainsKey(prefabName))
        {
            LogWarning($"对象池 '{prefabName}' 已存在，无需重复注册。");
            return;
        }

        // 首次注册时初始化池根对象 设为 DontDestroyOnLoad 保证跨场景持久化
        if (_poolRoot == null)
        {
            GameObject _poolRootObj = new GameObject("@PoolRoot");
            _poolRoot = _poolRootObj.transform;
            DontDestroyOnLoad(_poolRootObj);
        }
        _parents[prefabName] = new GameObject(prefabName + "Pool").transform;
        _parents[prefabName].SetParent(_poolRoot);

        // 生成一个对象池实例
        // actionOnGet：对象从池中取出时调用
        // actionOnRelease：对象归还池时调用
        var pool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(prefab, _parents[prefabName]),
            actionOnGet: obj =>
            {
                obj.SetActive(true);
                // 如果对象实现了 IPoolable，调用 OnSpawn
                if (obj.TryGetComponent<IPoolable>(out var poolable))
                {
                    poolable.OnSpawn();
                }
            },
            actionOnRelease: obj =>
            {
                obj.SetActive(false);
                if (obj.TryGetComponent<IPoolable>(out var poolable))
                {
                    poolable.OnDespawn();
                }
            },
            actionOnDestroy: obj => Destroy(obj),
            collectionCheck: false,
            defaultCapacity: initialCount,
            maxSize: maxSize);

        // 将该对象池 添加入 对象池字典
        _pools[prefabName] = pool;
        Log($"对象池 '{prefabName}' 注册成功，预加载 {initialCount} 个对象。");

        // 预实例化：按 initialCount 预先创建并归还对象，确保池中已有指定数量的备用对象
        if (initialCount > 0)
        {
            var warmup = new GameObject[initialCount];
            for (int i = 0; i < initialCount; i++)
            {
                warmup[i] = pool.Get();
            }
            for (int i = 0; i < initialCount; i++)
            {
                pool.Release(warmup[i]);
            }
        }
    }

    /// <summary>
    /// 从池中获取一个对象
    /// </summary>
    public GameObject Get(string prefabName, Vector3 position, Quaternion rotation)
    {
        // 如果该池没有被注册过 直接返回
        if (!_pools.TryGetValue(prefabName, out var pool))
        {
            LogError($"对象池 '{prefabName}' 未找到，是否忘记注册？");
            return null;
        }

        // 否则 从池中得到一个对象 设置好位置旋转后返回出去
        GameObject obj = pool.Get();
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        return obj;
    }

    /// <summary>
    /// 归还对象到池
    /// </summary>
    public void Release(string prefabName, GameObject obj)
    {
        // 如果该池没有被注册过 直接返回
        if (!_pools.TryGetValue(prefabName, out var pool)) return;
        // 否则 将该对象回收入对象池
        pool.Release(obj);
    }

    /// <summary>
    /// 取消注册单个对象池
    /// </summary>
    /// <param name="prefabName">预设体名</param>
    /// <param name="releaseAll">是否同时释放池中所有活跃对象（推荐 true）</param>
    public void Unregister(string prefabName, bool releaseAll = true)
    {
        if (!_pools.TryGetValue(prefabName, out var pool))
        {
            LogWarning($"对象池 '{prefabName}' 未找到。");
            return;
        }

        if (releaseAll && _parents.TryGetValue(prefabName, out var parent))
        {
            // 直接销毁父物体即可，无需先 ReleaseAll
            // ObjectPool.ReleaseAll 只是将对象 SetActive(false)，销毁后这些引用自然失效
            // 注意：销毁后该 pool 绝不能再被使用
            Object.Destroy(parent.gameObject);
        }

        // 无论 releaseAll 是否为 true，都需要 Dispose 防止内存泄漏
        pool.Dispose();
        _parents.Remove(prefabName);

        _pools.Remove(prefabName);
        Log($"对象池 '{prefabName}' 已取消注册。");
    }

    /// <summary>
    /// 清空所有对象池
    /// </summary>
    /// <param name="releaseAll">是否同时释放池中所有对象</param>
    public void ClearAll(bool releaseAll = true)
    {
        // 先复制 Key 列表避免迭代中修改字典
        foreach (var name in new List<string>(_pools.Keys))
        {
            Unregister(name, releaseAll);
        }
        Log("已清空所有对象池。");
    }
    #endregion

    #region 日志
    private void Log(string msg) => Debug.Log($"[{GetType().Name}] {msg}");
    private void LogWarning(string msg) => Debug.LogWarning($"[{GetType().Name}] {msg}");
    private void LogError(string msg) => Debug.LogError($"[{GetType().Name}] {msg}");
    #endregion
}
