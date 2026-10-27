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
        [SerializeField, Min(1)] private int chargeCount = 1;
        [SerializeField, Min(0f)] private float chargePause = 0.25f;
        [SerializeField] private Color windupTint = new Color(1f, 0.60f, 0.20f, 1f);

        private ChargeState _state = ChargeState.Chasing;
        private float _timer;
        private Vector2 _chargeDirection;
        private SpriteRenderer _renderer;
        private int _chargesLeft;

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
            _chargesLeft = 0;
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
                        _chargesLeft = Mathf.Max(1, chargeCount);
                        _rb.linearVelocity = Vector2.zero;
                        SetWindupColor();
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
                        _chargesLeft--;

                        if (_chargesLeft > 0)
                        {
                            _state = ChargeState.Windup;
                            _timer = chargePause;
                            _rb.linearVelocity = Vector2.zero;
                            SetWindupColor();
                        }
                        else
                        {
                            _state = ChargeState.Recover;
                            _timer = recoverTime;
                        }
                    }
                    break;

                case ChargeState.Recover:
                    _rb.linearVelocity = Vector2.zero;

                    if (_timer <= 0f) _state = ChargeState.Chasing;
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
