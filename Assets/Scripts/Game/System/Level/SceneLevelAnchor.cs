using UnityEngine;
using UnityEngine.Tilemaps;
using ProcGen.Config;
using LcIcemFramework.Core;

/// <summary>
/// 场景关卡锚点
/// <para>在场景中持有关卡相关的 Tilemap 引用，供 LevelController Prefab 获取</para>
/// </summary>
public class SceneLevelAnchor : SingletonMono<SceneLevelAnchor>
{
    [Header("Tilemap 引用")]
    [SerializeField] private Tilemap _floorTilemap;
    [SerializeField] private Tilemap _wallTilemap;
    [SerializeField] private Tilemap _doorTilemap;

    [Header("瓦片配置")]
    [SerializeField] private TileInfo_SO _tileInfo;

    public Tilemap FloorTilemap => _floorTilemap;
    public Tilemap WallTilemap => _wallTilemap;
    public Tilemap DoorTilemap => _doorTilemap;
    public TileInfo_SO TileInfo => _tileInfo;

    protected override void Init() { }
}
