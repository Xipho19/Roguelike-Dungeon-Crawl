using System.Collections.Generic;
using Roguelike.Audio;
using Roguelike.Dungeon;
using Roguelike.Player;
using Roguelike.Visual;
using UnityEngine;

namespace Roguelike.Combat
{
    public enum BossState
    {
        Intro,
        Chase,
        RadialWindup,
        SpiralFiring,
        ChargeWindup,
        Charging,
        SlamWindup,
        SummonWindup,
        VolleyWindup,
        HomingWindup,
    }

    /// <summary>
    /// Boss 基类：按楼层主题切换形象，并拥有三套不同的攻击风格。
    ///
    /// 地牢 Ogre：三连冲锋 + 震地重击（近身压制）
    /// 墓穴 Big Zombie：召唤骷髅 + 腐蚀弹幕（人海与远程）
    /// 熔岩 Big Demon：螺旋弹幕 + 追踪火球（高密度弹幕）
    ///
    /// 所有 Boss 共用：追击、接触伤害、50% 血量进入二阶段、楼层成长。
    /// </summary>
    public sealed class BossEnemy : Enemy
    {
        public static BossEnemy Active { get; private set; }

        [Header("Boss 攻击")]
        [SerializeField] private EnemyProjectile projectilePrefab;

        [Header("通用节奏")]
        [SerializeField] private float chaseDuration = 2f;
        [SerializeField] private float radialWindup = 0.5f;
        [SerializeField] private float chargeWindup = 0.6f;
        [SerializeField] private float chargeDuration = 0.5f;
        [SerializeField] private float chargeSpeed = 11f;
        [SerializeField] private float projectileSpeed = 5f;
        [SerializeField] private float projectileLifetime = 4f;

        [Header("Ogre：蛮力冲锋")]
        [Tooltip("一次技能连续冲锋的次数")]
        [SerializeField] private int ogreChargeCount = 3;

        [Tooltip("连续冲锋之间的短暂停顿")]
        [SerializeField] private float ogreChargePause = 0.18f;

        [SerializeField] private float slamWindup = 0.8f;
        [SerializeField] private float slamRadius = 2.8f;
        [SerializeField] private int slamDamage = 2;
        [SerializeField] private Sprite slamSprite;

        [Header("Big Zombie：亡灵军团")]
        [SerializeField] private Enemy minionPrefab;
        [SerializeField] private int summonCount = 2;
        [SerializeField] private int maxSummons = 4;
        [SerializeField] private float summonWindup = 0.7f;
        [SerializeField] private int volleyCount = 5;
        [SerializeField] private float volleySpread = 50f;
        [SerializeField] private float volleySpeed = 4.5f;
        [SerializeField] private Color zombieProjectileTint = new Color(0.55f, 1f, 0.55f, 1f);

        [Header("Big Demon：炼狱弹幕")]
        [SerializeField] private int spiralWaves = 3;
        [SerializeField] private int spiralBullets = 10;
        [SerializeField] private float spiralStep = 15f;
        [SerializeField] private float spiralWaveInterval = 0.22f;
        [SerializeField] private int homingCount = 3;
        [SerializeField] private float homingSpeed = 3.2f;
        [SerializeField] private float homingTurnSpeed = 140f;
        [SerializeField] private float homingLifetime = 4f;
        [SerializeField] private Color demonProjectileTint = new Color(1f, 0.55f, 0.25f, 1f);

        [Tooltip("血量低于该比例进入二阶段")]
        [SerializeField] private float phase2Threshold = 0.5f;

        [Header("楼层成长")]
        [Tooltip("每多少层额外增加 1 发环形/扇形弹幕")]
        [SerializeField] private int extraBulletEveryFloors = 2;

        [SerializeField] private int maxExtraBullets = 2;

        [Tooltip("每层弹速成长比例")]
        [SerializeField] private float projectileSpeedGrowthPerFloor = 0.04f;

        [SerializeField] private float maxProjectileSpeedBonus = 0.15f;

        [Tooltip("每层冲锋速度成长比例")]
        [SerializeField] private float chargeSpeedGrowthPerFloor = 0.03f;

        [SerializeField] private float maxChargeSpeedBonus = 0.12f;

        [Header("Boss 形象")]
        [SerializeField] private Sprite[] ogreIdleFrames;
        [SerializeField] private Sprite[] ogreRunFrames;
        [SerializeField] private Sprite[] bigZombieIdleFrames;
        [SerializeField] private Sprite[] bigZombieRunFrames;
        [SerializeField] private Sprite[] bigDemonIdleFrames;
        [SerializeField] private Sprite[] bigDemonRunFrames;

