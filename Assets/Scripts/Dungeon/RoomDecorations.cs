using UnityEngine;
using Random = System.Random;

namespace Roguelike.Dungeon
{
    /// <summary>
    /// 按房间类型摆放 0x72 的墙面与地面装饰。
    ///
    /// 全部是纯视觉对象，不加碰撞体，排序在角色之下：
    ///   - 顶墙：旗帜、破洞/苔藓
    ///   - 特殊房：左右立柱
    ///   - 地面边缘：木箱、骷髅
    ///   - 宝箱房：中央后方一个宝箱
    ///
    /// 位置固定在房间边缘，避开中央战斗区、敌人出生点和门洞。
    /// 装饰对象由种子决定，同一房间每次重建结果一致。
    /// </summary>
    public sealed class RoomDecorations : MonoBehaviour
    {
        [SerializeField] private Transform container;
        [SerializeField] private Sprite[] banners = new Sprite[0];
        [SerializeField] private Sprite columnTop;
        [SerializeField] private Sprite columnMid;
        [SerializeField] private Sprite columnBase;
        [SerializeField] private Sprite crate;
        [SerializeField] private Sprite skull;
        [SerializeField] private Sprite wallHole;
        [SerializeField] private Sprite wallGoo;
        [SerializeField] private Sprite chest;

        public void Apply(RoomType type, DoorDirections doors, int seed)
        {
            if (container == null) container = transform;

            Clear();

            var rng = new Random(seed ^ 0x1f3a5c7);

            float wallTopY = RoomGeometry.FootprintHeight - RoomGeometry.WallThickness * 0.5f; // 8.5
            float innerBottomY = RoomGeometry.WallThickness; // 1.0
            float innerTopY = RoomGeometry.FootprintHeight - RoomGeometry.WallThickness; // 8.0
            bool upDoor = (doors & DoorDirections.Up) != 0;

            // ---- 顶墙：旗帜 ----
            Sprite banner = PickBanner(type, rng);
            int bannerCount = type == RoomType.Boss || type == RoomType.Treasure || type == RoomType.Shop ? 2 : 1;
            Add("Banner_0", banner, new Vector2(3.5f, wallTopY), 1);
            if (bannerCount > 1)
                Add("Banner_1", banner, new Vector2(11.5f, wallTopY), 1);

            // ---- 顶墙：破洞 / 苔藓（约三分之一房间） ----
            if (rng.NextDouble() < 0.35)
            {
                Sprite grime = rng.NextDouble() < 0.5 ? wallHole : wallGoo;
                float grimeX = upDoor ? 5.0f : 7.5f;
                Add("WallGrime", grime, new Vector2(grimeX, wallTopY), 1);
            }

            // ---- 特殊房：立柱 ----
            if (type == RoomType.Boss)
            {
                // Boss 房只在房间正中央立一根柱子，作为唯一掩体；
                // 四周留出的空间足够玩家完整绕柱，Boss 体型更大、转身更笨重。
                BuildColumn(new Vector2(RoomGeometry.Center.x, RoomGeometry.Center.y - 1f));
            }
            else if (type == RoomType.Treasure || type == RoomType.Shop)
            {
                BuildColumn(new Vector2(2.0f, innerBottomY + 0.5f));
                BuildColumn(new Vector2(13.0f, innerBottomY + 0.5f));
            }

            // ---- 地面边缘：木箱 / 骷髅 / 宝箱 ----
            switch (type)
            {
                case RoomType.Treasure:
                    // 放在侧后方，避免堵住上下门之间的通路
                    AddSolid("Chest", chest, new Vector2(11.0f, innerTopY - 1.4f), 2, new Vector2(0.9f, 0.75f), new Vector2(0f, -0.05f));
                    break;

                case RoomType.Shop:
                    AddSolid("Crate_L", crate, new Vector2(4.2f, innerBottomY + 0.7f), 2, new Vector2(0.85f, 1.05f), Vector2.zero);
                    AddSolid("Crate_R", crate, new Vector2(10.8f, innerBottomY + 0.7f), 2, new Vector2(0.85f, 1.05f), Vector2.zero);
                    break;

                case RoomType.Boss:
                    Add("Skull_L", skull, new Vector2(3.0f, innerTopY - 0.6f), 2);
                    Add("Skull_R", skull, new Vector2(12.0f, innerTopY - 0.6f), 2);
                    Add("Skull_B", skull, new Vector2(7.5f, innerBottomY + 0.5f), 2);
                    break;

                case RoomType.Start:
                    AddSolid("Crate", crate, new Vector2(12.8f, innerBottomY + 0.7f), 2, new Vector2(0.85f, 1.05f), Vector2.zero);
                    Add("Skull", skull, new Vector2(2.2f, innerBottomY + 0.5f), 2);
                    break;

                default:
                    int props = rng.Next(1, 4);
                    float[] edgeXs = { 2.2f, 5.0f, 10.0f, 12.8f };
                    int[] slots = { 0, 1, 2, 3 };

                    // 打乱槽位，保证同房间的道具不会叠在同一处
                    for (int i = slots.Length - 1; i > 0; i--)
                    {
                        int j = rng.Next(i + 1);
                        (slots[i], slots[j]) = (slots[j], slots[i]);
                    }

                    for (int i = 0; i < props; i++)
                    {
                        float x = edgeXs[slots[i]];
                        bool isCrate = rng.NextDouble() < 0.45;

                        if (isCrate)
                        {
                            AddSolid(
                                "Crate_" + i,
                                crate,
                                new Vector2(x, innerBottomY + 0.7f),
                                2,
                                new Vector2(0.85f, 1.05f),
                                Vector2.zero);
                        }
                        else
                        {
                            Add(
                                "Skull_" + i,
                                skull,
                                new Vector2(x, innerBottomY + 0.5f),
                                2);
                        }
                    }
                    break;
            }
        }

