using Roguelike.Dungeon;
using UnityEditor;
using UnityEngine;

namespace Roguelike.EditorTools
{
    /// <summary>
    /// 房间图生成器的压力自检。
    /// 菜单：Tools / Roguelike / 生成器压力测试
    ///
    /// 每次改动生成算法后点一下，会跑 500 个种子并检查：
    ///   1. 结构校验无错误（连通性、起点/Boss 存在等）
    ///   2. 相同种子 + 相同配置必须得到完全相同的拓扑（可复现性）
    /// 结果输出到 Console。
    /// </summary>
    public static class DungeonSelfTest
    {
        private const int SeedCount = 500;

        [MenuItem("Tools/Roguelike/生成器压力测试 (500 seeds)")]
        public static void Run()
        {
            var config = new DungeonConfig();

            int failures = 0;
            int totalRooms = 0;
            int minRooms = int.MaxValue;
            int maxRooms = 0;
            int maxDist = 0;
            string firstFailure = null;

            for (int seed = 1; seed <= SeedCount; seed++)
            {
                DungeonData data = DungeonGenerator.Generate(config, seed);

                foreach (string issue in data.Validate())
                {
                    if (!issue.StartsWith("[错误]")) continue;
                    failures++;
                    if (firstFailure == null) firstFailure = $"seed {seed}: {issue}";
                }

                // 可复现性：同一 seed + 同一 config 必须生成同一张图
                DungeonData again = DungeonGenerator.Generate(config, seed);
                if (!SameTopology(data, again))
                {
                    failures++;
                    if (firstFailure == null) firstFailure = $"seed {seed}: 同种子结果不一致";
                }

                totalRooms += data.RoomCount;
                minRooms = Mathf.Min(minRooms, data.RoomCount);
                maxRooms = Mathf.Max(maxRooms, data.RoomCount);
                maxDist = Mathf.Max(maxDist, data.MaxDistance);
            }

            if (failures == 0)
            {
                Debug.Log($"[Dungeon] 压力测试通过：{SeedCount} 个种子，平均 {totalRooms / (float)SeedCount:F1} 房间，" +
                          $"范围 {minRooms}~{maxRooms}，最远 {maxDist} 步。");
            }
            else
            {
                Debug.LogError($"[Dungeon] 压力测试失败 {failures} 项，首个问题：{firstFailure}");
            }
        }

        private static bool SameTopology(DungeonData a, DungeonData b)
        {
            if (a.RoomCount != b.RoomCount || a.MaxDistance != b.MaxDistance) return false;
            if (a.Start.Grid != b.Start.Grid || a.Boss.Grid != b.Boss.Grid) return false;

            for (int x = 0; x < a.ColumnWidths.Count; x++)
                if (a.GetColumnWidth(x) != b.GetColumnWidth(x)) return false;

            for (int y = 0; y < a.RowHeights.Count; y++)
                if (a.GetRowHeight(y) != b.GetRowHeight(y)) return false;

            foreach (RoomNode room in a.Rooms)
            {
                if (!b.TryGetRoom(room.Grid, out RoomNode other)) return false;
                if (other.Type != room.Type || other.Doors != room.Doors) return false;
                if (other.Width != room.Width || other.Height != room.Height) return false;
                if ((other.WorldOrigin - room.WorldOrigin).sqrMagnitude > 1e-4f) return false;
            }
            return true;
        }
    }
}
