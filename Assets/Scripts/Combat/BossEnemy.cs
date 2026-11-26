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
        BlinkWindup,
    }

    /// <summary>
    /// Boss 基类：按楼层主题切换形象，并拥有三套不同的攻击风格。
    ///
    /// 地牢 Ogre：三连冲锋 + 震地重击（近身压制）
    /// 墓穴 Big Zombie：召唤骷髅 + 腐蚀弹幕（人海与远程）
    /// 黄金城 Big Demon：螺旋弹幕 + 追踪火球（高密度弹幕）
    /// 下水道 Sewer Troll：额外冲锋 + 震地毒液环
    /// 遗迹 Ruins Golem：震地重击 + 石弹扇形弹
    /// 黑曜 Obsidian King：暗紫弹幕 + 额外追踪球
    ///
    /// 所有 Boss 共用：追击、接触伤害、50% 血量进入二阶段、楼层成长。
    /// 最终层黑曜君王额外拥有 66% / 33% 两个转阶段点、第三阶段与专属连招。
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

        [Header("Boss 伤害")]
        [Tooltip("第 1 层所有直接攻击的伤害；之后每层 +1")]
        [SerializeField] private int baseFloorDamage = 3;

        private int _attackDamage = 3;

        public int CurrentAttackDamage => _attackDamage;

        [Header("Ogre：蛮力冲锋")]
        [Tooltip("一次技能连续冲锋的次数")]
        [SerializeField] private int ogreChargeCount = 3;

        [Tooltip("连续冲锋之间的短暂停顿")]
        [SerializeField] private float ogreChargePause = 0.18f;

        [SerializeField] private float slamWindup = 0.8f;
        [SerializeField] private float slamRadius = 2.8f;
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

        [Header("Big Demon：黄金城弹幕")]
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

        [Header("第 4-6 层 Boss 形象")]
        [SerializeField] private Sprite[] sewerTrollIdleFrames;
        [SerializeField] private Sprite[] sewerTrollRunFrames;
        [SerializeField] private Sprite[] ruinsGolemIdleFrames;
        [SerializeField] private Sprite[] ruinsGolemRunFrames;
        [SerializeField] private Sprite[] obsidianKingIdleFrames;
        [SerializeField] private Sprite[] obsidianKingRunFrames;

        private SpriteRenderer _body;
        private SpriteAnimator _animator;
        private readonly List<Enemy> _summons = new List<Enemy>();

        private static readonly Color FinalBossTint = new Color(0.68f, 0.4f, 1f, 1f);
        private static readonly Color OgreShockwaveTint = new Color(1f, 0.62f, 0.22f, 1f);
        private static readonly Color GolemShockwaveTint = new Color(0.7f, 0.8f, 1f, 1f);

        private BossState _state = BossState.Intro;
        private FloorBossVisual _archetype = FloorBossVisual.BigDemon;
        private BossVisualDefinition _visualDefinition;
        private float _timer;
        private bool _phase2;
        private bool _phase3;
        private bool _isFinalBoss;
        private int _attackCounter;
        private Vector2 _chargeDirection;
        private int _chargesLeft;
        private int _spiralWavesLeft;
        private int _spiralIndex;
        private float _spiralTimer;
        private int _floorIndex = 1;

        public BossState CurrentState => _state;
        public bool IsPhase2 => _phase2;
        public bool IsPhase3 => _phase3;
        public bool IsFinalBoss => _isFinalBoss;
        public int CurrentPhase => _phase3 ? 3 : (_phase2 ? 2 : 1);
        public int PhaseCount => _isFinalBoss ? 3 : 2;
        public FloorBossVisual Archetype => _archetype;

        public string DisplayName
        {
            get
            {
                if (_visualDefinition != null &&
                    !string.IsNullOrEmpty(_visualDefinition.displayName))
                    return _visualDefinition.displayName;

                switch (_archetype)
                {
                    case FloorBossVisual.Ogre: return "蓝晶雏龙";
                    case FloorBossVisual.BigZombie: return "死亡骑士";
                    case FloorBossVisual.BigDemon: return "赤金狮蝎";
                    case FloorBossVisual.SewerTroll: return "瘟疫鼠王";
                    case FloorBossVisual.RuinsGolem: return "深蓝守望者";
                    case FloorBossVisual.ObsidianKing: return "深渊魔君";
                    default: return "地牢 Boss";
                }
            }
        }

        public string BossSubtitle
        {
            get
            {
                if (_visualDefinition != null &&
                    !string.IsNullOrEmpty(_visualDefinition.subtitle))
                    return _visualDefinition.subtitle;

                if (_isFinalBoss) return "黑曜王座的主人 · 最终试炼";
                switch (_archetype)
                {
                    case FloorBossVisual.Ogre: return "蛮力与震地";
                    case FloorBossVisual.BigZombie: return "亡者军团与腐蚀";
                    case FloorBossVisual.BigDemon: return "螺旋弹幕与追踪火球";
                    case FloorBossVisual.SewerTroll: return "毒沼与连续冲锋";
                    case FloorBossVisual.RuinsGolem: return "石甲与落石";
                    case FloorBossVisual.ObsidianKing: return "黑曜王座的主人";
                    default: return string.Empty;
                }
            }
        }

        public string PhaseLabel
        {
            get
            {
                if (_phase3) return "最终阶段";
                if (_phase2) return _isFinalBoss ? "第二阶段" : "狂暴阶段";
                return _isFinalBoss ? "第一阶段" : string.Empty;
            }
        }

        public string HudTitle
        {
            get
            {
                string title = $"◆ {DisplayName} ◆";
                return string.IsNullOrEmpty(PhaseLabel) ? title : $"{title}  ·  {PhaseLabel}";
            }
        }

        /// <summary>血量/移速由生成方按楼层缩放；这里只记录层数用于攻击成长。</summary>
        public void ApplyFloorScaling(int floor)
        {
            _floorIndex = Mathf.Max(1, floor);
            _attackDamage = Mathf.Max(1, baseFloorDamage + (_floorIndex - 1));
            SetContactDamage(_attackDamage);
        }

        /// <summary>最终层 Boss 额外获得第三阶段与更大体型。</summary>
        public void SetFinalBoss(bool isFinal)
        {
            _isFinalBoss = isFinal;
            if (!isFinal) _phase3 = false;
        }

        /// <summary>按楼层主题切换 Boss 的形象与攻击风格；行为由 _archetype 决定。</summary>
        public void SetVisual(FloorBossVisual visual)
        {
            _archetype = visual;

            if (_body == null) _body = GetComponentInChildren<SpriteRenderer>();
            if (_animator == null) _animator = GetComponent<SpriteAnimator>();
            if (_animator == null) return;

            BossVisualDefinition definition =
                Resources.Load<BossVisualDefinition>($"BossVisuals/{visual}");
            if (definition != null)
            {
                _visualDefinition = definition;
                float definitionScale = definition.visualScale * (_isFinalBoss ? 1.18f : 1f);
                if (_body != null && _body.transform != transform)
                    _body.transform.localScale = new Vector3(definitionScale, definitionScale, 1f);

                _animator.ConfigureDirectional(
                    definition.idleDown,
                    definition.runDown,
                    definition.idleSide,
                    definition.runSide,
                    definition.idleUp,
                    definition.runUp);
                return;
            }

            _visualDefinition = null;
            Sprite[] idle;
            Sprite[] run;
            float visualScale = _isFinalBoss ? 1.18f : 1f;

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
                case FloorBossVisual.SewerTroll:
                    idle = sewerTrollIdleFrames;
                    run = sewerTrollRunFrames;
                    break;
                case FloorBossVisual.RuinsGolem:
                    idle = ruinsGolemIdleFrames;
                    run = ruinsGolemRunFrames;
                    break;
                case FloorBossVisual.ObsidianKing:
                    idle = obsidianKingIdleFrames;
                    run = obsidianKingRunFrames;
                    break;
                default:
                    idle = bigDemonIdleFrames;
                    run = bigDemonRunFrames;
                    break;
            }

            if (_body != null && _body.transform != transform)
                _body.transform.localScale = new Vector3(visualScale, visualScale, 1f);

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

        private float ChargeSpeedNow =>
            chargeSpeed * (_phase3 ? 1.45f : (_phase2 ? 1.25f : 1f)) * FloorChargeSpeedScale;

        private int PhaseBulletBonus => _phase3 ? 4 : (_phase2 ? 2 : 0);
        private int PhaseChargeBonus => _phase3 ? 2 : (_phase2 ? 1 : 0);
        private int PhaseExtraSummons => _phase3 ? 2 : (_phase2 ? 1 : 0);
        private float PhaseWindupScale => _phase3 ? 0.65f : (_phase2 ? 0.8f : 1f);

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
            _phase3 = false;
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

            CombatDebugHud hud = FindFirstObjectByType<CombatDebugHud>();
            if (hud != null)
                hud.ShowToast($"◆ {DisplayName} ◆\n{BossSubtitle}", 2.6f);
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

            float firstThreshold = _isFinalBoss ? 0.66f : phase2Threshold;
            if (!_phase2 && Health <= MaxHealth * firstThreshold)
                EnterPhase2();
            else if (_isFinalBoss && _phase2 && !_phase3 && Health <= MaxHealth * 0.33f)
                EnterPhase3();

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

                case BossState.BlinkWindup:
                    _rb.linearVelocity = Vector2.zero;
                    if (_timer <= 0f)
                    {
                        PerformBlink();
                        EnterChase();
                    }
                    break;
            }

            ClampBossToRoom();
        }

        /// <summary>大体积 Boss 的精灵不能伸进墙体：按当前 Sprite 尺寸把根节点夹在房间内。</summary>
        private void ClampBossToRoom()
        {
            if (_rb == null || _body == null || Room == null) return;

            Bounds bounds = _body.bounds;
            Rect interior = Room.InteriorRect;
            Vector2 position = _rb.position;

            float halfWidth = bounds.extents.x;
            float fullHeight = bounds.size.y;
            float minX = interior.xMin + halfWidth;
            float maxX = interior.xMax - halfWidth;
            float minY = interior.yMin;
            float maxY = interior.yMax - fullHeight;

            if (maxX < minX) position.x = interior.center.x;
            else position.x = Mathf.Clamp(position.x, minX, maxX);

            if (maxY < minY) position.y = minY;
            else position.y = Mathf.Clamp(position.y, minY, maxY);

            if (position == _rb.position) return;

            _rb.position = position;
            transform.position = new Vector3(position.x, position.y, transform.position.z);
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
            _timer = _phase3 ? chaseDuration * 0.5f : (_phase2 ? chaseDuration * 0.7f : chaseDuration);
        }

        private void ChooseAttack()
        {
            _attackCounter++;
            _rb.linearVelocity = Vector2.zero;

            if (_isFinalBoss)
            {
                ChooseFinalAttack();
                return;
            }

            switch (_archetype)
            {
                case FloorBossVisual.Ogre:
                case FloorBossVisual.SewerTroll:
                    ChooseOgreAttack();
                    break;
                case FloorBossVisual.BigZombie:
                    ChooseZombieAttack();
                    break;
                case FloorBossVisual.RuinsGolem:
                    ChooseGolemAttack();
                    break;
                case FloorBossVisual.BigDemon:
                case FloorBossVisual.ObsidianKing:
                    ChooseDemonAttack();
                    break;
                default:
                    ChooseDemonAttack();
                    break;
            }
        }

        /// <summary>黑曜君王：随阶段增加招式，第三阶段同时使用召唤与闪现。</summary>
        private void ChooseFinalAttack()
        {
            int phase = CurrentPhase;
            int cycle = phase >= 3 ? 4 : (phase == 2 ? 3 : 2);
            int step = (_attackCounter - 1) % cycle;

            if (phase == 1)
            {
                if (step == 0) StartSpiralWindup();
                else StartVolleyWindup();
                return;
            }

            if (phase == 2)
            {
                if (step == 0) StartSpiralWindup();
                else if (step == 1) StartHomingWindup();
                else StartBlinkWindup();
                return;
            }

            if (step == 0) StartSpiralWindup();
            else if (step == 1) StartHomingWindup();
            else if (step == 2) StartBlinkWindup();
            else StartSummonWindup();
        }

        private void StartSpiralWindup()
        {
            _state = BossState.RadialWindup;
            _timer = _phase3 ? radialWindup * 0.55f : (_phase2 ? radialWindup * 0.7f : radialWindup);
            FlashColor(_isFinalBoss ? FinalBossTint : new Color(1f, 0.55f, 0.2f, 1f));
        }

        private void StartVolleyWindup()
        {
            _state = BossState.VolleyWindup;
            _timer = _phase3 ? 0.3f : (_phase2 ? 0.38f : 0.45f);
            FlashColor(_isFinalBoss ? new Color(0.82f, 0.55f, 1f, 1f) : new Color(0.65f, 1f, 0.6f, 1f));
        }

        private void StartHomingWindup()
        {
            _state = BossState.HomingWindup;
            _timer = _phase3 ? 0.34f : (_phase2 ? 0.42f : 0.5f);
            FlashColor(_isFinalBoss ? FinalBossTint : new Color(1f, 0.3f, 0.2f, 1f));
        }

        private void StartBlinkWindup()
        {
            _state = BossState.BlinkWindup;
            _timer = _phase3 ? 0.22f : 0.32f;
            FlashColor(_isFinalBoss || _archetype == FloorBossVisual.ObsidianKing
                ? FinalBossTint
                : new Color(1f, 0.55f, 0.2f, 1f));
        }

        private void StartSummonWindup()
        {
            if (!CanSummon())
            {
                StartVolleyWindup();
                return;
            }

            _state = BossState.SummonWindup;
            _timer = _phase3 ? summonWindup * 0.75f : summonWindup;
            FlashColor(FinalBossTint);
        }

        // ---------- Ogre：三连冲锋 + 震地重击 ----------

        private void ChooseOgreAttack()
        {
            if (_attackCounter % 2 == 1)
            {
                int extraCharges = _archetype == FloorBossVisual.SewerTroll ? 1 : 0;
                _chargesLeft = Mathf.Max(1, ogreChargeCount + extraCharges + PhaseChargeBonus);
                _state = BossState.ChargeWindup;
                _timer = chargeWindup;
                FlashColor(_archetype == FloorBossVisual.SewerTroll
                    ? new Color(0.5f, 1f, 0.8f, 1f)
                    : new Color(1f, 0.6f, 0.2f, 1f));
            }
            else
            {
                _state = BossState.SlamWindup;
                _timer = slamWindup * PhaseWindupScale;
                FlashColor(_archetype == FloorBossVisual.SewerTroll
                    ? new Color(0.5f, 1f, 0.8f, 1f)
                    : new Color(1f, 0.25f, 0.2f, 1f));
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
            float radius = slamRadius + (_phase3 ? 1f : (_phase2 ? 0.6f : 0f));
            AudioService.Play("explosion", 0.8f);
            ExplosionFlash.Spawn(_rb.position, slamSprite, radius, 0.35f);
            Color slamFlash = _archetype == FloorBossVisual.SewerTroll
                ? new Color(0.5f, 1f, 0.8f, 1f)
                : _archetype == FloorBossVisual.RuinsGolem
                    ? new Color(0.72f, 0.8f, 1f, 1f)
                    : new Color(1f, 0.3f, 0.2f, 1f);
            FlashColor(slamFlash);

            if (_target == null) return;

            PlayerHealth health = _target.GetComponentInParent<PlayerHealth>();
            if (health != null && Vector2.Distance(_rb.position, _target.position) <= radius)
                health.TryDamage(_attackDamage);

            // 每个 Boss 的震地都有不同后续：
            // 食人魔向外扩散岩石冲击波，下水道巨魔喷毒液，遗迹守卫甩出石弹环。
            if (_isFinalBoss)
            {
                FireRing(10 + FloorBulletBonus, 0f, FinalBossTint);
                AudioService.Play("bookFlip2", 0.55f);
            }
            else if (_archetype == FloorBossVisual.SewerTroll)
            {
                FireRing(6 + FloorBulletBonus, 0f);
                AudioService.Play("bookFlip2", 0.5f);
            }
            else if (_archetype == FloorBossVisual.Ogre)
            {
                FireRing(8 + FloorBulletBonus, 0f, OgreShockwaveTint);
                AudioService.Play("bookFlip2", 0.45f);
            }
            else if (_archetype == FloorBossVisual.RuinsGolem)
            {
                FireRing(8 + FloorBulletBonus, 0f, GolemShockwaveTint);
                AudioService.Play("bookFlip2", 0.45f);
            }
        }

        // ---------- Ruins Golem：震地重击 + 石弹 ----------

        private void ChooseGolemAttack()
        {
            if (_attackCounter % 2 == 1)
            {
                _state = BossState.SlamWindup;
                _timer = slamWindup * PhaseWindupScale;
                FlashColor(new Color(0.72f, 0.8f, 1f, 1f));
            }
            else
            {
                _state = BossState.VolleyWindup;
                _timer = 0.45f;
                FlashColor(new Color(0.82f, 0.86f, 0.72f, 1f));
            }
        }

        // ---------- Big Zombie：召唤骷髅 + 腐蚀弹幕 ----------

        private void ChooseZombieAttack()
        {
            if (_attackCounter % 2 == 1 && CanSummon())
            {
                _state = BossState.SummonWindup;
                _timer = summonWindup;
                FlashColor(_archetype == FloorBossVisual.RuinsGolem
                    ? new Color(0.9f, 0.82f, 0.55f, 1f)
                    : new Color(0.55f, 1f, 0.55f, 1f));
            }
            else
            {
                _state = BossState.VolleyWindup;
                _timer = 0.45f;
                FlashColor(_archetype == FloorBossVisual.RuinsGolem
                    ? new Color(0.95f, 0.86f, 0.55f, 1f)
                    : new Color(0.65f, 1f, 0.6f, 1f));
            }
        }

        private bool CanSummon()
        {
            if (minionPrefab == null) return false;

            CleanupSummons();
            return _summons.Count < maxSummons + PhaseExtraSummons;
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

            int cap = maxSummons + PhaseExtraSummons;
            int wanted = Mathf.Max(1, summonCount + PhaseExtraSummons);
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
                if (minion.Room == null) minion.SetRoom(Room);

                Color summonTint = new Color(0.7f, 1f, 0.7f, 1f);
                minion.Initialize(1f, 2.4f, 0.9f, summonTint, false, _flow);
                minion.ActivateCombat();

                if (_flow != null) _flow.RegisterEnemy(minion);
                _summons.Add(minion);
            }

            AudioService.Play("bookOpen", 0.7f);
            FlashColor(_isFinalBoss ? FinalBossTint : new Color(0.55f, 1f, 0.55f, 1f));
            FireRing(5 + FloorBulletBonus, 0f, _isFinalBoss
                ? FinalBossTint
                : new Color(0.55f, 1f, 0.55f, 1f));
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

            int count = Mathf.Max(1, volleyCount + FloorBulletBonus + PhaseBulletBonus);
            float spread = volleySpread + (_phase3 ? 25f : (_phase2 ? 15f : 0f));
            Vector2 aim = ((Vector2)_target.position - _rb.position).normalized;
            if (aim.sqrMagnitude < 0.0001f) aim = Vector2.right;

            float start = -spread * 0.5f;
            float step = count > 1 ? spread / (count - 1) : 0f;

            for (int i = 0; i < count; i++)
            {
                Vector2 direction = RotateDirection(aim, start + step * i);
                EnemyProjectile bullet = ProjectilePool.Spawn(
                    projectilePrefab,
                    (Vector3)(_rb.position + direction * 0.9f),
                    Quaternion.identity);

                bullet.Initialize(
                    direction,
                    volleySpeed * FloorProjectileSpeedScale,
                    _attackDamage,
                    projectileLifetime);

                Color volleyTint = _archetype == FloorBossVisual.RuinsGolem
                    ? new Color(0.72f, 0.78f, 0.9f, 1f)
                    : zombieProjectileTint;
                bullet.ConfigureVisual(volleyTint, 1.15f);
            }

            AudioService.Play("bookFlip2", 0.4f);
        }

        // ---------- Big Demon：螺旋弹幕 + 追踪火球 ----------

        private void ChooseDemonAttack()
        {
            // 大恶魔的招牌瞬移：每三次行动闪现到玩家附近并留下弹幕。
            if (_archetype == FloorBossVisual.BigDemon && _attackCounter % 3 == 0)
            {
                StartBlinkWindup();
                return;
            }

            if (_attackCounter % 2 == 1)
            {
                _state = BossState.RadialWindup;
                _timer = _phase3 ? radialWindup * 0.55f : (_phase2 ? radialWindup * 0.7f : radialWindup);
                FlashColor(_archetype == FloorBossVisual.ObsidianKing
                    ? new Color(0.72f, 0.4f, 1f, 1f)
                    : new Color(1f, 0.55f, 0.2f, 1f));
            }
            else
            {
                _state = BossState.HomingWindup;
                _timer = _phase2 ? 0.42f : 0.5f;
                FlashColor(_archetype == FloorBossVisual.ObsidianKing
                    ? new Color(0.8f, 0.4f, 1f, 1f)
                    : new Color(1f, 0.3f, 0.2f, 1f));
            }
        }

        private void BeginSpiral()
        {
            _spiralWavesLeft = Mathf.Max(1, spiralWaves + (_phase3 ? 2 : (_phase2 ? 1 : 0)));
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

            int count = Mathf.Max(1, spiralBullets + FloorBulletBonus + PhaseBulletBonus);
            FireRing(count, _spiralIndex * spiralStep);
            _spiralIndex++;
            _spiralWavesLeft--;
            _spiralTimer = Mathf.Max(0.05f, spiralWaveInterval);

            if (_spiralWavesLeft <= 0) EnterChase();
        }

        private void FireRing(int count, float offsetDegrees, Color? tintOverride = null)
        {
            if (projectilePrefab == null) return;

            int bullets = Mathf.Max(1, count);
            float step = 360f / bullets;

            for (int i = 0; i < bullets; i++)
            {
                float angle = (i * step + offsetDegrees) * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                EnemyProjectile bullet = ProjectilePool.Spawn(
                    projectilePrefab,
                    (Vector3)(_rb.position + direction * 0.9f),
                    Quaternion.identity);

                bullet.Initialize(
                    direction,
                    projectileSpeed * FloorProjectileSpeedScale,
                    _attackDamage,
                    projectileLifetime);

                Color ringTint = tintOverride ?? (_archetype == FloorBossVisual.SewerTroll
                    ? new Color(0.55f, 1f, 0.78f, 1f)
                    : _archetype == FloorBossVisual.ObsidianKing
                        ? new Color(0.72f, 0.38f, 1f, 1f)
                        : demonProjectileTint);
                bullet.ConfigureVisual(ringTint, 1f);
            }
        }

        private void FireHomingOrbs()
        {
            if (projectilePrefab == null || _target == null) return;

            int archetypeBonus = _archetype == FloorBossVisual.ObsidianKing ? 1 : 0;
            int count = Mathf.Max(1, homingCount + archetypeBonus + PhaseBulletBonus);
            Vector2 aim = ((Vector2)_target.position - _rb.position).normalized;
            if (aim.sqrMagnitude < 0.0001f) aim = Vector2.right;

            float spread = 30f;
            float start = -spread * 0.5f;
            float step = count > 1 ? spread / (count - 1) : 0f;

            for (int i = 0; i < count; i++)
            {
                Vector2 direction = RotateDirection(aim, start + step * i);
                EnemyProjectile bullet = ProjectilePool.Spawn(
                    projectilePrefab,
                    (Vector3)(_rb.position + direction * 0.9f),
                    Quaternion.identity);

                bullet.Initialize(
                    direction,
                    homingSpeed * FloorProjectileSpeedScale,
                    _attackDamage,
                    homingLifetime);

                Color homingTint = _archetype == FloorBossVisual.ObsidianKing
                    ? new Color(0.72f, 0.38f, 1f, 1f)
                    : demonProjectileTint;
                bullet.ConfigureVisual(homingTint, 1.3f);
                bullet.SetHoming(_target, homingTurnSpeed + (_phase3 ? 70f : (_phase2 ? 40f : 0f)));
            }

            AudioService.Play("creak1", 0.6f);
        }

        private void PerformBlink()
        {
            if (_target == null || _rb == null) return;

            Vector2 origin = _rb.position;
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 2.8f;
            Vector2 destination = ClampToRoom((Vector2)_target.position + offset);

            ExplosionFlash.Spawn(origin, slamSprite, 1.6f, 0.25f);

            _rb.position = destination;
            transform.position = new Vector3(destination.x, destination.y, transform.position.z);

            ExplosionFlash.Spawn(destination, slamSprite, 1.9f, 0.3f);
            FlashColor(_isFinalBoss || _archetype == FloorBossVisual.ObsidianKing
                ? FinalBossTint
                : new Color(1f, 0.55f, 0.2f, 1f));
            AudioService.Play("teleport", 0.65f, 0f);

            // 最终 Boss 的瞬移落点会爆发一圈弹幕，逼玩家重新走位。
            if (_isFinalBoss)
                FireRing(8 + FloorBulletBonus, 0f, FinalBossTint);
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

            _state = BossState.Chase;
            _timer = 0.7f;
            if (_rb != null) _rb.linearVelocity = Vector2.zero;

            FlashColor(_isFinalBoss ? FinalBossTint : new Color(1f, 0.35f, 0.35f, 1f));
            AudioService.Play("creak2", 0.8f);

            if (_isFinalBoss)
            {
                FireRing(10 + FloorBulletBonus, 0f, FinalBossTint);
                ExplosionFlash.Spawn(_rb.position, slamSprite, 2.6f, 0.35f);
            }

            Debug.Log("[Boss] 进入二阶段：" + _archetype);
        }

        private void EnterPhase3()
        {
            _phase3 = true;
            moveSpeed *= 1.18f;

            _state = BossState.Chase;
            _timer = 0.6f;
            if (_rb != null) _rb.linearVelocity = Vector2.zero;

            FlashColor(FinalBossTint);
            AudioService.Play("creak2", 0.95f);
            FireRing(14 + FloorBulletBonus, 18f, FinalBossTint);
            ExplosionFlash.Spawn(_rb.position, slamSprite, 3.2f, 0.4f);
            Debug.Log("[Boss] 进入最终阶段：黑曜君王");
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
