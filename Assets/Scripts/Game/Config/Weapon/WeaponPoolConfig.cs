using System.Linq;
using UnityEngine;

/// <summary>
/// 武器随机池 - 肉鸽随机抽取
/// </summary>
[CreateAssetMenu(fileName = "Weapon_Pool", menuName = "KikoSurge/武器/武器池")]
public class WeaponPoolConfig : ScriptableObject
{
    [Header("普通武器池")]
    public GunConfig[] commonGuns;

    [Header("稀有武器池")]
    public GunConfig[] rareGuns;

    [Header("掉率")]
    [Range(0f, 1f)]
    public float rareChance = 0.2f;

    /// <summary>
    /// 根据权重随机抽取一把武器
    /// </summary>
    public GunConfig PickRandom()
    {
        // 决定抽普通还是稀有
        GunConfig[] pool;
        if (rareGuns != null && rareGuns.Length > 0 && Random.value < rareChance)
        {
            pool = rareGuns;
        }
        else
        {
            pool = commonGuns;
        }

        if (pool == null || pool.Length == 0)
            return null;

        // 权重随机
        int totalWeight = pool.Sum(g => g.weight);
        if (totalWeight <= 0)
            return pool[Random.Range(0, pool.Length)];

        int rand = Random.Range(0, totalWeight);
        int current = 0;
        foreach (var gun in pool)
        {
            current += gun.weight;
            if (rand < current)
                return gun;
        }

        return pool[0];
    }
}
