using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 所有武器配置的统一SO
/// </summary>
[CreateAssetMenu(fileName = "AllWeaponDefs_SO", menuName = "KikoSurge/武器/所有武器集中配置")]
public class AllWeaponDefs_SO : ScriptableObject
{
    public List<WeaponDefBase> weaponDefs;
}
