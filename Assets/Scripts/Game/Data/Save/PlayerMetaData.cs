using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家全局进度数据
/// </summary>
[Serializable]
public class PlayerMetaData
{
    [Serializable]
    public class IntHashSet
    {
        public int[] items = Array.Empty<int>();

        public static IntHashSet FromHashSet(HashSet<int> hashSet)
        {
            return new IntHashSet { items = new List<int>(hashSet).ToArray() };
        }

        public HashSet<int> ToHashSet()
        {
            return new HashSet<int>(items);
        }
    }

    public IntHashSet unlockedRoleIds = new();
    public IntHashSet unlockedWeaponIds = new();
    public IntHashSet unlockedRelicIds = new();

    public float globalMaxHealthBonus = 0f;
    public float globalAtkBonus = 0f;
    public float globalDefBonus = 0f;

    public int totalGamesPlayed = 0;
    public int totalVictories = 0;
    public int currentDifficulty = 0;

    public static PlayerMetaData CreateDefault()
    {
        return new PlayerMetaData
        {
            unlockedRoleIds = IntHashSet.FromHashSet(new HashSet<int> { 0 }),
            unlockedWeaponIds = new IntHashSet(),
            unlockedRelicIds = new IntHashSet(),
            globalMaxHealthBonus = 0f,
            globalAtkBonus = 0f,
            globalDefBonus = 0f,
            totalGamesPlayed = 0,
            totalVictories = 0,
            currentDifficulty = 0
        };
    }

    public bool IsRoleUnlocked(int roleId) => unlockedRoleIds.ToHashSet().Contains(roleId);
    public bool IsWeaponUnlocked(int weaponId) => unlockedWeaponIds.ToHashSet().Contains(weaponId);
    public bool IsRelicUnlocked(int relicId) => unlockedRelicIds.ToHashSet().Contains(relicId);

    public void UnlockRole(int roleId)
    {
        var set = unlockedRoleIds.ToHashSet();
        set.Add(roleId);
        unlockedRoleIds = IntHashSet.FromHashSet(set);
        Debug.Log($"[PlayerMetaData] Role {roleId} unlocked");
    }

    public void UnlockWeapon(int weaponId)
    {
        var set = unlockedWeaponIds.ToHashSet();
        set.Add(weaponId);
        unlockedWeaponIds = IntHashSet.FromHashSet(set);
        Debug.Log($"[PlayerMetaData] Weapon {weaponId} unlocked");
    }

    public void UnlockRelic(int relicId)
    {
        var set = unlockedRelicIds.ToHashSet();
        set.Add(relicId);
        unlockedRelicIds = IntHashSet.FromHashSet(set);
        Debug.Log($"[PlayerMetaData] Relic {relicId} unlocked");
    }

    public void ApplyGameResult(bool isVictory)
    {
        totalGamesPlayed++;

        if (isVictory)
        {
            totalVictories++;
        }

        Debug.Log($"[PlayerMetaData] Game result applied: victory={isVictory}, totalGames={totalGamesPlayed}");
    }
}
