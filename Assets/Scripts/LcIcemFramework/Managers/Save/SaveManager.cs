using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

using LcIcemFramework.Core;
using LcIcemFramework.Util.Const;
using LcIcemFramework.Util.Data;
using LcIcemFramework.Util.Crypto;

namespace LcIcemFramework
{

/// <summary>
/// 存档管理器（多槽位）
/// </summary>
public class SaveManager : SingletonMono<SaveManager>
{
    protected override void Init() { }
    // 最大存档槽位数。
    public const int MAX_SLOT = Constants.MAX_SLOT;

    // 存档文件夹路径
    private string _saveDir => Path.Combine(Application.persistentDataPath, "saves");
    // 存档文件名前缀
    private const string SAVE_PREFIX = "save_";
    // 存档文件名后缀
    private const string SAVE_EXT = ".json";


    /// <summary>
    /// 获取指定槽位的存档文件路径。
    /// </summary>
    private string GetSlotPath(int slot)
    {
        return Path.Combine(_saveDir, $"{SAVE_PREFIX}{slot}{SAVE_EXT}");
    }

    /// <summary>
    /// 保存数据到指定槽位（Json序列化 + AES加密）。
    /// <para> 槽位索引从 0 开始算，有效范围 0~MAX_SLOT-1 </para>
    /// </summary>
    public void Save(int slot, SaveData data)
    {
        // 如果 该存档槽位不存在 直接返回空
        if (slot < 0 || slot >= MAX_SLOT)
        {
            LogError($"无效存档槽位: {slot}，有效范围 0~{MAX_SLOT - 1}");
            return;
        }
        // 否则 开始读取存档
        try
        {
            // 将 存档对象 序列化为 Json字符串
            string json = JsonUtil.ToJson(data);
            // 将 Json字符串 加密为 加密字符串(通过AES密钥)
            string encrypted = EncryptUtil.AESEncrypt(json, Constants.SAVE_ENCRYPTION_KEY);
            // 将 加密字符串 写入文件
            Directory.CreateDirectory(_saveDir);
            File.WriteAllText(GetSlotPath(slot), encrypted);
            Log($"存档已保存至槽位 {slot}");
        }
        catch (Exception e)
        {
            LogError($"保存失败: {e.Message}");
        }
    }

    /// <summary>
    /// 从指定槽位加载数据（AES解密 + Json反序列化）。
    /// <para> 槽位索引从 0 开始算，有效范围 0~MAX_SLOT-1 </para>
    /// </summary>
    public SaveData Load(int slot)
    {
        // 如果 该存档槽位不存在 直接返回空
        if (slot < 0 || slot >= MAX_SLOT)
        {
            LogError($"无效存档槽位: {slot}");
            return default;
        }
        // 否则 开始加载存档
        try
        {
            // 获取 对应槽的存档路径
            string path = GetSlotPath(slot);
            // 如果文件不存在 返回空
            if (!File.Exists(path))
            {
                Log($"槽位 {slot} 无存档");
                return null;
            }
            // 从 存档路径 中读取 加密字符串
            string encrypted = File.ReadAllText(path);
            // 将 加密字符串 解密为 Json字符串(通过AES密钥)
            string json = EncryptUtil.AESDecrypt(encrypted, Constants.SAVE_ENCRYPTION_KEY);
            // 将 Json字符串 反序列化为 存档对象 然后返回
            return JsonUtil.FromJson<SaveData>(json);
        }
        catch (Exception e)
        {
            LogError($"加载失败（文件损坏?）: {e.Message}");
            return default;
        }
    }

    /// <summary>
    /// 删除指定槽位的存档。
    /// </summary>
    public bool Delete(int slot)
    {
        // 如果该存档槽位不存在 返回false
        if (slot < 0 || slot >= MAX_SLOT) return false;
        // 拼接存档实际路径
        string path = GetSlotPath(slot);
        // 如果文件不存在 返回false
        if (!File.Exists(path)) return false;

        // 否则 删除对应槽位存档
        File.Delete(path);
        Log($"槽位 {slot} 存档已删除");
        return true;
    }

    /// <summary>
    /// 指定槽位是否存在存档。
    /// </summary>
    public bool Exists(int slot)
    {
        if (slot < 0 || slot >= MAX_SLOT) return false;
        return File.Exists(GetSlotPath(slot));
    }

    /// <summary>
    /// 获取所有已有存档的槽位列表。
    /// </summary>
    public int[] GetUsedSlots()
    {
        var used = new List<int>();
        for (int i = 0; i < MAX_SLOT; i++)
            if (Exists(i)) used.Add(i);
        return used.ToArray();
    }

    #region 日志
    private void Log(string msg) => Debug.Log($"[SaveManager] {msg}");
    private void LogWarning(string msg) => Debug.LogWarning($"[SaveManager] {msg}");
    private void LogError(string msg) => Debug.LogError($"[SaveManager] {msg}");
    #endregion
}
}
