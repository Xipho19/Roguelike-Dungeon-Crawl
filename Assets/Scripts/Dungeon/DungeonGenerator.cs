using System;
using System.Collections.Generic;
using UnityEngine;

namespace Roguelike.Dungeon
{
    /// <summary>
    /// 房间图生成器：给定种子与配置，生成一层的房间拓扑。
    ///
    /// 算法（灵感来自以撒的"手工房间 + 随机拓扑"）：
    ///   1. 起点放在网格中心
    ///   2. 随机游走生成"起点 → Boss"的主路径（带方向惯性，走廊更直）
    ///   3. 从主路径上随机生长若干分支，天然形成死胡同
    ///   4. BFS 计算每个房间到起点的步数
    ///   5. 按"死胡同优先、距离更远优先"分配宝箱 / 商店 / 秘密房
    ///   6. 由相邻关系推导每扇门的方向
    ///
    /// 整个过程只操作纯数据，不触碰任何 GameObject，因此可直接在 EditMode 测试中调用。
    /// 相同 seed + 相同 config 必然得到完全相同的结果。
    /// </summary>
    public static class DungeonGenerator
    {
        public static DungeonData Generate(DungeonConfig config, int seed)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            DungeonConfig cfg = config.Clone();
            cfg.Clamp();

            var rng = new SeededRandom(seed);
            var data = new DungeonData(seed, cfg);

            // ---------- 1. 起点 ----------
            var startPos = new Vector2Int(cfg.width / 2, cfg.height / 2);
            RoomNode start = data.AddRoom(startPos);
            start.Type = RoomType.Start;
            data.Start = start;
            start.DistanceFromStart = 0;

            // ---------- 2. 主路径 ----------
            var mainPath = new List<RoomNode> { start };
            RoomNode current = start;
            Vector2Int? lastDir = null;

            for (int i = 1; i < cfg.mainPathLength; i++)
            {
                Vector2Int? dir = PickFreeNeighbor(data, cfg, current, lastDir, cfg.straightBias, rng);
                if (dir == null) break; // 走进死角，主路径提前结束

                RoomNode next = data.AddRoom(current.Grid + dir.Value);
                Connect(current, next);
                mainPath.Add(next);

                current = next;
                lastDir = dir;
            }

            // 极端参数下至少保证有两个房间，否则退化成单格
            if (mainPath.Count < 2)
            {
                Vector2Int? dir = PickFreeNeighbor(data, cfg, start, null, 0f, rng);
                if (dir == null)
                    throw new InvalidOperationException("网格过小，无法生成地牢。");

                RoomNode next = data.AddRoom(start.Grid + dir.Value);
                Connect(start, next);
                mainPath.Add(next);
            }

            // ---------- 3. Boss 放在主路径末端 ----------
            RoomNode boss = mainPath[mainPath.Count - 1];
            boss.Type = RoomType.Boss;
            data.Boss = boss;

            // ---------- 4. 分支 ----------
            GrowBranches(data, cfg, mainPath, rng);

            // ---------- 5. 距离 ----------
            ComputeDistances(data);

            // ---------- 6. 特殊房间 ----------
            AssignSpecialRooms(data, cfg);

            // ---------- 7. 门 ----------
            ComputeDoors(data);

            // ---------- 8. 房间尺寸与世界布局 ----------
            AssignRoomLayout(data, cfg, seed);

            return data;
        }

