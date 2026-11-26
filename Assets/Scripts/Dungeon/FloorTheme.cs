using Roguelike.Combat;
using UnityEngine;

namespace Roguelike.Dungeon
{
    public enum FloorBossVisual
    {
        Ogre = 0,
        BigZombie = 1,
        BigDemon = 2,

        // 第 4-6 层专属 Boss；只追加，不能重排上面的序列化值
        SewerTroll = 3,
        RuinsGolem = 4,
        ObsidianKing = 5,
    }

    /// <summary>
    /// 一层地牢的视觉主题：名称、地板/墙体贴图与染色、相机背景。
    /// 资产放在 Resources/FloorThemes，由 RunManager 按 order 排序后循环使用。
    /// </summary>
    [CreateAssetMenu(menuName = "Roguelike/Floor Theme", fileName = "FloorTheme_New")]
    public sealed class FloorTheme : ScriptableObject
    {
        [Header("展示")]
        public string displayName = "地牢";

        [Tooltip("越小越靠前；相同 order 按资产名排序")]
        public int order;

        [Header("配色")]
        [Tooltip("与地板贴图相乘的染色")]
        public Color floorTint = Color.white;

        [Tooltip("与墙体贴图相乘的染色；同时用于墙面装饰")]
        public Color wallTint = Color.white;

        [Tooltip("相机纯色背景")]
        public Color cameraBackground = new Color(0.055f, 0.05f, 0.075f, 1f);

        [Header("楼层贴图")]
        [Tooltip("无缝地面贴图；设置后用一张 Tiled Sprite 铺满整个房间，忽略 floorVariants。适合没有格子接缝的大块石地")]
        public Sprite floorTexture;

        [Tooltip("该楼层使用的地板变体；留空时沿用房间 Prefab 的默认 0x72 地砖")]
        public Sprite[] floorVariants = new Sprite[0];

        [Tooltip("32px 高清图块通常不应随机翻转，否则纹理和阴影会出现接缝")]
        public bool allowFloorFlip = true;

        [Tooltip("特殊房间是否用房间类型颜色给地板染色；高清主题建议关闭，改用装饰区分")]
        public bool useRoomTypeTint = true;

        [Tooltip("隐藏旧版 0x72 的旗帜/墙垢/骷髅装饰；木箱、宝箱与立柱保留")]
        public bool hideLegacyDecorations;

        [Tooltip("隐藏旧版 16px 红色骷髅门槛地毯；高清主题可先关闭，后续换专属门标记")]
        public bool hideDoorIndicators;

        [Tooltip("在门洞处铺 3×1 / 1×3 的石板门槛，避免开门后露出黑色空洞")]
        public bool useDoorThresholds;

        [Header("场景道具（可选；留空时沿用 0x72 预制体贴图）")]
        [Tooltip("可破坏木箱的替换贴图（FBU 主题用陶罐）")]
        public Sprite crateSprite;

        [Tooltip("可破坏木箱的备用贴图，用于同房间交替")]
        public Sprite crateAltSprite;

        [Tooltip("可破坏陶罐/木箱的随机变体池；非空时优先使用")]
        public Sprite[] crateSprites = new Sprite[0];

        [Tooltip("宝箱关闭贴图")]
        public Sprite chestSprite;

        [Tooltip("宝箱打开贴图")]
        public Sprite chestOpenSprite;

        [Tooltip("骷髅/骸骨装饰贴图")]
        public Sprite skullSprite;

        [Tooltip("Boss/商店/宝箱房立柱的整块替换贴图（1×2 单位）")]
        public Sprite columnSprite;

        [Tooltip("遮罩立柱的下半段；与上半段一起使用时，上半段会盖住身后的角色")]
        public Sprite columnLowerSprite;

        [Tooltip("遮罩立柱的上半段；排序在角色之上，用于实现“可走到石像后面但被遮住”")]
        public Sprite columnUpperSprite;

        [Tooltip("火盆装饰贴图")]
        public Sprite brazierSprite;

        [Tooltip("石瓮装饰贴图（1×2 单位）")]
        public Sprite urnSprite;

        [Tooltip("随机地面杂物池，用于普通房间角落")]
        public Sprite[] floorClutterSprites = new Sprite[0];

        [Tooltip("旧版通用墙体贴图；横/竖墙未指定时回退到这里")]
        public Sprite wallSprite;

        [Tooltip("横向墙体使用的贴图；留空时回退到 wallSprite")]
        public Sprite wallHorizontalSprite;

        [Tooltip("纵向墙体使用的贴图；留空时回退到 wallSprite")]
        public Sprite wallVerticalSprite;

        [Header("Boss")]
        [Tooltip("该主题楼层使用的 Boss 形象")]
        public FloorBossVisual bossVisual = FloorBossVisual.BigDemon;

        [Header("小怪池")]
        [Tooltip("该楼层普通战斗房和挑战房使用的小怪；留空时回退到 Flow 的默认六种")]
        public Enemy[] commonEnemies = new Enemy[0];

        [Header("BGM")]
        [Tooltip("该楼层循环播放的背景音乐；留空则不播放")]
        public AudioClip bgm;

        [Range(0f, 1f)]
        [Tooltip("背景音乐音量")]
        public float bgmVolume = 0.5f;
    }
}
