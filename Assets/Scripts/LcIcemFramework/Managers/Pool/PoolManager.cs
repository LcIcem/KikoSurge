using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

using LcIcemFramework.Core;
using LcIcemFramework.Util.Const;
using LcIcemFramework;
using System;

namespace LcIcemFramework
{

/// <summary>
/// 对象池管理器
/// <list type="bullet">
///     <item>支持首次 Get 时自动注册（懒注册）</item>
///     <item>支持池空闲超时自动清理（防内存泄漏）</item>
/// </list>
/// </summary>
public class PoolManager : SingletonMono<PoolManager>
{
    #region 配置
    /// <summary> 池对象闲置超时时间（秒），超时后自动销毁 </summary>
    [SerializeField] private float _idleTimeout = 60f;
    /// <summary> 空闲检测间隔（秒） </summary>
    [SerializeField] private float _cleanupInterval = 10f;
    #endregion

    #region 字段
    // 对象池字典  键：预设体名  值：ObjectPool对象池
    private Dictionary<string, ObjectPool<GameObject>> _pools = new Dictionary<string, ObjectPool<GameObject>>();
    // 父对象字典  键：预设体名  值：父对象的Transform
    private Dictionary<string, Transform> _parents = new Dictionary<string, Transform>();
    // 活跃对象字典  键：预设体名  值：该池的活跃对象数量
    private Dictionary<string, int> _activeCounts = new Dictionary<string, int>();
    // 池最后使用时间  键：预设体名  值：最后使用时间戳（Time.time）
    private Dictionary<string, float> _lastUseTimes = new Dictionary<string, float>();
    // 待清理标记  键：预设体名  值：是否正在等待清理
    private Dictionary<string, bool> _pendingCleanup = new Dictionary<string, bool>();
    // 活跃对象 -> 所属池名  用于 Release(obj) 时反向查找
    private Dictionary<GameObject, string> _objToPoolName = new Dictionary<GameObject, string>();

    // 活跃对象计数用锁，防止并发
    private readonly object _lock = new object();

    // 池根对象 过场景时不销毁
    private static Transform _poolRoot;
    // 空闲检测协程
    private Coroutine _cleanupCoroutine;
    #endregion

    protected override void Init()
    {
        // 启动空闲检测协程
        _cleanupCoroutine = StartCoroutine(CleanupIdlePools());
    }

    #region 私有方法

    /// <summary>
    /// 懒注册：首次 Get 时如果没有池则自动注册
    /// </summary>
    private void EnsurePoolRegistered(string prefabName, GameObject prefab)
    {
        if (_pools.ContainsKey(prefabName)) return;

        // 检查 prefab 是否有效
        if (prefab == null)
        {
            LogError($"懒注册池 '{prefabName}' 失败：prefab 为 null。");
            return;
        }

        try
        {
            // 验证 prefab 是否已被销毁
            _ = prefab.transform;
        }
        catch (MissingReferenceException)
        {
            LogError($"懒注册池 '{prefabName}' 失败：prefab 已被销毁。");
            return;
        }

        // 初始化池根
        if (_poolRoot == null)
        {
            GameObject poolRootObj = new GameObject("@PoolRoot");
            _poolRoot = poolRootObj.transform;
            DontDestroyOnLoad(_poolRoot);
        }

        _parents[prefabName] = new GameObject(prefabName + "Pool").transform;
        _parents[prefabName].SetParent(_poolRoot);

        var pool = new ObjectPool<GameObject>(
            createFunc: () =>
            {
                try
                {
                    GameObject newObj = Instantiate(prefab, _parents[prefabName]);
                    if (newObj == null)
                    {
                        LogError($"createFunc 为池 '{prefabName}' 创建了 null 对象！");
                    }
                    return newObj;
                }
                catch (Exception ex)
                {
                    LogError($"createFunc 为池 '{prefabName}' 创建对象时异常：{ex.GetType().Name}: {ex.Message}");
                    return null;
                }
            },
            actionOnGet: obj =>
            {
                if (obj == null)
                {
                    LogError($"actionOnGet 为池 '{prefabName}' 收到 null 对象！");
                    return;
                }
                try
                {
                    obj.SetActive(true);
                }
                catch (MissingReferenceException)
                {
                    // 对象已被销毁，尝试重新创建
                    LogWarning($"actionOnGet 为池 '{prefabName}' 检测到已销毁对象，尝试重新创建。");
                    obj = Instantiate(prefab, _parents[prefabName]);
                    if (obj != null)
                        obj.SetActive(true);
                    else
                        return;
                }
                lock (_lock)
                {
                    if (_activeCounts.ContainsKey(prefabName))
                        _activeCounts[prefabName]++;
                    else
                        _activeCounts[prefabName] = 1;
                    _objToPoolName[obj] = prefabName;
                }
                if (obj != null && obj.TryGetComponent<IPoolable>(out var poolable))
                    poolable.OnSpawn();
            },
            actionOnRelease: obj =>
            {
                if (obj == null) return;
                try
                {
                    obj.SetActive(false);
                }
                catch (MissingReferenceException)
                {
                    // 对象已销毁，忽略
                }
                lock (_lock)
                {
                    if (_activeCounts.ContainsKey(prefabName))
                        _activeCounts[prefabName]--;
                    _objToPoolName.Remove(obj);
                }
                if (obj.TryGetComponent<IPoolable>(out var poolable))
                    poolable.OnDespawn();
            },
            actionOnDestroy: obj => Destroy(obj),
            collectionCheck: false,
            defaultCapacity: 0,
            maxSize: Constants.MAX_POOL_SIZE);

        _pools[prefabName] = pool;
        _activeCounts[prefabName] = 0;
        _lastUseTimes[prefabName] = Time.time;

        Log($"池 '{prefabName}' 自动注册（懒注册）。");
    }

