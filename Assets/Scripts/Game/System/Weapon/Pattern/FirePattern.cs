using UnityEngine;

/// <summary>
/// 开火模式抽象类（SO）
/// </summary>
public abstract class FirePattern : ScriptableObject
{
    public abstract void Fire(GameObject projectile, Transform firePoint);
}