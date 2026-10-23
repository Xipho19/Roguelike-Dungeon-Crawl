using Roguelike.Player;
using UnityEngine;

namespace Roguelike.Dungeon
{
    /// <summary>
    /// 门洞触发盒。挂在每个"有门"的方向上（左右两侧各一个，不依赖墙体归属）。
    ///
    /// 触发条件：
    ///   1. 进入者带有 PlayerRoomTracker；
    ///   2. 玩家当前所在房间就是本触发盒所属房间；
    ///   3. 玩家速度朝向该门（避免贴门站立或反向移动时误切）；
    ///   4. 不在切换冷却中。
    /// 满足后请求房间切换。
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class RoomDoorTrigger : MonoBehaviour
    {
        [SerializeField] private WallSide side;

        private RoomView _room;

        public WallSide Side => side;

        public void Setup(WallSide wallSide) => side = wallSide;

        private void OnTriggerStay2D(Collider2D other)
        {
            if (_room == null) _room = GetComponentInParent<RoomView>();
            if (_room == null) return;

            var tracker = other.GetComponentInParent<PlayerRoomTracker>();
            if (tracker == null || tracker.CurrentRoom != _room) return;
            if (!tracker.CanTransition) return;
            if (_room.DoorsLocked) return;

            Vector2 direction = DirUtil.ToOffset(side.ToDoor());
            if (Vector2.Dot(tracker.Velocity, direction) <= 0.1f) return;

            tracker.RequestTransition(side);
        }
    }
}
