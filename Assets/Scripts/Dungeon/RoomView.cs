using System.Collections.Generic;
using UnityEngine;

namespace Roguelike.Dungeon
{
    /// <summary>
    /// 场景中一个房间实例的表现与数据入口。
    ///
    /// DungeonBuilder 在实例化房间 Prefab 后会调用 <see cref="Configure"/> 写入：
    ///   - 房间类型 / 网格坐标 / 门位
    ///   - 每面墙的开启状态（门洞或封死）
    ///   - 门标记、入口点的可用状态
    ///
    /// 房间本地坐标原点位于 footprint 的左下角，因此世界坐标 = 网格坐标 × Spacing。
    /// </summary>
    public sealed class RoomView : MonoBehaviour
    {
        [Header("房间数据（由 DungeonBuilder 写入）")]
        [SerializeField] private RoomType roomType;
        [SerializeField] private Vector2Int gridPosition;
        [SerializeField] private DoorDirections doors;
        [SerializeField] private int distanceFromStart = -1;

        [Header("外框尺寸")]
        [Tooltip("外框宽高（含 1 格墙），由对应尺寸的 Prefab 决定")]
        [SerializeField] private Vector2Int footprint = new Vector2Int(RoomGeometry.FootprintWidth, RoomGeometry.FootprintHeight);

        [Header("场景引用")]
        [Tooltip("房间中心点")]
        [SerializeField] private Transform centerPoint;

        [Tooltip("顺序：上 / 右 / 下 / 左")]
        [SerializeField] private Transform[] entryPoints = new Transform[4];

        [Tooltip("顺序：上 / 右 / 下 / 左")]
        [SerializeField] private Transform[] doorMarkers = new Transform[4];

        [Tooltip("未清空战斗房的门槛警示地砖，顺序：上 / 右 / 下 / 左")]
        [SerializeField] private SpriteRenderer[] doorIndicators = new SpriteRenderer[4];

        [Tooltip("门洞触发盒，只要该方向有门就启用，与墙体归属无关")]
        [SerializeField] private Transform[] doorTriggers = new Transform[4];

        [Tooltip("封门板，顺序：上 / 右 / 下 / 左")]
        [SerializeField] private RoomDoorBlocker[] doorBlockers = new RoomDoorBlocker[4];

        [Tooltip("顺序：上 / 右 / 下 / 左")]
        [SerializeField] private RoomWall[] walls = new RoomWall[4];

        [Tooltip("敌人出生点，供后续战斗系统使用")]
        [SerializeField] private Transform[] enemySpawnPoints = new Transform[0];

        [Header("美术变化")]
        [Tooltip("逐格随机地砖")]
        [SerializeField] private RoomFloor roomFloor;
        [Tooltip("按房间类型摆放的墙面与地面装饰")]
        [SerializeField] private RoomDecorations decorations;

        [Header("调试显示")]
        [SerializeField] private SpriteRenderer floorRenderer;
        [SerializeField] private SpriteRenderer typeBadge;
        [SerializeField] private Color floorBaseColor = new Color(0.22f, 0.23f, 0.28f, 1f);

        private bool _hasFloorTheme;
        private Color _floorThemeTint = Color.white;

        public RoomType RoomType => roomType;
        public Vector2Int GridPosition => gridPosition;
        public DoorDirections Doors => doors;
        public Vector2Int Footprint => footprint;
        public int Width => footprint.x;
        public int Height => footprint.y;

        /// <summary>到起点房的最短步数，用于战斗难度与敌人编成。</summary>
        public int DistanceFromStart => distanceFromStart;

        public Transform CenterPoint => centerPoint;

        /// <summary>当前门是否处于锁死状态。</summary>
        public bool DoorsLocked { get; private set; }

        private bool _cleared;

        /// <summary>房间是否已被清空（由战斗系统写入）。</summary>
        public bool IsCleared
        {
            get => _cleared;
            set
            {
                if (_cleared == value) return;

                _cleared = value;
                RefreshDoorIndicators();
            }
        }

        /// <summary>宝箱房是否已经生成过奖励，防止重复刷新。</summary>
        public bool RewardSpawned { get; set; }

        /// <summary>宝箱房奖励是否已被领取。</summary>
        public bool RewardClaimed { get; set; }

        /// <summary>玩家是否进入过该房间（Tab 地图）。</summary>
        public bool IsVisited { get; set; }

        /// <summary>商店是否已经进货。</summary>
        public bool ShopStocked { get; set; }

        /// <summary>秘密房祭坛是否已经生成，防止重复刷新。</summary>
        public bool SecretEventSpawned { get; set; }

