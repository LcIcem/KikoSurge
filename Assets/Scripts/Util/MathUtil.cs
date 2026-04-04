using System.Linq;
using UnityEngine;
/// <summary>
/// 数学工具类
/// </summary>
public static class MathUtil
{
    /// <summary>
    /// 生成 x=Asinωt、y=Asin(ωt+φ) 的动点坐标
    /// </summary>
    public static Vector3 GetXYOscPoint(float t, float A, float omega, float phi)
    {
        float x = A * Mathf.Sin(omega * t);
        float y = A * Mathf.Sin(omega * t + phi);
        return new Vector2(x, y).ToVector3();
    }

    /// <summary>
    /// from → to 的方向向量（归一化）
    /// </summary>
    public static Vector2 DirectionTo(Vector2 from, Vector2 to)
    {
        return (to - from).normalized;
    }

    /// <summary>
    /// 方向向量 → 角度（度）
    /// </summary>
    public static float AngleFromDir(Vector2 dir) 
    {
        return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    }

    /// <summary>
    /// 角度（度）→ 方向向量
    /// </summary>
    public static Vector2 DirFromAngle(float deg) {
        float rad = deg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    /// <summary>
    /// XY 平面距离
    /// </summary>
    public static float DistanceXY(Vector3 from, Vector3 to) {
        return Vector2.Distance(from, to);
    }

    /// <summary>
    /// 角度插值（正确处理 0°/360° 跨越）
    /// </summary>
    public static float LerpAngle(float a, float b, float t) {
        return Mathf.LerpAngle(a, b, t);
    }

    /// <summary>
    /// 随机圆内点（用于随机方向）
    /// </summary>
    /// <param name="radius">圆的半径</param>
    public static Vector2 PointInCircle(float radius) {
        // 从 0~360度 中随机一个角度
        float angle = Random.Range(0f, Mathf.PI * 2);
        // 对随机值开方 实现均匀分布
        float r = Mathf.Sqrt(Random.value) * radius;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
    }

    /// <summary>
    /// 加权随机（用于掉落表）
    /// </summary>
    /// <param name="options"></param>
    public static int WeightedRandom(params (int weight, int value)[] options) {
        if (options == null || options.Length == 0) return default;
        // 获得权重和
        int total = options.Sum(o => o.weight);
        // 从权重和中随机一个数
        int r = Random.Range(0, total);
        int acc = 0;
        // 累加权重判断 该随机数 落在哪个权重区间
        foreach (var (weight, value) in options)
        {
            acc += weight;
            if (r < acc) return value;
        }
        // 正常来说 随机数 肯定会落在某个区间 这步是为了防止编译报错
        return options[^1].value;
    }

}