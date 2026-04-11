using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 角色信息SO配置数据
/// </summary>
[CreateAssetMenu(fileName = "RoleInfo_SO", menuName = "KikoSurge/角色信息配置")]
public class RoleInfo_SO : ScriptableObject
{
    public List<RoleInfo> roleInfos;
}
