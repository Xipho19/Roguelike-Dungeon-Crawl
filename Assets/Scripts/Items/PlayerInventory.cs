using System;
using System.Collections.Generic;
using UnityEngine;

namespace Roguelike.Items
{
    /// <summary>本局持有的道具列表。运行时状态，不写回 ScriptableObject 资产。</summary>
    public sealed class PlayerInventory : MonoBehaviour
    {
        [SerializeField] private PlayerStats stats;

        private readonly List<ItemDefinition> _items = new List<ItemDefinition>();

        public IReadOnlyList<ItemDefinition> Items => _items;

        /// <summary>拾取新道具时触发，供 HUD 显示提示。</summary>
        public event Action<ItemDefinition> ItemAdded;

        private void Awake()
        {
            if (stats == null) stats = GetComponent<PlayerStats>();
        }

        public void AddItem(ItemDefinition item)
        {
            if (item == null) return;

            _items.Add(item);

            if (stats != null) stats.Rebuild(_items);

            ItemAdded?.Invoke(item);
        }

        /// <summary>重开一局时清空背包并把属性恢复到基础值。</summary>
        public void Clear()
        {
            _items.Clear();

            if (stats != null) stats.Rebuild(_items);
        }
    }
}
