using System;
using System.Collections.Generic;
using UnityEngine;

namespace Roguelike.Items
{
    /// <summary>
    /// 本局持有的自动道具列表。运行时状态，不写回 ScriptableObject 资产。
    /// </summary>
    public sealed class PlayerInventory : MonoBehaviour
    {
        [SerializeField] private PlayerStats stats;

        private readonly List<ItemDefinition> _items = new List<ItemDefinition>();

        public IReadOnlyList<ItemDefinition> Items => _items;

        /// <summary>拾取道具时触发，供 HUD 显示提示。</summary>
        public event Action<ItemDefinition> ItemAdded;

        /// <summary>背包被整体清空（重开一局）。</summary>
        public event Action Cleared;

        private void Awake()
        {
            if (stats == null) stats = GetComponent<PlayerStats>();
        }

        public void AddItem(ItemDefinition item)
        {
            if (item == null) return;

            _items.Add(item);
            RebuildStats();
            ItemAdded?.Invoke(item);
        }

        public bool Owns(ItemDefinition item)
        {
            if (item == null) return false;

            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i] == item) return true;
            }

            return false;
        }

        public ItemDefinition FindSynergyItem(ItemSynergy synergy)
        {
            if (synergy == ItemSynergy.None) return null;

            for (int i = 0; i < _items.Count; i++)
            {
                ItemDefinition item = _items[i];
                if (item != null && item.synergy == synergy) return item;
            }

            return null;
        }

        public bool HasSynergy(ItemSynergy synergy) => FindSynergyItem(synergy) != null;

        /// <summary>重开一局时清空背包并把属性恢复到基础值。</summary>
        public void Clear()
        {
            _items.Clear();
            RebuildStats();
            Cleared?.Invoke();
        }

        private void RebuildStats()
        {
            if (stats != null) stats.Rebuild(_items);
        }
    }
}
