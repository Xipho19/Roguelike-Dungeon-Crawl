using System;

namespace Roguelike.Progression
{
    public enum MetaUpgradeEffect
    {
        MaxHealth,
        MaxShield,
        MoveSpeed,
        Damage,
        CoinBonus,
        FireInterval,
    }

    /// <summary>
    /// 一个永久强化节点。当前版本每个节点 1 级，后续可扩展 maxLevel。
    /// </summary>
    public sealed class MetaUpgradeDefinition
    {
        public readonly string id;
        public readonly string displayName;
        public readonly string description;
        public readonly string glyph;
        public readonly string prerequisiteId;
        public readonly int ring;
        public readonly float angleDegrees;
        public readonly MetaUpgradeEffect effect;
        public readonly float value;
        public readonly int cost;
        public readonly int maxLevel;

        public MetaUpgradeDefinition(
            string id,
            string displayName,
            string description,
            string glyph,
            int ring,
            float angleDegrees,
            MetaUpgradeEffect effect,
            float value,
            int cost,
            string prerequisiteId = null,
            int maxLevel = 1)
        {
            this.id = id;
            this.displayName = displayName;
            this.description = description;
            this.glyph = glyph;
            this.ring = ring;
            this.angleDegrees = angleDegrees;
            this.effect = effect;
            this.value = value;
            this.cost = cost;
            this.prerequisiteId = prerequisiteId;
            this.maxLevel = Math.Max(1, maxLevel);
        }
    }

    /// <summary>
    /// 从中心向外发散的三层结构：中心核心 → 内环 6 个节点 → 外环 6 个强化节点。
    /// 六条分支分别对应生命、护盾、移速、伤害、金币和射速。
    /// </summary>
    public static class MetaUpgrades
    {
        public const string CoreId = "core";
        public const string CoreName = "冒险者核心";
        public const string CoreDescription = "所有永久强化的起点。六条分支从这里向外延伸。";

        public static readonly MetaUpgradeDefinition[] Nodes =
        {
            new MetaUpgradeDefinition(
                "life1", "坚韧", "最大生命 +1。", "心",
                1, -90f, MetaUpgradeEffect.MaxHealth, 1f, 20),
            new MetaUpgradeDefinition(
                "life2", "生命洪流", "最大生命额外 +2。", "命",
                2, -90f, MetaUpgradeEffect.MaxHealth, 2f, 45, "life1"),

            new MetaUpgradeDefinition(
                "guard1", "壁垒", "护盾上限 +1。", "盾",
                1, -30f, MetaUpgradeEffect.MaxShield, 1f, 20),
            new MetaUpgradeDefinition(
                "guard2", "不破之盾", "护盾上限额外 +2。", "御",
                2, -30f, MetaUpgradeEffect.MaxShield, 2f, 45, "guard1"),

            new MetaUpgradeDefinition(
                "speed1", "疾行", "移动速度 +4%。", "风",
                1, 30f, MetaUpgradeEffect.MoveSpeed, 1.04f, 20),
            new MetaUpgradeDefinition(
                "speed2", "风之步伐", "移动速度额外 +6%。", "驰",
                2, 30f, MetaUpgradeEffect.MoveSpeed, 1.06f, 45, "speed1"),

            new MetaUpgradeDefinition(
                "power1", "锋锐", "伤害 +5%。", "刃",
                1, 90f, MetaUpgradeEffect.Damage, 1.05f, 25),
            new MetaUpgradeDefinition(
                "power2", "破坏本能", "伤害额外 +8%。", "斩",
                2, 90f, MetaUpgradeEffect.Damage, 1.08f, 55, "power1"),

            new MetaUpgradeDefinition(
                "fortune1", "贪婪", "金币获取 +1。", "金",
                1, 150f, MetaUpgradeEffect.CoinBonus, 1f, 20),
            new MetaUpgradeDefinition(
                "fortune2", "点金术", "金币获取额外 +2。", "财",
                2, 150f, MetaUpgradeEffect.CoinBonus, 2f, 50, "fortune1"),

            new MetaUpgradeDefinition(
                "haste1", "迅捷", "射击间隔 -3%。", "速",
                1, 210f, MetaUpgradeEffect.FireInterval, 0.97f, 25),
            new MetaUpgradeDefinition(
                "haste2", "连射", "射击间隔额外 -5%。", "连",
                2, 210f, MetaUpgradeEffect.FireInterval, 0.95f, 55, "haste1"),
        };

        public static MetaUpgradeDefinition Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            for (int i = 0; i < Nodes.Length; i++)
            {
                if (Nodes[i].id == id) return Nodes[i];
            }

            return null;
        }

        public static MetaUpgradeDefinition FindByPrerequisite(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            for (int i = 0; i < Nodes.Length; i++)
            {
                if (Nodes[i].prerequisiteId == id) return Nodes[i];
            }

            return null;
        }
    }
}