    /// <summary>
    /// 完整销毁一个池（内部使用，由自动清理调用）
    /// </summary>
    private void DestroyPool(string poolName)
    {
        if (!_pools.TryGetValue(poolName, out var pool)) return;

        // 销毁父对象（子对象全部销毁）
        if (_parents.TryGetValue(poolName, out var parent))
            UnityEngine.Object.Destroy(parent.gameObject);

        // 释放池内存
        pool.Dispose();

        // 从所有字典移除
        _pools.Remove(poolName);
        _parents.Remove(poolName);
        _activeCounts.Remove(poolName);
        _lastUseTimes.Remove(poolName);
        _pendingCleanup.Remove(poolName);
        // 清理所有指向该池的活跃对象引用
        var keysToRemove = new List<GameObject>();
        foreach (var kvp in _objToPoolName)
        {
            if (kvp.Value == poolName)
                keysToRemove.Add(kvp.Key);
        }
        foreach (var key in keysToRemove)
            _objToPoolName.Remove(key);

        Log($"池 '{poolName}' 已自动清理。");
    }

    /// <summary>
    /// 空闲检测协程：定期检查所有池的闲置时间
    /// </summary>
    private IEnumerator CleanupIdlePools()
    {
        while (true)
        {
            yield return new WaitForSeconds(_cleanupInterval);

            float currentTime = Time.time;
            var poolNames = new List<string>(_pools.Keys);

            foreach (var poolName in poolNames)
            {
                // 有待清理标记的，跳过（由 Get 取消）
                if (_pendingCleanup.TryGetValue(poolName, out var pending) && pending)
                    continue;

                // 没有活跃对象的池才检查
                if (_activeCounts.TryGetValue(poolName, out var activeCount) && activeCount > 0)
                    continue;

                // 检查是否超时
                if (_lastUseTimes.TryGetValue(poolName, out var lastUse))
                {
                    if (currentTime - lastUse >= _idleTimeout)
                    {
                        StartCoroutine(CleanupPoolCo(poolName));
                    }
                }
            }
        }
    }

    /// <summary>
    /// 清理池协程：等一帧后再真正销毁，给 Get 留出取消窗口
    /// </summary>
    private IEnumerator CleanupPoolCo(string poolName)
    {
        _pendingCleanup[poolName] = true;
        yield return null;  // 等一帧

        if (_pendingCleanup.TryGetValue(poolName, out var pending) && pending)
        {
            DestroyPool(poolName);
        }
    }

    #endregion

    #region 公开 API

