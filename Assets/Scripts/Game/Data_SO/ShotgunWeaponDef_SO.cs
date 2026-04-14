using UnityEngine;

/// <summary>
/// 霰弹枪武器配置 SO
/// </summary>
[CreateAssetMenu(fileName = "ShotgunWeaponDef_SO", menuName = "KikoSurge/武器/霰弹枪")]
public class ShotgunWeaponDef_SO : WeaponDefBase
{
    [Header("霰弹枪特有")]
    public float BulletSpeed = 20f;
    public int PelletCount = 6;
    public float SpreadAngle = 30f;
    public float FalloffStart = 5f;
    public float Range = 50f;

    private void OnValidate()
    {
        if (Type != WeaponType.Shotgun)
            Type = WeaponType.Shotgun;
    }
}
