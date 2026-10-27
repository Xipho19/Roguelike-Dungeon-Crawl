using UnityEngine;

namespace Roguelike.Combat
{
    /// <summary>
    /// 区域型敌人：追到一定距离后停下蓄力，在脚下留下泥潭/冰面/毒雾。
    /// 由泥怪、冰僵尸等复用，具体效果由 Hazard Prefab 的参数决定。
    /// </summary>
    public sealed class TrapperEnemy : Enemy
    {
        [Header("区域效果")]
        [SerializeField] private EnemyHazard hazardPrefab;
        [SerializeField] private float triggerRange = 3.2f;
        [SerializeField] private float dropInterval = 3.5f;
        [SerializeField] private float dropWindup = 0.5f;
        [SerializeField] private int hazardDamage = 1;
        [SerializeField] private float hazardTickInterval = 0.75f;
        [SerializeField] private float hazardLifetime = 2.5f;
        [SerializeField] private float hazardRadius = 1.0f;
        [SerializeField, Range(0f, 1f)] private float slowFactor = 1f;
        [SerializeField] private float slowDuration;
        [SerializeField] private Color hazardTint = new Color(0.45f, 0.3f, 0.2f, 0.55f);
        [SerializeField] private Color windupTint = new Color(0.7f, 0.55f, 0.3f, 1f);

        private SpriteRenderer _renderer;
        private bool _dropping;
        private float _dropUntil;
        private float _nextDropTime;

        protected override void Start()
        {
            base.Start();

            if (hazardPrefab == null)
                hazardPrefab = Resources.Load<EnemyHazard>("Combat/EnemyHazard");

            _renderer = GetComponentInChildren<SpriteRenderer>();
            if (_renderer != null) _baseColor = _renderer.color;
        }

        protected override void OnCombatStarted()
        {
            base.OnCombatStarted();
            _dropping = false;
            _nextDropTime = Time.time + 1.2f;
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

            if (_dropping)
            {
                _rb.linearVelocity = Vector2.zero;

                if (Time.time >= _dropUntil)
                {
                    _dropping = false;
                    RestoreColor();
                    SpawnHazard();
                    _nextDropTime = Time.time + dropInterval;
                }

                return;
            }

            Vector2 toTarget = (Vector2)_target.position - _rb.position;
            float distance = toTarget.magnitude;
            Vector2 direction = distance > 0.0001f ? toTarget / distance : Vector2.zero;

            _rb.linearVelocity = GetSteeredDirection(direction) * moveSpeed;

            if (distance <= triggerRange && Time.time >= _nextDropTime)
            {
                _dropping = true;
                _dropUntil = Time.time + dropWindup;
                _rb.linearVelocity = Vector2.zero;
                SetWindupColor();
            }
        }

        private void SpawnHazard()
        {
            if (hazardPrefab == null || _rb == null) return;

            EnemyHazard hazard = Instantiate(
                hazardPrefab,
                _rb.position,
                Quaternion.identity,
                transform.parent);

            hazard.Initialize(
                ScaleAttackDamage(hazardDamage),
                hazardTickInterval,
                hazardLifetime,
                hazardRadius,
                slowFactor,
                slowDuration,
                hazardTint);
        }

        private void SetWindupColor()
        {
            if (_renderer != null) _renderer.color = windupTint;
        }

        private void RestoreColor()
        {
            if (_renderer != null) _renderer.color = _baseColor;
        }
    }
}
