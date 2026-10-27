using Roguelike.Audio;
using Roguelike.Player;
using UnityEngine;

namespace Roguelike.Combat
{
    /// <summary>
    /// 蓄力近战怪：追到近身后停下蓄力，再对身前扇形范围造成伤害。
    /// 支持单段或两段斩击，供兽人战士、骷髅和僵尸复用。
    /// </summary>
    public sealed class CleaveEnemy : Enemy
    {
        private enum CleaveState
        {
            Chasing,
            Windup,
            SecondWindup,
            Recover,
        }

        [Header("蓄力斩击")]
        [SerializeField] private float triggerRange = 1.6f;
        [SerializeField] private float windupTime = 0.55f;
        [SerializeField] private int damage = 2;
        [SerializeField] private float radius = 1.4f;
        [SerializeField, Range(10f, 180f)] private float halfAngle = 60f;
        [SerializeField] private float recoverTime = 0.65f;
        [SerializeField] private float attackCooldown = 2.2f;
        [SerializeField] private bool secondStrike;
        [SerializeField] private float secondDelay = 0.22f;
        [SerializeField] private Color windupTint = new Color(1f, 0.65f, 0.2f, 1f);

        private CleaveState _state = CleaveState.Chasing;
        private SpriteRenderer _renderer;
        private float _timer;
        private float _nextAttackTime;
        private Vector2 _attackDirection;

        protected override void Start()
        {
            base.Start();
            _renderer = GetComponentInChildren<SpriteRenderer>();
            if (_renderer != null) _baseColor = _renderer.color;
        }

        protected override void OnCombatStarted()
        {
            base.OnCombatStarted();
            _state = CleaveState.Chasing;
            _timer = 0f;
            _nextAttackTime = 0f;
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

            _timer -= Time.fixedDeltaTime;

            Vector2 toTarget = (Vector2)_target.position - _rb.position;
            float distance = toTarget.magnitude;
            Vector2 direction = distance > 0.0001f ? toTarget / distance : Vector2.zero;

            switch (_state)
            {
                case CleaveState.Chasing:
                    _rb.linearVelocity = GetSteeredDirection(direction) * moveSpeed;

                    if (distance <= triggerRange && Time.time >= _nextAttackTime)
                    {
                        _state = CleaveState.Windup;
                        _timer = windupTime;
                        _attackDirection = direction;
                        _rb.linearVelocity = Vector2.zero;
                        SetWindupColor();
                    }
                    break;

                case CleaveState.Windup:
                    _rb.linearVelocity = Vector2.zero;

                    if (_timer <= 0f)
                    {
                        PerformCleave();

                        if (secondStrike)
                        {
                            _state = CleaveState.SecondWindup;
                            _timer = secondDelay;
                        }
                        else
                        {
                            EnterRecover();
                        }
                    }
                    break;

                case CleaveState.SecondWindup:
                    _rb.linearVelocity = Vector2.zero;

                    if (_timer <= 0f)
                    {
                        PerformCleave();
                        EnterRecover();
                    }
                    break;

                case CleaveState.Recover:
                    _rb.linearVelocity = Vector2.zero;
                    if (_timer <= 0f)
                    {
                        _state = CleaveState.Chasing;
                        RestoreColor();
                    }
                    break;
            }
        }

        private void EnterRecover()
        {
            _state = CleaveState.Recover;
            _timer = recoverTime;
            _nextAttackTime = Time.time + attackCooldown;
            RestoreColor();
        }

        private void PerformCleave()
        {
            if (_target == null || _rb == null) return;

            Vector2 toTarget = (Vector2)_target.position - _rb.position;
            if (toTarget.magnitude > radius) return;
            if (toTarget.sqrMagnitude > 0.0001f &&
                Vector2.Angle(_attackDirection, toTarget) > halfAngle) return;

            PlayerHealth health = _target.GetComponentInParent<PlayerHealth>();
            if (health != null)
                health.TryDamage(ScaleAttackDamage(damage));

            AudioService.Play("chop", 0.45f);
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
