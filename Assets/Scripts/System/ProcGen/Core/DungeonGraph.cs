using System.Collections.Generic;
using UnityEngine;

namespace ProcGen.Core
{
    /// <summary>
    /// 地牢图结构：纯图数据（房间/走廊/连接关系）。
    /// 所有瓦片相关数据由 DungeonTileData 提供。
    /// </summary>
    public class DungeonGraph
    {
        public List<Room> allRooms;        // 所有房间列表
        public List<Corridor> corridors;   // 所有走廊列表
        public int startRoomId;           // 起始房间ID
        public int goalRoomId;            // 终点房间ID

        /// <summary>生成时实际使用的地图范围（可能因 ExpandMap 扩容而大于 config 原始值）</summary>
        public RectInt mapBounds { get; internal set; }


        // O(1) 按 ID 查找
        private Dictionary<int, Room> _roomById;
        private Dictionary<int, Corridor> _corridorById;

        public DungeonGraph()
        {
            allRooms = new List<Room>();
            corridors = new List<Corridor>();
            _roomById = new Dictionary<int, Room>();
            _corridorById = new Dictionary<int, Corridor>();
        }

        /// <summary>
        /// 构建 O(1) 查找字典（所有房间/走廊添加完毕后调用一次）
        /// </summary>
        public void BuildIndexes()
        {
            _roomById.Clear();
            _corridorById.Clear();
            foreach (var r in allRooms)    _roomById[r.id] = r;
            foreach (var c in corridors)  _corridorById[c.id] = c;
        }

        /// <summary>O(1) 根据ID获取房间</summary>
        public Room GetRoom(int id) => _roomById.GetValueOrDefault(id, null);

        /// <summary>O(1) 根据ID获取走廊</summary>
        public Corridor GetCorridor(int id) => _corridorById.GetValueOrDefault(id, null);
    }
}
