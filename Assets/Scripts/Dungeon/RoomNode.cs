using System.Collections.Generic;
using UnityEngine;

namespace Roguelike.Dungeon
{
    /// <summary>
    /// 房间图中的一个节点。只描述"这格是什么房间、连着谁"，不含场景对象。
    /// 后续 DungeonBuilder 会按这些数据实例化 Room Prefab。
    /// </summary>
    public sealed class RoomNode
    {
        /// <summary>房间在生成网格中的整数坐标。</summary>
        public Vector2Int Grid { get; internal set; }

        /// <summary>房间类型，默认普通战斗房。</summary>
        public RoomType Type { get; internal set; } = RoomType.Combat;

        /// <summary>由相邻关系推导出的门位，生成结束前为 None。</summary>
        public DoorDirections Doors { get; internal set; }

        /// <summary>到起点房间的最短步数，BFS 计算；-1 表示尚未计算。</summary>
        public int DistanceFromStart { get; internal set; } = -1;

        /// <summary>房间外框宽度（含墙），由尺寸分配 pass 写入。</summary>
        public int Width { get; internal set; } = RoomGeometry.FootprintWidth;

        /// <summary>房间外框高度（含墙），由尺寸分配 pass 写入。</summary>
        public int Height { get; internal set; } = RoomGeometry.FootprintHeight;

        /// <summary>房间外框左下角在地牢本地坐标中的位置。</summary>
        public Vector2 WorldOrigin { get; internal set; }

        /// <summary>房间外框（地牢本地坐标）。</summary>
        public Rect WorldRect => new Rect(WorldOrigin, new Vector2(Width, Height));

        /// <summary>所有直接相邻且连通的房间。</summary>
        public List<RoomNode> Neighbors { get; } = new List<RoomNode>(4);

        public RoomNode(Vector2Int grid)
        {
            Grid = grid;
        }

        public int Degree => Neighbors.Count;

        /// <summary>度为 1 即为死胡同（起点只有一扇门时也算，调用方需自行排除起点）。</summary>
        public bool IsDeadEnd => Neighbors.Count == 1;

        public bool IsConnectedTo(RoomNode other) => other != null && Neighbors.Contains(other);

        public override string ToString() => $"{Type}@{Grid}";
    }
}
