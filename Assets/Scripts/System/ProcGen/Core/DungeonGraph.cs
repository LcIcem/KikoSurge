using System.Collections.Generic;
using UnityEngine;

namespace ProcGen.Core
{
    /// <summary>地牢图结构：包含所有房间和走廊的数据集合</summary>
    public class DungeonGraph
    {
        private static readonly Vector2Int[] _wallDirections = new Vector2Int[]
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
            new Vector2Int(1, 1),
            new Vector2Int(-1, 1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, -1)
        };

        public List<Room> allRooms;    // 所有房间列表
        public List<Corridor> corridors; // 所有走廊列表
        public int startRoomId;        // 起始房间ID
        public int goalRoomId;         // 终点房间ID

        public DungeonGraph()
        {
            allRooms = new List<Room>();
            corridors = new List<Corridor>();
        }

        /// <summary>根据ID获取对应房间</summary>
        public Room GetRoom(int id)
        {
            return allRooms.Find(r => r.id == id);
        }

        /// <summary>获取所有地面网格坐标（房间+走廊）</summary>
        public HashSet<Vector2Int> GetAllFloorTiles()
        {
            var tiles = new HashSet<Vector2Int>();

            // 收集所有房间地面
            foreach (var room in allRooms)
            {
                for (int x = room.gridPos.x; x < room.gridPos.x + room.size.x; x++)
                {
                    for (int y = room.gridPos.y; y < room.gridPos.y + room.size.y; y++)
                    {
                        tiles.Add(new Vector2Int(x, y));
                    }
                }
            }

            // 收集所有走廊地面
            foreach (var corridor in corridors)
            {
                foreach (var tile in corridor.pathTiles)
                {
                    tiles.Add(tile);
                }
            }

            return tiles;
        }

        /// <summary>获取所有墙壁网格坐标（地面周围一圈）</summary>
        public HashSet<Vector2Int> GetAllWallTiles()
        {
            var floorTiles = GetAllFloorTiles();
            return GetWallTilesAround(floorTiles);
        }

        /// <summary>获取所有房间的地面网格坐标（不含走廊）</summary>
        public HashSet<Vector2Int> GetAllRoomFloorTiles()
        {
            var tiles = new HashSet<Vector2Int>();
            foreach (var room in allRooms)
            {
                for (int x = room.gridPos.x; x < room.gridPos.x + room.size.x; x++)
                {
                    for (int y = room.gridPos.y; y < room.gridPos.y + room.size.y; y++)
                    {
                        tiles.Add(new Vector2Int(x, y));
                    }
                }
            }
            return tiles;
        }

        /// <summary>获取指定类型房间的地面网格坐标（不含走廊）</summary>
        public HashSet<Vector2Int> GetRoomFloorTilesByType(RoomType type)
        {
            var tiles = new HashSet<Vector2Int>();

            foreach (var room in allRooms)
            {
                if (room.roomType != type)
                    continue;

                for (int x = room.gridPos.x; x < room.gridPos.x + room.size.x; x++)
                {
                    for (int y = room.gridPos.y; y < room.gridPos.y + room.size.y; y++)
                    {
                        tiles.Add(new Vector2Int(x, y));
                    }
                }
            }

            return tiles;
        }

        /// <summary>获取指定类型房间的墙壁网格坐标（仅围绕该类型房间，排除与走廊相邻的墙壁）</summary>
        public HashSet<Vector2Int> GetRoomWallTilesByType(RoomType type)
        {
            var roomFloors = GetRoomFloorTilesByType(type);
            var roomWalls = GetWallTilesAround(roomFloors);
            return ExcludeWallsAdjacentTo(roomWalls, GetCorridorFloorTiles());
        }

        /// <summary>获取走廊地面网格坐标（不含房间）</summary>
        public HashSet<Vector2Int> GetCorridorFloorTiles()
        {
            var tiles = new HashSet<Vector2Int>();
            foreach (var corridor in corridors)
            {
                foreach (var tile in corridor.pathTiles)
                {
                    tiles.Add(tile);
                }
            }
            return tiles;
        }

        /// <summary>获取走廊墙壁网格坐标（走廊周围墙壁，重叠部分归房间）</summary>
        public HashSet<Vector2Int> GetCorridorWallTiles()
        {
            var allWalls = GetAllWallTiles();
            var roomFloors = GetAllRoomFloorTiles();
            var roomWalls = GetWallTilesAround(roomFloors);
            allWalls.ExceptWith(roomWalls);
            return allWalls;
        }


        /// <summary>从墙壁集合中排除与指定地面相邻的墙壁（工具方法）</summary>
        private HashSet<Vector2Int> ExcludeWallsAdjacentTo(HashSet<Vector2Int> walls, HashSet<Vector2Int> floorTiles)
        {
            var adjacentToFloor = new HashSet<Vector2Int>();
            foreach (var tile in floorTiles)
            {
                foreach (var dir in _wallDirections)
                {
                    adjacentToFloor.Add(tile + dir);
                }
            }

            walls.ExceptWith(adjacentToFloor);
            return walls;
        }

        /// <summary>计算给定地面格子集合周围的墙壁（工具方法）</summary>
        private HashSet<Vector2Int> GetWallTilesAround(HashSet<Vector2Int> floorTiles)
        {
            var wallTiles = new HashSet<Vector2Int>();

            foreach (var tile in floorTiles)
            {
                foreach (var dir in _wallDirections)
                {
                    var neighbor = tile + dir;
                    if (!floorTiles.Contains(neighbor))
                    {
                        wallTiles.Add(neighbor);
                    }
                }
            }

            return wallTiles;
        }
    }
}
