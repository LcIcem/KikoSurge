using UnityEngine;

/// <summary>
/// 武器配置基类（抽象）
/// </summary>
public abstract class WeaponDefBase : ScriptableObject
{
    [Header("标识")]
    [Tooltip("唯一ID，用于字典查找")]
    public int WeaponId;

    [Tooltip("武器名称")]
    public string WeaponName;

    [Header("基础属性")]
    public WeaponType Type;
    public float Damage;
    public float FireRate;
    public float ReloadTime;
    public int MagazineSize;
    public float RecoilForce;

    [Header("预设体")]
    [Tooltip("武器可视化预设体的 Addressables 地址")]
    public string WeaponPrefabAddress;

    [Tooltip("子弹预设体的 Addressables 地址")]
    public string BulletPrefabAddress;

    [Header("UI图标")]
    [Tooltip("HUD显示用的武器图标")]
    public Sprite Icon;
}
