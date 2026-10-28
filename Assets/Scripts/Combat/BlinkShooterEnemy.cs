using Roguelike.Dungeon;
using UnityEngine;

namespace Roguelike.Combat
{
    /// <summary>
    /// 闪现射手：保持距离，蓄力后闪现到玩家侧后方，再发射扇形弹幕。
    /// 用于墓穴层的 Wogol，和普通射击者拉开差异。
    /// </summary>
    public sealed class BlinkShooterEnemy : Enemy
    {
        private enum BlinkState
        {
            Chasing,
            BlinkWindup,
            ShootWindup,
            Recover,
        }

        [Header("闪现")]
        [SerializeField] private float preferredDistance = 4f;
        [SerializeField] private float blinkInterval = 4f;
        [SerializeField] private float blinkWindup = 0.35f;
        [SerializeField] private float blinkDistance = 3.2f;

        [Header("扇形射击")]
        [SerializeField] private EnemyProjectile projectilePrefab;
        [SerializeField, Min(1)] private int projectileCount = 3;
        [SerializeField] private float spreadAngle = 30f;
        [SerializeField] private float projectileSpeed = 5f;
        [SerializeField] private float projectileLifetime = 4f;
        [SerializeField] private float shootWindup = 0.35f;
        [SerializeField] private float recoverTime = 0.5f;
        [SerializeField] private Color projectileTint = new Color(0.85f, 0.6f, 1f, 1f);

        private BlinkState _state = BlinkState.Chasing;
        private SpriteRenderer _renderer;
        private float _timer;
        private float _nextBlinkTime;

        protected override void Start()
        {
            base.Start();

            if (projectilePrefab == null)
                projectilePrefab = Resources.Load<EnemyProjectile>("Combat/EnemyProjectile");

            _renderer = GetComponentInChildren<SpriteRenderer>();
            if (_renderer != null) _baseColor = _renderer.color;
        }

        protected override void OnCombatStarted()
        {
            base.OnCombatStarted();
            _state = BlinkState.Chasing;
            _timer = 0f;
            _nextBlinkTime = Time.time + 1.5f;
            RestoreAlpha();
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

            _timer -= Time.fixedDeltaTime;

            Vector2 toTarget = (Vector2)_target.position - _rb.position;
            float distance = toTarget.magnitude;
            Vector2 direction = distance > 0.0001f ? toTarget / distance : Vector2.zero;

            switch (_state)
            {
                case BlinkState.Chasing:
                    Vector2 movement;
                    if (distance > preferredDistance + 0.6f) movement = direction;
                    else if (distance < preferredDistance - 0.6f) movement = -direction;
                    else movement = new Vector2(-direction.y, direction.x) * 0.7f;

                    _rb.linearVelocity = GetSteeredDirection(movement.normalized) * moveSpeed;

                    if (Time.time >= _nextBlinkTime)
                    {
                        _state = BlinkState.BlinkWindup;
                        _timer = blinkWindup;
                        _rb.linearVelocity = Vector2.zero;
                        SetBlinkAlpha();
                    }
                    break;

                case BlinkState.BlinkWindup:
                    _rb.linearVelocity = Vector2.zero;

                    if (_timer <= 0f)
                    {
                        TeleportNearTarget();
                        _state = BlinkState.ShootWindup;
                        _timer = shootWindup;
                        RestoreAlpha();
                    }
                    break;

                case BlinkState.ShootWindup:
                    _rb.linearVelocity = Vector2.zero;

                    if (_timer <= 0f)
                    {
                        FireSpread();
                        _state = BlinkState.Recover;
                        _timer = recoverTime;
                        _nextBlinkTime = Time.time + blinkInterval;
                    }
                    break;

                case BlinkState.Recover:
                    _rb.linearVelocity = Vector2.zero;
                    if (_timer <= 0f) _state = BlinkState.Chasing;
                    break;
            }
        }

        private void TeleportNearTarget()
        {
            if (_target == null || _rb == null) return;

            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * blinkDistance;
            Vector2 destination = (Vector2)_target.position + offset;

            if (Room != null)
            {
                Rect interior = Room.InteriorRect;
                const float margin = 0.8f;
                destination.x = Mathf.Clamp(destination.x, interior.xMin + margin, interior.xMax - margin);
                destination.y = Mathf.Clamp(destination.y, interior.yMin + margin, interior.yMax - margin);
            }

            _rb.position = destination;
            transform.position = new Vector3(destination.x, destination.y, transform.position.z);
        }

        private void FireSpread()
        {
            if (projectilePrefab == null || _rb == null) return;

            Vector2 direction = ((Vector2)_target.position - _rb.position).normalized;
            int count = Mathf.Max(1, projectileCount);
            float step = count > 1 ? spreadAngle / (count - 1) : 0f;
            float start = -spreadAngle * 0.5f;

            for (int i = 0; i < count; i++)
            {
                Vector2 shotDirection = Rotate(direction, start + step * i);
                Vector2 spawnPosition = _rb.position + shotDirection * 0.8f;

                EnemyProjectile bullet = ProjectilePool.Spawn(
                    projectilePrefab,
                    new Vector3(spawnPosition.x, spawnPosition.y, 0f),
                    Quaternion.identity);

                bullet.Initialize(
                    shotDirection,
                    projectileSpeed,
                    ScaleAttackDamage(1),
                    projectileLifetime);

                bullet.ConfigureVisual(projectileTint);
            }
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

        private void SetBlinkAlpha()
        {
            if (_renderer == null) return;
            _renderer.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, 0.35f);
        }

        private void RestoreAlpha()
        {
            if (_renderer != null) _renderer.color = _baseColor;
        }
    }
}
