using UnityEngine;

namespace Roguelike.Combat
{
    /// <summary>
    /// 基础追击怪的短扑版本：追到一定距离后蓄力，向玩家方向短距离扑击。
    /// 用于地牢层的哥布林/近战小怪，和冲锋者的长距离直线冲锋区分开。
    /// </summary>
    public sealed class LungeEnemy : Enemy
    {
        private enum LungeState
        {
            Chasing,
            Windup,
            Lunging,
            Recover,
        }

        [Header("短扑")]
        [SerializeField] private float triggerRange = 2.8f;
        [SerializeField] private float windupTime = 0.3f;
        [SerializeField] private float lungeSpeed = 8f;
        [SerializeField] private float lungeDuration = 0.22f;
        [SerializeField] private float recoverTime = 0.55f;
        [SerializeField] private float lungeCooldown = 2.6f;
        [SerializeField] private Color windupTint = new Color(1f, 0.65f, 0.2f, 1f);

        private LungeState _state = LungeState.Chasing;
        private SpriteRenderer _renderer;
        private float _timer;
        private float _nextLungeTime;
        private Vector2 _lungeDirection;

        protected override void Start()
        {
            base.Start();
            _renderer = GetComponentInChildren<SpriteRenderer>();
            if (_renderer != null) _baseColor = _renderer.color;
        }

        protected override void OnCombatStarted()
        {
            base.OnCombatStarted();
            _state = LungeState.Chasing;
            _timer = 0f;
            _nextLungeTime = 0f;
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
                case LungeState.Chasing:
                    _rb.linearVelocity = GetSteeredDirection(direction) * moveSpeed;

                    if (distance <= triggerRange && Time.time >= _nextLungeTime)
                    {
                        _state = LungeState.Windup;
                        _timer = windupTime;
                        _rb.linearVelocity = Vector2.zero;
                        SetWindupColor();
                    }
                    break;

                case LungeState.Windup:
                    _rb.linearVelocity = Vector2.zero;

                    if (_timer <= 0f)
                    {
                        _lungeDirection = direction;
                        _state = LungeState.Lunging;
                        _timer = lungeDuration;
                        RestoreColor();
                    }
                    break;

                case LungeState.Lunging:
                    _rb.linearVelocity = _lungeDirection * lungeSpeed;

                    if (_timer <= 0f)
                    {
                        _state = LungeState.Recover;
                        _timer = recoverTime;
                        _nextLungeTime = Time.time + lungeCooldown;
                    }
                    break;

                case LungeState.Recover:
                    _rb.linearVelocity = Vector2.zero;
                    if (_timer <= 0f) _state = LungeState.Chasing;
                    break;
            }
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
