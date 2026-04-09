# ProcGen 模块使用文档

> 地牢程序化生成模块 | Unity 2022.3 LTS | 命名空间 `ProcGen.*`

---

## 1. 模块概览

ProcGen 负责程序化生成 2D 俯视角地牢地图，输出地牢图数据（`DungeonGraph`），由 `DungeonBuilder` 将其渲染为 Unity Tilemap。

### 目录结构

```
Assets/Scripts/Dungeon/
├── Config/                       — 配置数据结构
│   ├── DungeonModel_SO.cs        — 地牢总配置
│   ├── RoomTemplateConfig_SO.cs   — 房间模板配置
│   └── RoomConfigData.cs          — 单条房间配置（类）
├── Core/                         — 核心数据模型
│   ├── RoomType.cs               — 房间类型枚举
│   ├── Room.cs                   — 房间数据
│   ├── Corridor.cs                — 走廊数据
│   └── DungeonGraph.cs            — 地牢图（格子查询API）
├── Seed/                         — 种子系统
│   ├── GameRandom.cs              — 种子随机数封装
│   └── ISpawnable.cs             — 可接受随机数生成器的接口
├── Generator/                    — 生成算法
│   ├── IDungeonGenerator.cs       — 生成器接口
│   └── RoomFirstGenerator.cs      — Room-First + MST 算法实现
├── Runtime/                       — 运行时
│   └── DungeonBuilder.cs          — Tilemap 场景构建器
└── Editor/                        — 编辑器扩展
    └── DungeonConfigEditorWindow.cs — 地牢配置编辑器窗口
```

### 核心类型一览

| 类型 | 命名空间 | 用途 |
|------|---------|------|
| `DungeonModel_SO` | `ProcGen.Config` | 地牢生成总配置（地图尺寸/房间数量/走廊宽度） |
| `RoomTemplateConfig_SO` | `ProcGen.Config` | 房间模板配置（持有所有 `RoomConfigData`） |
| `RoomConfigData` | `ProcGen.Config` | 可序列化类，单条房间尺寸约束，含默认值 |
| `GameRandom` | `ProcGen.Seed` | 种子随机数封装，确保生成可复现 |
| `IDungeonGenerator` | `ProcGen.Generator` | 生成器接口（可替换算法） |
| `RoomFirstGenerator` | `ProcGen.Generator` | 当前实现：Room-First + MST 算法 |
| `DungeonGraph` | `ProcGen.Core` | 地牢图结构，包含所有房间/走廊/格子查询API |
| `Room` | `ProcGen.Core` | 房间数据（位置/尺寸/类型/相连房间） |
| `Corridor` | `ProcGen.Core` | 走廊数据（路径格/包围盒） |
| `RoomType` | `ProcGen.Core` | 房间类型枚举（9种） |
| `DungeonBuilder` | `ProcGen.Runtime` | 将 `DungeonGraph` 实例化为 Unity Tilemap |
| `ISpawnable` | `ProcGen.Seed` | 可接受随机数生成器的接口（用于房间内容生成） |
| `DungeonConfigEditorWindow` | `ProcGen.Editor` | 地牢配置编辑器窗口（面板管理所有 SO） |

---

## 2. 编辑器配置工具

### DungeonConfigEditorWindow

打开：**KikoSurge → Dungeon → 地牢配置编辑器**

在单一面板中管理所有配置 SO，支持创建、编辑、批量操作：

```
┌──────────────────────────────────────────────────────────────┐
│  [地牢配置 (DungeonModel)]  [房间模板 (RoomTemplate)]        │
├───────────────┬──────────────────────────────────────────────┤
│  资源列表      │  编辑：DungeonModel_Default                  │
│  [+ 新建][↺]  │                                              │
│               │  ▸ 地图尺寸                                  │
│  ▸ Dungeon1   │    地图宽度 / 高度 / 边界留空                │
│    Dungeon2   │  ▸ 房间模板配置                              │
│  ▸ RoomTemp1  │    [拖拽 RoomTemplateConfig_SO]              │
│    RoomTemp2  │  ▸ 走廊 / 普通房间 / 特殊房间               │
│               │    精英房: 2个  30%额外概率                 │
│               │    宝藏间: 3个   0%额外概率                 │
│               │    商店:   1个   0%额外概率                 │
│               │    休息室: 1个   0%额外概率                 │
│               │    事件房: 1个   0%额外概率                 │
│               │    Boss:  1个   0%额外概率                  │
└───────────────┴──────────────────────────────────────────────┘
```