        /// <summary>
        /// 为每一列随机一个宽度、每一行随机一个高度，并计算每个房间的世界原点。
        /// 使用独立的随机流，避免改变已有种子的拓扑结果。
        /// </summary>
        private static void AssignRoomLayout(DungeonData data, DungeonConfig cfg, int seed)
        {
            var rng = new SeededRandom(unchecked(seed * 0x27D4EB2D ^ 0x165667B1));

            var usedColumns = new bool[cfg.width];
            var usedRows = new bool[cfg.height];

            for (int i = 0; i < data.Rooms.Count; i++)
            {
                RoomNode room = data.Rooms[i];
                usedColumns[room.Grid.x] = true;
                usedRows[room.Grid.y] = true;
            }

            // 只在真正有房间的列/行里分配尺寸，保证每种大小都能实际出现
            var columnIndices = new List<int>(cfg.width);
            for (int x = 0; x < cfg.width; x++)
                if (usedColumns[x]) columnIndices.Add(x);

            var widths = new List<int>(columnIndices.Count);
            for (int i = 0; i < columnIndices.Count; i++)
                widths.Add(RoomGeometry.AllowedWidths[i % RoomGeometry.AllowedWidths.Length]);
            Shuffle(widths, rng);

            for (int i = 0; i < columnIndices.Count; i++)
                data.SetColumnWidth(columnIndices[i], widths[i]);

            var rowIndices = new List<int>(cfg.height);
            for (int y = 0; y < cfg.height; y++)
                if (usedRows[y]) rowIndices.Add(y);

            var heights = new List<int>(rowIndices.Count);
            for (int i = 0; i < rowIndices.Count; i++)
                heights.Add(RoomGeometry.AllowedHeights[i % RoomGeometry.AllowedHeights.Length]);
            Shuffle(heights, rng);

            for (int i = 0; i < rowIndices.Count; i++)
                data.SetRowHeight(rowIndices[i], heights[i]);

            // 没有房间的空列/空行使用最小尺寸，避免地图边缘出现大块空白
            for (int x = 0; x < cfg.width; x++)
                if (!usedColumns[x]) data.SetColumnWidth(x, RoomGeometry.MinFootprintWidth);

            for (int y = 0; y < cfg.height; y++)
                if (!usedRows[y]) data.SetRowHeight(y, RoomGeometry.MinFootprintHeight);

            // 起点和 Boss 使用更宽敞的行列，保证出生空间与 Boss 场地
            if (data.Start != null)
            {
                data.SetColumnWidth(data.Start.Grid.x, Mathf.Max(data.GetColumnWidth(data.Start.Grid.x), 17));
                data.SetRowHeight(data.Start.Grid.y, Mathf.Max(data.GetRowHeight(data.Start.Grid.y), 11));
            }

            if (data.Boss != null)
            {
                data.SetColumnWidth(data.Boss.Grid.x, 19);
                data.SetRowHeight(data.Boss.Grid.y, 11);
            }

            data.ApplyLayout();
        }

