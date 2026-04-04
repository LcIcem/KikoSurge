using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 文本工具
/// </summary>
public static class StringUtil
{

    /// <summary>
    /// RichText 颜色（UGUI TextMeshPro 兼容）
    /// </summary>
    public static string Colorize(string text, Color color)
    {
        string hex = ColorUtility.ToHtmlStringRGB(color);
        return $"<color=#{hex}>{text}</color>";
    }

    /// <summary>
    /// 数字格式化（1000 → 1K，1000000 → 1M）
    /// </summary>
    public static string NumberFormat(long number)
    {
        if (number >= 1_000_000) return $"{number / 1_000_000.0:F1}M";
        if (number >= 1_000) return $"{number / 1_000.0:F1}K";
        return number.ToString();
    }

    /// <summary>
    /// 字符串拼接（支持集合）
    /// </summary>
    public static string Join<T>(string separator, IEnumerable<T> items)
    {
        return string.Join(separator, items);
    }

    /// <summary>
    /// CSV 行解析
    /// </summary>
    public static string[] ParseCsvLine(string line)
    {
        return line.Split(',');
    }
}