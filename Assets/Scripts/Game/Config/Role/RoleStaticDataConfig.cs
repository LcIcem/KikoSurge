using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 角色静态配置数据SO
/// </summary>
[CreateAssetMenu(fileName = "RoleStaticData_Config", menuName = "KikoSurge/角色/角色静态配置")]
public class RoleStaticDataConfig : ScriptableObject
{
    public List<RoleStaticData> roleStaticDataList = new();

    /// <summary>
    /// 根据角色ID获取角色静态数据
    /// </summary>
    public RoleStaticData GetRoleStaticData(int roleId)
    {
        return roleStaticDataList.Find(r => r.roleId == roleId);
    }

    /// <summary>
    /// 获取默认角色（ID=0）
    /// </summary>
    public RoleStaticData GetDefaultRole()
    {
        return roleStaticDataList.Count > 0 ? roleStaticDataList[0] : null;
    }
}
