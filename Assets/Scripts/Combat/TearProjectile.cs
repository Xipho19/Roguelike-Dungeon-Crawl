using System.Collections.Generic;
using Roguelike.Player;
using UnityEngine;

namespace Roguelike.Combat
{
    /// <summary>
    /// 玩家的眼泪弹。
    ///
    /// 使用 Dynamic Rigidbody2D + Trigger 碰撞体：
    ///   - 命中敌人 → 造成伤害；有穿透次数时继续飞行，否则销毁；
    ///   - 命中墙体（非 Trigger 碰撞体）→ 销毁；
    ///   - 忽略玩家与其它 Trigger。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class TearProjectile : MonoBehaviour
    {
        [SerializeField] private float speed = 14f;
        [SerializeField] private float lifetime = 1f;
        [SerializeField] private float damage = 1f;

        private readonly HashSet<Enemy> _hitEnemies = new HashSet<Enemy>();

        private Rigidbody2D _rb;
        private int _remainingPierce;
        private float _dieAt;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        public void Initialize(Vector2 direction, float shotSpeed, float amount, float life, int pierce)
        {
            speed = shotSpeed;
            damage = amount;
            lifetime = life;
            _remainingPierce = Mathf.Max(0, pierce);

            if (_rb == null) _rb = GetComponent<Rigidbody2D>();
            _rb.linearVelocity = direction.normalized * speed;

            _dieAt = Time.time + lifetime;
        }

        private void Update()
        {
            if (Time.time >= _dieAt) Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // 忽略玩家自己
            if (other.GetComponentInParent<PlayerHealth>() != null) return;

            // 可击破的场景道具：每次命中计数一次，眼泪随即消失
            var breakable = other.GetComponentInParent<BreakableProp>();
            if (breakable != null)
            {
                breakable.TakeHit();
                Destroy(gameObject);
                return;
            }

            var enemy = other.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                if (!_hitEnemies.Add(enemy)) return; // 同一敌人只结算一次

                Vector2 pushDirection = _rb != null ? _rb.linearVelocity : Vector2.zero;

                enemy.TakeDamage(damage);
                enemy.ApplyKnockback(pushDirection, 5f);

                if (_remainingPierce > 0)
                {
                    _remainingPierce--;
                    return; // 继续飞行
                }

                Destroy(gameObject);
                return;
            }

            // 撞到实心墙体
            if (!other.isTrigger) Destroy(gameObject);
        }
    }
}
