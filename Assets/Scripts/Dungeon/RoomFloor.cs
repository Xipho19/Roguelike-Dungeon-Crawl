using UnityEngine;
using Random = System.Random;

namespace Roguelike.Dungeon
{
    /// <summary>
    /// 房间地板的逐格变化。
    ///
    /// Room Prefab 里预置 15×9 个 1×1 的 SpriteRenderer；
    /// 每个房间用自己的种子从 0x72 的 8 种地砖中挑选，并随机翻转，
    /// 因此同一种子布局稳定，不同房间/不同种子图案不同。
    /// </summary>
    public sealed class RoomFloor : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] tiles = new SpriteRenderer[0];
        [SerializeField] private Sprite[] variants = new Sprite[0];

        private Sprite[] _defaultVariants;
        private bool _allowFlip = true;
        private SpriteRenderer _tiledRenderer;
        private bool _tiledActive;

        private void Awake()
        {
            _defaultVariants = variants;
        }

        /// <summary>
        /// 应用楼层主题的地板变体，并沿用房间自己的种子重绘。
        /// themeVariants 为空时恢复 Prefab 的默认地砖。
        /// </summary>
        public void ApplyTheme(Sprite[] themeVariants, int seed, bool allowFlip = true)
        {
            if (themeVariants != null && themeVariants.Length > 0)
                variants = themeVariants;
            else if (_defaultVariants != null && _defaultVariants.Length > 0)
                variants = _defaultVariants;

            _allowFlip = allowFlip;
            SetTiledActive(false);
            Apply(seed);
        }

        /// <summary>
        /// 用一张无缝贴图铺满整个房间内部。
        /// 只保留一个 Tiled SpriteRenderer，隐藏逐格地砖，彻底消除 1×1 格子接缝。
        /// </summary>
        public void ApplyTiled(Sprite sprite, Color tint, float interiorWidth, float interiorHeight)
        {
            if (sprite == null || interiorWidth <= 0f || interiorHeight <= 0f) return;

            if (_tiledRenderer == null)
            {
                var go = new GameObject("Floor_Tiled");
                go.transform.SetParent(transform, false);
                _tiledRenderer = go.AddComponent<SpriteRenderer>();
                _tiledRenderer.sortingOrder = tiles != null && tiles.Length > 0 && tiles[0] != null
                    ? tiles[0].sortingOrder
                    : 0;
            }

            _tiledRenderer.sprite = sprite;
            _tiledRenderer.drawMode = SpriteDrawMode.Tiled;
            _tiledRenderer.tileMode = SpriteTileMode.Continuous;
            _tiledRenderer.size = new Vector2(interiorWidth, interiorHeight);
            _tiledRenderer.transform.localPosition = new Vector3(
                interiorWidth * 0.5f + RoomGeometry.WallThickness,
                interiorHeight * 0.5f + RoomGeometry.WallThickness,
                0f);
            _tiledRenderer.transform.localScale = Vector3.one;
            _tiledRenderer.flipX = false;
            _tiledRenderer.flipY = false;
            _tiledRenderer.color = tint;
            _tiledRenderer.enabled = true;

            SetTiledActive(true);
        }

        private void SetTiledActive(bool active)
        {
            _tiledActive = active;

            if (_tiledRenderer != null) _tiledRenderer.enabled = active;

            if (tiles == null) return;
            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i] != null) tiles[i].enabled = !active;
            }
        }

        /// <summary>按种子给每格挑地砖。可重复调用，结果只取决于种子。</summary>
        public void Apply(int seed)
        {
            if (tiles == null || tiles.Length == 0) return;
            if (variants == null || variants.Length == 0) return;

            var rng = new Random(seed);

            for (int i = 0; i < tiles.Length; i++)
            {
                SpriteRenderer tile = tiles[i];
                if (tile == null) continue;

                // 额外 5 份权重留给第 0 号“干净”地砖，避免满屋都是裂纹
                int index = rng.Next(0, variants.Length + 5);
                if (index >= variants.Length) index = 0;

                Sprite sprite = variants[index];
                if (sprite != null) tile.sprite = sprite;

                if (_allowFlip)
                {
                    tile.flipX = rng.Next(2) == 0;
                    tile.flipY = rng.Next(2) == 0;
                }
                else
                {
                    tile.flipX = false;
                    tile.flipY = false;
                }
            }
        }

        /// <summary>调试配色：统一给所有地砖染色。</summary>
        public void SetTint(Color color)
        {
            if (_tiledRenderer != null) _tiledRenderer.color = color;

            if (tiles == null) return;

            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i] != null) tiles[i].color = color;
            }
        }

        /// <summary>当前是否在用整间平铺地面。</summary>
        public bool TiledActive => _tiledActive;

#if UNITY_EDITOR
        public void EditorBind(SpriteRenderer[] tileRenderers, Sprite[] floorVariants)
        {
            tiles = tileRenderers;
            variants = floorVariants;
        }
#endif
    }
}
