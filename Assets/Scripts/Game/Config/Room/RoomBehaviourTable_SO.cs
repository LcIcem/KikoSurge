using System;
using System.Collections.Generic;
using ProcGen.Core;
using UnityEngine;

[CreateAssetMenu(fileName = "RoomBehaviourTable_", menuName = "KikoSurge/Room/房间行为配置表")]
public class RoomBehaviourTable_SO : ScriptableObject
{
    [Header("普通房间行为配置")]
    [SerializeReference] public List<RoomBehaviourEntry> normalRoomEntries = new();

    [Header("精英房间行为配置")]
    [SerializeReference] public List<RoomBehaviourEntry> eliteRoomEntries = new();

    [Header("Boss房间行为配置")]
    [SerializeReference] public List<RoomBehaviourEntry> bossRoomEntries = new();

    /// <summary>
    /// 根据房间类型获取对应的行为配置
    /// </summary>
    public List<RoomBehaviourEntry> GetEntriesByRoomType(RoomType roomType)
    {
        return roomType switch
        {
            RoomType.Normal => normalRoomEntries,
            RoomType.Elite => eliteRoomEntries,
            RoomType.Boss => bossRoomEntries,
            _ => new List<RoomBehaviourEntry>()
        };
    }
}
