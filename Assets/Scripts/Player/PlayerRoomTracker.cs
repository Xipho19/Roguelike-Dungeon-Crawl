using System;
using Roguelike.Dungeon;
using UnityEngine;

namespace Roguelike.Player
{
    /// <summary>
    /// 跟踪玩家当前所在房间，并处理房间之间的切换。
    ///
    /// 切换规则：
    ///   1. 玩家在 A 房间，从 A 的某扇门离开；
    ///   2. 传送到该门在邻居 B 中"对应方向"的入口点（1.5 格内）；
    ///   3. 更新 CurrentRoom 并触发 RoomChanged，相机据此吸附。
    ///
    /// 切换后有一个短冷却，避免刚落地就被反向触发。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerRoomTracker : MonoBehaviour
    {
        [SerializeField] private DungeonBuilder builder;

        [Tooltip("切换后的冷却时间（秒）")]
        [SerializeField] private float transitionCooldown = 0.15f;

        private Rigidbody2D _rb;
        private RoomView _currentRoom;
        private float _cooldownUntil;

        public RoomView CurrentRoom => _currentRoom;

        /// <summary>当前是否允许切换房间。</summary>
        public bool CanTransition => _currentRoom != null && Time.time >= _cooldownUntil;

        /// <summary>移动速度，供门触发判断玩家是否在朝门内走。</summary>
        public Vector2 Velocity => _rb != null ? _rb.linearVelocity : Vector2.zero;

        /// <summary>房间切换后触发（包含初始化时的第一次）。</summary>
        public event Action<RoomView> RoomChanged;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            if (builder == null) builder = FindFirstObjectByType<DungeonBuilder>();
            if (builder == null)
            {
                Debug.LogWarning("[Player] 找不到 DungeonBuilder，房间跟踪已禁用。", this);
                enabled = false;
                return;
            }

            // 编辑模式构建的 DungeonData 不会进入 Play Mode，这里按需重建一次
            builder.EnsureBuilt();

            if (builder.Data == null)
            {
                Debug.LogWarning("[Player] DungeonBuilder 未能构建地牢，房间跟踪已禁用。", this);
                enabled = false;
                return;
            }

            // 每局都从起点房开始，避免沿用在旧地图里的坐标
            _currentRoom = builder.GetRoom(builder.Data.Start.Grid);

            if (_currentRoom != null)
            {
                Vector2 center = _currentRoom.WorldCenter;

                if (_rb != null)
                    _rb.position = center;
                else
                    transform.position = new Vector3(center.x, center.y, transform.position.z);
            }
            else
            {
                _currentRoom = FindRoomAt(transform.position);
            }

            if (_currentRoom != null)
            {
                _currentRoom.IsVisited = true;
                RoomChanged?.Invoke(_currentRoom);
            }
        }

        /// <summary>尝试从当前房间的某一侧门离开。成功返回 true。</summary>
        public bool RequestTransition(WallSide side)
        {
            if (!CanTransition || builder == null || _currentRoom == null) return false;

            DoorDirections door = side.ToDoor();
            if ((_currentRoom.Doors & door) == 0) return false;

            Vector2Int targetGrid = _currentRoom.GridPosition + DirUtil.ToOffset(door);
            RoomView target = builder.GetRoom(targetGrid);
            if (target == null) return false;

            // 从 A 的上门离开 → 到达 B 时位于 B 的下门入口
            WallSide opposite = Opposite(side);
            Transform entry = target.GetEntryPoint(opposite);
            Vector2 destination = entry != null ? (Vector2)entry.position : target.WorldCenter;

            // 直接设置刚体位置，避免与插值/物理同步冲突
            if (_rb != null)
                _rb.position = destination;
            else
                transform.position = new Vector3(destination.x, destination.y, transform.position.z);

            _currentRoom = target;
            _currentRoom.IsVisited = true;
            _cooldownUntil = Time.time + transitionCooldown;

            RoomChanged?.Invoke(target);
            return true;
        }

        /// <summary>直接传送到指定房间（复活、传送、调试用）。</summary>
        public void TeleportTo(RoomView room)
        {
            if (room == null) return;

            Vector2 center = room.WorldCenter;

            if (_rb != null)
            {
                _rb.position = center;
                _rb.linearVelocity = Vector2.zero;
            }
            else
            {
                transform.position = new Vector3(center.x, center.y, transform.position.z);
            }

            _currentRoom = room;
            _currentRoom.IsVisited = true;
            _cooldownUntil = Time.time + transitionCooldown;
            RoomChanged?.Invoke(room);
        }

        private RoomView FindRoomAt(Vector2 position)
        {
            if (builder == null) return null;

            // 房间尺寸不统一，直接按世界矩形查找；共享墙处的重叠由遍历顺序决定。
            return builder.GetRoomAt(position);
        }

        private static WallSide Opposite(WallSide side)
        {
            switch (side)
            {
                case WallSide.Up: return WallSide.Down;
                case WallSide.Down: return WallSide.Up;
                case WallSide.Left: return WallSide.Right;
                default: return WallSide.Left;
            }
        }
    }
}
