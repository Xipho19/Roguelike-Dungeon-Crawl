using UnityEngine;

namespace Roguelike.Combat
{
    /// <summary>
    /// 冲锋者：先普通追击，进入触发距离后短暂蓄力（变橙），
    /// 然后沿锁定方向高速冲锋，结束后进入硬直。
    /// </summary>
    public sealed class ChargerEnemy : Enemy
    {
        private enum ChargeState
        {
            Chasing,
            Windup,
            Charging,
            Recover,
        }

        [Header("冲锋")]
        [SerializeField] private float triggerRange = 6f;
        [SerializeField] private float windupTime = 0.55f;
        [SerializeField] private float chargeSpeed = 12f;
        [SerializeField] private float chargeDuration = 0.45f;
        [SerializeField] private float recoverTime = 0.8f;

        private ChargeState _state = ChargeState.Chasing;
        private float _timer;
        private Vector2 _chargeDirection;
        private SpriteRenderer _renderer;

        protected override void Start()
        {
            base.Start();

            _renderer = GetComponentInChildren<SpriteRenderer>();
            if (_renderer != null) _baseColor = _renderer.color;
        }

        protected override void OnCombatStarted()
        {
            base.OnCombatStarted();

            _state = ChargeState.Chasing;
            _timer = 0f;
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
                case ChargeState.Chasing:
                    _rb.linearVelocity = GetSteeredDirection(direction) * moveSpeed;

                    if (distance <= triggerRange)
                    {
                        _state = ChargeState.Windup;
                        _timer = windupTime;
                        _rb.linearVelocity = Vector2.zero;

                        if (_renderer != null) _renderer.color = new Color(1f, 0.60f, 0.20f, 1f);
                    }
                    break;

                case ChargeState.Windup:
                    _rb.linearVelocity = Vector2.zero;

                    if (_timer <= 0f)
                    {
                        _chargeDirection = direction;
                        _state = ChargeState.Charging;
                        _timer = chargeDuration;
                        RestoreColor();
                    }
                    break;

                case ChargeState.Charging:
                    _rb.linearVelocity = _chargeDirection * chargeSpeed;

                    if (_timer <= 0f)
                    {
                        _state = ChargeState.Recover;
                        _timer = recoverTime;
                    }
                    break;

                case ChargeState.Recover:
                    _rb.linearVelocity = Vector2.zero;

                    if (_timer <= 0f) _state = ChargeState.Chasing;
                    break;
            }
        }

        private void RestoreColor()
        {
            if (_renderer != null) _renderer.color = _baseColor;
        }
    }
}