        private SpriteRenderer _body;
        private SpriteAnimator _animator;
        private readonly List<Enemy> _summons = new List<Enemy>();

        private BossState _state = BossState.Intro;
        private FloorBossVisual _archetype = FloorBossVisual.BigDemon;
        private float _timer;
        private bool _phase2;
        private int _attackCounter;
        private Vector2 _chargeDirection;
        private int _chargesLeft;
        private int _spiralWavesLeft;
        private int _spiralIndex;
        private float _spiralTimer;
        private int _floorIndex = 1;

        public BossState CurrentState => _state;
        public bool IsPhase2 => _phase2;
        public FloorBossVisual Archetype => _archetype;

        /// <summary>血量/移速由生成方按楼层缩放；这里只记录层数用于攻击成长。</summary>
        public void ApplyFloorScaling(int floor)
        {
            _floorIndex = Mathf.Max(1, floor);
        }

        /// <summary>按楼层主题切换 Boss 的形象与攻击风格；行为由 _archetype 决定。</summary>
        public void SetVisual(FloorBossVisual visual)
        {
            _archetype = visual;

            if (_animator == null) _animator = GetComponent<SpriteAnimator>();
            if (_animator == null) return;

            Sprite[] idle;
            Sprite[] run;

            switch (visual)
            {
                case FloorBossVisual.Ogre:
                    idle = ogreIdleFrames;
                    run = ogreRunFrames;
                    break;
                case FloorBossVisual.BigZombie:
                    idle = bigZombieIdleFrames;
                    run = bigZombieRunFrames;
                    break;
                default:
                    idle = bigDemonIdleFrames;
                    run = bigDemonRunFrames;
                    break;
            }

            if (idle == null || idle.Length == 0) return;
            _animator.Configure(idle, run);
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

        private float ChargeSpeedNow => chargeSpeed * (_phase2 ? 1.25f : 1f) * FloorChargeSpeedScale;

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

            if (_animator == null) _animator = GetComponent<SpriteAnimator>();
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
            _chargesLeft = 0;
            _spiralWavesLeft = 0;
            _summons.Clear();
        }

        protected override void OnCombatStarted()
        {
            base.OnCombatStarted();

            _state = BossState.Intro;
            _timer = 0.8f;
            _attackCounter = 0;
            _chargesLeft = 0;
            _spiralWavesLeft = 0;
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
                    if (_timer <= 0f) BeginSpiral();
                    break;

                case BossState.SpiralFiring:
                    _rb.linearVelocity = Vector2.zero;
                    UpdateSpiral();
                    break;

                case BossState.ChargeWindup:
                    _rb.linearVelocity = Vector2.zero;
                    if (_timer <= 0f) BeginCharge();
                    break;

                case BossState.Charging:
                    _rb.linearVelocity = _chargeDirection * ChargeSpeedNow;
                    if (_timer <= 0f) EndChargeStep();
                    break;

                case BossState.SlamWindup:
                    _rb.linearVelocity = Vector2.zero;
                    if (_timer <= 0f)
                    {
                        PerformSlam();
                        EnterChase();
                    }
                    break;

                case BossState.SummonWindup:
                    _rb.linearVelocity = Vector2.zero;
                    if (_timer <= 0f)
                    {
                        PerformSummon();
                        EnterChase();
                    }
                    break;

                case BossState.VolleyWindup:
                    _rb.linearVelocity = Vector2.zero;
                    if (_timer <= 0f)
                    {
                        FireVolley();
                        EnterChase();
                    }
                    break;

                case BossState.HomingWindup:
                    _rb.linearVelocity = Vector2.zero;
                    if (_timer <= 0f)
                    {
                        FireHomingOrbs();
                        EnterChase();
                    }
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
            _rb.linearVelocity = Vector2.zero;

            switch (_archetype)
            {
                case FloorBossVisual.Ogre:
                    ChooseOgreAttack();
                    break;
                case FloorBossVisual.BigZombie:
                    ChooseZombieAttack();
                    break;
                default:
                    ChooseDemonAttack();
                    break;
            }
        }

        // ---------- Ogre：三连冲锋 + 震地重击 ----------

        private void ChooseOgreAttack()
        {
            if (_attackCounter % 2 == 1)
            {
                _chargesLeft = Mathf.Max(1, ogreChargeCount + (_phase2 ? 1 : 0));
                _state = BossState.ChargeWindup;
                _timer = chargeWindup;
                FlashColor(new Color(1f, 0.6f, 0.2f, 1f));
            }
            else
            {
                _state = BossState.SlamWindup;
                _timer = _phase2 ? slamWindup * 0.8f : slamWindup;
                FlashColor(new Color(1f, 0.25f, 0.2f, 1f));
            }
        }

