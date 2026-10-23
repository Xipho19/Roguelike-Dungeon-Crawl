using Roguelike.Player;
using UnityEngine;

namespace Roguelike.Combat
{
    /// <summary>
    /// 敌方弹丸：只伤害玩家，撞墙或超时消失，不会被其它敌人阻挡。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class EnemyProjectile : MonoBehaviour
    {
        private Rigidbody2D _rb;
        private int _damage;
        private float _dieAt;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        public void Initialize(Vector2 direction, float speed, int damage, float lifetime)
        {
            _damage = damage;
            _dieAt = Time.time + lifetime;

            if (_rb == null) _rb = GetComponent<Rigidbody2D>();
            _rb.linearVelocity = direction.normalized * speed;
        }

        private void Update()
        {
            if (Time.time >= _dieAt) Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // 不打自己人
            if (other.GetComponentInParent<Enemy>() != null) return;

            var health = other.GetComponentInParent<PlayerHealth>();
            if (health != null)
            {
                health.TryDamage(_damage);
                Destroy(gameObject);
                return;
            }

            if (!other.isTrigger) Destroy(gameObject);
        }
    }
}
