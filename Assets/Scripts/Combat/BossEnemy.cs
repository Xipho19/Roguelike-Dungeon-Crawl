using Roguelike.Audio;
using Roguelike.Dungeon;
using UnityEngine;

namespace Roguelike.Combat
{
    public enum BossState
    {
        Intro,
        Chase,
        RadialWindup,
        ChargeWindup,
        Charging,
    }

    /// <summary>
    /// Boss：在追击基础上加入两种攻击模式，并在 50% 血量进入二阶段。
    ///
    /// 循环：
    ///   追击 2s → 环形弹幕 / 蓄力冲锋 交替 → 追击 …
    /// 二阶段：
    ///   移速与冲锋速度提升、追击时间缩短、弹幕数量翻倍。
    /// </summary>
    public sealed class BossEnemy : Enemy
    {
        public static BossEnemy Active { get; private set; }

        [Header("Boss 攻击")]
        [SerializeField] private EnemyProjectile projectilePrefab;

        [SerializeField] private float chaseDuration = 2f;
        [SerializeField] private float radialWindup = 0.5f;
        [SerializeField] private float chargeWindup = 0.6f;
        [SerializeField] private float chargeDuration = 0.5f;
        [SerializeField] private float chargeSpeed = 11f;
        [SerializeField] private int radialBullets = 8;
        [SerializeField] private float projectileSpeed = 5f;
        [SerializeField] private float projectileLifetime = 4f;

        [Tooltip("血量低于该比例进入二阶段")]
        [SerializeField] private float phase2Threshold = 0.5f;

        [Header("楼层成长")]
        [Tooltip("每多少层额外增加 1 发环形弹幕")]
        [SerializeField] private int extraBulletEveryFloors = 2;

        [SerializeField] private int maxExtraBullets = 2;

        [Tooltip("每层弹速成长比例")]
        [SerializeField] private float projectileSpeedGrowthPerFloor = 0.04f;

        [SerializeField] private float maxProjectileSpeedBonus = 0.15f;

        [Tooltip("每层冲锋速度成长比例")]
        [SerializeField] private float chargeSpeedGrowthPerFloor = 0.03f;

        [SerializeField] private float maxChargeSpeedBonus = 0.12f;

        private SpriteRenderer _body;
        private BossState _state = BossState.Intro;
        private float _timer;
        private bool _phase2;
        private int _attackCounter;
        private Vector2 _chargeDirection;
        private int _floorIndex = 1;

        public BossState CurrentState => _state;
        public bool IsPhase2 => _phase2;

        /// <summary>血量/移速由生成方按楼层缩放；这里只记录层数用于攻击成长。</summary>
        public void ApplyFloorScaling(int floor)
        {
            _floorIndex = Mathf.Max(1, floor);
        }

        private int FloorBulletBonus =>
            extraBulletEveryFloors <= 0
                ? 0
                : Mathf.Clamp((_floorIndex - 1) / extraBulletEveryFloors, 0, Mathf.Max(0, maxExtraBullets));

        private float FloorProjectileSpeedScale =>
            1f + Mathf.Clamp(
                projectileSpeedGrowthPerFloor * Mathf.Max(0, _floorIndex - 1),
                0f,
                Mathf.Max(0f, maxProjectileSpeedBonus));

        private float FloorChargeSpeedScale =>
            1f + Mathf.Clamp(
                chargeSpeedGrowthPerFloor * Mathf.Max(0, _floorIndex - 1),
                0f,
                Mathf.Max(0f, maxChargeSpeedBonus));

        protected override void OnEnable()
        {
            base.OnEnable();
            Active = this;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (Active == this) Active = null;
        }

        protected override void Start()
        {
            base.Start();

            if (projectilePrefab == null)
                projectilePrefab = Resources.Load<EnemyProjectile>("Combat/EnemyProjectile");

            _body = GetComponentInChildren<SpriteRenderer>();
            _state = BossState.Intro;
            _timer = 0.8f;
        }

        public override void Initialize(
            float health,
            float speed,
            float scale,
            Color tint,
            bool isBoss,
            DungeonFlowController flow,
            bool playSpawnAnimation = true)
        {
            base.Initialize(health, speed, scale, tint, isBoss, flow, playSpawnAnimation);

            _state = BossState.Intro;
            _timer = 0.8f;
            _phase2 = false;
            _attackCounter = 0;
        }