        private void BeginCharge()
        {
            Vector2 direction = _target != null
                ? ((Vector2)_target.position - _rb.position).normalized
                : _chargeDirection;

            if (direction.sqrMagnitude < 0.0001f) direction = Vector2.right;

            _chargeDirection = direction;
            _state = BossState.Charging;
            _timer = chargeDuration;
            _rb.linearVelocity = _chargeDirection * ChargeSpeedNow;
        }

        private void EndChargeStep()
        {
            _chargesLeft--;

            if (_chargesLeft > 0)
            {
                _state = BossState.ChargeWindup;
                _timer = Mathf.Max(0.12f, ogreChargePause);
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            EnterChase();
        }

        private void PerformSlam()
        {
            float radius = slamRadius + (_phase2 ? 0.6f : 0f);
            AudioService.Play("explosion", 0.8f);
            ExplosionFlash.Spawn(_rb.position, slamSprite, radius, 0.35f);
            FlashColor(new Color(1f, 0.3f, 0.2f, 1f));

            if (_target == null) return;

            PlayerHealth health = _target.GetComponentInParent<PlayerHealth>();
            if (health != null && Vector2.Distance(_rb.position, _target.position) <= radius)
                health.TryDamage(slamDamage);
        }

        // ---------- Big Zombie：召唤骷髅 + 腐蚀弹幕 ----------

        private void ChooseZombieAttack()
        {
            if (_attackCounter % 2 == 1 && CanSummon())
            {
                _state = BossState.SummonWindup;
                _timer = summonWindup;
                FlashColor(new Color(0.55f, 1f, 0.55f, 1f));
            }
            else
            {
                _state = BossState.VolleyWindup;
                _timer = 0.45f;
                FlashColor(new Color(0.65f, 1f, 0.6f, 1f));
            }
        }

        private bool CanSummon()
        {
            if (minionPrefab == null) return false;

            CleanupSummons();
            return _summons.Count < maxSummons + (_phase2 ? 1 : 0);
        }

        private void CleanupSummons()
        {
            for (int i = _summons.Count - 1; i >= 0; i--)
                if (_summons[i] == null) _summons.RemoveAt(i);
        }

        private void PerformSummon()
        {
            if (minionPrefab == null) return;

            CleanupSummons();

            int cap = maxSummons + (_phase2 ? 1 : 0);
            int wanted = Mathf.Max(1, summonCount + (_phase2 ? 1 : 0));
            int count = Mathf.Min(wanted, Mathf.Max(0, cap - _summons.Count));
            if (count <= 0) return;

            float baseAngle = Random.Range(0f, 360f);
            float step = 360f / Mathf.Max(1, count);

            for (int i = 0; i < count; i++)
            {
                float angle = (baseAngle + i * step) * Mathf.Deg2Rad;
                Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 1.5f;
                Vector2 position = ClampToRoom(_rb.position + offset);

                Enemy minion = Instantiate(minionPrefab, position, Quaternion.identity, transform.parent);
                minion.name = "BossSummon";
                minion.Initialize(1f, 2.4f, 0.9f, new Color(0.7f, 1f, 0.7f, 1f), false, _flow);
                minion.ActivateCombat();

                if (_flow != null) _flow.RegisterEnemy(minion);
                _summons.Add(minion);
            }

            AudioService.Play("bookOpen", 0.7f);
            FlashColor(new Color(0.55f, 1f, 0.55f, 1f));
        }

        private Vector2 ClampToRoom(Vector2 point)
        {
            RoomView room = Room;
            if (room == null) return point;

            Rect interior = room.InteriorRect;
            point.x = Mathf.Clamp(point.x, interior.xMin + 0.8f, interior.xMax - 0.8f);
            point.y = Mathf.Clamp(point.y, interior.yMin + 0.8f, interior.yMax - 0.8f);
            return point;
        }

        private void FireVolley()
        {
            if (projectilePrefab == null || _target == null) return;

            int count = Mathf.Max(1, volleyCount + FloorBulletBonus + (_phase2 ? 2 : 0));
            float spread = volleySpread + (_phase2 ? 15f : 0f);
            Vector2 aim = ((Vector2)_target.position - _rb.position).normalized;
            if (aim.sqrMagnitude < 0.0001f) aim = Vector2.right;

            float start = -spread * 0.5f;
            float step = count > 1 ? spread / (count - 1) : 0f;

            for (int i = 0; i < count; i++)
            {
                Vector2 direction = RotateDirection(aim, start + step * i);
                EnemyProjectile bullet = Instantiate(
                    projectilePrefab,
                    _rb.position + direction * 0.9f,
                    Quaternion.identity);

                bullet.Initialize(
                    direction,
                    volleySpeed * FloorProjectileSpeedScale,
                    1,
                    projectileLifetime);

                bullet.ConfigureVisual(zombieProjectileTint, 1.15f);
            }

            AudioService.Play("bookFlip2", 0.4f);
        }

        // ---------- Big Demon：螺旋弹幕 + 追踪火球 ----------

        private void ChooseDemonAttack()
        {
            if (_attackCounter % 2 == 1)
            {
                _state = BossState.RadialWindup;
                _timer = _phase2 ? radialWindup * 0.7f : radialWindup;
                FlashColor(new Color(1f, 0.55f, 0.2f, 1f));
            }
            else
            {
                _state = BossState.HomingWindup;
                _timer = 0.5f;
                FlashColor(new Color(1f, 0.3f, 0.2f, 1f));
            }
        }

        private void BeginSpiral()
        {
            _spiralWavesLeft = Mathf.Max(1, spiralWaves + (_phase2 ? 1 : 0));
            _spiralIndex = 0;
            _spiralTimer = 0f;
            _state = BossState.SpiralFiring;
        }

        private void UpdateSpiral()
        {
            if (_spiralWavesLeft <= 0)
            {
                EnterChase();
                return;
            }

            _spiralTimer -= Time.fixedDeltaTime;
            if (_spiralTimer > 0f) return;

            int count = Mathf.Max(1, spiralBullets + FloorBulletBonus + (_phase2 ? 2 : 0));
            FireRing(count, _spiralIndex * spiralStep);
            _spiralIndex++;
            _spiralWavesLeft--;
            _spiralTimer = Mathf.Max(0.05f, spiralWaveInterval);

            if (_spiralWavesLeft <= 0) EnterChase();
        }

        private void FireRing(int count, float offsetDegrees)
        {
            if (projectilePrefab == null) return;

            int bullets = Mathf.Max(1, count);
            float step = 360f / bullets;

            for (int i = 0; i < bullets; i++)
            {
                float angle = (i * step + offsetDegrees) * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                EnemyProjectile bullet = Instantiate(
                    projectilePrefab,
                    _rb.position + direction * 0.9f,
                    Quaternion.identity);

                bullet.Initialize(
                    direction,
                    projectileSpeed * FloorProjectileSpeedScale,
                    1,
                    projectileLifetime);

                bullet.ConfigureVisual(demonProjectileTint, 1f);
            }
        }

        private void FireHomingOrbs()
        {
            if (projectilePrefab == null || _target == null) return;

            int count = Mathf.Max(1, homingCount + (_phase2 ? 2 : 0));
            Vector2 aim = ((Vector2)_target.position - _rb.position).normalized;
            if (aim.sqrMagnitude < 0.0001f) aim = Vector2.right;

            float spread = 30f;
            float start = -spread * 0.5f;
            float step = count > 1 ? spread / (count - 1) : 0f;

            for (int i = 0; i < count; i++)
            {
                Vector2 direction = RotateDirection(aim, start + step * i);
                EnemyProjectile bullet = Instantiate(
                    projectilePrefab,
                    _rb.position + direction * 0.9f,
                    Quaternion.identity);

                bullet.Initialize(
                    direction,
                    homingSpeed * FloorProjectileSpeedScale,
                    1,
                    homingLifetime);

                bullet.ConfigureVisual(demonProjectileTint, 1.3f);
                bullet.SetHoming(_target, homingTurnSpeed + (_phase2 ? 40f : 0f));
            }

            AudioService.Play("creak1", 0.6f);
        }

        private static Vector2 RotateDirection(Vector2 direction, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);

            return new Vector2(
                direction.x * cos - direction.y * sin,
                direction.x * sin + direction.y * cos);
        }

        private void EnterPhase2()
        {
            _phase2 = true;
            moveSpeed *= 1.25f;
            FlashColor(new Color(1f, 0.35f, 0.35f, 1f));
            AudioService.Play("creak2", 0.8f);
            Debug.Log("[Boss] 进入二阶段：" + _archetype);
        }

        protected override void Die()
        {
            // Boss 倒下时，残留的召唤物一起崩解，避免房间因小怪未清而锁住
            for (int i = 0; i < _summons.Count; i++)
                if (_summons[i] != null && !_summons[i].IsDead)
                    _summons[i].TakeDamage(9999f);

            _summons.Clear();
            base.Die();
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
