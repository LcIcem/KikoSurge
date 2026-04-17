using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 所有武器配置的统一SO
/// </summary>
[CreateAssetMenu(fileName = "Weapon_Config_Registry", menuName = "KikoSurge/武器/总配置")]
public class WeaponConfigRegistry : ScriptableObject
{
    public List<GunConfig> weaponConfigs;
}
