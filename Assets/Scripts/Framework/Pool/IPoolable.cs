using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 池化对象接口 所有需要对象池管理的类都需要实现该接口
/// </summary>
public interface IPoolable
{
    // 对象从池中取出时调用（初始化）
    void OnSpawn();
    // 对象归还池时调用（重置状态）
    void OnDespawn();
}
