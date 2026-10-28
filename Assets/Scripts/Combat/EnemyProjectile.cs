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
        private SpriteRenderer _renderer;
        private Vector3 _baseScale = Vector3.one;
        private Color _baseColor = Color.white;
        private int _damage;
        private float _dieAt;
        private Transform _homingTarget;
        private float _homingTurnSpeed;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            _renderer = GetComponentInChildren<SpriteRenderer>();
            _baseScale = transform.localScale;
            if (_renderer != null) _baseColor = _renderer.color;
        }

        public void Initialize(Vector2 direction, float speed, int damage, float lifetime)
        {
            _damage = damage;
            _dieAt = Time.time + lifetime;
            _homingTarget = null;
            _homingTurnSpeed = 0f;
            transform.localScale = _baseScale;
            if (_renderer != null) _renderer.color = _baseColor;

            if (_rb == null) _rb = GetComponent<Rigidbody2D>();
            _rb.linearVelocity = direction.normalized * speed;
        }

        /// <summary>回收本颗弹丸，而不是直接销毁，供对象池复用。</summary>
        public void Despawn()
        {
            ProjectilePool.Release(this);
        }

        /// <summary>按 Boss 类型给弹丸染色/缩放；只影响这一颗。</summary>
        public void ConfigureVisual(Color tint, float scale = 1f)
        {
            if (_renderer == null) _renderer = GetComponentInChildren<SpriteRenderer>();
            if (_renderer != null) _renderer.color = tint;

            transform.localScale = _baseScale * Mathf.Max(0.1f, scale);
        }

        /// <summary>让弹丸缓慢转向目标（追踪火球）。</summary>
        public void SetHoming(Transform target, float turnSpeedDegrees)
        {
            _homingTarget = target;
            _homingTurnSpeed = Mathf.Max(0f, turnSpeedDegrees);
        }

        private void Update()
        {
            if (Time.time >= _dieAt)
            {
                Despawn();
                return;
            }

            if (_homingTarget == null || _homingTurnSpeed <= 0f || _rb == null) return;

            Vector2 velocity = _rb.linearVelocity;
            if (velocity.sqrMagnitude < 0.0001f) return;

            Vector2 toTarget = (Vector2)_homingTarget.position - _rb.position;
            if (toTarget.sqrMagnitude < 0.0001f) return;

            float angle = Vector2.SignedAngle(velocity, toTarget);
            float maxStep = _homingTurnSpeed * Time.deltaTime;
            float step = Mathf.Clamp(angle, -maxStep, maxStep);

            _rb.linearVelocity = Rotate(velocity.normalized, step) * velocity.magnitude;
        }

        private static Vector2 Rotate(Vector2 direction, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);

            return new Vector2(
                direction.x * cos - direction.y * sin,
                direction.x * sin + direction.y * cos);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // 不打自己人
            if (other.GetComponentInParent<Enemy>() != null) return;

            var health = other.GetComponentInParent<PlayerHealth>();
            if (health != null)
            {
                health.TryDamage(_damage);
                Despawn();
                return;
            }

            if (!other.isTrigger) Despawn();
        }
    }
}