        /// <summary>秘密房祭坛是否已经完成交易。</summary>
        public bool SecretEventUsed { get; set; }
        public IReadOnlyList<Transform> EntryPoints => entryPoints;
        public IReadOnlyList<Transform> DoorMarkers => doorMarkers;
        public IReadOnlyList<RoomWall> Walls => walls;
        public IReadOnlyList<Transform> EnemySpawnPoints => enemySpawnPoints;

        /// <summary>房间在世界空间中的矩形（footprint）。</summary>
        public Rect WorldRect => new Rect(transform.position, new Vector2(footprint.x, footprint.y));

        /// <summary>房间内部（不含墙）的世界矩形。</summary>
        public Rect InteriorRect
        {
            get
            {
                float inset = RoomGeometry.WallThickness;
                return new Rect(
                    (Vector2)transform.position + new Vector2(inset, inset),
                    new Vector2(footprint.x - inset * 2f, footprint.y - inset * 2f));
            }
        }

        /// <summary>房间中心的世界坐标。</summary>
        public Vector2 WorldCenter =>
            (Vector2)transform.position + new Vector2(footprint.x * 0.5f, footprint.y * 0.5f);

        public RoomWall GetWall(WallSide side)
        {
            int index = (int)side;
            return walls != null && index >= 0 && index < walls.Length ? walls[index] : null;
        }

        public Transform GetDoorMarker(WallSide side)
        {
            int index = (int)side;
            return doorMarkers != null && index >= 0 && index < doorMarkers.Length ? doorMarkers[index] : null;
        }

        public Transform GetEntryPoint(WallSide side)
        {
            int index = (int)side;
            return entryPoints != null && index >= 0 && index < entryPoints.Length ? entryPoints[index] : null;
        }

        public Transform GetDoorTrigger(WallSide side)
        {
            int index = (int)side;
            return doorTriggers != null && index >= 0 && index < doorTriggers.Length ? doorTriggers[index] : null;
        }

        public RoomDoorBlocker GetDoorBlocker(WallSide side)
        {
            int index = (int)side;
            return doorBlockers != null && index >= 0 && index < doorBlockers.Length ? doorBlockers[index] : null;
        }

        /// <summary>统一设置四扇门的锁死状态。</summary>
        public void SetDoorsLocked(bool locked, bool instant)
        {
            DoorsLocked = locked;

            for (int i = 0; i < 4; i++)
            {
                WallSide side = (WallSide)i;
                RoomDoorBlocker blocker = GetDoorBlocker(side);
                if (blocker != null)
                    blocker.SetLocked(locked && HasDoor(side), instant);
            }
        }

        public bool HasDoor(WallSide side) => (doors & side.ToDoor()) != 0;

        /// <summary>
        /// 未清空的战斗房 / Boss 房，在每扇门的门槛上显示警示地砖，
        /// 让玩家在进入前就能分辨这间房是否已经打过。
        /// </summary>
        private void RefreshDoorIndicators()
        {
            bool danger = (roomType == RoomType.Combat || roomType == RoomType.Boss || roomType == RoomType.Challenge) && !_cleared;

            for (int i = 0; i < 4; i++)
            {
                if (doorIndicators == null || i >= doorIndicators.Length) continue;

                SpriteRenderer indicator = doorIndicators[i];
                if (indicator == null) continue;

                indicator.enabled = danger && HasDoor((WallSide)i);
            }
        }

        /// <summary>
        /// 根据房间节点配置门洞、墙体归属与入口点，并按需应用调试配色。
        /// </summary>
        public void Configure(RoomNode node, bool debugVisuals = true)
        {
            if (node == null) return;

            roomType = node.Type;
            gridPosition = node.Grid;
            doors = node.Doors;
            distanceFromStart = node.DistanceFromStart;

            if (node.Width != footprint.x || node.Height != footprint.y)
            {
                Debug.LogError(
                    $"[Dungeon] 房间 {node.Grid} 需要 {node.Width}x{node.Height} 的 Prefab，" +
                    $"实际是 {footprint.x}x{footprint.y}。请重新生成房间 Prefab 后再构建。",
                    this);
            }

            for (int i = 0; i < 4; i++)
            {
                WallSide side = (WallSide)i;
                bool hasDoor = (node.Doors & side.ToDoor()) != 0;
                bool owned = IsSideOwned(node, side);

                RoomWall wall = GetWall(side);
                if (wall != null) wall.Apply(owned, hasDoor);

                Transform marker = GetDoorMarker(side);
                if (marker != null) marker.gameObject.SetActive(owned && hasDoor);

                // 触发盒两侧都需要：从任意一侧走向门洞时，都要能切换到对面房间
                Transform trigger = GetDoorTrigger(side);
                if (trigger != null) trigger.gameObject.SetActive(hasDoor);

                // 封门板：有门才显示，初始为开启状态
                RoomDoorBlocker blocker = GetDoorBlocker(side);
                if (blocker != null)
                {
                    blocker.gameObject.SetActive(hasDoor);
                    blocker.SetLocked(false, true);
                }

                Transform entry = GetEntryPoint(side);
                if (entry != null) entry.gameObject.SetActive(hasDoor);
            }

            DoorsLocked = false;
            RefreshDoorIndicators();
            ApplyDebugVisuals(node.Type, debugVisuals);
        }

