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

        public int Seed { get; }
        public DungeonConfig Config { get; }

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

            int treasure = 0, shop = 0, secret = 0;
            for (int i = 0; i < _ordered.Count; i++)
            {
                switch (_ordered[i].Type)
                {
                    case RoomType.Treasure: treasure++; break;
                    case RoomType.Shop: shop++; break;
                    case RoomType.Secret: secret++; break;
                }
            }

            if (Config.treasureCount > 0 && treasure < Config.treasureCount)
                issues.Add($"[警告] 宝箱房数量不足：{treasure}/{Config.treasureCount}");
            if (Config.shopCount > 0 && shop < Config.shopCount)
                issues.Add($"[警告] 商店数量不足：{shop}/{Config.shopCount}");
            if (Config.secretCount > 0 && secret < Config.secretCount)
                issues.Add($"[警告] 秘密房数量不足：{secret}/{Config.secretCount}");

            if (RoomCount <= 2)
                issues.Add("[警告] 房间数过少，几乎不成形");

            return issues;
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
                default: return 'c';
            }
        }
    }
}