    /// <summary>
    /// 从池中获取一个对象（通过预设体名称，自动注册）
    /// </summary>
    public GameObject Get(string prefabName, Vector3 position, Quaternion rotation)
    {
        return Get<GameObject>(prefabName, position, rotation);
    }

    /// <summary>
    /// 从池中获取一个对象（通过预设体，直接使用预设体实例化）
    /// </summary>
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            LogError("[PoolManager] 预设体为 null，无法获取对象");
            return null;
        }

        string prefabName = prefab.name;

        // 如果有待清理标记，先取消清理
        if (_pendingCleanup.TryGetValue(prefabName, out var pending) && pending)
        {
            _pendingCleanup[prefabName] = false;
            _lastUseTimes[prefabName] = Time.time;
            Log($"池 '{prefabName}' 清理被取消（被 Get 唤醒）。");
        }

        // 懒注册（使用传入的 prefab，不走 Addressables）
        if (!_pools.TryGetValue(prefabName, out var pool))
        {
            EnsurePoolRegistered(prefabName, prefab);
            if (!_pools.TryGetValue(prefabName, out pool))
            {
                // EnsurePoolRegistered 失败，prefab 无效，直接实例化
                LogError($"池 '{prefabName}' 注册失败，直接实例化对象。");
                GameObject fallbackObj = Instantiate(prefab, position, rotation);
                if (fallbackObj.TryGetComponent<IPoolable>(out var poolable))
                    poolable.OnSpawn();
                return fallbackObj;
            }
        }

        GameObject obj = pool.Get();

        // 检查对象是否有效
        bool isValid = false;
        try
        {
            isValid = obj != null && obj.transform != null;
        }
        catch (MissingReferenceException)
        {
            isValid = false;
        }

        if (!isValid)
        {
            LogWarning($"池 '{prefabName}' 返回了无效对象，尝试重新实例化。");
            try
            {
                pool.Release(obj);
            }
            catch (System.Exception)
            {
                // 忽略释放失败
            }
            // 直接用传入的 prefab 实例化
            obj = Instantiate(prefab, _parents[prefabName]);

            // 手动注册到池的追踪系统
            lock (_lock)
            {
                if (_activeCounts.ContainsKey(prefabName))
                    _activeCounts[prefabName]++;
                else
                    _activeCounts[prefabName] = 1;
                _objToPoolName[obj] = prefabName;
            }

            // 调用 OnSpawn 确保对象正确激活
            if (obj != null && obj.TryGetComponent<IPoolable>(out var poolable))
                poolable.OnSpawn();
        }

        obj.transform.position = position;
        obj.transform.rotation = rotation;

        // 更新最后使用时间
        _lastUseTimes[prefabName] = Time.time;

