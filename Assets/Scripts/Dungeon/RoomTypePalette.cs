using UnityEngine;

namespace Roguelike.Dungeon
{
    /// <summary>
    /// 房间类型的调试配色。
    ///
    /// 颜色与编辑器里「房间图生成器」的图例保持一致，
    /// 这样在窗口里看到的地图和场景里的地板颜色能直接对应上。
    /// </summary>
    public static class RoomTypePalette
    {
        /// <summary>战斗房（默认类型）的地板基色。</summary>
        public static readonly Color FloorBase = new Color(0.22f, 0.23f, 0.28f, 1f);

        /// <summary>房间类型的代表色，用于图例、中心标记和地板染色。</summary>
        public static Color Accent(RoomType type)
        {
            switch (type)
            {
                case RoomType.Start: return new Color(0.20f, 0.72f, 0.36f);
                case RoomType.Boss: return new Color(0.82f, 0.22f, 0.24f);
                case RoomType.Treasure: return new Color(0.93f, 0.72f, 0.16f);
                case RoomType.Shop: return new Color(0.20f, 0.60f, 0.85f);
                case RoomType.Secret: return new Color(0.60f, 0.36f, 0.80f);
                default: return new Color(0.45f, 0.47f, 0.55f);
            }
        }

        /// <summary>特殊房间（非普通战斗房）才需要额外的视觉标记。</summary>
        public static bool IsSpecial(RoomType type) => type != RoomType.Combat;

        /// <summary>地板染色：普通房保持基色，特殊房间向代表色混合。</summary>
        public static Color FloorTint(RoomType type, Color baseColor)
        {
            if (!IsSpecial(type)) return baseColor;
            return Color.Lerp(baseColor, Accent(type), 0.42f);
        }
    }
}
