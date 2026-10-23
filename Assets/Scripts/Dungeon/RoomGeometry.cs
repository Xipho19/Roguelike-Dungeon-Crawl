using UnityEngine;

namespace Roguelike.Dungeon
{
    /// <summary>
    /// 房间的统一几何规格（单位：世界单位，1 单位 = 1 瓦片）。
    ///
    /// 布局（footprint 15 × 9）：
    ///   外墙厚度 1 格，内部可用区域 13 × 7；
    ///   四边门洞宽 3 格，位于每面墙正中。
    ///
    /// 相邻房间的间距取 Footprint - 1，使两间房共用同一面 1 格厚的墙，
    /// 既不会出现缝隙，也不会出现双墙。
    /// </summary>
    public static class RoomGeometry
    {
        public const int FootprintWidth = 15;
        public const int FootprintHeight = 9;
        public const int InteriorWidth = FootprintWidth - 2;   // 13
        public const int InteriorHeight = FootprintHeight - 2; // 7

        public const float WallThickness = 1f;
        public const float DoorWidth = 3f;

        /// <summary>相邻房间原点的间距（14, 8）。</summary>
        public static readonly Vector2Int Spacing = new Vector2Int(FootprintWidth - 1, FootprintHeight - 1);

        /// <summary>房间本地坐标下的中心点。</summary>
        public static readonly Vector2 Center = new Vector2(FootprintWidth * 0.5f, FootprintHeight * 0.5f);

        /// <summary>敌人出生点（房间本地坐标）。</summary>
        public static readonly Vector2[] EnemySpawnPoints =
        {
            new Vector2(3.5f, 2f),
            new Vector2(11.5f, 2f),
            new Vector2(3.5f, 7f),
            new Vector2(11.5f, 7f),
        };

        /// <summary>门标记的世界/本地坐标（位于墙中心）。</summary>
        public static Vector2 DoorCenter(WallSide side)
        {
            switch (side)
            {
                case WallSide.Up: return new Vector2(Center.x, FootprintHeight - WallThickness * 0.5f);
                case WallSide.Right: return new Vector2(FootprintWidth - WallThickness * 0.5f, Center.y);
                case WallSide.Down: return new Vector2(Center.x, WallThickness * 0.5f);
                default: return new Vector2(WallThickness * 0.5f, Center.y);
            }
        }

        /// <summary>入口点：从门洞向房内退 2 格，供后续玩家/相机定位使用。</summary>
        public static Vector2 EntryPoint(WallSide side)
        {
            Vector2 door = DoorCenter(side);
            switch (side)
            {
                case WallSide.Up: return door + Vector2.down * 1.5f;
                case WallSide.Right: return door + Vector2.left * 1.5f;
                case WallSide.Down: return door + Vector2.up * 1.5f;
                default: return door + Vector2.right * 1.5f;
            }
        }

        /// <summary>
        /// 一面墙的三段几何：A 段 / 门段 / B 段。
        /// 门段有门时隐藏，因此这里只描述位置和尺寸。
        /// </summary>
        public static WallSegment[] GetWallSegments(WallSide side)
        {
            switch (side)
            {
                case WallSide.Up: return BuildHorizontal(FootprintHeight - WallThickness * 0.5f);
                case WallSide.Down: return BuildHorizontal(WallThickness * 0.5f);
                case WallSide.Right: return BuildVertical(FootprintWidth - WallThickness * 0.5f);
                default: return BuildVertical(WallThickness * 0.5f);
            }
        }

        private static WallSegment[] BuildHorizontal(float y)
        {
            float sideLength = (FootprintWidth - DoorWidth) * 0.5f;
            return new[]
            {
                new WallSegment(new Vector2(sideLength * 0.5f, y), new Vector2(sideLength, WallThickness)),
                new WallSegment(new Vector2(FootprintWidth * 0.5f, y), new Vector2(DoorWidth, WallThickness)),
                new WallSegment(new Vector2(FootprintWidth - sideLength * 0.5f, y), new Vector2(sideLength, WallThickness)),
            };
        }

        private static WallSegment[] BuildVertical(float x)
        {
            float top = FootprintHeight - WallThickness;
            float bottom = WallThickness;
            float span = top - bottom;
            float sideLength = (span - DoorWidth) * 0.5f;
            float centerY = (top + bottom) * 0.5f;

            return new[]
            {
                new WallSegment(new Vector2(x, bottom + sideLength * 0.5f), new Vector2(WallThickness, sideLength)),
                new WallSegment(new Vector2(x, centerY), new Vector2(WallThickness, DoorWidth)),
                new WallSegment(new Vector2(x, top - sideLength * 0.5f), new Vector2(WallThickness, sideLength)),
            };
        }
    }

    /// <summary>墙体的一段：中心与尺寸，均在房间本地坐标下。</summary>
    public struct WallSegment
    {
        public readonly Vector2 Center;
        public readonly Vector2 Size;

        public WallSegment(Vector2 center, Vector2 size)
        {
            Center = center;
            Size = size;
        }
    }
}
