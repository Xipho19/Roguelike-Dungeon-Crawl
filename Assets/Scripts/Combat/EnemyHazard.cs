using Roguelike.Player;
using UnityEngine;

namespace Roguelike.Combat
{
    /// <summary>
    /// 敌人留下的地面危险区域：泥潭、冰面、毒雾共用。
    /// 只伤害玩家；冰面额外施加减速。
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class EnemyHazard : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private CircleCollider2D trigger;

        private int _damage;
        private float _tickInterval = 0.8f;
        private float _nextTickTime;
        private float _dieAt;
        private float _slowFactor = 1f;
        private float _slowDuration;
        private Color _color = Color.white;

        private void Awake()
        {
            if (body == null) body = GetComponentInChildren<SpriteRenderer>();
            if (trigger == null) trigger = GetComponent<CircleCollider2D>();
            if (trigger != null) trigger.isTrigger = true;
        }

        public void Initialize(
            int damage,
            float tickInterval,
            float lifetime,
            float radius,
            float slowFactor,
            float slowDuration,
            Color tint)
        {
            _damage = Mathf.Max(0, damage);
            _tickInterval = Mathf.Max(0.2f, tickInterval);
            _dieAt = Time.time + Mathf.Max(0.3f, lifetime);
            _slowFactor = Mathf.Clamp01(slowFactor);
            _slowDuration = Mathf.Max(0f, slowDuration);
            _color = tint;

            transform.localScale = Vector3.one * Mathf.Max(0.3f, radius) * 2f;

            if (body != null)
            {
                body.color = tint;
                body.sortingOrder = 1;
            }

            if (trigger != null)
            {
                trigger.radius = 0.5f;
                trigger.isTrigger = true;
            }

            _nextTickTime = Time.time;
        }

        private void Update()
        {
            if (Time.time >= _dieAt)
            {
                Destroy(gameObject);
                return;
            }

            if (body == null) return;

            Color color = _color;
            color.a = _color.a * Mathf.Lerp(0.65f, 1f, Mathf.PingPong(Time.time * 2f, 1f));
            body.color = color;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (Time.time < _nextTickTime) return;

            PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
            if (health == null || health.IsDead) return;

            PlayerController controller = other.GetComponentInParent<PlayerController>();
            if (controller != null && _slowFactor < 0.999f && _slowDuration > 0f)
                controller.ApplySlow(_slowFactor, _slowDuration);

            health.TryDamage(_damage);
            _nextTickTime = Time.time + _tickInterval;
        }
    }
}