        protected override void OnCombatStarted()
        {
            base.OnCombatStarted();

            _state = BossState.Intro;
            _timer = 0.8f;
            _attackCounter = 0;
            _rb.linearVelocity = Vector2.zero;
        }

        protected override void FixedUpdate()
        {
            if (IsDead || _rb == null) return;

            // 玩家进入 Boss 房之前站在场地中央待机
            if (!CombatActive)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            if (_target == null) return;

            if (!_phase2 && Health <= MaxHealth * phase2Threshold)
                EnterPhase2();

            _timer -= Time.fixedDeltaTime;

            switch (_state)
            {
                case BossState.Intro:
                    _rb.linearVelocity = Vector2.zero;
                    if (_timer <= 0f) EnterChase();
                    break;

                case BossState.Chase:
                    MoveTowardTarget();
                    if (_timer <= 0f) ChooseAttack();
                    break;

                case BossState.RadialWindup:
                    _rb.linearVelocity = Vector2.zero;
                    if (_timer <= 0f)
                    {
                        FireRadial();
                        EnterChase();
                    }
                    break;

                case BossState.ChargeWindup:
                    _rb.linearVelocity = Vector2.zero;
                    if (_timer <= 0f)
                    {
                        Vector2 direction = (Vector2)_target.position - _rb.position;
                        _chargeDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
                        _state = BossState.Charging;
                        _timer = chargeDuration;
                    }
                    break;

                case BossState.Charging:
                    _rb.linearVelocity = _chargeDirection *
                        (chargeSpeed * (_phase2 ? 1.25f : 1f) * FloorChargeSpeedScale);
                    if (_timer <= 0f) EnterChase();
                    break;
            }
        }

        private void MoveTowardTarget()
        {
            Vector2 toTarget = (Vector2)_target.position - _rb.position;
            Vector2 direction = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector2.zero;
            _rb.linearVelocity = GetSteeredDirection(direction) * moveSpeed;
        }

        private void EnterChase()
        {
            _state = BossState.Chase;
            _timer = _phase2 ? chaseDuration * 0.7f : chaseDuration;
        }

        private void ChooseAttack()
        {
            _attackCounter++;

            if (_attackCounter % 2 == 1)
            {
                _state = BossState.RadialWindup;
                _timer = _phase2 ? radialWindup * 0.7f : radialWindup;
                FlashColor(new Color(1f, 0.65f, 0.2f, 1f));
            }
            else
            {
                _state = BossState.ChargeWindup;
                _timer = _phase2 ? chargeWindup * 0.75f : chargeWindup;
                FlashColor(new Color(1f, 0.25f, 0.25f, 1f));
            }

            _rb.linearVelocity = Vector2.zero;
        }

        private void FireRadial()
        {
            if (projectilePrefab == null) return;

            int count = radialBullets + FloorBulletBonus + (_phase2 ? 4 : 0);
            int rings = _phase2 ? 2 : 1;
            float step = 360f / count;

            for (int ring = 0; ring < rings; ring++)
            {
                float offset = ring * step * 0.5f;

                for (int i = 0; i < count; i++)
                {
                    float angle = (i * step + offset) * Mathf.Deg2Rad;
                    Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                    EnemyProjectile bullet = Instantiate(
                        projectilePrefab,
                        _rb.position + direction * 0.9f,
                        Quaternion.identity);

                    bullet.Initialize(direction,
                        projectileSpeed * (_phase2 ? 1.2f : 1f) * FloorProjectileSpeedScale,
                        1,
                        projectileLifetime);
                }
            }
        }

        private void EnterPhase2()
        {
            _phase2 = true;
            moveSpeed *= 1.25f;
            FlashColor(new Color(1f, 0.35f, 0.35f, 1f));
            AudioService.Play("creak2", 0.8f);
            Debug.Log("[Boss] 进入二阶段");
        }

        private void FlashColor(Color color)
        {
            if (_body != null) _body.color = color;

            CancelInvoke(nameof(RestoreColor));
            Invoke(nameof(RestoreColor), 0.25f);
        }

        private void RestoreColor()
        {
            if (_body != null) _body.color = _baseColor;
        }
    }
}
