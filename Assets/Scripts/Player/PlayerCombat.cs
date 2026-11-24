using System.Collections.Generic;
using Roguelike.Audio;
using Roguelike.Combat;
using Roguelike.Core;
using Roguelike.Dungeon;
using Roguelike.Items;
using Roguelike.Weapons;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Roguelike.Player
{
    /// <summary>
    /// 玩家战斗入口：鼠标瞄准，左键使用当前武器。
    ///
    /// 远程武器复用 TearProjectile 的对象池、房间边界和全部自动道具效果；
    /// 武士刀走独立的扇形近战检测，保留通用的加伤、暴击与中毒。
    /// 伤害/射速/弹速/穿透/发数都经过 PlayerStats 的道具修饰器结算。
    /// </summary>
    public sealed class PlayerCombat : MonoBehaviour
    {
        [Header("射击")]
        [SerializeField] private TearProjectile tearPrefab;

        [Tooltip("用于读取爆炸眼泪等触发型被动；留空时自动查找")]
        [SerializeField] private PlayerInventory inventory;

        [Tooltip("用于把眼泪限制在发射时所在的房间内；留空时自动查找")]
        [SerializeField] private PlayerRoomTracker tracker;

        [Tooltip("用于金币加伤类道具；留空时自动查找")]
        [SerializeField] private PlayerWallet wallet;

        [Tooltip("用于低血量增伤类道具；留空时自动查找")]
        [SerializeField] private PlayerHealth health;

        [Tooltip("两格武器背包；留空时自动查找或补上")]
        [SerializeField] private WeaponInventory weapons;

        [Tooltip("手持武器显示；留空时自动查找或补上")]
        [SerializeField] private WeaponVisual weaponVisual;

        [Tooltip("没有 PlayerStats / 武器时的回退值")]
        [SerializeField] private float fireInterval = 0.24f;

        [SerializeField] private float tearSpeed = 14f;
        [SerializeField] private float tearDamage = 1f;
        [SerializeField] private float tearLifetime = 1f;

        [Tooltip("多发时每发之间的夹角（度）")]
        [SerializeField] private float spreadStep = 8f;

        [Tooltip("弹丸生成点距离玩家中心的距离")]
        [SerializeField] private float spawnOffset = 0.8f;

        private Camera _camera;
        private PlayerStats _stats;
        private RunManager _runManager;
        private PauseMenu _pauseMenu;
        private RoomCamera _roomCamera;
        private float _nextFireTime;
        private int _shotCounter;
        private int _comboStep;
        private float _comboUntil;
        private Vector2 _aimDirection = Vector2.right;

        public Vector2 AimDirection => _aimDirection;
        public WeaponInventory Weapons => weapons;

        private void Awake()
        {
            _camera = Camera.main;
            _stats = GetComponent<PlayerStats>();
            if (inventory == null) inventory = GetComponent<PlayerInventory>();
            if (tracker == null) tracker = GetComponent<PlayerRoomTracker>();
            if (wallet == null) wallet = GetComponent<PlayerWallet>();
            if (health == null) health = GetComponent<PlayerHealth>();

            _runManager = FindFirstObjectByType<RunManager>();
            _pauseMenu = FindFirstObjectByType<PauseMenu>();
            if (_camera != null) _roomCamera = _camera.GetComponent<RoomCamera>();

            if (weapons == null) weapons = GetComponent<WeaponInventory>();
            if (weapons == null) weapons = gameObject.AddComponent<WeaponInventory>();

            if (weaponVisual == null) weaponVisual = GetComponent<WeaponVisual>();
            if (weaponVisual == null) weaponVisual = gameObject.AddComponent<WeaponVisual>();
        }

        private void OnEnable()
        {
            if (weapons != null) weapons.Changed += HandleWeaponChanged;
        }

        private void OnDisable()
        {
            if (weapons != null) weapons.Changed -= HandleWeaponChanged;
        }

        private void Start()
        {
            if (tearPrefab == null)
                tearPrefab = Resources.Load<TearProjectile>("Combat/Tear");
        }

        private void HandleWeaponChanged()
        {
            _comboStep = 0;
            _comboUntil = 0f;
            _shotCounter = 0;
        }

        private void Update()
        {
            UpdateAimDirection();

            if (!CanAct()) return;
            if (!IsFireHeld()) return;

            WeaponDefinition weapon = weapons != null ? weapons.Current : null;
            if (weapon != null && weapon.IsMelee)
                TryMelee(_aimDirection);
            else
                Fire(_aimDirection);
        }

        /// <summary>朝给定方向发射，成功返回 true（受射速、弹匣和装填限制）。</summary>
        public bool Fire(Vector2 direction)
        {
            if (!CanAct()) return false;
            if (Time.time < _nextFireTime) return false;
            if (tearPrefab == null || direction.sqrMagnitude < 0.0001f) return false;

            WeaponDefinition weapon = weapons != null ? weapons.Current : null;
            if (weapon == null || weapon.IsMelee) return false;

            if (weapon.UsesMagazine)
            {
                if (weapons.IsReloading) return false;
                if (weapons.Ammo <= 0 || !weapons.TryConsumeRound())
                {
                    weapons.StartReload();
                    return false;
                }
            }

            direction.Normalize();

            float interval = Resolve(StatType.FireInterval, weapon.fireInterval, fireInterval);
            float damage = Resolve(StatType.Damage, weapon.damage, tearDamage);
            float speed = Resolve(StatType.TearSpeed, weapon.projectileSpeed, tearSpeed);
            float lifetime = Resolve(StatType.TearLifetime, weapon.projectileLifetime, tearLifetime);
            int pierce = ResolveInt(StatType.Pierce, weapon.pierce);
            int shots = Mathf.Max(1, ResolveInt(StatType.ShotCount, weapon.projectiles));

            damage = ApplyDamageBonuses(damage);
            shots = ApplyRhythm(shots);

            var modifiers = BuildModifiers(weapon);

            _nextFireTime = Time.time + Mathf.Max(0.05f, interval);
            AudioService.Play(weapon.fireSound, weapon.fireVolume);

            float shotSpread = weapon.spreadStep > 0f ? weapon.spreadStep : spreadStep;
            float startAngle = -(shots - 1) * shotSpread * 0.5f;

            for (int i = 0; i < shots; i++)
            {
                Vector2 shotDirection = Rotate(direction, startAngle + i * shotSpread);
                Vector2 spawnPosition = (Vector2)transform.position + shotDirection * spawnOffset;

                TearProjectile tear = ProjectilePool.Spawn(
                    tearPrefab,
                    new Vector3(spawnPosition.x, spawnPosition.y, 0f),
                    Quaternion.identity);
                if (tear == null) continue;

                tear.Initialize(
                    shotDirection,
                    speed,
                    damage,
                    lifetime,
                    pierce,
                    modifiers);
                tear.ConfigureVisual(
                    weapon.projectileFrames,
                    weapon.projectileScale,
                    weapon.projectileColor,
                    weapon.rotateProjectile);
            }

            return true;
        }

        /// <summary>武士刀挥砍：前方扇形范围伤害，第三次命中造成强化斩击。</summary>
        public bool TryMelee(Vector2 direction)
        {
            if (!CanAct()) return false;
            if (Time.time < _nextFireTime) return false;
            if (direction.sqrMagnitude < 0.0001f) return false;

            WeaponDefinition weapon = weapons != null ? weapons.Current : null;
            if (weapon == null || !weapon.IsMelee) return false;

            direction.Normalize();

            int comboHits = Mathf.Max(1, weapon.comboHits);
            if (_comboStep > 0 && Time.time > _comboUntil) _comboStep = 0;
            _comboStep = _comboStep % comboHits + 1;
            bool finisher = _comboStep >= comboHits;
            _comboUntil = Time.time + Mathf.Max(0.1f, weapon.comboWindow);

            if (weaponVisual != null) weaponVisual.PlaySwing(_comboStep);

            float interval = Resolve(StatType.FireInterval, weapon.fireInterval, fireInterval);
            float damage = Resolve(StatType.Damage, weapon.damage, tearDamage);
            if (finisher) damage *= Mathf.Max(1f, weapon.comboFinisherScale);
            damage = ApplyDamageBonuses(damage);

            TearModifiers modifiers = BuildModifiers(weapon);
            float radius = weapon.meleeRadius + (finisher ? 0.25f : 0f);
            float halfArc = Mathf.Clamp(weapon.meleeArc, 20f, 360f) * 0.5f;

            _nextFireTime = Time.time + Mathf.Max(0.05f, interval);
            bool axe = weapon.kind == WeaponKind.Waraxe;

            string swingSound = weapon.fireSound;
            float swingVolume = weapon.fireVolume;
            if (finisher && !string.IsNullOrEmpty(weapon.finisherSound))
            {
                swingSound = weapon.finisherSound;
                swingVolume = weapon.finisherVolume > 0.0001f
                    ? weapon.finisherVolume
                    : weapon.fireVolume;
            }
            else if (_comboStep > 1 && !string.IsNullOrEmpty(weapon.altSound))
            {
                swingSound = weapon.altSound;
                swingVolume = weapon.altVolume > 0.0001f
                    ? weapon.altVolume
                    : weapon.fireVolume;
            }

            AudioService.Play(swingSound, swingVolume);
            if (finisher && _roomCamera != null)
                _roomCamera.Shake(0.15f, axe ? 0.13f : 0.095f);

            float aimAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Vector2 side = new Vector2(-direction.y, direction.x);
            if (axe)
                SpawnWaraxeSlash(weapon, direction, aimAngle);
            else
                SpawnComboSlash(weapon, direction, side, aimAngle);

            RoomView room = tracker != null ? tracker.CurrentRoom : null;
            Collider2D[] overlaps = Physics2D.OverlapCircleAll(transform.position, radius);
            var damaged = new HashSet<Enemy>();
            var hitProps = new HashSet<BreakableProp>();

            for (int i = 0; i < overlaps.Length; i++)
            {
                Collider2D collider = overlaps[i];
                if (collider == null) continue;

                Vector2 toTarget = (Vector2)collider.bounds.center - (Vector2)transform.position;
                if (toTarget.sqrMagnitude > 0.0001f &&
                    Vector2.Angle(direction, toTarget) > halfArc)
                    continue;

                var prop = collider.GetComponentInParent<BreakableProp>();
                if (prop != null)
                {
                    RoomView propRoom = prop.GetComponentInParent<RoomView>();
                    if (room != null && propRoom != null && propRoom != room) continue;

                    if (hitProps.Add(prop)) prop.TakeHit();
                    continue;
                }

                Enemy enemy = collider.GetComponentInParent<Enemy>();
                if (enemy == null || enemy.IsDead || !enemy.CombatActive) continue;
                if (room != null && enemy.Room != room) continue;
                if (!damaged.Add(enemy)) continue;

                float dealt = damage;
                if (modifiers.critChance > 0f && Random.value < modifiers.critChance)
                    dealt *= 2f;

                enemy.TakeDamage(dealt);
                enemy.ApplyKnockback(
                    direction,
                    weapon.meleeKnockback * (finisher ? 1.25f : 1f));

                if (modifiers.poisonTicks > 0 && modifiers.poisonDamage > 0f)
                {
                    enemy.ApplyPoison(
                        modifiers.poisonDamage,
                        modifiers.poisonTicks,
                        modifiers.poisonInterval);
                }
            }

            return true;
        }

        /// <summary>
        /// 三段斩的刀光差异：
        ///   1. 右肩起手，向右下扫；
        ///   2. 左肩起手，反向回扫；
        ///   3. 金色大范围交叉重斩，附带短暂镜头震动。
        /// </summary>
        private void SpawnComboSlash(
            WeaponDefinition weapon,
            Vector2 direction,
            Vector2 side,
            float aimAngle)
        {
            float baseAngle = aimAngle - 90f;
            float scale = weapon.slashScale;
            Vector2 center = (Vector2)transform.position + direction * 0.72f;

            switch (_comboStep)
            {
                case 1:
                    MeleeSlashEffect.Spawn(
                        weapon.slashFrames,
                        center + side * 0.30f,
                        baseAngle,
                        scale * 0.96f,
                        new Color(1f, 1f, 1f, 1f),
                        -34f,
                        28f,
                        scale * 1.25f,
                        0.22f,
                        30f);
                    break;

                case 2:
                    MeleeSlashEffect.Spawn(
                        weapon.slashFrames,
                        center - side * 0.30f,
                        baseAngle,
                        scale * 1.02f,
                        new Color(0.36f, 0.78f, 1f, 1f),
                        34f,
                        -28f,
                        scale * 1.32f,
                        0.24f,
                        30f);
                    break;

                default:
                    MeleeSlashEffect.Spawn(
                        weapon.slashFrames,
                        center,
                        baseAngle,
                        scale * 1.38f,
                        new Color(1f, 0.86f, 0.38f, 1f),
                        -20f,
                        20f,
                        scale * 1.85f,
                        0.34f,
                        30f);

                    // 第二道斜斩，与主刀光交叉成明显的重击轮廓。
                    MeleeSlashEffect.Spawn(
                        weapon.slashFrames,
                        center + side * 0.28f,
                        baseAngle + 58f,
                        scale * 0.9f,
                        new Color(1f, 0.72f, 0.28f, 0.78f),
                        26f,
                        -26f,
                        scale * 1.3f,
                        0.3f,
                        30f);

                    // 白色高光弧，让第三段的爆发在混战中也能看清。
                    MeleeSlashEffect.Spawn(
                        weapon.slashFrames,
                        center - side * 0.22f,
                        baseAngle - 58f,
                        scale * 1.08f,
                        new Color(1f, 0.96f, 0.82f, 0.9f),
                        -24f,
                        24f,
                        scale * 1.45f,
                        0.28f,
                        30f);
                    break;
            }
        }

        /// <summary>战斧旋斩：第一段两道对向弧光，第二段四向全周重击。</summary>
        private void SpawnWaraxeSlash(WeaponDefinition weapon, Vector2 direction, float aimAngle)
        {
            bool finisher = _comboStep >= Mathf.Max(1, weapon.comboHits);
            int count = finisher ? 4 : 2;
            float baseAngle = aimAngle - 90f;
            float scale = weapon.slashScale;
            Vector2 center = (Vector2)transform.position + direction * 0.15f;
            Color color = finisher
                ? new Color(1f, 0.82f, 0.30f, 1f)
                : new Color(0.82f, 0.94f, 1f, 1f);

            for (int i = 0; i < count; i++)
            {
                float angle = baseAngle + i * (360f / count);
                MeleeSlashEffect.Spawn(
                    weapon.slashFrames,
                    center,
                    angle,
                    scale * (finisher ? 1.15f : 1f),
                    color,
                    -45f,
                    45f,
                    scale * (finisher ? 1.5f : 1.2f),
                    finisher ? 0.22f : 0.16f,
                    20f);
            }
        }

        private float ApplyDamageBonuses(float damage)
        {
            ItemDefinition coinDamage = FindSynergy(ItemSynergy.CoinDamage);
            if (coinDamage != null && wallet != null)
            {
                float step = coinDamage.effectRadius >= 1f ? coinDamage.effectRadius : 10f;
                float maxBonus = coinDamage.effectPower > 0f ? coinDamage.effectPower : 2f;
                damage += Mathf.Min(maxBonus, Mathf.Floor(wallet.Coins / step) * 0.5f);
            }

            ItemDefinition bloodRage = FindSynergy(ItemSynergy.BloodRage);
            if (bloodRage != null && health != null && health.MaxHealth > 0)
            {
                float missing = 1f - Mathf.Clamp01((float)health.Health / health.MaxHealth);
                damage += Mathf.Max(0f, bloodRage.effectPower) * missing;
            }

            ItemDefinition collector = FindSynergy(ItemSynergy.ItemCountDamage);
            if (collector != null && inventory != null)
            {
                int step = Mathf.Max(1, collector.triggerCount);
                float bonus = Mathf.Floor((float)inventory.Items.Count / step) * 0.5f;
                damage += Mathf.Min(Mathf.Max(0f, collector.effectPower), bonus);
            }

            return damage;
        }

        private int ApplyRhythm(int shots)
        {
            ItemDefinition rhythm = FindSynergy(ItemSynergy.EveryNthShot);
            if (rhythm == null)
            {
                _shotCounter = 0;
                return shots;
            }

            _shotCounter++;
            int every = Mathf.Max(1, rhythm.triggerCount);
            if (_shotCounter >= every)
            {
                _shotCounter = 0;
                shots += Mathf.Max(1, Mathf.RoundToInt(rhythm.effectPower));
            }

            return shots;
        }

        private TearModifiers BuildModifiers(WeaponDefinition weapon)
        {
            ItemDefinition explosive = FindSynergy(ItemSynergy.ExplosiveTears);
            ItemDefinition homing = FindSynergy(ItemSynergy.HomingTears);
            ItemDefinition poison = FindSynergy(ItemSynergy.PoisonTears);
            ItemDefinition crit = FindSynergy(ItemSynergy.CritTears);
            ItemDefinition split = FindSynergy(ItemSynergy.SplitTears);
            ItemDefinition aura = FindSynergy(ItemSynergy.TearAura);
            ItemDefinition bounce = FindSynergy(ItemSynergy.BounceTears);

            float explosionRadius = explosive != null ? explosive.effectRadius : 0f;
            float explosionDamage = explosive != null ? explosive.effectPower : 0f;

            // 火焰法杖等武器自带爆炸；与爆炸眼泪取更强的数值，避免叠加过头。
            if (weapon != null)
            {
                explosionRadius = Mathf.Max(explosionRadius, weapon.projectileExplosionRadius);
                explosionDamage = Mathf.Max(explosionDamage, weapon.projectileExplosionDamage);
            }

            return new TearModifiers
            {
                ownerRoom = tracker != null ? tracker.CurrentRoom : null,
                explosionRadius = explosionRadius,
                explosionDamage = explosionDamage,
                homingTurnSpeed = homing != null ? homing.effectPower : 0f,
                homingRange = homing != null ? homing.effectRadius : 0f,
                poisonDamage = poison != null ? poison.effectPower : 0f,
                poisonTicks = poison != null ? 4 : 0,
                poisonInterval = 0.6f,
                critChance = crit != null ? crit.effectPower : 0f,
                splitCount = split != null ? split.triggerCount : 0,
                splitDamageScale = split != null ? split.effectPower : 0.5f,
                auraDamage = aura != null ? aura.effectPower : 0f,
                auraRadius = aura != null ? aura.effectRadius : 0f,
                auraInterval = 0.3f,
                bounceCount = bounce != null ? Mathf.Max(0, bounce.triggerCount) : 0,
                bounceSpeedScale = bounce != null ? Mathf.Clamp(bounce.effectPower, 0.1f, 1f) : 1f,
            };
        }

        private ItemDefinition FindSynergy(ItemSynergy synergy)
        {
            return inventory != null ? inventory.FindSynergyItem(synergy) : null;
        }

        private float Resolve(StatType type, float weaponValue, float fallback)
        {
            if (_stats == null) return weaponValue > 0f ? weaponValue : fallback;
            return _stats.Resolve(type, weaponValue);
        }

        private int ResolveInt(StatType type, int weaponValue)
        {
            if (_stats == null) return weaponValue;
            return Mathf.Max(0, Mathf.RoundToInt(_stats.Resolve(type, weaponValue)));
        }

        private bool CanAct()
        {
            if (Time.timeScale <= 0.001f) return false;
            if (MapUI.IsOpen) return false;
            if (CombatDebugHud.IsInventoryOpen) return false;
            if (_pauseMenu != null && _pauseMenu.IsPaused) return false;
            if (_runManager != null && _runManager.State != RunManager.RunState.Playing) return false;
            if (health != null && health.IsDead) return false;
            return true;
        }

        private void UpdateAimDirection()
        {
            if (TryReadPointerPosition(out Vector3 pointer) &&
                TryScreenToDirection(pointer, out Vector2 direction))
            {
                _aimDirection = direction;
            }
        }

        private bool IsFireHeld()
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            if (mouse != null) return mouse.leftButton.isPressed;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButton(0);
#else
            return false;
#endif
        }

        private static bool TryReadPointerPosition(out Vector3 position)
        {
            position = Vector3.zero;

#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                position = mouse.position.ReadValue();
                return true;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            position = Input.mousePosition;
            return true;
#else
            return false;
#endif
        }

        private bool TryScreenToDirection(Vector3 screenPosition, out Vector2 direction)
        {
            direction = Vector2.zero;

            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return false;

            Vector3 world = _camera.ScreenToWorldPoint(screenPosition);
            direction = (Vector2)world - (Vector2)transform.position;
            return direction.sqrMagnitude > 0.0001f;
        }

        private static Vector2 Rotate(Vector2 vector, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);

            return new Vector2(
                vector.x * cos - vector.y * sin,
                vector.x * sin + vector.y * cos);
        }
    }
}
