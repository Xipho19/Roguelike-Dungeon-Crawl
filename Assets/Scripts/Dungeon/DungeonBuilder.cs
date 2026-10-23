using System.Collections.Generic;
using UnityEngine;

namespace Roguelike.Dungeon
{
    /// <summary>
    /// 把 <see cref="DungeonData"/> 转换为场景中的房间实例。
    ///
    /// 坐标约定：
    ///   房间世界坐标 = 本物体位置 + 网格坐标 × roomSpacing
    ///   每个房间 Prefab 的原点位于 footprint 左下角。
    ///
    /// 该类不参与房间拓扑生成，只负责实例化与配置；可以在编辑器和运行时通用。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DungeonBuilder : MonoBehaviour
    {
        public const string PrefabResourcePath = "Dungeon/Room";

        [Header("生成参数")]
        [SerializeField] private DungeonConfig config = new DungeonConfig();
        [SerializeField] private int seed = 20261022;

        [Header("房间")]
        [Tooltip("留空时会尝试从 Resources/Dungeon/Room 加载")]
        [SerializeField] private RoomView roomPrefab;

        [SerializeField] private Vector2Int roomSpacing = new Vector2Int(RoomGeometry.FootprintWidth - 1, RoomGeometry.FootprintHeight - 1);

        [Header("调试")]
        [Tooltip("按房间类型给地板着色，并给特殊房间加中心色块")]
        [SerializeField] private bool debugRoomVisuals = true;

        [Header("运行时")]
        [SerializeField] private bool buildOnStart;

        private readonly List<RoomView> _rooms = new List<RoomView>();
        private readonly Dictionary<Vector2Int, RoomView> _byGrid = new Dictionary<Vector2Int, RoomView>();
        private DungeonData _data;

        public DungeonData Data => _data;
        public IReadOnlyList<RoomView> Rooms => _rooms;
        public DungeonConfig Config => config;
        public int Seed => seed;
        public Vector2Int RoomSpacing => roomSpacing;
        public RoomView RoomPrefab => roomPrefab;

        private void Start()
        {
            if (buildOnStart) EnsureBuilt();
        }

        /// <summary>
        /// 保证场景里已经有一层地牢。
        ///
        /// DungeonData 是纯运行时对象，不会随场景序列化；进入 Play Mode 后 _data 一定为 null。
        /// 因此运行时需要按需重建一次（同 seed 会得到完全相同的布局）。
        /// </summary>
        public DungeonData EnsureBuilt()
        {
            if (_data == null) BuildFromSeed();
            return _data;
        }

        /// <summary>重开一局时更换种子。</summary>
        public void SetSeed(int newSeed) => seed = newSeed;

        /// <summary>按当前 seed 与 config 生成一层并构建。</summary>
        public DungeonData BuildFromSeed()
        {
            return Build(DungeonGenerator.Generate(config, seed));
        }

        /// <summary>
        /// 用给定的拓扑数据构建场景。传入 null 时会按 seed 重新生成。
        /// 调用前会清空本物体下已有的房间。
        /// </summary>
        public DungeonData Build(DungeonData data)
        {
            if (data == null) data = DungeonGenerator.Generate(config, seed);

            _data = data;
            Clear();

            RoomView prefab = ResolvePrefab();
            if (prefab == null)
            {
                Debug.LogError("[Dungeon] 找不到房间 Prefab，请先执行 Tools/Roguelike/生成房间 Prefab。", this);
                return data;
            }

            Vector3 origin = transform.position;

            for (int i = 0; i < data.Rooms.Count; i++)
            {
                RoomNode node = data.Rooms[i];

                RoomView view = Instantiate(prefab, transform);
                view.name = $"Room_{node.Grid.x}_{node.Grid.y}_{node.Type}";
                view.transform.position = origin + new Vector3(
                    node.Grid.x * roomSpacing.x,
                    node.Grid.y * roomSpacing.y,
                    0f);

                view.Configure(node, debugRoomVisuals);
                view.ApplyRoomVariation(seed);

                _rooms.Add(view);
                _byGrid[node.Grid] = view;
            }

            return data;
        }

        /// <summary>销毁本物体下所有已生成的房间。</summary>
        public void Clear()
        {
            _rooms.Clear();
            _byGrid.Clear();

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
        }

        public RoomView GetRoom(Vector2Int grid)
        {
            return _byGrid.TryGetValue(grid, out RoomView room) ? room : null;
        }

        /// <summary>世界坐标 → 房间网格坐标（不考虑墙体，仅按 footprint 估算）。</summary>
        public Vector2Int WorldToGrid(Vector2 worldPosition)
        {
            Vector3 local = worldPosition - (Vector2)transform.position;
            return new Vector2Int(
                Mathf.FloorToInt(local.x / roomSpacing.x),
                Mathf.FloorToInt(local.y / roomSpacing.y));
        }

        private RoomView ResolvePrefab()
        {
            if (roomPrefab != null) return roomPrefab;

            roomPrefab = Resources.Load<RoomView>(PrefabResourcePath);
#if UNITY_EDITOR
            if (roomPrefab == null)
                roomPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<RoomView>("Assets/Resources/Dungeon/Room.prefab");
#endif
            return roomPrefab;
        }

        /// <summary>运行时/编辑器切换调试配色，不需要重建房间。</summary>
        public void SetDebugVisuals(bool enabled)
        {
            debugRoomVisuals = enabled;

            for (int i = 0; i < _rooms.Count; i++)
                _rooms[i].ApplyDebugVisuals(_rooms[i].RoomType, enabled);
        }

        [ContextMenu("Build Dungeon")]
        private void BuildInEditor()
        {
            BuildFromSeed();
        }

        [ContextMenu("切换房间类型配色")]
        private void ToggleDebugVisuals()
        {
            SetDebugVisuals(!debugRoomVisuals);
        }
    }
}
