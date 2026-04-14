using UnityEngine;

/// <summary>
/// 枪械武器配置 SO
/// </summary>
[CreateAssetMenu(fileName = "GunWeaponDef_SO", menuName = "KikoSurge/武器/单发枪械")]
public class GunWeaponDef_SO : WeaponDefBase
{
    [Header("枪械特有")]
    public float BulletSpeed = 20f;
    public float Spread = 2f;
    public float Range = 50f;

    private void OnValidate()
    {
        if (Type != WeaponType.Gun)
            Type = WeaponType.Gun;
    }
}
