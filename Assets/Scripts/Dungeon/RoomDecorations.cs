using System.Collections.Generic;
using Roguelike.Combat;
using Roguelike.Visual;
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
        private Sprite _activeCrateSprite;
        private Sprite[] _crateVariants;
        private FloorTheme _theme;
        private Random _applyRng;
        private Color _wallTint = Color.white;
        private bool _hasWallTint;
        private bool _legacyDecorVisible = true;

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

        /// <summary>
        /// 只隐藏旧版纯视觉装饰（旗帜、墙面污渍）；木箱、宝箱、立柱、骷髅等玩法物件保留。
        /// 骷髅现在有碰撞体、可被玩家踩到，隐藏会导致"看不见的墙"。
        /// </summary>
        public void SetLegacyDecorVisible(bool visible)
        {
            _legacyDecorVisible = visible;
            ApplyLegacyVisibility();
        }

        private void ApplyLegacyVisibility()
        {
            if (container == null) return;

            for (int i = 0; i < container.childCount; i++)
            {
                Transform child = container.GetChild(i);
                string n = child.name;
                if (n.StartsWith("Banner_") || n.StartsWith("WallGrime"))
                    child.gameObject.SetActive(_legacyDecorVisible);
            }
        }

        public void Apply(RoomType type, DoorDirections doors, int seed, FloorTheme theme = null,
            int width = 0, int height = 0)
        {
            if (container == null) container = transform;

            // 房间尺寸必须用实例的真实 footprint：Prefab 上序列化的默认值可能和本房间不一致，
            // 会导致贴墙物件按错误的墙位摆放（越界进墙或悬空）。
            if (width > 0) roomWidth = width;
            if (height > 0) roomHeight = height;

            Clear();

            var rng = new Random(seed ^ 0x1f3a5c7);

            Sprite chestSprite = theme != null && theme.chestSprite != null ? theme.chestSprite : chest;
            Sprite chestOpenSprite = theme != null && theme.chestOpenSprite != null ? theme.chestOpenSprite : chestOpen;
            Sprite skullSprite = theme != null && theme.skullSprite != null ? theme.skullSprite : skull;

            _theme = theme;
            _applyRng = rng;
            _crateVariants = theme != null && theme.crateSprites != null && theme.crateSprites.Length > 0
                ? theme.crateSprites
                : null;
            _activeCrateSprite = PickCrateSprite(rng);

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
                BuildColumn(new Vector2(centerX, centerY - 1f), theme);
            }
            else if ((type == RoomType.Treasure || type == RoomType.Shop) && roomWidth >= 13 && roomHeight >= 9)
            {
                // 小房间跳过侧柱，避免遮挡左右入口和三件商品
                BuildColumn(new Vector2(2.0f, innerBottomY + 0.5f), theme);
                BuildColumn(new Vector2(roomWidth - 2.0f, innerBottomY + 0.5f), theme);
            }

            // ---- 地面边缘：木箱 / 骷髅 / 宝箱 ----
            switch (type)
            {
                case RoomType.Treasure:
                {
                    // 放在侧后方，避免堵住上下门之间的通路
                    GameObject chestObject = AddSolid(
                        "Chest",
                        chestSprite,
                        new Vector2(roomWidth - 4.0f, innerTopY - 1.4f),
                        2,
                        new Vector2(0.9f, 0.75f),
                        new Vector2(0f, -0.05f));
                    MakeChestBreakable(chestObject, chestOpenSprite);
                    break;
                }

                case RoomType.Shop:
                {
                    GameObject leftCrate = AddSolid("Crate_L", PickCrateSprite(rng), new Vector2(roomWidth * 0.28f, innerBottomY + 0.7f), 2, new Vector2(0.85f, 1.05f), Vector2.zero);
                    GameObject rightCrate = AddSolid("Crate_R", PickCrateSprite(rng), new Vector2(roomWidth * 0.72f, innerBottomY + 0.7f), 2, new Vector2(0.85f, 1.05f), Vector2.zero);
                    MakeCrateBreakable(leftCrate);
                    MakeCrateBreakable(rightCrate);
                    break;
                }

                case RoomType.Boss:
                    // 靠墙角摆放：避开 x=3.5 / width-3.5 的敌人出生点；x/y 都按贴图尺寸贴齐内墙
                    AddSolidClutter("Skull_L", skullSprite,
                        new Vector2(ClutteredLeftX(skullSprite, RoomGeometry.WallThickness, 0.8f),
                            ClutteredTopY(skullSprite, innerTopY)));
                    AddSolidClutter("Skull_R", skullSprite,
                        new Vector2(ClutteredRightX(skullSprite, roomWidth - RoomGeometry.WallThickness, 0.8f),
                            ClutteredTopY(skullSprite, innerTopY)));
                    // 底部骷髅挪开中线，避免堵住下门入口
                    AddSolidClutter("Skull_B", skullSprite,
                        new Vector2(centerX - 3.2f, ClutteredBottomY(skullSprite, innerBottomY)));
                    break;

                case RoomType.Start:
                    MakeCrateBreakable(AddSolid("Crate", PickCrateSprite(rng), new Vector2(roomWidth - 2.2f, innerBottomY + 0.7f), 2, new Vector2(0.85f, 1.05f), Vector2.zero));
                    AddSolidClutter("Skull", skullSprite,
                        new Vector2(ClutteredLeftX(skullSprite, RoomGeometry.WallThickness, 1.2f),
                            ClutteredBottomY(skullSprite, innerBottomY)));
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
                                PickCrateSprite(rng),
                                new Vector2(x, innerBottomY + 0.7f),
                                2,
                                new Vector2(0.85f, 1.05f),
                                Vector2.zero));
                        }
                        else
                        {
                            AddSolidClutter("Skull_" + i, skullSprite, new Vector2(x, ClutteredBottomY(skullSprite, innerBottomY)));
                        }
                    }

                    if (combat && layout != 0)
                        ApplyCombatLayout(layout, centerX, centerY);
                    break;
            }

            ApplyThemeDecor(type, theme, rng, innerBottomY, innerTopY, centerX);
            ApplyLegacyVisibility();
        }

        /// <summary>
        /// 主题专属的场景装饰：Boss 房火盆、宝箱房石瓮、普通房间角落杂物。
        /// 全部是纯视觉对象，不新增碰撞，也不占用中央战斗区。
        /// </summary>
        private void ApplyThemeDecor(
            RoomType type,
            FloorTheme theme,
            Random rng,
            float innerBottomY,
            float innerTopY,
            float centerX)
        {
            if (theme == null) return;

            float topY = ClutteredTopY(theme.brazierSprite, innerTopY);

            if (type == RoomType.Boss)
            {
                float brazierY = ClutteredBottomY(theme.brazierSprite, innerBottomY);
                Add("Brazier_L", theme.brazierSprite, new Vector2(2.0f, brazierY), 2);
                Add("Brazier_R", theme.brazierSprite, new Vector2(roomWidth - 2.0f, brazierY), 2);
                return;
            }

            if (type == RoomType.Treasure)
            {
                Add("Brazier_L", theme.brazierSprite,
                    new Vector2(ClutteredLeftX(theme.brazierSprite, RoomGeometry.WallThickness, 0.8f), topY), 2);
                // 放在右上角：远离所有敌人出生点（x=3.5 / width-3.5 / centerX）与门洞入口
                AddThemeClutter("Urn_R", theme.urnSprite,
                    new Vector2(ClutteredRightX(theme.urnSprite, roomWidth - RoomGeometry.WallThickness, 0.8f),
                        ClutteredTopY(theme.urnSprite, innerTopY)));
                return;
            }

            if (type == RoomType.Shop)
            {
                AddThemeClutter("Urn_L", theme.urnSprite,
                    new Vector2(ClutteredLeftX(theme.urnSprite, RoomGeometry.WallThickness, 1.0f),
                        ClutteredTopY(theme.urnSprite, innerTopY)));
                return;
            }

            // 起点房 / 战斗房 / 挑战房 / 秘密房：上方两角随机摆放 1–2 件杂物。
            // x 固定在 1.8 附近，避开 3.5 处的敌人出生点；y 按贴图高度贴齐内墙，
            // 避免高物件（石柱、雕花柱）顶部插进墙里。
            Sprite left = PickThemeClutter(theme, rng);
            if (left != null && (type == RoomType.Start || rng.NextDouble() < 0.7))
                AddThemeClutter("Clutter_L", left,
                    new Vector2(ClutteredLeftX(left, RoomGeometry.WallThickness, 0.8f), ClutteredTopY(left, innerTopY)));

            Sprite right = PickThemeClutter(theme, rng);
            if (right != null && (type == RoomType.Start || rng.NextDouble() < 0.7))
                AddThemeClutter("Clutter_R", right,
                    new Vector2(ClutteredRightX(right, roomWidth - RoomGeometry.WallThickness, 0.8f), ClutteredTopY(right, innerTopY)));
        }

        /// <summary>
        /// 上方贴墙摆放时，按贴图实际高度算出中心点，让物件顶部刚好贴住内墙下沿。
        /// 这样 1 格的小物件仍在原位，2–3 格高的石柱也不会插进墙里。
        /// </summary>
        private static float ClutteredTopY(Sprite sprite, float innerTopY)
        {
            if (sprite == null) return innerTopY - 1f;

            float height = SpriteUnits(sprite).y;
            return innerTopY - height * 0.5f - 0.05f;
        }

        /// <summary>底部贴墙摆放时的中心点，让物件底部刚好落在内墙下沿之上。</summary>
        private static float ClutteredBottomY(Sprite sprite, float innerBottomY)
        {
            if (sprite == null) return innerBottomY + 0.5f;

            float height = SpriteUnits(sprite).y;
            return innerBottomY + height * 0.5f + 0.05f;
        }

        /// <summary>
        /// 左/右贴墙摆放时的中心点：按贴图宽度内缩，避免宽物件（天使雕像）横向插进墙里。
        /// </summary>
        private static float ClutteredLeftX(Sprite sprite, float innerLeftX, float fallbackOffset)
        {
            if (sprite == null) return innerLeftX + fallbackOffset;

            float width = SpriteUnits(sprite).x;
            return innerLeftX + width * 0.5f + 0.05f;
        }

        private static float ClutteredRightX(Sprite sprite, float innerRightX, float fallbackOffset)
        {
            if (sprite == null) return innerRightX - fallbackOffset;

            float width = SpriteUnits(sprite).x;
            return innerRightX - width * 0.5f - 0.05f;
        }

        /// <summary>贴图的世界尺寸（宽, 高）。</summary>
        private static Vector2 SpriteUnits(Sprite sprite)
        {
            float ppu = Mathf.Max(1f, sprite.pixelsPerUnit);
            return new Vector2(sprite.rect.width / ppu, sprite.rect.height / ppu);
        }

        private static Sprite PickThemeClutter(FloorTheme theme, Random rng)
        {
            Sprite[] pool = theme.floorClutterSprites;
            if (pool == null || pool.Length == 0) return null;

            for (int i = 0; i < 6; i++)
            {
                Sprite sprite = pool[rng.Next(pool.Length)];
                if (sprite != null) return sprite;
            }

            return null;
        }

        /// <summary>
        /// 主题杂物的处理：
        ///   - 陶罐/陶瓮：有碰撞，可破坏（掉金币）
        ///   - 水晶、骸骨：有碰撞，不可破坏
        ///   - 碎石等其余杂物：纯装饰
        /// </summary>
        private void AddThemeClutter(string objectName, Sprite sprite, Vector2 position)
        {
            if (sprite == null) return;

            if (IsThemePot(sprite))
            {
                // 陶瓮是 1×2 的竖长贴图，碰撞体要比矮陶罐高，但底部不封口，避免顶到门洞
                bool tall = sprite.rect.height > sprite.rect.width * 1.5f;
                MakeCrateBreakable(AddSolid(
                    objectName,
                    sprite,
                    position,
                    2,
                    tall ? new Vector2(0.8f, 1.55f) : new Vector2(0.75f, 0.9f),
                    tall ? new Vector2(0f, -0.1f) : Vector2.zero));
                return;
            }

            if (IsSolidClutter(sprite))
            {
                AddSolidClutter(objectName, sprite, position);
                return;
            }

            Add(objectName, sprite, position, 2);
        }

        /// <summary>水晶、骸骨、墓碑、石棺这类看起来实心的杂物要给碰撞，不能直接穿过去。</summary>
        private static bool IsSolidClutter(Sprite sprite)
        {
            if (sprite == null) return false;

            string n = sprite.name.ToLower();
            return n.Contains("crystal")
                || n.Contains("bone")
                || n.Contains("skull")
                || n.Contains("grave")
                || n.Contains("tomb")
                || n.Contains("coffin")
                || n.Contains("pillar")
                || n.Contains("column")
                || n.Contains("sarcophagus")
                || n.Contains("ore")
                || n.Contains("idol")
                || n.Contains("shrine")
                || n.Contains("statue");
        }

        /// <summary>按贴图尺寸生成贴合地面的方形碰撞体；不可破坏，仅作为障碍。</summary>
        private void AddSolidClutter(string objectName, Sprite sprite, Vector2 position)
        {
            if (sprite == null) return;

            float unitWidth = sprite.rect.width / Mathf.Max(1f, sprite.pixelsPerUnit);
            float unitHeight = sprite.rect.height / Mathf.Max(1f, sprite.pixelsPerUnit);

            // 高一点的墓碑/石棺要能挡住上半身，所以高度上限放到 1.6
            Vector2 size = new Vector2(
                Mathf.Clamp(unitWidth * 0.7f, 0.4f, 0.95f),
                Mathf.Clamp(unitHeight * 0.55f, 0.35f, 1.6f));

            // 碰撞体略向下压，贴合"放在地上"的视觉
            AddSolid(objectName, sprite, position, 2, size, new Vector2(0f, -unitHeight * 0.15f));
        }

        /// <summary>识别主题里的所有陶制品：木箱替换贴图、陶瓮、以及名字以 prop_pot 开头的杂物。</summary>
        private bool IsThemePot(Sprite sprite)
        {
            if (sprite == null || _theme == null) return false;

            if (sprite == _theme.crateSprite || sprite == _theme.crateAltSprite || sprite == _theme.urnSprite)
                return true;

            if (_theme.crateSprites != null)
            {
                for (int i = 0; i < _theme.crateSprites.Length; i++)
                {
                    if (_theme.crateSprites[i] == sprite) return true;
                }
            }

            return sprite.name.StartsWith("prop_pot");
        }

        private Sprite PickCrateSprite(Random rng)
        {
            if (_crateVariants != null && _crateVariants.Length > 0)
            {
                for (int i = 0; i < 8; i++)
                {
                    Sprite sprite = _crateVariants[rng.Next(_crateVariants.Length)];
                    if (sprite != null) return sprite;
                }
            }

            if (_theme != null && _theme.crateSprite != null) return _theme.crateSprite;
            return crate;
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
                _activeCrateSprite != null ? _activeCrateSprite : crate,
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
        private void BuildColumn(Vector2 basePosition, FloorTheme theme, float colliderHeight = 2.7f)
        {
            // 以石像底边所在的世界 Y 作为排序锚点：
            // 站在后方（Y 更大）的角色小于该值，会被上半段盖住；站在前方（Y 更小）的角色大于该值，会挡住石像。
            int baseOrder = DepthSort.OrderFor(transform.position.y + basePosition.y);

            if (theme != null && theme.columnLowerSprite != null && theme.columnUpperSprite != null)
            {
                // 石像分上下两段：下半段在角色之下，上半段在角色之上。
                // 玩家/敌人可以从石像后方通过，但身体会被上半段遮住。
                Add("Column_Statue_Base", theme.columnLowerSprite, basePosition + Vector2.up * 0.5f, baseOrder, false);
                Add("Column_Statue_Top", theme.columnUpperSprite, basePosition + Vector2.up * 1.5f, baseOrder + 1, false);
                // 只给底座留碰撞，身高部分可穿行
                AddBoxCollider("Column_Obstacle", basePosition + Vector2.up * 0.35f, new Vector2(0.75f, 0.7f));
                return;
            }

            if (theme != null && theme.columnSprite != null)
            {
                // 兼容旧主题的整块雕像：完整碰撞。
                Add("Column_Statue", theme.columnSprite, basePosition + Vector2.up, baseOrder, false);
                AddBoxCollider("Column_Obstacle", basePosition + Vector2.up, new Vector2(0.75f, colliderHeight));
                return;
            }

            Add("Column_Base", columnBase, basePosition, baseOrder, true);
            Add("Column_Mid", columnMid, basePosition + Vector2.up, baseOrder, true);
            Add("Column_Top", columnTop, basePosition + Vector2.up * 2f, baseOrder + 1, true);

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

            // 实心道具参与 Y 排序：角色走到后面会被挡住，走到前面则挡住它
            var renderer = go.GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.sortingOrder = DepthSort.OrderFor(go.transform.position.y);

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

                if (Application.isPlaying)
                {
                    // Destroy 要到帧末才生效；先脱离父节点，避免同一帧内重建时
                    // 新旧两套装饰同时存在（表现为旗帜/木箱重复、旧素材残留）。
                    child.transform.SetParent(null, false);
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
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