        private Sprite PickBanner(RoomType type, Random rng)
        {
            if (banners == null || banners.Length == 0) return null;

            switch (type)
            {
                case RoomType.Boss:
                    return banners[Mathf.Min(2, banners.Length - 1)]; // 红
                case RoomType.Treasure:
                    return banners[Mathf.Min(3, banners.Length - 1)]; // 黄
                case RoomType.Shop:
                case RoomType.Start:
                    return banners[Mathf.Min(1, banners.Length - 1)]; // 绿
                default:
                    return banners[rng.Next(banners.Length)];
            }
        }

        /// <summary>底座 + 柱身 + 柱头，自下而上叠成一根 3 格高的立柱。</summary>
        private void BuildColumn(Vector2 basePosition, float colliderHeight = 2.7f)
        {
            Add("Column_Base", columnBase, basePosition, 1);
            Add("Column_Mid", columnMid, basePosition + Vector2.up, 1);
            Add("Column_Top", columnTop, basePosition + Vector2.up * 2f, 1);

            // 三段拼成的整体用一个方形碰撞体挡住；Boss 房略矮，留出穿行缝隙
            AddBoxCollider("Column_Obstacle", basePosition + Vector2.up, new Vector2(0.75f, colliderHeight));
        }

        /// <summary>创建纯视觉 Sprite；返回对象以便需要时追加碰撞体。</summary>
        private GameObject Add(string objectName, Sprite sprite, Vector2 localPosition, int sortingOrder)
        {
            if (sprite == null) return null;

            var go = new GameObject(objectName);
            go.transform.SetParent(container, false);
            go.transform.localPosition = localPosition;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;

            return go;
        }

        /// <summary>视觉 + 实心碰撞体（木箱、宝箱）。</summary>
        private void AddSolid(
            string objectName,
            Sprite sprite,
            Vector2 localPosition,
            int sortingOrder,
            Vector2 colliderSize,
            Vector2 colliderOffset)
        {
            GameObject go = Add(objectName, sprite, localPosition, sortingOrder);
            if (go == null) return;

            var box = go.AddComponent<BoxCollider2D>();
            box.size = colliderSize;
            box.offset = colliderOffset;
        }

        /// <summary>只有碰撞体的障碍物（立柱整体）。</summary>
        private void AddBoxCollider(string objectName, Vector2 localPosition, Vector2 size)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(container, false);
            go.transform.localPosition = localPosition;

            var box = go.AddComponent<BoxCollider2D>();
            box.size = size;
        }

        private void Clear()
        {
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                GameObject child = container.GetChild(i).gameObject;

                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
        }

#if UNITY_EDITOR
        public void EditorBind(
            Transform decorContainer,
            Sprite[] bannerSprites,
            Sprite columnTopSprite,
            Sprite columnMidSprite,
            Sprite columnBaseSprite,
            Sprite crateSprite,
            Sprite skullSprite,
            Sprite wallHoleSprite,
            Sprite wallGooSprite,
            Sprite chestSprite)
        {
            container = decorContainer;
            banners = bannerSprites;
            columnTop = columnTopSprite;
            columnMid = columnMidSprite;
            columnBase = columnBaseSprite;
            crate = crateSprite;
            skull = skullSprite;
            wallHole = wallHoleSprite;
            wallGoo = wallGooSprite;
            chest = chestSprite;
        }
#endif
    }
}
