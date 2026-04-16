using System.Collections.Generic;
using Game.Event;
using LcIcemFramework.Core;
using ProcGen.Core;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 小地图系统
/// <para>根据地牢图生成小地图，显示房间图标和走廊折线</para>
/// <para>挂到 HubPanel 所在 GameObject 上</para>
/// </summary>
public class MiniMap : MonoBehaviour
{
    [Header("房间图标")]
    public Sprite startRoomIcon;
    public Sprite normalRoomIcon;
    public Sprite goalRoomIcon;
    public Sprite treasureRoomIcon;
    public Sprite shopRoomIcon;
    public Sprite eliteRoomIcon;
    public Sprite restRoomIcon;
    public Sprite eventRoomIcon;
    public Sprite bossRoomIcon;

    [Header("走廊线材质")]
    public Material corridorMaterial;

    [Header("颜色")]
    public Color unvisitedColor = new Color(1f, 1f, 1f, 0.4f);
    public Color currentRoomColor = Color.yellow;
    public Color visitedColor = Color.white;
    public Color corridorColor = new Color(0.8f, 0.8f, 0.8f, 0.6f);

    [Header("缩放")]
    public float mapScale = 0.1f;

    [Header("闪烁效果")]
    public float blinkSpeed = 1.5f;  // 每秒闪烁次数

    [SerializeField] private RectTransform _mapContainer;

    // 运行时数据
    private Dictionary<int, Image> _roomIcons = new();
    private HashSet<int> _visitedRooms = new();
    private int _currentRoomId = -1;
    private Coroutine _blinkCoroutine;

    // 事件回调委托（存储引用以便正确取消订阅）
    private System.Action<RoomEnterParams> _onRoomEnterHandler;
    private System.Action<int> _onRoomClearedHandler;
    private System.Action<int> _onLayerEnterHandler;

    void Start()
    {
        _onRoomEnterHandler = p => OnRoomEnter(p);
        _onRoomClearedHandler = roomId => OnRoomCleared(roomId);
        _onLayerEnterHandler = layerIndex => OnLayerEnter(layerIndex);

        EventCenter.Instance.Subscribe(GameEventID.OnRoomEnter, _onRoomEnterHandler);
        EventCenter.Instance.Subscribe(GameEventID.OnRoomCleared, _onRoomClearedHandler);
        EventCenter.Instance.Subscribe(GameEventID.OnLayerEnter, _onLayerEnterHandler);

        // 订阅完成后主动构建一次（处理首次进入时事件已发布的情况）
        var levelCtrl = GameLifecycleManager.Instance.LevelController;
        if (levelCtrl != null && levelCtrl.IsBuildCompleted)
        {
            BuildMiniMap();
        }
    }

    void OnDestroy()
    {
        if (_blinkCoroutine != null)
        {
            StopCoroutine(_blinkCoroutine);
        }
        EventCenter.Instance.Unsubscribe(GameEventID.OnRoomEnter, _onRoomEnterHandler);
        EventCenter.Instance.Unsubscribe(GameEventID.OnRoomCleared, _onRoomClearedHandler);
        EventCenter.Instance.Unsubscribe(GameEventID.OnLayerEnter, _onLayerEnterHandler);
    }

    private void OnLayerEnter(int layerIndex)
    {
        BuildMiniMap();
    }

    private void BuildMiniMap()
    {
        // 清空现有
        ClearMap();

        var levelCtrl = GameLifecycleManager.Instance.LevelController;
        if (levelCtrl == null) return;

        var graph = levelCtrl.CurrentGraph;
        var tileData = levelCtrl.GetTileData();
        if (graph == null) return;

        // 获取地图边界
        var bounds = graph.mapBounds;

        // 计算动态缩放比例，使地图填满容器（留一点边距）
        float containerWidth = _mapContainer.rect.width;
        float containerHeight = _mapContainer.rect.height;
        float mapWidth = bounds.width * mapScale;
        float mapHeight = bounds.height * mapScale;
        float fillScale = Mathf.Min(containerWidth / mapWidth, containerHeight / mapHeight);
        float finalScale = mapScale * fillScale;

        // 计算居中偏移：使地图在容器中居中
        float offsetX = (containerWidth - bounds.width * finalScale) * 0.5f;
        float offsetY = (containerHeight - bounds.height * finalScale) * 0.5f;

        // 构建房间图标
        foreach (var room in graph.allRooms)
        {
            CreateRoomIcon(room, bounds, finalScale, offsetX, offsetY);
        }

        // 构建走廊线
        foreach (var corridor in graph.corridors)
        {
            CreateCorridorLine(corridor, bounds, finalScale, offsetX, offsetY);
        }

        // 门也当作走廊，画相邻房间之间的连接线
        CreateDoorConnections(graph, tileData, bounds, finalScale, offsetX, offsetY);
    }

