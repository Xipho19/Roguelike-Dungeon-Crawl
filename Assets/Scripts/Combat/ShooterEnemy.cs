using UnityEngine;

namespace Roguelike.Combat
{
    /// <summary>
    /// 射击者：与玩家保持中距离，左右绕行，蓄力后发射一枚瞄准弹。
    /// 蓄力时停止移动并变黄，给玩家留出反应时间。
    /// </summary>
    public sealed class ShooterEnemy : Enemy
    {
        [Header("射击")]
        [SerializeField] private EnemyProjectile projectilePrefab;
        [SerializeField] private float preferredDistance = 4.5f;
        [SerializeField] private float shootInterval = 2.2f;
        [SerializeField] private float windupTime = 0.55f;
        [SerializeField] private float projectileSpeed = 6f;
        [SerializeField] private float projectileLifetime = 4f;

        private SpriteRenderer _renderer;
        private float _nextShotTime;
        private float _windupUntil;
        private bool _windingUp;

        protected override void Start()
        {
            base.Start();

            if (projectilePrefab == null)
                projectilePrefab = Resources.Load<EnemyProjectile>("Combat/EnemyProjectile");

            _renderer = GetComponentInChildren<SpriteRenderer>();
            if (_renderer != null) _baseColor = _renderer.color;

            _nextShotTime = Time.time + Random.Range(0.8f, 1.6f);
        }

        protected override void OnCombatStarted()
        {
            base.OnCombatStarted();

            _windingUp = false;
            _nextShotTime = Time.time + Random.Range(0.6f, 1.2f);
            RestoreColor();
        }

        protected override void FixedUpdate()
        {
            if (IsDead || _rb == null) return;

            if (!CombatActive)
            {
                UpdatePassiveBehavior();
                return;
            }

            if (_target == null) return;

            Vector2 toTarget = (Vector2)_target.position - _rb.position;
            float distance = toTarget.magnitude;
            Vector2 direction = distance > 0.0001f ? toTarget / distance : Vector2.zero;

            if (_windingUp)
            {
                _rb.linearVelocity = Vector2.zero;

                if (Time.time >= _windupUntil)
                {
                    Fire(direction);
                    _windingUp = false;
                    _nextShotTime = Time.time + shootInterval;
                    RestoreColor();
                }
                return;
            }

            // 太远靠近、太近后退、距离合适则绕行
            Vector2 movement;
            if (distance > preferredDistance + 0.6f) movement = direction;
            else if (distance < preferredDistance - 0.6f) movement = -direction;
            else movement = new Vector2(-direction.y, direction.x) * 0.7f;

            if (movement.sqrMagnitude > 0.0001f)
            {
                Vector2 steered = GetSteeredDirection(movement.normalized);
                _rb.linearVelocity = steered * (moveSpeed * Mathf.Clamp01(movement.magnitude));
            }
            else
            {
                _rb.linearVelocity = Vector2.zero;
            }

            if (Time.time >= _nextShotTime)
            {
                _windingUp = true;
                _windupUntil = Time.time + windupTime;
                _rb.linearVelocity = Vector2.zero;

                if (_renderer != null) _renderer.color = new Color(1f, 0.78f, 0.30f, 1f);
            }
        }

        private void Fire(Vector2 direction)
        {
            if (projectilePrefab == null) return;

            EnemyProjectile bullet = Instantiate(
                projectilePrefab,
                _rb.position + direction * 0.8f,
                Quaternion.identity);

            bullet.Initialize(direction, projectileSpeed, ScaleAttackDamage(1), projectileLifetime);
        }

        private void RestoreColor()
        {
            if (_renderer != null) _renderer.color = _baseColor;
        }
    }
}
