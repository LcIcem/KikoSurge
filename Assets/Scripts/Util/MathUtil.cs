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

}