using System;
using ProcGen.Config;
using UnityEngine;

/// <summary>
/// 遗物效果基类
/// </summary>
[Serializable]
public abstract class RelicEffect
{
    public abstract void ApplyToDungeonModel(DungeonModel_SO model);
    public abstract void ApplyToRoomEntry(RoomBehaviourEntry entry);
}

/// <summary>
/// 地牢生成效果
/// </summary>
[Serializable]
public class DungeonGenerationEffect : RelicEffect
{
    public int extraEliteChance;
    public int extraTreasureChance;

    public override void ApplyToDungeonModel(DungeonModel_SO model)
    {
        if (model == null) return;
        model.eliteExtraChance += extraEliteChance;
        model.treasureExtraChance += extraTreasureChance;
    }

    public override void ApplyToRoomEntry(RoomBehaviourEntry entry) { }
}

/// <summary>
/// 房间行为效果
/// </summary>
[Serializable]
public class RoomBehaviorEffect : RelicEffect
{
    public int enemyCountBonus;
    public float eliteChanceBonus;
    public float lootMultiplier;

    public override void ApplyToDungeonModel(DungeonModel_SO model) { }

    public override void ApplyToRoomEntry(RoomBehaviourEntry entry)
    {
        if (entry is EnemyBehaviourEntry enemyEntry)
        {
            enemyEntry.minWaves += enemyCountBonus;
            enemyEntry.maxWaves += enemyCountBonus;
        }
    }
}
