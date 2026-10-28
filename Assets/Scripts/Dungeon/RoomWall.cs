using UnityEngine;

namespace Roguelike.Dungeon
{
    /// <summary>房间的四个方向，顺序与 DoorDirections 的位顺序一致。</summary>
    public enum WallSide
    {
        Up = 0,
        Right = 1,
        Down = 2,
        Left = 3,
    }

    public static class WallSideUtil
    {
        public static readonly WallSide[] All = { WallSide.Up, WallSide.Right, WallSide.Down, WallSide.Left };

        /// <summary>方向 → 对应的门位掩码。</summary>
        public static DoorDirections ToDoor(this WallSide side) => (DoorDirections)(1 << (int)side);
    }

    /// <summary>
    /// 房间的一面墙。预制体里由 A 段 / 门段 / B 段三个带碰撞体的 Sprite 组成。
    /// DungeonBuilder 根据门位决定是否隐藏中间的"门段"，从而形成门洞或封死。
    /// </summary>
    public sealed class RoomWall : MonoBehaviour
    {
        [SerializeField] private WallSide side;

        [Tooltip("门洞所在的那一段；有门时隐藏，无门时保留")]
        [SerializeField] private GameObject doorSegment;

        private SpriteRenderer[] _renderers;
        private Sprite[] _defaultSprites;

        public WallSide Side => side;

        /// <summary>门段当前是否启用（启用 = 墙是封死的）。</summary>
        public bool IsDoorSegmentActive => doorSegment != null && doorSegment.activeSelf;

        public void Setup(WallSide wallSide, GameObject doorSeg)
        {
            side = wallSide;
            doorSegment = doorSeg;
        }

        /// <summary>
        /// owned = false：整面墙隐藏，由相邻房间绘制（避免双墙）。
        /// owned = true ：按 hasDoor 打开或封死门洞。
        /// </summary>
        public void Apply(bool owned, bool hasDoor)
        {
            gameObject.SetActive(owned);
            if (!owned) return;

            if (doorSegment != null)
                doorSegment.SetActive(!hasDoor);
        }

        /// <summary>替换整面墙的砖块贴图；传 null 恢复 Prefab 默认贴图。</summary>
        public void SetSprite(Sprite sprite)
        {
            CacheRenderers();

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null) continue;

                Sprite target = sprite != null ? sprite : _defaultSprites[i];
                if (target != null) _renderers[i].sprite = target;
            }
        }

        /// <summary>按楼层主题染色；包括当前隐藏的门段，避免开门后颜色不一致。</summary>
        public void SetTint(Color color)
        {
            CacheRenderers();

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                    _renderers[i].color = color;
            }
        }

        private void CacheRenderers()
        {
            if (_renderers != null) return;

            _renderers = GetComponentsInChildren<SpriteRenderer>(true);
            _defaultSprites = new Sprite[_renderers.Length];

            for (int i = 0; i < _renderers.Length; i++)
                _defaultSprites[i] = _renderers[i] != null ? _renderers[i].sprite : null;
        }
    }
}
