using Roguelike.Audio;
using Roguelike.Player;
using UnityEngine;

namespace Roguelike.Items
{
    /// <summary>统一的道具发放：加入背包并结算一次性的回血 / 金币效果。</summary>
    public static class ItemGrant
    {
        public static void Apply(
            ItemDefinition item,
            PlayerInventory inventory,
            PlayerHealth health,
            PlayerWallet wallet)
        {
            if (item == null) return;

            if (inventory != null) inventory.AddItem(item);

            if (item.healAmount > 0 && health != null)
                health.Heal(item.healAmount);

            if (item.coinBonus > 0 && wallet != null)
                wallet.AddCoins(item.coinBonus);

            AudioService.Play("bookFlip1", 0.7f);
        }
    }
}
