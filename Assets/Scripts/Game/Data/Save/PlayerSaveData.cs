using System;
using UnityEngine;

/// <summary>
/// 玩家存档数据
/// <para>包含全局进度数据和单局进度数据</para>
/// </summary>
[Serializable]
public class PlayerSaveData : LcIcemFramework.SaveData
{
    [Header("存档元信息")]
    public int slotId;
    public long lastPlayedTimestamp;
    public long totalPlayTimeSeconds;  // 累计游玩时长（秒）
    public int lastSelectedRoleId;     // 上次选择的角色ID
    public int version = 1;

    [Header("全局进度数据")]
    public PlayerMetaData metaData;

    [Header("单局进度数据（无则为null）")]
    public SessionData sessionData;

    /// <summary>
    /// 是否有进行中的游戏（SessionData 不为 null）
    /// </summary>
    public bool HasActiveSession => sessionData != null;

    /// <summary>
    /// 创建新存档
    /// </summary>
    public static PlayerSaveData CreateNew(int slotId)
    {
        return new PlayerSaveData
        {
            slotId = slotId,
            lastPlayedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            totalPlayTimeSeconds = 0,
            lastSelectedRoleId = 0,
            version = 1,
            metaData = PlayerMetaData.CreateDefault(),
            sessionData = null
        };
    }

    /// <summary>
    /// 获取最后游玩时间的格式化字符串
    /// </summary>
    public string GetLastPlayedString()
    {
        if (lastPlayedTimestamp == 0)
            return "从未游玩";

        DateTimeOffset dto = DateTimeOffset.FromUnixTimeSeconds(lastPlayedTimestamp);
        DateTime localTime = dto.LocalDateTime;

        // 今天显示时间，其他显示日期
        if (localTime.Date == DateTime.Today)
        {
            return $"今天 {localTime:HH:mm}";
        }
        if (localTime.Date == DateTime.Today.AddDays(-1))
        {
            return $"昨天 {localTime:HH:mm}";
        }
        return $"{localTime:yyyy-MM-dd HH:mm}";
    }

    /// <summary>
    /// 更新最后游玩时间
    /// </summary>
    public void UpdateLastPlayed()
    {
        lastPlayedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    /// <summary>
    /// 累计游玩时长
    /// </summary>
    public void AddPlayTime(long seconds)
    {
        totalPlayTimeSeconds += seconds;
    }
}
