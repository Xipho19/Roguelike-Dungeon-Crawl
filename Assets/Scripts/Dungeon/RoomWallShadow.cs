using UnityEngine;

namespace Roguelike.Dungeon
{
    /// <summary>
    /// 墙体投在地面上的柔和阴影：沿房间内侧四面墙铺一条渐变条带。
    ///
    /// 纯视觉，无碰撞，排序在地砖之上、门标记之下，因此不会影响任何玩法逻辑。
    /// 门洞位置留空，避免阴影盖住门口。
    /// </summary>
    public sealed class RoomWallShadow : MonoBehaviour
    {
        private const string SpritePath = "FBU/wall_shadow";

        [Tooltip("阴影延伸进房间的深度（世界单位）")]
        [SerializeField] private float depth = 0.75f;

        [Tooltip("阴影最深处的整体强度")]
        [SerializeField, Range(0f, 1f)] private float strength = 0.85f;

        /// <summary>阴影条带的 sortingOrder：在地砖（-10）之上、门标记（-8）之下。</summary>
        public const int SortingOrder = -9;

        private static Sprite _sprite;

        public static Sprite ShadowSprite
        {
            get
            {
                if (_sprite == null) _sprite = Resources.Load<Sprite>(SpritePath);
                return _sprite;
            }
        }

        /// <summary>按房间尺寸重建四面墙的阴影条带。</summary>
        public void Rebuild(int width, int height, DoorDirections doors)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }

            Sprite sprite = ShadowSprite;
            if (sprite == null) return;

            float w = width;
            float h = height;
            float t = RoomGeometry.WallThickness;
            float d = Mathf.Max(0.1f, depth);

            // 四面墙：内边界到内边界
            AddStrip(sprite, "Shadow_Up", new Vector2(w * 0.5f, h - t - d * 0.5f), new Vector2(w - t * 2f, d), 0f, doors, DoorDirections.Up, true);
            AddStrip(sprite, "Shadow_Down", new Vector2(w * 0.5f, t + d * 0.5f), new Vector2(w - t * 2f, d), 180f, doors, DoorDirections.Down, true);
            AddStrip(sprite, "Shadow_Left", new Vector2(t + d * 0.5f, h * 0.5f), new Vector2(h - t * 2f, d), 90f, doors, DoorDirections.Left, false);
            AddStrip(sprite, "Shadow_Right", new Vector2(w - t - d * 0.5f, h * 0.5f), new Vector2(h - t * 2f, d), 270f, doors, DoorDirections.Right, false);
        }

        /// <summary>铺一条阴影；该侧有门时拆成两段，避开门洞。</summary>
        private void AddStrip(
            Sprite sprite,
            string objectName,
            Vector2 center,
            Vector2 size,
            float angle,
            DoorDirections doors,
            DoorDirections side,
            bool horizontal)
        {
            bool hasDoor = (doors & side) != 0;
            float doorHalf = RoomGeometry.DoorWidth * 0.5f;

            if (!hasDoor)
            {
                CreateBar(sprite, objectName, center, size, angle);
                return;
            }

            // 两种朝向下 size.x 都是"条带长度"，size.y 都是"伸进房间的深度"
            float half = size.x * 0.5f;
            if (half <= doorHalf + 0.05f) return;

            float piece = half - doorHalf;
            float offset = doorHalf + piece * 0.5f;

            if (horizontal)
            {
                CreateBar(sprite, objectName + "_A", center + new Vector2(-offset, 0f), new Vector2(piece, size.y), angle);
                CreateBar(sprite, objectName + "_B", center + new Vector2(offset, 0f), new Vector2(piece, size.y), angle);
            }
            else
            {
                CreateBar(sprite, objectName + "_A", center + new Vector2(0f, -offset), new Vector2(piece, size.y), angle);
                CreateBar(sprite, objectName + "_B", center + new Vector2(0f, offset), new Vector2(piece, size.y), angle);
            }
        }

        private void CreateBar(Sprite sprite, string objectName, Vector2 localPosition, Vector2 size, float angle)
        {
            if (size.x <= 0.01f || size.y <= 0.01f) return;

            var go = new GameObject(objectName);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(0f, 0f, angle);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.drawMode = SpriteDrawMode.Simple;
            renderer.sortingOrder = SortingOrder;
            renderer.color = new Color(0f, 0f, 0f, strength);

            // 贴图本身 1×1 世界单位（32px / PPU32），用缩放拉成条带
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
        }
    }
}
