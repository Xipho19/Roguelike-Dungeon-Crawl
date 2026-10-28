using Roguelike.Audio;
using UnityEngine;

namespace Roguelike.Combat
{
    /// <summary>
    /// 可击破的场景道具（宝箱 / 木箱）。
    ///
    /// 每次被玩家的眼泪命中计数一次：
    ///   - 宝箱：3 次后打开，掉落 3–6 枚金币，保留打开的贴图；
    ///   - 木箱：3 次后破碎，30% 概率掉落 1 枚金币。
    /// </summary>
    public sealed class BreakableProp : MonoBehaviour
    {
        [Header("耐久")]
        [SerializeField] private int hitsRequired = 3;

        [Header("掉落")]
        [SerializeField] private int minCoins = 1;
        [SerializeField] private int maxCoins = 1;
        [SerializeField, Range(0f, 1f)] private float dropChance = 1f;
        [SerializeField] private float scatterRadius = 0.55f;

        [Header("开启")]
        [Tooltip("击破后替换的贴图（例如打开的宝箱）；留空则直接销毁")]
        [SerializeField] private Sprite openedSprite;

        [Tooltip("开启后保留对象并关闭碰撞体")]
        [SerializeField] private bool keepOpenedObject;

        private SpriteRenderer _renderer;
        private Color _baseColor = Color.white;
        private int _hits;
        private bool _opened;

        public int Hits => _hits;
        public bool IsOpened => _opened;

        /// <summary>由房间装饰生成逻辑写入耐久与掉落配置。</summary>
        public void Setup(
            int hits,
            int coinsMin,
            int coinsMax,
            float chance,
            Sprite openSprite,
            bool keepOpened)
        {
            hitsRequired = Mathf.Max(1, hits);
            minCoins = Mathf.Max(0, coinsMin);
            maxCoins = Mathf.Max(minCoins, coinsMax);
            dropChance = Mathf.Clamp01(chance);
            openedSprite = openSprite;
            keepOpenedObject = keepOpened;
        }

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            if (_renderer != null) _baseColor = _renderer.color;
        }

        /// <summary>被眼泪命中一次；达到耐久后开启或破碎。</summary>
        public void TakeHit()
        {
            if (_opened) return;

            _hits++;

            if (_hits >= Mathf.Max(1, hitsRequired))
            {
                Break();
                return;
            }

            FlashHit();
            AudioService.Play("chop", 0.35f);
        }

        private void Break()
        {
            _opened = true;

            DropCoins();
            DisableColliders();

            if (keepOpenedObject)
            {
                if (_renderer != null)
                {
                    if (openedSprite != null) _renderer.sprite = openedSprite;
                    _renderer.color = _baseColor;
                }

                AudioService.Play("bookOpen", 0.6f);
                return;
            }

            AudioService.Play("crate_break", 0.9f, 0.03f);
            Destroy(gameObject);
        }

        private void DropCoins()
        {
            if (minCoins <= 0 || Random.value > dropChance) return;

            int count = Random.Range(minCoins, maxCoins + 1);
            if (count <= 0) return;

            var coinPrefab = Resources.Load<Pickup>("Combat/Coin");
            if (coinPrefab == null) return;

            for (int i = 0; i < count; i++)
            {
                Vector2 offset = Random.insideUnitCircle * scatterRadius;
                if (offset.sqrMagnitude < 0.0001f) offset = Vector2.up * 0.2f;

                Instantiate(
                    coinPrefab,
                    (Vector2)transform.position + offset,
                    Quaternion.identity,
                    transform.parent);
            }
        }

        private void DisableColliders()
        {
            Collider2D[] colliders = GetComponents<Collider2D>();
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = false;
        }

        private void FlashHit()
        {
            if (_renderer == null) return;

            StopCoroutine(nameof(FlashRoutine));
            StartCoroutine(nameof(FlashRoutine));
        }

        private System.Collections.IEnumerator FlashRoutine()
        {
            _renderer.color = new Color(1f, 0.55f, 0.55f, 1f);
            yield return new WaitForSeconds(0.08f);
            if (_renderer != null) _renderer.color = _baseColor;
        }
    }
}