RoomTemplate 视图中每个模板可折叠/展开，支持新增、删除、批量折叠/展开。

---

## 3. 配置方法

### DungeonModel_SO（地牢总配置）

创建：`右键 → Create → KikoSurge/Dungeon/地牢配置`

| 字段 | 说明 |
|------|------|
| `mapWidth` / `mapHeight` | 地图尺寸（格） |
| `borderSize` | 边界留空（格） |
| `roomTemplateConfig` | 引用 `RoomTemplateConfig_SO` |
| `corridorWidth` | 走廊宽度（格） |
| `normalRoomCount` | 保证生成的普通房间数 |
| `normalExtraChance` | 额外生成普通房间的概率（0-100） |
| `*RoomCount` | 各特殊房间保证数量 |
| `*ExtraChance` | 各特殊房间额外生成概率 |

### RoomTemplateConfig_SO（房间模板配置）

创建：`右键 → Create → KikoSurge/Dungeon/房间模板配置`

持有所有 `RoomConfigData` 列表，每条定义一种尺寸约束。

### RoomConfigData（单条房间配置）

| 字段 | 默认值 | 说明 |
|------|--------|------|
| `displayName` | `""` | 显示名称（用于区分同名类型的多个配置） |
| `roomType` | `Normal` | 房间类型 |
| `minSize` | `(5, 5)` | 最小尺寸（格） |
| `maxSize` | `(10, 10)` | 最大尺寸（格） |
| `minDistFromStart` | `0` | 距起点房间中心的最小曼哈顿距离（格）。`0` = 不限制；`>=1` = 至少该距离 |
| `maxDistFromStart` | `-1` | 距起点房间中心的最大曼哈顿距离（格）。`-1` = 不限制；`0` = 必须在起点处 |

距离约束说明：所有约束基于**曼哈顿距离**，计算起点房间与候选房间中心之间的距离。

### RoomType 枚举

```
Start    — 起始房间（玩家出生点）
Normal   — 普通房间
Goal     — 终点房间（MST 最远点自动指定）
Treasure — 宝藏间
Shop     — 商店
Elite    — 精英房
Rest     — 休息室
Event    — 事件房
Boss     — Boss房
```

---

## 4. 随机数机制

### GameRandom

所有随机操作均通过 `GameRandom`，确保同一种子产生完全相同的地牢。

```csharp
// 从字符串创建（用户可分享）
var rng = new GameRandom("MyMap1");

// 从数值创建（从存档恢复）
var rng = new GameRandom(123456789L);

// 生成随机数
int value = rng.Range(0, 100);    // [0, 100)
float ratio = rng.Value();        // [0, 1)
bool success = rng.RollChance(30); // 30% 概率

// 从列表随机选取（自动 shuffle）
rng.Shuffle(roomConfigs);         // Fisher-Yates 洗牌

// 重置到种子初始状态（调试/确定性重放用，正常游戏不要调用）
rng.Reset();
```

### ISpawnable 接口

房间内的可生成内容（敌人刷新、奖励掉落等）实现此接口：

```csharp
public class EnemySpawner : MonoBehaviour, ISpawnable
{
    public void SetRng(GameRandom rng)
    {
        // 用随机数驱动内部生成，确保同一种子下产生相同敌人配置
    }
}
```

---

## 5. 生成流程（RoomFirstGenerator）

算法流程分 7 步：

```
Step 1  放置起点房间（Start）
Step 2  生成特殊房间（Treasure/Shop/Rest/Event/Boss/Elite）
Step 3  生成普通房间（Normal）
Step 4  MST 连通所有房间（Prim 算法，保证连通且无环）
Step 5  确定终点 + Elite 概率补充
Step 6  生成走廊（L 型走廊，仅铺在空地上）
Step 7  构建并返回 DungeonGraph
```

### 特殊房间放置策略

每个特殊类型使用**保证数量 + 概率额外**的混合模式：

- **保证数量**：`treasureRoomCount` 等字段，确保最少生成 N 个
- **概率额外**：对应 `*ExtraChance` 字段，额外生成一个的概率
- **距离约束**：`minDistFromStart` / `maxDistFromStart` 控制房间与起点的曼哈顿距离范围
- **多配置支持**：同一种 RoomType 可以有多条 `RoomConfigData`，生成时由种子随机选择

