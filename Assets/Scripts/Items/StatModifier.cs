using System;
using UnityEngine;

namespace Roguelike.Items
{
    /// <summary>可被道具修改的属性。</summary>
    public enum StatType
    {
        MaxHealth,
        Damage,
        FireInterval,
        MoveSpeed,
        TearSpeed,
        TearLifetime,
        Pierce,
        ShotCount,
        CoinBonus,
    }

    /// <summary>修改方式：加法 / 乘法。</summary>
    public enum StatOp
    {
        Add,
        Multiply,
    }

    /// <summary>
    /// 一条属性修改。加法先结算，乘法最后统一相乘。
    /// 保存在 ItemDefinition 里，运行时由 PlayerStats 汇总计算，不会写回资产。
    /// </summary>
    [Serializable]
    public struct StatModifier
    {
        public StatType stat;
        public StatOp op;
        public float value;

        public StatModifier(StatType stat, StatOp op, float value)
        {
            this.stat = stat;
            this.op = op;
            this.value = value;
        }
    }
}
