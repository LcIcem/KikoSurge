using System.Collections.Generic;
using UnityEngine.Tilemaps;

/// <summary>
/// 瓦片资源信息类
/// </summary>
[System.Serializable]
public class TileInfo
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