    private void ClearMap()
    {
        if (_blinkCoroutine != null)
        {
            StopCoroutine(_blinkCoroutine);
            _blinkCoroutine = null;
        }

        _roomIcons.Clear();
        _visitedRooms.Clear();
        _currentRoomId = -1;

        // 销毁所有子对象
        foreach (Transform child in _mapContainer)
        {
            Destroy(child.gameObject);
        }
    }

    private Sprite GetIcon(RoomType type) => type switch
    {
        RoomType.Start => startRoomIcon,
        RoomType.Normal => normalRoomIcon,
        RoomType.Goal => goalRoomIcon,
        RoomType.Treasure => treasureRoomIcon,
        RoomType.Shop => shopRoomIcon,
        RoomType.Elite => eliteRoomIcon,
        RoomType.Rest => restRoomIcon,
        RoomType.Event => eventRoomIcon,
        RoomType.Boss => bossRoomIcon,
        _ => normalRoomIcon
    };

    private void CreateRoomIcon(Room room, RectInt bounds, float scale, float offsetX, float offsetY)
    {
        if (_mapContainer == null)
        {
            Debug.LogError("[MiniMap] _mapContainer is null! Please assign it in inspector");
            return;
        }
        var icon = new GameObject($"Room_{room.id}");
        icon.transform.SetParent(_mapContainer);
        var image = icon.AddComponent<Image>();
        image.sprite = GetIcon(room.roomType);
        image.color = unvisitedColor;  // 初始为淡色

        var rect = icon.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(0, 0);
        rect.pivot = new Vector2(0, 0);
        rect.anchoredPosition =
            new Vector2((room.gridPos.x - bounds.xMin) * scale + offsetX,
                        (room.gridPos.y - bounds.yMin) * scale + offsetY);
        rect.sizeDelta =
            new Vector2(room.size.x * scale, room.size.y * scale);

        _roomIcons[room.id] = image;
    }

    private void CreateCorridorLine(Corridor corridor, RectInt bounds, float scale, float offsetX, float offsetY)
    {
        if (corridor.pathTiles == null || corridor.pathTiles.Count < 2)
            return;

        var path = corridor.pathTiles;

        // 起点多画一格
        Vector2Int firstDir = path.Count >= 2 ? path[1] - path[0] : Vector2Int.right;
        Vector2Int extendedStart = path[0] + firstDir;

        // 终点多画一格
        Vector2Int lastDir = path.Count >= 2 ? path[path.Count - 1] - path[path.Count - 2] : Vector2Int.left;
        Vector2Int extendedEnd = path[path.Count - 1] + lastDir;

        var allPoints = new List<Vector3>();

        // 起点多一格
        allPoints.Add(new Vector3(
            (extendedStart.x - bounds.xMin) * scale + offsetX,
            (extendedStart.y - bounds.yMin) * scale + offsetY, 0));

        // 走廊路径本身
        for (int i = 0; i < path.Count; i++)
        {
            allPoints.Add(new Vector3(
                (path[i].x - bounds.xMin) * scale + offsetX,
                (path[i].y - bounds.yMin) * scale + offsetY, 0));
        }

        // 终点多一格
        allPoints.Add(new Vector3(
            (extendedEnd.x - bounds.xMin) * scale + offsetX,
            (extendedEnd.y - bounds.yMin) * scale + offsetY, 0));

        // 用 Image 组件画线段（线细一些）
        for (int i = 0; i < allPoints.Count - 1; i++)
        {
            CreateCorridorSegment(allPoints[i], allPoints[i + 1], scale * 0.4f);
        }
    }

