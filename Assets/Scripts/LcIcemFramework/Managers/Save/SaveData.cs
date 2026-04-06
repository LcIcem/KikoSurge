namespace LcIcemFramework.Managers.Save
{
/// <summary>
/// 存档数据结构
/// </summary>
public abstract class SaveData
{
    public int HighScore;
    public int HighestWave;
    public float TotalPlayTime;
    public int MetaUpgradePoints;
    public long LastPlayedTimestamp;
}
}