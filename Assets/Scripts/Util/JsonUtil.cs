using System;
using System.IO;
using LitJson;

/// <summary>
/// LitJson 封装工具类
/// <para>统一封装 JSON 文件读写操作，读文件路径直接返回解析结果</para>
/// </summary>
public static class JsonUtil
{
    /// <summary>
    /// 序列化
    /// </summary>
    public static string ToJson(object data)
    {
        return JsonMapper.ToJson(data);
    }

    /// <summary>
    /// 反序列化
    /// </summary>
    public static T FromJson<T>(string path)
    {
        return JsonMapper.ToObject<T>(path);
    }

    /// <summary>
    /// 从 JSON 文件加载并反序列化
    /// </summary>
    /// <param name="path">完整文件路径</param>
    /// <returns>反序列化后的对象，文件不存在或解析失败返回 default</returns>
    public static T LoadFromFile<T>(string path)
    {
        if (!File.Exists(path)) return default;

        try
        {
            string json = File.ReadAllText(path);
            if (string.IsNullOrEmpty(json)) return default;
            return JsonMapper.ToObject<T>(json);
        }
        catch (Exception)
        {
            return default;
        }
    }

    /// <summary>
    /// 序列化并保存到 JSON 文件（自动创建目录）
    /// </summary>
    /// <param name="path">完整文件路径</param>
    /// <param name="obj">要序列化的对象</param>
    /// <returns>是否保存成功</returns>
    public static bool SaveToFile<T>(string path, T obj)
    {
        try
        {
            string json = JsonMapper.ToJson(obj);
            File.WriteAllText(path, json);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
