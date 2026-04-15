using UnityEngine;

/// <summary>
/// 伤害源接口，子弹通过此接口获取伤害值
/// </summary>
public interface IDamageSource
{
    float GetDamage();
    GameObject GetGameObject();
}
