using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 角色信息SO配置数据
/// </summary>
[CreateAssetMenu(fileName = "RoleInfo_Config", menuName = "KikoSurge/角色/角色信息")]
public class RoleInfoConfig : ScriptableObject
{
    public List<RoleInfo> roleInfos;
}
