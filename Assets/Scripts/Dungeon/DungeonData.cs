using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Roguelike.Dungeon
{
    /// <summary>
    /// 一层的完整房间拓扑数据：包含所有房间节点、起点 / Boss 引用、种子与配置。
    /// 这是纯数据，不引用任何场景对象，可以直接在 EditMode 测试里断言。
    /// </summary>
    public sealed class DungeonData
    {
        private readonly Dictionary<Vector2Int, RoomNode> _rooms = new Dictionary<Vector2Int, RoomNode>();
        private readonly List<RoomNode> _ordered = new List<RoomNode>();
        private readonly int[] _columnWidths;
        private readonly int[] _rowHeights;

        public int Seed { get; }
        public DungeonConfig Config { get; }

        /// <summary>每一列房间的外框宽度（含墙）。</summary>
        public IReadOnlyList<int> ColumnWidths => _columnWidths;

        /// <summary>每一行房间的外框高度（含墙）。</summary>
        public IReadOnlyList<int> RowHeights => _rowHeights;

        /// <summary>出生房。</summary>
        public RoomNode Start { get; internal set; }

        /// <summary>主路径末端的 Boss 房。</summary>
        public RoomNode Boss { get; internal set; }

        /// <summary>按生成顺序排列的所有房间。</summary>
        public IReadOnlyList<RoomNode> Rooms => _ordered;

        public int RoomCount => _ordered.Count;

        /// <summary>离起点最远的房间步数。</summary>
        public int MaxDistance { get; internal set; }

        public DungeonData(int seed, DungeonConfig config)
        {
            Seed = seed;
            Config = config;

            _columnWidths = new int[config.width];
            _rowHeights = new int[config.height];

            for (int x = 0; x < _columnWidths.Length; x++)
                _columnWidths[x] = RoomGeometry.FootprintWidth;

            for (int y = 0; y < _rowHeights.Length; y++)
                _rowHeights[y] = RoomGeometry.FootprintHeight;
        }

        public int GetColumnWidth(int x) => _columnWidths[x];
        public int GetRowHeight(int y) => _rowHeights[y];

        internal void SetColumnWidth(int x, int width)
        {
            if (x >= 0 && x < _columnWidths.Length) _columnWidths[x] = width;
        }

        internal void SetRowHeight(int y, int height)
        {
            if (y >= 0 && y < _rowHeights.Length) _rowHeights[y] = height;
        }

        /// <summary>
        /// 用当前列宽 / 行高计算每个房间的世界原点与尺寸。
        /// 相邻列/行共享 1 格墙，因此前缀和每次减去 WallThickness。
        /// </summary>
        internal void ApplyLayout()
        {
            var originX = new int[_columnWidths.Length];
            var originY = new int[_rowHeights.Length];

            for (int x = 1; x < originX.Length; x++)
                originX[x] = originX[x - 1] + _columnWidths[x - 1] - (int)RoomGeometry.WallThickness;

            for (int y = 1; y < originY.Length; y++)
                originY[y] = originY[y - 1] + _rowHeights[y - 1] - (int)RoomGeometry.WallThickness;

            for (int i = 0; i < _ordered.Count; i++)
            {
                RoomNode room = _ordered[i];
                Vector2Int grid = room.Grid;

                if (grid.x < 0 || grid.y < 0 ||
                    grid.x >= _columnWidths.Length || grid.y >= _rowHeights.Length)
                    continue;

                room.Width = _columnWidths[grid.x];
                room.Height = _rowHeights[grid.y];
                room.WorldOrigin = new Vector2(originX[grid.x], originY[grid.y]);
            }
        }

        /// <summary>所有实际房间的世界空间包围盒。</summary>
        public Rect GetWorldBounds()
        {
            if (_ordered.Count == 0) return new Rect(0f, 0f, 0f, 0f);

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            for (int i = 0; i < _ordered.Count; i++)
            {
                RoomNode room = _ordered[i];
                minX = Mathf.Min(minX, room.WorldOrigin.x);
                minY = Mathf.Min(minY, room.WorldOrigin.y);
                maxX = Mathf.Max(maxX, room.WorldOrigin.x + room.Width);
                maxY = Mathf.Max(maxY, room.WorldOrigin.y + room.Height);
            }

            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        internal RoomNode AddRoom(Vector2Int grid)
        {
            if (_rooms.ContainsKey(grid))
                throw new System.InvalidOperationException($"房间 {grid} 已存在，生成器逻辑有误。");

            var room = new RoomNode(grid);
            _rooms.Add(grid, room);
            _ordered.Add(room);
            return room;
        }

        public bool TryGetRoom(Vector2Int grid, out RoomNode room) => _rooms.TryGetValue(grid, out room);

        public RoomNode GetRoom(Vector2Int grid)
        {
            _rooms.TryGetValue(grid, out RoomNode room);
            return room;
        }

        public IEnumerable<RoomNode> RoomsOfType(RoomType type)
        {
            for (int i = 0; i < _ordered.Count; i++)
                if (_ordered[i].Type == type)
                    yield return _ordered[i];
        }

        /// <summary>所有房间占用的整数矩形范围。</summary>
        public RectInt GetBounds()
        {
            if (_ordered.Count == 0) return new RectInt(0, 0, 0, 0);

            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            foreach (RoomNode r in _ordered)
            {
                minX = Mathf.Min(minX, r.Grid.x);
                minY = Mathf.Min(minY, r.Grid.y);
                maxX = Mathf.Max(maxX, r.Grid.x);
                maxY = Mathf.Max(maxY, r.Grid.y);
            }
            return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        /// <summary>
        /// 结构与玩法层面的自检。返回空列表表示通过。
        /// 生成器本身不会因为这些问题抛异常，方便在编辑器里反复试参数。
        /// </summary>
        public List<string> Validate()
        {
            var issues = new List<string>();

            if (Start == null) issues.Add("[错误] 缺少起点房间");
            if (Boss == null) issues.Add("[错误] 缺少 Boss 房间");
            if (Start != null && Boss != null && Start == Boss) issues.Add("[错误] 起点与 Boss 是同一个房间");

            // 连通性：从起点 BFS 必须覆盖所有房间
            if (Start != null)
            {
                var visited = new HashSet<RoomNode>();
                var queue = new Queue<RoomNode>();
                visited.Add(Start);
                queue.Enqueue(Start);
                while (queue.Count > 0)
                {
                    RoomNode room = queue.Dequeue();
                    for (int i = 0; i < room.Neighbors.Count; i++)
                    {
                        RoomNode n = room.Neighbors[i];
                        if (visited.Add(n)) queue.Enqueue(n);
                    }
                }

                int unreachable = RoomCount - visited.Count;
                if (unreachable > 0) issues.Add($"[错误] 存在 {unreachable} 个从起点不可达的房间");
            }

            if (Boss != null && Boss.Degree != 1)
                issues.Add("[警告] Boss 房不是死胡同，玩家可能绕过它");

            int treasure = 0, shop = 0, secret = 0, challenge = 0;
            for (int i = 0; i < _ordered.Count; i++)
            {
                switch (_ordered[i].Type)
                {
                    case RoomType.Treasure: treasure++; break;
                    case RoomType.Shop: shop++; break;
                    case RoomType.Secret: secret++; break;
                    case RoomType.Challenge: challenge++; break;
                }
            }

            if (Config.treasureCount > 0 && treasure < Config.treasureCount)
                issues.Add($"[警告] 宝箱房数量不足：{treasure}/{Config.treasureCount}");
            if (Config.shopCount > 0 && shop < Config.shopCount)
                issues.Add($"[警告] 商店数量不足：{shop}/{Config.shopCount}");
            if (Config.challengeCount > 0 && challenge < Config.challengeCount)
                issues.Add($"[警告] 挑战房数量不足：{challenge}/{Config.challengeCount}");
            if (Config.secretCount > 0 && secret < Config.secretCount)
                issues.Add($"[警告] 秘密房数量不足：{secret}/{Config.secretCount}");

            if (RoomCount <= 2)
                issues.Add("[警告] 房间数过少，几乎不成形");

            ValidateLayout(issues);

            return issues;
        }

        private void ValidateLayout(List<string> issues)
        {
            if (_columnWidths.Length != Config.width || _rowHeights.Length != Config.height)
            {
                issues.Add("[错误] 列宽 / 行高数组尺寸与配置不符");
                return;
            }

            for (int x = 0; x < _columnWidths.Length; x++)
            {
                int width = _columnWidths[x];
                if (width < RoomGeometry.MinFootprintWidth || width % 2 == 0)
                    issues.Add($"[错误] 第 {x} 列宽度非法：{width}");
            }

            for (int y = 0; y < _rowHeights.Length; y++)
            {
                int height = _rowHeights[y];
                if (height < RoomGeometry.MinFootprintHeight || height % 2 == 0)
                    issues.Add($"[错误] 第 {y} 行高度非法：{height}");
            }

            for (int i = 0; i < _ordered.Count; i++)
            {
                RoomNode room = _ordered[i];
                if (room.Width != _columnWidths[room.Grid.x] || room.Height != _rowHeights[room.Grid.y])
                    issues.Add($"[错误] 房间 {room.Grid} 的尺寸与所在行列不一致");

                Vector2 expected = ExpectedOrigin(room.Grid);
                if ((room.WorldOrigin - expected).sqrMagnitude > 0.0001f)
                    issues.Add($"[错误] 房间 {room.Grid} 的世界原点与布局不符");
            }

            // 相邻房间：共享墙长度、门心必须一致
            for (int i = 0; i < _ordered.Count; i++)
            {
                RoomNode room = _ordered[i];
                for (int n = 0; n < room.Neighbors.Count; n++)
                {
                    RoomNode neighbor = room.Neighbors[n];

                    if (neighbor.Grid == room.Grid + Vector2Int.right)
                        ValidateHorizontalEdge(room, neighbor, issues);
                    else if (neighbor.Grid == room.Grid + Vector2Int.up)
                        ValidateVerticalEdge(room, neighbor, issues);
                }
            }

            // 房间内部不能互相重叠（共享墙带允许重叠）
            for (int i = 0; i < _ordered.Count; i++)
            {
                Rect a = InteriorRect(_ordered[i]);
                for (int j = i + 1; j < _ordered.Count; j++)
                {
                    Rect b = InteriorRect(_ordered[j]);
                    if (a.Overlaps(b))
                        issues.Add($"[错误] 房间 {_ordered[i].Grid} 与 {_ordered[j].Grid} 的内部区域重叠");
                }
            }
        }

        private void ValidateHorizontalEdge(RoomNode left, RoomNode right, List<string> issues)
        {
            if (left.Height != right.Height || Mathf.Abs(left.WorldOrigin.y - right.WorldOrigin.y) > 0.0001f)
                issues.Add($"[错误] 横向邻居 {left.Grid} / {right.Grid} 的行高或原点不一致");

            if (Mathf.Abs((left.WorldOrigin.x + left.Width - RoomGeometry.WallThickness) - right.WorldOrigin.x) > 0.0001f)
                issues.Add($"[错误] 横向邻居 {left.Grid} / {right.Grid} 的共享墙未对齐");

            Vector2 doorA = left.WorldOrigin + RoomGeometry.DoorCenter(WallSide.Right, left.Width, left.Height);
            Vector2 doorB = right.WorldOrigin + RoomGeometry.DoorCenter(WallSide.Left, right.Width, right.Height);
            if ((doorA - doorB).sqrMagnitude > 0.0001f)
                issues.Add($"[错误] 横向邻居 {left.Grid} / {right.Grid} 的门心不重合");
        }

        private void ValidateVerticalEdge(RoomNode bottom, RoomNode top, List<string> issues)
        {
            if (bottom.Width != top.Width || Mathf.Abs(bottom.WorldOrigin.x - top.WorldOrigin.x) > 0.0001f)
                issues.Add($"[错误] 纵向邻居 {bottom.Grid} / {top.Grid} 的列宽或原点不一致");

            if (Mathf.Abs((bottom.WorldOrigin.y + bottom.Height - RoomGeometry.WallThickness) - top.WorldOrigin.y) > 0.0001f)
                issues.Add($"[错误] 纵向邻居 {bottom.Grid} / {top.Grid} 的共享墙未对齐");

            Vector2 doorA = bottom.WorldOrigin + RoomGeometry.DoorCenter(WallSide.Up, bottom.Width, bottom.Height);
            Vector2 doorB = top.WorldOrigin + RoomGeometry.DoorCenter(WallSide.Down, top.Width, top.Height);
            if ((doorA - doorB).sqrMagnitude > 0.0001f)
                issues.Add($"[错误] 纵向邻居 {bottom.Grid} / {top.Grid} 的门心不重合");
        }

        private Vector2 ExpectedOrigin(Vector2Int grid)
        {
            float x = 0f;
            float y = 0f;

            for (int i = 0; i < grid.x; i++)
                x += _columnWidths[i] - RoomGeometry.WallThickness;

            for (int j = 0; j < grid.y; j++)
                y += _rowHeights[j] - RoomGeometry.WallThickness;

            return new Vector2(x, y);
        }

        private static Rect InteriorRect(RoomNode room)
        {
            float inset = RoomGeometry.WallThickness;
            return new Rect(
                room.WorldOrigin + new Vector2(inset, inset),
                new Vector2(room.Width - inset * 2f, room.Height - inset * 2f));
        }

        /// <summary>把房间图打印成 ASCII，方便在 Console 里快速检查（上北下南）。</summary>
        public string ToDebugString()
        {
            RectInt b = GetBounds();
            var sb = new StringBuilder();
            sb.AppendLine($"Seed={Seed}  Rooms={RoomCount}  MaxDist={MaxDistance}");

            for (int y = b.yMax - 1; y >= b.yMin; y--)
            {
                for (int x = b.xMin; x < b.xMax; x++)
                {
                    if (_rooms.TryGetValue(new Vector2Int(x, y), out RoomNode room))
                        sb.Append(' ').Append(Glyph(room.Type)).Append(' ');
                    else
                        sb.Append(" . ");
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private static char Glyph(RoomType type)
        {
            switch (type)
            {
                case RoomType.Start: return 'S';
                case RoomType.Boss: return 'B';
                case RoomType.Treasure: return 'T';
                case RoomType.Shop: return '$';
                case RoomType.Secret: return '?';
                case RoomType.Challenge: return '!';
                default: return 'c';
            }
        }
    }
}
