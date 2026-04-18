using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public static class ItemIDAssigner
{
    private const int SEGMENT = 1000;

    // 配置根路径
    private const string CONFIG_ROOT = "Assets/Addressables/Groups/Config/Item";

    // 从文件夹名到 ItemType 的映射
    private static readonly Dictionary<string, ItemType> FolderTypeMap = new Dictionary<string, ItemType>();

    static ItemIDAssigner()
    {
        FolderTypeMap.Clear();
        foreach (ItemType type in System.Enum.GetValues(typeof(ItemType)))
        {
            FolderTypeMap[type.ToString()] = type;
        }
    }

    public static void RegisterFolderMapping(string folderName, ItemType type)
    {
        FolderTypeMap[folderName] = type;
    }

    [MenuItem("Assets/物品工具/分配新ID", true)]
    private static bool ValidateAssignID()
    {
        return Selection.activeObject is ItemConfig;
    }

    [MenuItem("Assets/物品工具/分配新ID")]
    private static void AssignNewID()
    {
        var config = Selection.activeObject as ItemConfig;
        if (config == null) return;

        // 从路径获取文件夹名，推断 ItemType
        string assetPath = AssetDatabase.GetAssetPath(config);
        string folderName = Path.GetDirectoryName(assetPath).Replace("\\", "/").Split('/').Last();

        if (!FolderTypeMap.TryGetValue(folderName, out ItemType inferredType))
        {
            Debug.LogError($"无法从文件夹名 [{folderName}] 推断 ItemType，请确保文件夹名与 ItemType 枚举名称一致");
            return;
        }

        ItemType type = inferredType;

        // 检查是否已经有有效ID（ID > 0 且类型匹配）
        int existingIdType = config.Id / SEGMENT;
        if (config.Id > 0 && existingIdType == (int)type)
        {
            Debug.Log($"物品已有有效ID: {config.Id}，无需重新分配");
            return;
        }

        // 扫描同目录下所有 ItemConfig
        string folderPath = Path.GetDirectoryName(assetPath);
        int maxIndex = -1;
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(ItemConfig).Name}", new[] { folderPath });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var item = AssetDatabase.LoadAssetAtPath<ItemConfig>(path);
            if (item != null && item.Type == type)
            {
                int index = item.Id % SEGMENT;
                if (index > maxIndex) maxIndex = index;
            }
        }

        int newID = (int)type * SEGMENT + maxIndex + 1;

        if (newID >= (int)(type + 1) * SEGMENT)
        {
            Debug.LogError($"[{type}] 类型ID已满，无法分配新ID");
            return;
        }

        Undo.RecordObject(config, "Assign Item ID");
        config.Id = newID;
        EditorUtility.SetDirty(config);

        Debug.Log($"[{type}] 分配新ID: {newID}");
    }

    [MenuItem("Assets/物品工具/验证所有物品ID")]
    private static void VerifyAllIDs()
    {
        if (!AssetDatabase.IsValidFolder(CONFIG_ROOT))
        {
            Debug.LogError($"配置路径不存在: {CONFIG_ROOT}");
            return;
        }

        int errorCount = 0;
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(ItemConfig).Name}", new[] { CONFIG_ROOT });

        // 用于检测重复ID
        var idToItems = new Dictionary<int, List<string>>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var item = AssetDatabase.LoadAssetAtPath<ItemConfig>(path);
            if (item == null) continue;

            string folderName = Path.GetDirectoryName(path).Replace("\\", "/").Split('/').Last();
            if (!FolderTypeMap.TryGetValue(folderName, out ItemType expectedType))
            {
                Debug.LogWarning($"无法识别文件夹 [{folderName}] 对应的 ItemType，跳过: {item.name}");
                continue;
            }

            int idType = item.Id / SEGMENT;
            if (idType != (int)expectedType)
            {
                Debug.LogError($"ID不匹配: {item.name} (ID:{item.Id}) - 期望类型{expectedType}({folderName}), 实际{idType}");
                errorCount++;
            }

            // 记录ID用于检测重复
            if (item.Id > 0)
            {
                if (!idToItems.ContainsKey(item.Id))
                    idToItems[item.Id] = new List<string>();
                idToItems[item.Id].Add($"{item.name} ({folderName})");
            }
        }

        // 检测重复ID
        foreach (var kvp in idToItems)
        {
            if (kvp.Value.Count > 1)
            {
                Debug.LogError($"ID重复: [{kvp.Key:D4}] 被以下物品使用: {string.Join(", ", kvp.Value)}");
                errorCount++;
            }
        }

        Debug.Log(errorCount == 0 ? "所有物品ID验证通过" : $"发现 {errorCount} 个错误");
    }

    [MenuItem("Assets/物品工具/查看物品列表")]
    private static void ShowItemList()
    {
        if (!AssetDatabase.IsValidFolder(CONFIG_ROOT))
        {
            Debug.LogError($"配置路径不存在: {CONFIG_ROOT}");
            return;
        }

        var grouped = new Dictionary<ItemType, List<(string name, int id)>>();
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(ItemConfig).Name}", new[] { CONFIG_ROOT });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var item = AssetDatabase.LoadAssetAtPath<ItemConfig>(path);
            if (item == null) continue;

            string folderName = Path.GetDirectoryName(path).Replace("\\", "/").Split('/').Last();
            if (!FolderTypeMap.TryGetValue(folderName, out ItemType type))
                continue;

            if (!grouped.ContainsKey(type))
                grouped[type] = new List<(string, int)>();

            grouped[type].Add((item.name, item.Id));
        }

        foreach (var kvp in grouped)
        {
            Debug.Log($"=== {kvp.Key} ===");
            foreach (var (name, id) in kvp.Value)
                Debug.Log($"  [{id:D4}] {name}");
        }
    }
}
