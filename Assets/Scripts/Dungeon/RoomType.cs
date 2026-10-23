using System;
using UnityEngine;

namespace Roguelike.Dungeon
{
    /// <summary>房间的玩法类型。</summary>
    public enum RoomType
    {
        Start,    // 出生房
        Combat,   // 普通战斗房（默认）
        Boss,     // Boss 房，位于主路径末端
        Treasure, // 宝箱房
        Shop,     // 商店
        Secret,   // 秘密房
    }

    /// <summary>门的方向位掩码，一个房间可用按位或表示多扇门。</summary>
    [Flags]
    public enum DoorDirections
    {
        None = 0,
        Up = 1 << 0,
        Right = 1 << 1,
        Down = 1 << 2,
        Left = 1 << 3,
    }

    /// <summary>方向相关的工具方法。方向顺序固定为 上 / 右 / 下 / 左。</summary>
    public static class DirUtil
    {
        public static readonly Vector2Int[] All =
        {
            new Vector2Int(0, 1),
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0),
        };

        public static readonly DoorDirections[] AllDoors =
        {
            DoorDirections.Up,
            DoorDirections.Right,
            DoorDirections.Down,
            DoorDirections.Left,
        };

        /// <summary>两个相邻格子之间的方向；不相邻返回 None。</summary>
        public static DoorDirections Between(Vector2Int from, Vector2Int to)
        {
            Vector2Int d = to - from;
            if (d.x == 1 && d.y == 0) return DoorDirections.Right;
            if (d.x == -1 && d.y == 0) return DoorDirections.Left;
            if (d.x == 0 && d.y == 1) return DoorDirections.Up;
            if (d.x == 0 && d.y == -1) return DoorDirections.Down;
            return DoorDirections.None;
        }

        public static Vector2Int ToOffset(DoorDirections dir)
        {
            switch (dir)
            {
                case DoorDirections.Up: return new Vector2Int(0, 1);
                case DoorDirections.Right: return new Vector2Int(1, 0);
                case DoorDirections.Down: return new Vector2Int(0, -1);
                case DoorDirections.Left: return new Vector2Int(-1, 0);
                default: return Vector2Int.zero;
            }
        }

        /// <summary>人类可读的门位，例如 "U D"。</summary>
        public static string Describe(DoorDirections doors)
        {
            if (doors == DoorDirections.None) return "无";
            var parts = new System.Collections.Generic.List<string>(4);
            if ((doors & DoorDirections.Up) != 0) parts.Add("上");
            if ((doors & DoorDirections.Right) != 0) parts.Add("右");
            if ((doors & DoorDirections.Down) != 0) parts.Add("下");
            if ((doors & DoorDirections.Left) != 0) parts.Add("左");
            return string.Join(" ", parts);
        }
    }
}