        private static void Shuffle<T>(List<T> list, SeededRandom rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // ------------------------------------------------------------------
        // 主路径 / 分支
        // ------------------------------------------------------------------

        /// <summary>
        /// 在 from 的四个邻格中挑一个"在网格内且未被占用"的方向。
        /// preferredDir 命中时以 preferredBias 的概率直接延续，制造长走廊。
        /// avoidDenseBlocks 打开时，会优先避开会产生 2×2 实心块的位置；
        /// 若所有候选都会成块，则退而求其次，保证生成不会卡死。
        /// </summary>
        private static Vector2Int? PickFreeNeighbor(
            DungeonData data,
            DungeonConfig cfg,
            RoomNode from,
            Vector2Int? preferredDir,
            float preferredBias,
            SeededRandom rng)
        {
            var preferred = new List<Vector2Int>(4);
            var fallback = new List<Vector2Int>(4);

            for (int i = 0; i < DirUtil.All.Length; i++)
            {
                Vector2Int p = from.Grid + DirUtil.All[i];
                if (p.x < 0 || p.y < 0 || p.x >= cfg.width || p.y >= cfg.height) continue;
                if (data.TryGetRoom(p, out _)) continue;

                if (cfg.avoidDenseBlocks && FormsDenseBlock(data, p))
                    fallback.Add(DirUtil.All[i]);
                else
                    preferred.Add(DirUtil.All[i]);
            }

            List<Vector2Int> candidates = preferred.Count > 0 ? preferred : fallback;
            if (candidates.Count == 0) return null;

            if (preferredDir.HasValue &&
                candidates.Contains(preferredDir.Value) &&
                rng.Chance(preferredBias))
            {
                return preferredDir.Value;
            }

            return candidates[rng.Next(candidates.Count)];
        }

        /// <summary>
        /// 判断在 p 放一个房间后，是否会与相邻已存在房间拼出一个 2×2 的实心块：
        /// 只要存在一个对角方向，使 p 的"两个直邻 + 一个斜邻"都已占用，就成立。
        /// </summary>
        private static bool FormsDenseBlock(DungeonData data, Vector2Int p)
        {
            if (!data.Config.avoidDenseBlocks) return false;

            for (int i = 0; i < Diagonals.Length; i++)
            {
                Vector2Int d = Diagonals[i];
                if (data.TryGetRoom(p + new Vector2Int(d.x, 0), out _) &&
                    data.TryGetRoom(p + new Vector2Int(0, d.y), out _) &&
                    data.TryGetRoom(p + d, out _))
                {
                    return true;
                }
            }
            return false;
        }

        private static readonly Vector2Int[] Diagonals =
        {
            new Vector2Int(1, 1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, 1),
            new Vector2Int(-1, -1),
        };

        private static void GrowBranches(
            DungeonData data,
            DungeonConfig cfg,
            List<RoomNode> mainPath,
            SeededRandom rng)
        {
            if (cfg.branchCount <= 0 || mainPath.Count <= 1) return;

            int made = 0;
            int guard = cfg.branchCount * 8 + 8; // 防止锚点被围死时死循环

            while (made < cfg.branchCount && guard-- > 0)
            {
                // 锚点只从主路径里选，且排除 Boss —— Boss 必须保持死胡同
                int anchorIndex = rng.Next(0, mainPath.Count - 1);
                RoomNode anchor = mainPath[anchorIndex];

                int length = rng.Next(cfg.branchLengthMin, cfg.branchLengthMax + 1);
                RoomNode cur = anchor;
                int moved = 0;

                for (int i = 0; i < length; i++)
                {
                    Vector2Int? dir = PickFreeNeighbor(data, cfg, cur, null, 0f, rng);
                    if (dir == null) break;

                    RoomNode next = data.AddRoom(cur.Grid + dir.Value);
                    Connect(cur, next);
                    cur = next;
                    moved++;
                }

                if (moved > 0) made++;
            }
        }

        private static void Connect(RoomNode a, RoomNode b)
        {
            if (!a.Neighbors.Contains(b)) a.Neighbors.Add(b);
            if (!b.Neighbors.Contains(a)) b.Neighbors.Add(a);
        }

        // ------------------------------------------------------------------
        // 后处理
        // ------------------------------------------------------------------

        private static void ComputeDistances(DungeonData data)
        {
            var visited = new HashSet<RoomNode>();
            var queue = new Queue<RoomNode>();

            foreach (RoomNode r in data.Rooms) r.DistanceFromStart = -1;

            if (data.Start == null) return;

            data.Start.DistanceFromStart = 0;
            visited.Add(data.Start);
            queue.Enqueue(data.Start);

            int max = 0;
            while (queue.Count > 0)
            {
                RoomNode room = queue.Dequeue();
                for (int i = 0; i < room.Neighbors.Count; i++)
                {
                    RoomNode n = room.Neighbors[i];
                    if (!visited.Add(n)) continue;

                    n.DistanceFromStart = room.DistanceFromStart + 1;
                    max = Mathf.Max(max, n.DistanceFromStart);
                    queue.Enqueue(n);
                }
            }

            data.MaxDistance = max;
        }

        /// <summary>
        /// 特殊房间分配规则：
        ///   候选池 = 所有非起点非 Boss 的房间，按「死胡同优先 → 距离远优先 → 坐标稳定排序」。
        /// 先用死胡同，死胡同不够再用更深处的普通房补齐，保证配额一定被填满。
        /// </summary>
        private static void AssignSpecialRooms(DungeonData data, DungeonConfig cfg)
        {
            var deadEnds = new List<RoomNode>();
            var others = new List<RoomNode>();

            foreach (RoomNode room in data.Rooms)
            {
                if (room == data.Start || room == data.Boss) continue;
                if (room.IsDeadEnd) deadEnds.Add(room);
                else others.Add(room);
            }

            Comparison<RoomNode> depthFirst = (a, b) =>
            {
                int c = b.DistanceFromStart.CompareTo(a.DistanceFromStart);
                if (c != 0) return c;
                c = a.Grid.x.CompareTo(b.Grid.x);
                return c != 0 ? c : a.Grid.y.CompareTo(b.Grid.y);
            };

            deadEnds.Sort(depthFirst);
            others.Sort(depthFirst);

            var pool = new List<RoomNode>(deadEnds);
            pool.AddRange(others);

            int cursor = 0;
            cursor = Take(pool, cursor, cfg.treasureCount, RoomType.Treasure);
            cursor = Take(pool, cursor, cfg.shopCount, RoomType.Shop);
            Take(pool, cursor, cfg.secretCount, RoomType.Secret);
        }

        private static int Take(List<RoomNode> pool, int from, int count, RoomType type)
        {
            for (int i = 0; i < count && from < pool.Count; i++, from++)
                pool[from].Type = type;
            return from;
        }

        private static void ComputeDoors(DungeonData data)
        {
            foreach (RoomNode room in data.Rooms)
                room.Doors = DoorDirections.None;

            foreach (RoomNode room in data.Rooms)
            {
                for (int i = 0; i < room.Neighbors.Count; i++)
                    room.Doors |= DirUtil.Between(room.Grid, room.Neighbors[i].Grid);
            }
        }
    }
}