### 地图自动扩容

当某个房间在当前最大尝试次数内无法放置时，触发**地图扩容**：

- 地图宽高扩大 1.5 倍
- 已放置房间按比例重新缩放位置
- 扩容阈值由 `DUNGEON_EXPANSION_THRESHOLD`（`GameConstants`）控制

### 走廊生成规则

- **起点/终点**：使用房间边界点（`GetClosestEdgePoint`），而非中心
- **只走空地**：`AddCorridorTileIfFree` 排除已有房间区域
- **走廊宽度**：`corridorWidth` 控制走廊宽度（默认 2 格）
- **包围盒**：`Corridor.bounds` 存储走廊包围盒（RectInt）

---

## 6. DungeonGraph API

地牢图数据容器，提供格子级别查询。

### 地面格查询

```csharp
// 获取所有地面格（房间 + 走廊）
HashSet<Vector2Int> all = graph.GetAllFloorTiles();

// 获取所有房间地面（不含走廊）
HashSet<Vector2Int> rooms = graph.GetAllRoomFloorTiles();

// 获取指定类型房间的地面
HashSet<Vector2Int> treasures = graph.GetRoomFloorTilesByType(RoomType.Treasure);

// 获取走廊地面（不含房间）
HashSet<Vector2Int> corridors = graph.GetCorridorFloorTiles();
```

### 墙壁格查询

**墙壁归属规则**：房间与走廊墙壁相交的部分归房间所有。

```csharp
// 获取所有墙壁
HashSet<Vector2Int> all = graph.GetAllWallTiles();

// 获取指定类型房间的墙壁（排除走廊相邻墙壁）
HashSet<Vector2Int> startWalls = graph.GetRoomWallTilesByType(RoomType.Start);

// 获取走廊墙壁（= 所有墙壁 - 房间墙壁）
HashSet<Vector2Int> corridorWalls = graph.GetCorridorWallTiles();
```

墙壁计算使用 8 方向（4 方向 + 4 对角），`GetWallTilesAround` 为共享工具方法。

### 房间查询

```csharp
// 根据ID获取房间
Room room = graph.GetRoom(0);

// 起点/终点房间ID
int startId = graph.startRoomId;
int goalId = graph.goalRoomId;

// 所有房间
List<Room> allRooms = graph.allRooms;
```

### Corridor 数据结构

```csharp
public class Corridor
{
    public int roomAId;              // 走廊一端连接的房间ID
    public int roomBId;              // 走廊另一端连接的房间ID
    public List<Vector2Int> pathTiles; // 走廊经过的网格坐标（只含空地，不含房间）
    public RectInt bounds;           // 走廊包围盒（左下角x,y + 宽高）
}
```

---

## 7. Tilemap 渲染对应关系

`DungeonBuilder` 将 `DungeonGraph` 中的格子数据映射到 Unity Tilemap：

| DungeonGraph 方法 | Tilemap 层 | Inspector 引用 |
|-----------------|----------|--------------|
| `GetRoomFloorTilesByType(RoomType.Normal)` | 地面层 | `_floorTile` |
| `GetRoomFloorTilesByType(RoomType.Start)` | 地面层 | `_floorStartTile` |
| `GetRoomFloorTilesByType(RoomType.Goal)` | 地面层 | `_floorGoalTile` |
| `GetAllWallTiles()` | 墙壁层 | `_wallTile` |

Inspector 配置项：

```
DungeonBuilder
├── Tilemap 引用
│   ├── _floorTilemap  — 地面层 Tilemap
│   └── _wallTilemap   — 墙壁层 Tilemap
├── 瓦片资源
│   ├── _floorTile       — 普通地面瓦片
│   ├── _floorStartTile  — 起点地面瓦片
│   ├── _floorGoalTile   — 终点地面瓦片
│   └── _wallTile        — 墙壁瓦片
└── 生成配置
    └── _dungeonModel    — DungeonModel_SO
```

---

## 8. 协作者

- 框架入口：`App.GameManager`
- 事件通信：`EventCenter`（发布 `RoomEntered` / `CorridorEntered` 等事件）
- 对象池：`PoolManager`（生成敌人/道具时使用）
