using Roguelike.Audio;
using Roguelike.Player;
using UnityEngine;

namespace Roguelike.Combat
{
    public enum PickupType
    {
        Coin,
        Heart,
    }

    /// <summary>
    /// 地面掉落物：金币 / 回血。
    /// 玩家碰到即自动拾取，带轻微上下浮动动画。
    /// </summary>
    public sealed class Pickup : MonoBehaviour
    {
        [SerializeField] private PickupType type = PickupType.Coin;
        [SerializeField] private int coinValue = 1;
        [SerializeField] private int healAmount = 1;

        [Header("动画")]
        [SerializeField] private float bobHeight = 0.12f;
        [SerializeField] private float bobSpeed = 3f;

        private Vector3 _basePosition;
        private float _phase;

        public PickupType Type => type;
        public int CoinValue => coinValue;
        public int HealAmount => healAmount;

        /// <summary>由工厂或生成方写入数值。</summary>
        public void Setup(PickupType pickupType, int coins, int heal)
        {
            type = pickupType;
            coinValue = coins;
            healAmount = heal;
        }

        private void Start()
        {
            _basePosition = transform.position;
            _phase = Random.value * Mathf.PI * 2f; // 错开浮动相位，避免所有掉落同步
        }

        private void Update()
        {
            transform.position = _basePosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed + _phase) * bobHeight);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var wallet = other.GetComponentInParent<PlayerWallet>();
            if (wallet == null) return; // 只允许玩家拾取

            if (type == PickupType.Coin)
            {
                wallet.AddCoins(coinValue);
                AudioService.Play("handleCoins", 0.65f);
            }
            else
            {
                var health = other.GetComponentInParent<PlayerHealth>();
                if (health != null) health.Heal(healAmount);
                AudioService.Play("bookFlip1", 0.5f);
            }

            Destroy(gameObject);
        }
    }
}