        return obj;
    }

    /// <summary>
    /// 从池中获取一个对象，并返回其 T 类型组件（通过预设体，直接使用预设体实例化）
    /// </summary>
    public T Get<T>(GameObject prefab, Vector3 position, Quaternion rotation) where T : class
    {
        GameObject obj = Get(prefab, position, rotation);
        if (obj == null) return null;

        if (typeof(T).Name == "GameObject")
            return obj as T;

        return obj.GetComponent<T>();
    }

    /// <summary>
    /// 从池中获取一个对象，并返回其 T 类型组件（通过预设体名称，自动注册）
    /// 注意：此方法通过 Addressables 加载预设体
    /// </summary>
    public T Get<T>(string prefabName, Vector3 position, Quaternion rotation) where T : class
    {
        // 如果有待清理标记，先取消清理
        if (_pendingCleanup.TryGetValue(prefabName, out var pending) && pending)
        {
            _pendingCleanup[prefabName] = false;
            // 时间刷新，重置为未使用
            _lastUseTimes[prefabName] = Time.time;
            Log($"池 '{prefabName}' 清理被取消（被 Get 唤醒）。");
        }

        // 懒注册
        if (!_pools.TryGetValue(prefabName, out var pool))
        {
            GameObject prefab = AddressablesManager.Instance.Load<GameObject>(prefabName);
            if (prefab == null)
            {
                LogError($"对象池 '{prefabName}' 未注册且无法从 Addressables 加载。");
                return null;
            }
            EnsurePoolRegistered(prefabName, prefab);
            if (!_pools.TryGetValue(prefabName, out pool))
            {
                // EnsurePoolRegistered 失败，prefab 无效，直接实例化
                LogError($"池 '{prefabName}' 注册失败，直接实例化对象。");
                GameObject fallbackObj = Instantiate(prefab, position, rotation);
                if (fallbackObj.TryGetComponent<IPoolable>(out var poolable))
                    poolable.OnSpawn();
                if (typeof(T).Name == "GameObject")
                    return fallbackObj as T;
                return fallbackObj.GetComponent<T>();
            }
        }

        GameObject obj = pool.Get();

        // 检查对象是否有效
        bool isValid = false;
        try
        {
            isValid = obj != null && obj.transform != null;
        }
        catch (MissingReferenceException)
        {
            isValid = false;
        }

        if (!isValid)
        {
            LogWarning($"池 '{prefabName}' 返回了无效对象，尝试重新加载。");
            try
            {
                pool.Release(obj);
            }
            catch (System.Exception)
            {
                // 忽略释放失败
            }
            GameObject prefab = AddressablesManager.Instance.Load<GameObject>(prefabName);
            if (prefab == null)
            {
                LogError($"无法重新加载 '{prefabName}' 的 prefab！");
                return null;
            }
            obj = Instantiate(prefab, _parents[prefabName]);

            // 手动注册到池的追踪系统
            lock (_lock)
            {
                if (_activeCounts.ContainsKey(prefabName))
                    _activeCounts[prefabName]++;
                else
                    _activeCounts[prefabName] = 1;
                _objToPoolName[obj] = prefabName;
            }

            // 调用 OnSpawn
            if (obj != null && obj.TryGetComponent<IPoolable>(out var poolable))
                poolable.OnSpawn();
        }

        obj.transform.position = position;
        obj.transform.rotation = rotation;

        // 更新最后使用时间
        _lastUseTimes[prefabName] = Time.time;

        if (typeof(T).Name == "GameObject")
        {
            return obj as T;
        }
                
        return obj != null ? obj.GetComponent<T>() : null;
    }

    /// <summary>
    /// 归还对象到池
    /// </summary>
    public void Release(GameObject obj)
    {
        if (obj == null) return;

        // 通过活跃对象字典找到所属池
        if (_objToPoolName.TryGetValue(obj, out var poolName) && _pools.TryGetValue(poolName, out var pool))
        {
            pool.Release(obj);
            _lastUseTimes[poolName] = Time.time;
            _objToPoolName.Remove(obj);
            return;
        }

        // 不在任何池中，直接销毁（兜底）
        UnityEngine.Object.Destroy(obj);
    }

    /// <summary>
    /// 手动清理指定池
    /// </summary>
    public void Clear(string poolName)
    {
        if (!_pools.ContainsKey(poolName)) return;

        // 取消待清理标记
        if (_pendingCleanup.ContainsKey(poolName))
            _pendingCleanup[poolName] = false;

        DestroyPool(poolName);
        Log($"池 '{poolName}' 已手动清理。");
    }

    /// <summary>
    /// 手动清理所有池
    /// </summary>
    public void ClearAll()
    {
        if (_cleanupCoroutine != null)
        {
            StopCoroutine(_cleanupCoroutine);
            _cleanupCoroutine = null;
        }

        foreach (var poolName in new List<string>(_pools.Keys))
        {
            DestroyPool(poolName);
        }

        _pendingCleanup.Clear();
        Log("已清理所有池。");

        // 重新启动空闲检测
        _cleanupCoroutine = StartCoroutine(CleanupIdlePools());
    }

    /// <summary>
    /// 获取指定池的活跃对象数量
    /// </summary>
    public int GetActiveCount(string poolName)
    {
        return _activeCounts.TryGetValue(poolName, out var count) ? count : 0;
    }

    /// <summary>
    /// 获取当前池数量
    /// </summary>
    public int GetPoolCount() => _pools.Count;

    #endregion

    #region 日志
    private void Log(string msg) => Debug.Log($"[PoolManager] {msg}");
    private void LogWarning(string msg) => Debug.LogWarning($"[PoolManager] {msg}");
    private void LogError(string msg) => Debug.LogError($"[PoolManager] {msg}");
    #endregion
}
}