        /// <summary>
        /// 按本局种子生成本房间的地砖与装饰。
        /// 必须在 <see cref="Configure"/> 之后调用（需要房间类型、门位与网格坐标）。
        /// </summary>
        public void ApplyRoomVariation(int runSeed)
        {
            int roomSeed = runSeed
                ^ (gridPosition.x * 73856093)
                ^ (gridPosition.y * 19349663);

            if (roomFloor != null) roomFloor.Apply(roomSeed);
            if (decorations != null) decorations.Apply(roomType, doors, roomSeed);
        }

        /// <summary>应用楼层主题：地板与墙体染色。</summary>
        public void ApplyFloorTheme(FloorTheme theme)
        {
            if (theme == null) return;

            _hasFloorTheme = true;
            _floorThemeTint = theme.floorTint;

            if (roomFloor != null) roomFloor.SetTint(_floorThemeTint);
            else if (floorRenderer != null) floorRenderer.color = _floorThemeTint;

            // 墙面装饰（旗帜、破洞/苔藓、立柱）自带砖墙底图，要和墙体一起染色
            if (decorations != null) decorations.SetWallTint(theme.wallTint);

            if (walls == null) return;

            for (int i = 0; i < walls.Length; i++)
            {
                if (walls[i] != null) walls[i].SetTint(theme.wallTint);
            }
        }

        /// <summary>
        /// 调试用视觉区分：特殊房间地板染色，并在中心放一个同色方块。
        /// 关闭后恢复基础地板色并隐藏标记。
        /// </summary>
        public void ApplyDebugVisuals(RoomType type, bool enabled)
        {
            Color baseTint = _hasFloorTheme ? _floorThemeTint : floorBaseColor;
            Color tint = enabled
                ? RoomTypePalette.FloorTint(type, baseTint)
                : baseTint;

            if (roomFloor != null) roomFloor.SetTint(tint);
            else if (floorRenderer != null) floorRenderer.color = tint;

            if (typeBadge == null) return;

            bool show = enabled && RoomTypePalette.IsSpecial(type);
            typeBadge.gameObject.SetActive(show);

            if (show)
                typeBadge.color = RoomTypePalette.Accent(type);
        }

        /// <summary>
        /// 墙的归属规则：
        ///   上 / 右 永远由本房间绘制；
        ///   下 / 左 若该方向存在邻居，则交给邻居绘制（邻居的"上/右"墙与本房间的"下/左"墙是同一面）。
        /// 这样每条相邻边恰好只有一面墙，门洞也不会重复。
        /// </summary>
        public static bool IsSideOwned(RoomNode node, WallSide side)
        {
            if (side == WallSide.Up || side == WallSide.Right) return true;

            Vector2Int offset = DirUtil.ToOffset(side.ToDoor());
            Vector2Int target = node.Grid + offset;

            for (int i = 0; i < node.Neighbors.Count; i++)
            {
                if (node.Neighbors[i].Grid == target) return false;
            }
            return true;
        }

#if UNITY_EDITOR
        /// <summary>仅供编辑器工厂在生成 Prefab 时写入引用。</summary>
        public void EditorBind(
            Transform center,
            Transform[] entries,
            Transform[] markers,
            SpriteRenderer[] indicators,
            Transform[] triggers,
            RoomDoorBlocker[] blockers,
            Transform[] enemies,
            RoomWall[] roomWalls,
            SpriteRenderer floor,
            SpriteRenderer badge,
            RoomFloor floorTiles,
            RoomDecorations roomDecorations,
            Vector2Int size)
        {
            footprint = size;
            centerPoint = center;
            entryPoints = entries;
            doorMarkers = markers;
            doorIndicators = indicators;
            doorTriggers = triggers;
            doorBlockers = blockers;
            enemySpawnPoints = enemies;
            walls = roomWalls;
            floorRenderer = floor;
            typeBadge = badge;
            roomFloor = floorTiles;
            decorations = roomDecorations;
            floorBaseColor = Color.white;
        }
#endif
    }
}
