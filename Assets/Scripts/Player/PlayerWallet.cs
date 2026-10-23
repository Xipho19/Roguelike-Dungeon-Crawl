using System;
using UnityEngine;

namespace Roguelike.Player
{
    /// <summary>玩家金币。后续可接入商店、重roll 等消费点。</summary>
    public sealed class PlayerWallet : MonoBehaviour
    {
        [SerializeField] private int coins;

        public int Coins => coins;

        public event Action<int> CoinsChanged;

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;

            coins += amount;
            CoinsChanged?.Invoke(coins);
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0 || coins < amount) return false;

            coins -= amount;
            CoinsChanged?.Invoke(coins);
            return true;
        }

        /// <summary>重开一局时清零。</summary>
        public void ResetCoins()
        {
            coins = 0;
            CoinsChanged?.Invoke(coins);
        }
    }
}
