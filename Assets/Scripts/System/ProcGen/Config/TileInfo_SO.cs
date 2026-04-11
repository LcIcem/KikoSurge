using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 瓦片资源配置（ScriptableObject）
/// 在 Project 窗口创建：Create → KikoSurge/Dungeon/瓦片配置
/// </summary>
[CreateAssetMenu(fileName = "TileInfo_SO", menuName = "KikoSurge/Dungeon/瓦片配置")]
public class TileInfo_SO : ScriptableObject
{
    public List<Tile> tiles = new List<Tile>();
}

/// <summary>
/// 瓦片资源数据结构
/// </summary>
[System.Serializable]
public class Tile
{
    public TileType type;
    public TileBase tile;
}
