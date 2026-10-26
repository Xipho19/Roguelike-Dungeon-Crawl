using System.Collections.Generic;
using UnityEngine;

namespace Roguelike.Dungeon
{
    /// <summary>
    /// 房间几何规格（单位：世界单位，1 单位 = 1 瓦片）。
    ///
    /// 房间外框尺寸可变，且不小于原来的 15 × 9：
    /// 宽度取自 {15,17,19}，高度取自 {9,11}。
    /// 为了让相邻房间继续共用一面 1 格厚的墙，同一行的房间高度相同、
    /// 同一列的房间宽度相同；这样墙长和居中的门洞都能精确对齐。
    ///
    /// </summary>
    public static class RoomGeometry
    {
        public const int FootprintWidth = 15;
        public const int FootprintHeight = 9;
        public const int InteriorWidth = FootprintWidth - 2;   // 13
        public const int InteriorHeight = FootprintHeight - 2; // 7

        public const int MinFootprintWidth = FootprintWidth;
        public const int MinFootprintHeight = FootprintHeight;

        public const float WallThickness = 1f;
        public const float DoorWidth = 3f;

        public static readonly int[] AllowedWidths = { 15, 17, 19 };
        public static readonly int[] AllowedHeights = { 9, 11 };

        /// <summary>默认（15×9）相邻房间原点的间距 (14, 8)。仅作兼容/回退。</summary>
        public static readonly Vector2Int Spacing = new Vector2Int(FootprintWidth - 1, FootprintHeight - 1);

        /// <summary>默认（15×9）房间本地坐标下的中心点。</summary>
        public static readonly Vector2 Center = new Vector2(FootprintWidth * 0.5f, FootprintHeight * 0.5f);

        /// <summary>默认（15×9）敌人出生点（房间本地坐标）。</summary>
        public static readonly Vector2[] EnemySpawnPoints = GetEnemySpawnPoints(FootprintWidth, FootprintHeight);

        public static bool IsSupportedSize(int width, int height)
        {
            for (int i = 0; i < AllowedWidths.Length; i++)
                if (AllowedWidths[i] == width) return IsSupportedHeight(height);
            return false;
        }

        private static bool IsSupportedHeight(int height)
        {
            for (int i = 0; i < AllowedHeights.Length; i++)
                if (AllowedHeights[i] == height) return true;
            return false;
        }

        public static Vector2 GetCenter(int width, int height) => new Vector2(width * 0.5f, height * 0.5f);

        public static float GetInteriorWidth(int width) => width - WallThickness * 2f;
        public static float GetInteriorHeight(int height) => height - WallThickness * 2f;

        /// <summary>门标记的本地坐标（位于墙中心）。</summary>
        public static Vector2 DoorCenter(WallSide side) => DoorCenter(side, FootprintWidth, FootprintHeight);

        public static Vector2 DoorCenter(WallSide side, int width, int height)
        {
            float cx = width * 0.5f;
            float cy = height * 0.5f;

            switch (side)
            {
                case WallSide.Up: return new Vector2(cx, height - WallThickness * 0.5f);
                case WallSide.Right: return new Vector2(width - WallThickness * 0.5f, cy);
                case WallSide.Down: return new Vector2(cx, WallThickness * 0.5f);
                default: return new Vector2(WallThickness * 0.5f, cy);
            }
        }

        /// <summary>入口点：从门洞向房内退 1.5 格。</summary>
        public static Vector2 EntryPoint(WallSide side) => EntryPoint(side, FootprintWidth, FootprintHeight);

        public static Vector2 EntryPoint(WallSide side, int width, int height)
        {
            Vector2 door = DoorCenter(side, width, height);

            switch (side)
            {
                case WallSide.Up: return door + Vector2.down * 1.5f;
                case WallSide.Right: return door + Vector2.left * 1.5f;
                case WallSide.Down: return door + Vector2.up * 1.5f;
                default: return door + Vector2.right * 1.5f;
            }
        }

        /// <summary>
        /// 敌人出生点：随房间尺寸增加。
        /// 15×9 为原版四角；更宽/更高的房间额外加入上下或左右中点，
        /// 但不占用正中心（中央区域留给布局/祭坛）。
        /// </summary>
        public static Vector2[] GetEnemySpawnPoints(int width, int height)
        {
            var xs = new List<float> { 3.5f };
            if (width >= 17) xs.Add(width * 0.5f);
            xs.Add(width - 3.5f);

            var ys = new List<float> { 2f };
            if (height >= 11) ys.Add(height * 0.5f);
            ys.Add(height - 2f);

            var points = new List<Vector2>
            {
                new Vector2(xs[0], ys[0]),
                new Vector2(xs[xs.Count - 1], ys[0]),
                new Vector2(xs[0], ys[ys.Count - 1]),
                new Vector2(xs[xs.Count - 1], ys[ys.Count - 1]),
            };

            if (xs.Count == 3)
            {
                float midX = xs[1];
                points.Add(new Vector2(midX, ys[0]));
                points.Add(new Vector2(midX, ys[ys.Count - 1]));
            }

            if (ys.Count == 3)
            {
                float midY = ys[1];
                points.Add(new Vector2(xs[0], midY));
                points.Add(new Vector2(xs[xs.Count - 1], midY));
            }

            return points.ToArray();
        }

        /// <summary>
        /// 一面墙的三段几何：A 段 / 门段 / B 段。
        /// 门段有门时隐藏，因此这里只描述位置和尺寸。
        /// </summary>
        public static WallSegment[] GetWallSegments(WallSide side) =>
            GetWallSegments(side, FootprintWidth, FootprintHeight);

        public static WallSegment[] GetWallSegments(WallSide side, int width, int height)
        {
            switch (side)
            {
                case WallSide.Up: return BuildHorizontal(height - WallThickness * 0.5f, width);
                case WallSide.Down: return BuildHorizontal(WallThickness * 0.5f, width);
                case WallSide.Right: return BuildVertical(width - WallThickness * 0.5f, height);
                default: return BuildVertical(WallThickness * 0.5f, height);
            }
        }

        /// <summary>Prefab 资源名约定：Room_{width}x{height}。</summary>
        public static string PrefabStem(int width, int height) => $"Room_{width}x{height}";

        private static WallSegment[] BuildHorizontal(float y, int width)
        {
            float sideLength = (width - DoorWidth) * 0.5f;
            return new[]
            {
                new WallSegment(new Vector2(sideLength * 0.5f, y), new Vector2(sideLength, WallThickness)),
                new WallSegment(new Vector2(width * 0.5f, y), new Vector2(DoorWidth, WallThickness)),
                new WallSegment(new Vector2(width - sideLength * 0.5f, y), new Vector2(sideLength, WallThickness)),
            };
        }

        private static WallSegment[] BuildVertical(float x, int height)
        {
            float top = height - WallThickness;
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
