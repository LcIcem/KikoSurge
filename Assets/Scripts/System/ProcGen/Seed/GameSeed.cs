using System;
using System.Collections.Generic;

namespace ProcGen.Seed
{
    /// <summary>游戏种子随机数封装
    /// 封装 Random，提供确定性的伪随机数生成
    /// 所有需要与种子机制挂钩的随机操作都通过此类
    /// </summary>
    public class GameSeed
    {
        private Random _rng;

        /// <summary>当前种子的数值表示（用于存档）</summary>
        public long SeedValue { get; private set; }

        /// <summary>原始种子字符串（用于分享）；随机种子时为数字字符串</summary>
        public string SeedString { get; private set; }

        /// <summary>从数值创建种子（用于读取存档）</summary>
        public GameSeed(long seed)
        {
            SeedValue = seed;
            SeedString = seed.ToString();
            _rng = new Random((int)(seed ^ (seed >> 31)));
        }

        /// <summary>从字符串创建种子（用于用户输入/分享）；随机种子时使用时间戳</summary>
        public GameSeed(string seedStr)
        {
            SeedString = string.IsNullOrEmpty(seedStr)
                ? Environment.TickCount.ToString()
                : seedStr;
            SeedValue = StringToSeed(SeedString);
            _rng = new Random((int)(SeedValue ^ (SeedValue >> 31)));
        }

        /// <summary>从字符串转换（纯哈希，不改变 SeedValue / SeedString）</summary>
        public static long StringToSeed(string seedStr)
        {
            if (string.IsNullOrEmpty(seedStr))
                return Environment.TickCount;

            // 简单的字符串Hash：累加每个字符 + 乘以位置系数
            long hash = 0;
            for (int i = 0; i < seedStr.Length; i++)
            {
                hash = hash * 31 + seedStr[i];
            }
            return hash;
        }

        /// <summary>生成一个随机整数 [min, max)（闭左开右）</summary>
        public int Range(int min, int max)
        {
            return _rng.Next(min, max);
        }

        /// <summary>生成一个随机浮点数 [0, 1)</summary>
        public float Value()
        {
            return (float)_rng.NextDouble();
        }

        /// <summary>生成一个随机浮点数 [min, max]</summary>
        public float Range(float min, float max)
        {
            return min + (float)_rng.NextDouble() * (max - min);
        }

        /// <summary>以百分比形式生成随机概率（0-100）</summary>
        public bool RollChance(int percentChance)
        {
            return _rng.Next(0, 100) < percentChance;
        }

        /// <summary>从数组中随机选一个元素</summary>
        public T Pick<T>(T[] array)
        {
            return array[_rng.Next(array.Length)];
        }

        /// <summary>从列表中随机选一个元素</summary>
        public T Pick<T>(List<T> list)
        {
            return list[_rng.Next(list.Count)];
        }

        /// <summary>打乱列表顺序（Fisher-Yates 洗牌）</summary>
        public void Shuffle<T>(List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = _rng.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        /// <summary>随机打乱数组（返回新数组，原数组不变）</summary>
        public T[] Shuffled<T>(T[] array)
        {
            var list = new List<T>(array);
            Shuffle(list);
            return list.ToArray();
        }
    }
}