    private void CreateCorridorSegment(Vector3 from, Vector3 to, float corridorWidth)
    {
        var seg = new GameObject("CorridorSeg");
        seg.transform.SetParent(_mapContainer);
        var image = seg.AddComponent<Image>();
        image.color = corridorColor;

        Vector3 dir = to - from;
        float length = dir.magnitude;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        var rect = seg.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(0, 0);
        rect.pivot = new Vector2(0, 0.5f);
        rect.anchoredPosition = from;
        rect.sizeDelta = new Vector2(length, corridorWidth);
        rect.localEulerAngles = new Vector3(0, 0, angle);
    }

    /// <summary>
    /// 将门也当作走廊，画相邻房间之间的连接线
    /// </summary>
    private void CreateDoorConnections(DungeonGraph graph, DungeonTileData tileData, RectInt bounds, float scale, float offsetX, float offsetY)
    {
        if (tileData == null) return;

        // 已连接的房间对（避免重复画线）
        var connectedPairs = new HashSet<string>();

        foreach (var room in graph.allRooms)
        {
            if (!tileData.TryGetRoomDoorTiles(room.id, out var doorTiles))
                continue;

            // 检查每个门是否与相邻房间的门相邻
            foreach (var doorPos in doorTiles)
            {
                // 检查4个方向相邻的瓦片
                foreach (var dir in new Vector2Int[] { new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 0), new Vector2Int(0, -1) })
                {
                    var adjacentPos = doorPos + dir;
                    int adjacentRoomId = tileData.GetRoomIdAt(adjacentPos);

                    // 跳过同房间和无效房间
                    if (adjacentRoomId < 0 || adjacentRoomId == room.id)
                        continue;

                    // 确保只画一次（房间对唯一）
                    string pairKey = adjacentRoomId < room.id
                        ? $"{adjacentRoomId}_{room.id}"
                        : $"{room.id}_{adjacentRoomId}";

                    if (connectedPairs.Contains(pairKey))
                        continue;
                    connectedPairs.Add(pairKey);

                    // 计算两个门的屏幕坐标并画线
                    Vector3 from = new Vector3(
                        (doorPos.x - bounds.xMin) * scale + offsetX,
                        (doorPos.y - bounds.yMin) * scale + offsetY, 0);
                    Vector3 to = new Vector3(
                        (adjacentPos.x - bounds.xMin) * scale + offsetX,
                        (adjacentPos.y - bounds.yMin) * scale + offsetY, 0);

                    CreateCorridorSegment(from, to, scale * 0.4f);
                }
            }
        }
    }

    private void OnRoomEnter(RoomEnterParams p)
    {
        _visitedRooms.Add(p.roomId);
        _currentRoomId = p.roomId;
        UpdateRoomVisited(p.roomId);
        UpdateCurrentHighlight(p.roomId);
    }

    private void OnRoomCleared(int roomId)
    {
        // 房间清理后的状态变化（可选）
    }

    private void UpdateRoomVisited(int roomId)
    {
        if (_roomIcons.TryGetValue(roomId, out var image))
        {
            image.color = visitedColor;
        }
    }

    private void UpdateCurrentHighlight(int roomId)
    {
        // 停止上一个闪烁协程
        if (_blinkCoroutine != null)
        {
            StopCoroutine(_blinkCoroutine);
            _blinkCoroutine = null;
        }

        // 遍历所有房间，重置高亮
        foreach (var kvp in _roomIcons)
        {
            kvp.Value.color = _visitedRooms.Contains(kvp.Key)
                ? visitedColor : unvisitedColor;
        }

        // 设置当前房间闪烁
        if (_roomIcons.TryGetValue(roomId, out var current))
        {
            current.color = currentRoomColor;
            _blinkCoroutine = StartCoroutine(BlinkRoom(current));
        }
    }

    private System.Collections.IEnumerator BlinkRoom(Image roomImage)
    {
        float blinkInterval = 1f / blinkSpeed;
        bool visible = true;

        while (true)
        {
            yield return new WaitForSeconds(blinkInterval);

            visible = !visible;
            float alpha = visible ? 1f : 0.3f;
            roomImage.color = new Color(currentRoomColor.r, currentRoomColor.g, currentRoomColor.b, alpha);
        }
    }
}
