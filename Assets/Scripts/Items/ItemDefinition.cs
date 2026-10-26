using System.Collections.Generic;
using UnityEngine;

namespace Roguelike.Items
{
    public enum ItemRarity
    {
        Common,
        Rare,
        Legendary,
    }

    public enum ItemSynergy
    {
        None = 0,
        ExplosiveTears = 1,
        KillHeal = 2,
        HomingTears = 3,
        PoisonTears = 4,
        DeathExplosion = 5,
        Thorns = 6,
        ShopDiscount = 7,
        CoinDamage = 8,
        RoomClearHeal = 9,
        RoomEntryShield = 10,
        LuckyDrops = 11,
        CritTears = 12,
        SplitTears = 13,
        TearAura = 14,
    }

    /// <summary>
    /// 道具的静态定义（ScriptableObject）。
    ///
    /// 只存"定义"，不存运行时状态：
    ///   - 属性变化通过 StatModifier 描述，由 PlayerStats 汇总；
    ///   - 即时效果（回血、金币）在拾取时结算一次；
    ///   - 触发型被动通过 synergy / effectPower / effectRadius / triggerCount 描述。
    /// </summary>
    [CreateAssetMenu(menuName = "Roguelike/Item Definition", fileName = "Item_New")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [Header("展示")]
        public string itemName = "未命名道具";

        [TextArea]
        public string description = string.Empty;

        public ItemRarity rarity = ItemRarity.Common;

        [Tooltip("道具台与商店里显示的图标；留空则使用 Prefab 默认图标")]
        public Sprite icon;

        [Tooltip("图标缺失时的回退染色；也用于 HUD 中的类型色")]
        public Color color = Color.white;

        [Header("属性修饰")]
        public List<StatModifier> modifiers = new List<StatModifier>();

        [Header("拾取时的一次性效果")]
        public int coinBonus;
        public int healAmount;

        [Header("触发型被动")]
        [Tooltip("伤害 / 治疗数值；没有触发效果时保持 0")]
        public float effectPower;

        [Tooltip("范围效果的半径（世界单位）")]
        public float effectRadius;

        [Tooltip("触发所需的击杀次数")]
        public int triggerCount = 1;

        public ItemSynergy synergy = ItemSynergy.None;

        [Tooltip("大于 0 时覆盖商店基础价格；楼层加价仍会叠加")]
        public int shopPrice;
    }
}
