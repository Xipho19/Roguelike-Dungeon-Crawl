using System;
using System.Collections.Generic;
using UnityEngine;

namespace Roguelike.Items
{
    /// <summary>
    /// 玩家属性管线。
    ///
    /// 计算方式：
    ///   最终值 = (基础值 + Σ加法修饰) × Π乘法修饰
    ///
    /// 所有系统（移动、射击、生命）都从这里读取最终值，
    /// 这样新增道具只需要填 ItemDefinition，不用改玩家代码。
    /// </summary>
    public sealed class PlayerStats : MonoBehaviour
    {
        [Header("基础属性（统一调参入口）")]
        [SerializeField] private float baseMaxHealth = 6f;
        [SerializeField] private float baseDamage = 1f;
        [SerializeField] private float baseFireInterval = 0.24f;
        [SerializeField] private float baseMoveSpeed = 6f;
        [SerializeField] private float baseTearSpeed = 14f;
        [SerializeField] private float baseTearLifetime = 1f;
        [SerializeField] private float basePierce;
        [SerializeField] private float baseShotCount = 1f;
        [SerializeField] private float baseCoinBonus;

        private readonly List<StatModifier> _modifiers = new List<StatModifier>();

        /// <summary>属性变化时触发（拾取道具后）。</summary>
        public event Action StatsChanged;

        public int MaxHealth => Mathf.Max(1, Mathf.RoundToInt(Get(StatType.MaxHealth)));
        public float Damage => Mathf.Max(0f, Get(StatType.Damage));
        public float FireInterval => Mathf.Max(0.05f, Get(StatType.FireInterval));
        public float MoveSpeed => Mathf.Max(0f, Get(StatType.MoveSpeed));
        public float TearSpeed => Mathf.Max(0f, Get(StatType.TearSpeed));
        public float TearLifetime => Mathf.Max(0.1f, Get(StatType.TearLifetime));
        public int Pierce => Mathf.Max(0, Mathf.RoundToInt(Get(StatType.Pierce)));
        public int ShotCount => Mathf.Max(1, Mathf.RoundToInt(Get(StatType.ShotCount)));
        public int CoinBonus => Mathf.Max(0, Mathf.RoundToInt(Get(StatType.CoinBonus)));

        /// <summary>根据当前持有的道具重新汇总修饰器。</summary>
        public void Rebuild(IReadOnlyList<ItemDefinition> items)
        {
            _modifiers.Clear();

            if (items != null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    ItemDefinition item = items[i];
                    if (item == null || item.modifiers == null) continue;
                    _modifiers.AddRange(item.modifiers);
                }
            }

            StatsChanged?.Invoke();
        }

        private float Get(StatType type)
        {
            float value = BaseValue(type);
            float multiplier = 1f;

            for (int i = 0; i < _modifiers.Count; i++)
            {
                StatModifier modifier = _modifiers[i];
                if (modifier.stat != type) continue;

                if (modifier.op == StatOp.Add) value += modifier.value;
                else multiplier *= modifier.value;
            }

            return value * multiplier;
        }

        private float BaseValue(StatType type)
        {
            switch (type)
            {
                case StatType.MaxHealth: return baseMaxHealth;
                case StatType.Damage: return baseDamage;
                case StatType.FireInterval: return baseFireInterval;
                case StatType.MoveSpeed: return baseMoveSpeed;
                case StatType.TearSpeed: return baseTearSpeed;
                case StatType.TearLifetime: return baseTearLifetime;
                case StatType.Pierce: return basePierce;
                case StatType.ShotCount: return baseShotCount;
                default: return baseCoinBonus;
            }
        }
    }
}
