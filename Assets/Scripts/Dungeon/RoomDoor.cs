using UnityEngine;

namespace Roguelike.Dungeon
{
    /// <summary>
    /// 门位置的标记组件，后续可挂触发器、开门动画或房间切换逻辑。
    /// 只有实际存在门的那一侧、且由本房间拥有墙体时才会启用。
    /// </summary>
    public sealed class RoomDoor : MonoBehaviour
    {
        [SerializeField] private WallSide side;

        public WallSide Side => side;

        public void Setup(WallSide wallSide) => side = wallSide;
    }
}
