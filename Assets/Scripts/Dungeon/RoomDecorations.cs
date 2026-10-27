using System.Collections.Generic;
using Roguelike.Combat;
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
        [SerializeField] private Sprite chestOpen;

        [Header("房间尺寸")]
        [SerializeField] private int roomWidth = RoomGeometry.FootprintWidth;
        [SerializeField] private int roomHeight = RoomGeometry.FootprintHeight;

        private readonly List<SpriteRenderer> _wallTinted = new List<SpriteRenderer>();
        private Color _wallTint = Color.white;
        private bool _hasWallTint;

        /// <summary>
        /// 墙面装饰的底图自带砖墙颜色，必须和主题墙体一起染色，
        /// 否则墓穴/熔岩层的墙会出现一块块原始棕色。
        /// </summary>
        public void SetWallTint(Color color)
        {
            _wallTint = color;
            _hasWallTint = true;

            for (int i = 0; i < _wallTinted.Count; i++)
                if (_wallTinted[i] != null) _wallTinted[i].color = color;
        }

        public void Apply(RoomType type, DoorDirections doors, int seed)
        {
            if (container == null) container = transform;

            Clear();

            var rng = new Random(seed ^ 0x1f3a5c7);

            float wallTopY = roomHeight - RoomGeometry.WallThickness * 0.5f;
            float innerBottomY = RoomGeometry.WallThickness;
            float innerTopY = roomHeight - RoomGeometry.WallThickness;
            float centerX = roomWidth * 0.5f;
            float centerY = roomHeight * 0.5f;
            bool upDoor = (doors & DoorDirections.Up) != 0;

            // ---- 顶墙：旗帜 ----
            Sprite banner = PickBanner(type, rng);
            int bannerCount = type == RoomType.Boss || type == RoomType.Treasure ||
                type == RoomType.Shop || type == RoomType.Challenge ? 2 : 1;
            Add("Banner_0", banner, new Vector2(3.5f, wallTopY), 1, true);
            if (bannerCount > 1)
                Add("Banner_1", banner, new Vector2(roomWidth - 3.5f, wallTopY), 1, true);

            // ---- 顶墙：破洞 / 苔藓（约三分之一房间） ----
            if (rng.NextDouble() < 0.35)
            {
                Sprite grime = rng.NextDouble() < 0.5 ? wallHole : wallGoo;
                float grimeX = upDoor ? centerX - 2.5f : centerX;
                Add("WallGrime", grime, new Vector2(grimeX, wallTopY), 1, true);
            }

            // ---- 特殊房：立柱 ----
            if (type == RoomType.Boss)
            {
                // Boss 房只在房间正中央立一根柱子，作为唯一掩体；
                // 四周留出的空间足够玩家完整绕柱，Boss 体型更大、转身更笨重。
                BuildColumn(new Vector2(centerX, centerY - 1f));
            }
            else if ((type == RoomType.Treasure || type == RoomType.Shop) && roomWidth >= 13 && roomHeight >= 9)
            {
                // 小房间跳过侧柱，避免遮挡左右入口和三件商品
                BuildColumn(new Vector2(2.0f, innerBottomY + 0.5f));
                BuildColumn(new Vector2(roomWidth - 2.0f, innerBottomY + 0.5f));
            }

            // ---- 地面边缘：木箱 / 骷髅 / 宝箱 ----
            switch (type)
            {
                case RoomType.Treasure:
                {
                    // 放在侧后方，避免堵住上下门之间的通路
                    GameObject chestObject = AddSolid(
                        "Chest",
                        chest,
                        new Vector2(roomWidth - 4.0f, innerTopY - 1.4f),
                        2,
                        new Vector2(0.9f, 0.75f),
                        new Vector2(0f, -0.05f));
                    MakeChestBreakable(chestObject, chestOpen);
                    break;
                }

                case RoomType.Shop:
                {
                    GameObject leftCrate = AddSolid("Crate_L", crate, new Vector2(roomWidth * 0.28f, innerBottomY + 0.7f), 2, new Vector2(0.85f, 1.05f), Vector2.zero);
                    GameObject rightCrate = AddSolid("Crate_R", crate, new Vector2(roomWidth * 0.72f, innerBottomY + 0.7f), 2, new Vector2(0.85f, 1.05f), Vector2.zero);
                    MakeCrateBreakable(leftCrate);
                    MakeCrateBreakable(rightCrate);
                    break;
                }

                case RoomType.Boss:
                    Add("Skull_L", skull, new Vector2(3.0f, innerTopY - 0.6f), 2);
                    Add("Skull_R", skull, new Vector2(roomWidth - 3.0f, innerTopY - 0.6f), 2);
                    Add("Skull_B", skull, new Vector2(centerX, innerBottomY + 0.5f), 2);
                    break;

                case RoomType.Start:
                    MakeCrateBreakable(AddSolid("Crate", crate, new Vector2(roomWidth - 2.2f, innerBottomY + 0.7f), 2, new Vector2(0.85f, 1.05f), Vector2.zero));
                    Add("Skull", skull, new Vector2(2.2f, innerBottomY + 0.5f), 2);
                    break;

                default:
                    bool combat = type == RoomType.Combat || type == RoomType.Challenge;
                    int layout = combat ? rng.Next(3) : 0;

                    // 中央布局已经占用了空间时，少放一些边缘道具，避免房间过挤
                    int props = combat && layout != 0 ? rng.Next(0, 2) : rng.Next(1, 4);

                    var safeSlots = new List<float>(4);
                    float[] candidates = { 2.2f, roomWidth / 3f, roomWidth * 2f / 3f, roomWidth - 2.2f };
                    for (int i = 0; i < candidates.Length; i++)
                    {
                        if (IsBottomSlotSafe(candidates[i], centerX))
                            safeSlots.Add(candidates[i]);
                    }

                    Shuffle(safeSlots, rng);
                    props = Mathf.Min(props, safeSlots.Count);

                    for (int i = 0; i < props; i++)
                    {
                        float x = safeSlots[i];
                        bool isCrate = rng.NextDouble() < 0.45;

                        if (isCrate)
                        {
                            MakeCrateBreakable(AddSolid(
                                "Crate_" + i,
                                crate,
                                new Vector2(x, innerBottomY + 0.7f),
                                2,
                                new Vector2(0.85f, 1.05f),
                                Vector2.zero));
                        }
                        else
                        {
                            Add("Skull_" + i, skull, new Vector2(x, innerBottomY + 0.5f), 2);
                        }
                    }

                    if (combat && layout != 0)
                        ApplyCombatLayout(layout, centerX, centerY);
                    break;
            }
        }

        /// <summary>
        /// 普通战斗房的中央布局模板：只使用可绕行的立柱与木箱，
        /// 避开四个敌人出生点和门洞中线，避免困住敌人。
        /// </summary>
        private void ApplyCombatLayout(int layout, float centerX, float centerY)
        {
            // 只使用低矮木箱，并保持在中线附近，避开上下门的入口点和四个敌人出生点
            switch (layout)
            {
                case 1:
                    AddSolidCrate("LayoutCrate_L", new Vector2(centerX - 2.5f, centerY));
                    AddSolidCrate("LayoutCrate_R", new Vector2(centerX + 2.5f, centerY));
                    break;

                case 2:
                    AddSolidCrate("LayoutCrate_0", new Vector2(centerX - 2.0f, centerY));
                    AddSolidCrate("LayoutCrate_1", new Vector2(centerX, centerY));
                    AddSolidCrate("LayoutCrate_2", new Vector2(centerX + 2.0f, centerY));
                    break;
            }
        }

        private void AddSolidCrate(string objectName, Vector2 localPosition)
        {
            MakeCrateBreakable(AddSolid(
                objectName,
                crate,
                localPosition,
                2,
                new Vector2(0.85f, 1.05f),
                Vector2.zero));
        }

        /// <summary>
        /// 底部边缘道具槽是否安全：避开下门中线、两个下方敌人出生点和墙角。
        /// </summary>
        private bool IsBottomSlotSafe(float x, float centerX)
        {
            if (x < 1.5f || x > roomWidth - 1.5f) return false;
            if (Mathf.Abs(x - centerX) <= 2.2f) return false;
            if (Mathf.Abs(x - 3.5f) <= 1.2f) return false;
            if (Mathf.Abs(x - (roomWidth - 3.5f)) <= 1.2f) return false;
            return true;
        }

        private static void Shuffle<T>(IList<T> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private Sprite PickBanner(RoomType type, Random rng)
        {
            if (banners == null || banners.Length == 0) return null;

            switch (type)
            {
                case RoomType.Boss:
                case RoomType.Challenge:
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
            Add("Column_Base", columnBase, basePosition, 1, true);
            Add("Column_Mid", columnMid, basePosition + Vector2.up, 1, true);
            Add("Column_Top", columnTop, basePosition + Vector2.up * 2f, 1, true);

            // 三段拼成的整体用一个方形碰撞体挡住；Boss 房略矮，留出穿行缝隙
            AddBoxCollider("Column_Obstacle", basePosition + Vector2.up, new Vector2(0.75f, colliderHeight));
        }

        /// <summary>创建纯视觉 Sprite；返回对象以便需要时追加碰撞体。</summary>
        private GameObject Add(
            string objectName,
            Sprite sprite,
            Vector2 localPosition,
            int sortingOrder,
            bool tintWithWall = false)
        {
            if (sprite == null) return null;

            var go = new GameObject(objectName);
            go.transform.SetParent(container, false);
            go.transform.localPosition = localPosition;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;

            if (tintWithWall)
            {
                _wallTinted.Add(renderer);
                if (_hasWallTint) renderer.color = _wallTint;
            }

            return go;
        }

        /// <summary>视觉 + 实心碰撞体（木箱、宝箱）。</summary>
        private GameObject AddSolid(
            string objectName,
            Sprite sprite,
            Vector2 localPosition,
            int sortingOrder,
            Vector2 colliderSize,
            Vector2 colliderOffset)
        {
            GameObject go = Add(objectName, sprite, localPosition, sortingOrder);
            if (go == null) return null;

            var box = go.AddComponent<BoxCollider2D>();
            box.size = colliderSize;
            box.offset = colliderOffset;
            return go;
        }

        /// <summary>木箱：3 次命中后破碎，30% 概率掉 1 金币。</summary>
        private static void MakeCrateBreakable(GameObject crateObject)
        {
            if (crateObject == null) return;

            var prop = crateObject.AddComponent<BreakableProp>();
            prop.Setup(3, 1, 1, 0.3f, null, false);
        }

        /// <summary>宝箱：3 次命中后打开，掉落 3–6 金币并保留打开贴图。</summary>
        private static void MakeChestBreakable(GameObject chestObject, Sprite openSprite)
        {
            if (chestObject == null) return;

            var prop = chestObject.AddComponent<BreakableProp>();
            prop.Setup(3, 3, 6, 1f, openSprite, true);
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
            _wallTinted.Clear();

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
            Sprite chestSprite,
            Sprite chestOpenSprite,
            int width,
            int height)
        {
            roomWidth = width;
            roomHeight = height;

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
            chestOpen = chestOpenSprite;
        }
#endif
    }
}
