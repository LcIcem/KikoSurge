using System;
using System.Collections.Generic;
using UnityEngine;

namespace LcIcemFramework.Util.Ext
{
/// <summary>
/// 扩展方法工具类
/// </summary>
public static class Extensions
{
    // Vector2扩展: 将Vector2转为Vector3
    public static Vector3 ToVector3(this Vector2 v, float z = 0)
    {
        return new Vector3(v.x, v.y, z);
    }

    // Transform扩展: 设置x/y/z分量
    public static void SetPosX(this Transform t, float x)
    {
        Vector3 p = t.position;
        p.x = x;
        t.position = p;
    }
    public static void SetPosY(this Transform t, float y)
    {
        Vector3 p = t.position;
        p.y = y;
        t.position = p;
    }
    public static void SetPosZ(this Transform t, float z)
    {
        Vector3 p = t.position;
        p.z = z;
        t.position = p;
    }

    // GameObject扩展：获取或添加组件
    public static T GetOrAddComponent<T>(this GameObject go) where T : Component
    {
        return go.GetComponent<T>() ?? go.AddComponent<T>();
    }

    // float扩展：四舍五入到指定小数位
    public static float Round(this float f, int decimals)
    {
        return (float)System.Math.Round(f, decimals);
    }

    //float扩展：判断与另一个浮点数是否相等
    public static bool IsEqualsTo(this float f, float other, float tolerance = 1e-5f)
    {
        return Mathf.Abs(f - other) < tolerance;
    }

    // Array扩展：随机取一个元素
    public static T Random<T>(this T[] array)
    {
        if (array == null || array.Length == 0) return default;
        return array[UnityEngine.Random.Range(0, array.Length)];
    }

    // List扩展：随机取一个元素
    public static T Random<T>(this List<T> list)
    {
        if (list == null || list.Count == 0) return default;
        return list[UnityEngine.Random.Range(0, list.Count)];
    }

    // string扩展：安全的ToString（null返回空字符串）
    public static string SafeToString(this string obj)
    {
        return obj?.ToString() ?? string.Empty;
    }
}
}
