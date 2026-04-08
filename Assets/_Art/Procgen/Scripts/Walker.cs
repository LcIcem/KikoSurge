using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

public class Walker : MonoBehaviour
{
    [Header("Tilemap引用")]
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap backgroundTilemap;

    [Header("Tile引用")]
    [SerializeField] private TileBase floorTile;
    [SerializeField] private TileBase backgroundTile;

    [Header("生成参数")]
    [SerializeField] private int maxIterations = 100;   // 最大迭代次数
    [SerializeField] private int walkersCount = 1;      // 要使用的walker数量
    [SerializeField][Range(0f, 1f)] private float directionChangeChance = 1f;       // 方向改变的几率
    [SerializeField] private Vector2Int startPositionOffset = new Vector2Int(0, 0); // 起始位置偏移
    [SerializeField] private Vector2Int customGridSize = new Vector2Int(20, 20);    // 自定义网格尺寸 定义关卡的尺寸
    [SerializeField] private int borderSize = 3;
    [SerializeField] private PlayerHandler playerHandler;

    // Grid 和 Walker 管理变量
    private bool[,] _visitedTiles;  // 记录已访问的瓦片
    private Vector2Int _gridSize;
    private Vector2Int _gridMin;     // grid的左下角
    private Vector2Int _gridMax;     // grid的右上角
    private Vector2Int _walkerMin;   // 可通行区域的左下角（内部边界）
    private Vector2Int _walkerMax;   // 可通行区域的右上角（内部边界）

    private PlayerInput _playerInput;


    private void Start()
    {
        CalculateGridBoudns();
        GenerateLevel();
    }
    
    // 监听Onrestart输入事件
    public void OnRestart(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            CalculateGridBoudns();
            GenerateLevel();
        }
    }

    // 根据customGridSize和borderSize 计算 grid 和 Walker 的边界
    private void CalculateGridBoudns()
    {
        // 确保 gridSize 能够包含 border
        _gridSize = new Vector2Int(
            Mathf.Max(2 * borderSize + 1, customGridSize.x),
            Mathf.Max(2 * borderSize + 1, customGridSize.y)
        );

        // 计算网格的左下角和右上角（以 (0,0) 为中心）
        _gridMin = new Vector2Int(-_gridSize.x / 2, -_gridSize.y / 2);
        _gridMax = new Vector2Int(_gridMin.x + _gridSize.x - 1, _gridMin.y + _gridSize.y - 1);

        // 根据 borderSize 定义可行走区域
        _walkerMin = new Vector2Int(_gridMin.x + borderSize, _gridMin.y + borderSize);
        _walkerMax = new Vector2Int(_gridMax.x - borderSize, _gridMax.y - borderSize);

        // 确保 walker 的边界合法
        _walkerMin.x = Mathf.Min(_walkerMin.x, _walkerMax.x);
        _walkerMin.y = Mathf.Min(_walkerMin.y, _walkerMax.y);
        _walkerMax.x = Mathf.Max(_walkerMin.x, _walkerMax.x);
        _walkerMax.y = Mathf.Max(_walkerMin.y, _walkerMax.y);
    }

    private void GenerateLevel()
    {
        // 清理瓦片地图
        groundTilemap.ClearAllTiles();
        backgroundTilemap.ClearAllTiles();

        // 初始化visited数组
        _visitedTiles = new bool[_gridSize.x, _gridSize.y];

        // 计算 walker 的初始位置
        Vector2Int startPos = new Vector2Int(
            _walkerMin.x + (_walkerMax.x - _walkerMin.x) / 2 + startPositionOffset.x,
            _walkerMin.y + (_walkerMax.y - _walkerMin.y) / 2 + startPositionOffset.y
        );

        // 限制起始位置的范围在可行走区域内
        startPos.x = Mathf.Clamp(startPos.x, _walkerMin.x, _walkerMax.x);
        startPos.y = Mathf.Clamp(startPos.y, _walkerMin.y, _walkerMax.y);

        // 为每一个walker运行RandomWalker算法
        for (int i = 0; i < walkersCount; i++)
        {
            RandomWalker(startPos);
        }

        // 填充瓦片地图
        FillTilemaps();
    }

    // 根据 visited数组 填充瓦片地图
    private void FillTilemaps()
    {
        for (int x = 0; x < _gridSize.x; x++)
        {
            for (int y = 0; y < _gridSize.y; y++)
            {
                // 将 数组坐标 映射为 tilemap 坐标
                Vector3Int tilePos = new Vector3Int(x + _gridMin.x, y + _gridMin.y, 0);
                if (_visitedTiles[x, y])
                {
                    // 将 floor 瓦片 填充到 ground 瓦片地图
                    groundTilemap.SetTile(tilePos, floorTile);
                }
                else
                {
                    backgroundTilemap.SetTile(tilePos, backgroundTile);
                }
            }
        }
    }

    // 从起始坐标开始随机行走
    private void RandomWalker(Vector2Int startPos)
    {
        Vector2Int currentPos = startPos;
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(0, 1),   // Up
            new Vector2Int(0, -1),  // Down
            new Vector2Int(1, 0),   // Right
            new Vector2Int(-1, 0)   // Left
        };
        Vector2Int currentDirection = directions[Random.Range(0, directions.Length)]; // 随机一个方向

        // 将起始位置标记为 visited
        if (IsWithinWalkerBounds(currentPos))
        {
            int indexX = currentPos.x - _gridMin.x;
            int indexY = currentPos.y - _gridMin.y;
            _visitedTiles[indexX, indexY] = true;
        }

        // 执行行走直到到达最大迭代次数
        for (int i = 0; i < maxIterations; i++)
        {
            // 根据 方向改变几率 改变随机到的方向
            if (Random.value < directionChangeChance)
            {
                currentDirection = directions[Random.Range(0, directions.Length)];
            }

            // 根据 方向 移动到下一个位置
            currentPos += currentDirection;

            // 将位置限制在 可行走边界
            currentPos.x = Mathf.Clamp(currentPos.x, _walkerMin.x, _walkerMax.x);
            currentPos.y = Mathf.Clamp(currentPos.y, _walkerMin.y, _walkerMax.y);

            // 标记当前位置为 visited
            int indexX = currentPos.x - _gridMin.x;
            int indexY = currentPos.y - _gridMin.y;
            if (!_visitedTiles[indexX, indexY])
            {
                _visitedTiles[indexX, indexY] = true;
            }
        }
    }

    // 检查位置是否在可行走边界内
    private bool IsWithinWalkerBounds(Vector2Int pos)
    {
        return pos.x >= _walkerMin.x && pos.x <= _walkerMax.x && pos.y >= _walkerMin.y && pos.y <= _walkerMax.y;
    }
}
