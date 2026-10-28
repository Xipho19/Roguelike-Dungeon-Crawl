using Roguelike.Audio;
using Roguelike.Combat;
using Roguelike.Dungeon;
using Roguelike.Items;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Roguelike.Player
{
    /// <summary>
    /// 玩家射击：鼠标瞄准，按住左键朝光标方向连发。
    ///
    /// 伤害、射速、弹速、射程、穿透、发数全部来自 PlayerStats，
    /// 因此道具只需要修改属性，不需要改这里。
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

        [Tooltip("没有 PlayerStats 时的回退值")]
        [SerializeField] private float fireInterval = 0.24f;

        [SerializeField] private float tearSpeed = 14f;
        [SerializeField] private float tearDamage = 1f;
        [SerializeField] private float tearLifetime = 1f;

        [Tooltip("多发时每发之间的夹角（度）")]
        [SerializeField] private float spreadStep = 8f;

        [Tooltip("眼泪生成点距离玩家中心的距离")]
        [SerializeField] private float spawnOffset = 0.8f;

        private Camera _camera;
        private PlayerStats _stats;
        private float _nextFireTime;
        private int _shotCounter;

        private void Awake()
        {
            _camera = Camera.main;
            _stats = GetComponent<PlayerStats>();
            if (inventory == null) inventory = GetComponent<PlayerInventory>();
            if (tracker == null) tracker = GetComponent<PlayerRoomTracker>();
            if (wallet == null) wallet = GetComponent<PlayerWallet>();
            if (health == null) health = GetComponent<PlayerHealth>();
        }

        private void Start()
        {
            if (tearPrefab == null)
                tearPrefab = Resources.Load<TearProjectile>("Combat/Tear");
        }

        private void Update()
        {
            if (!TryReadAim(out Vector2 direction)) return;
            Fire(direction);
        }

        /// <summary>朝给定方向发射，成功返回 true（受射速间隔限制）。</summary>
        public bool Fire(Vector2 direction)
        {
            if (Time.time < _nextFireTime) return false;
            if (tearPrefab == null || direction.sqrMagnitude < 0.0001f) return false;

            direction.Normalize();

            float interval = _stats != null ? _stats.FireInterval : fireInterval;
            float damage = _stats != null ? _stats.Damage : tearDamage;
            float speed = _stats != null ? _stats.TearSpeed : tearSpeed;
            float lifetime = _stats != null ? _stats.TearLifetime : tearLifetime;
            int pierce = _stats != null ? _stats.Pierce : 0;
            int shots = _stats != null ? _stats.ShotCount : 1;

            ItemDefinition explosive = inventory != null
                ? inventory.FindSynergyItem(ItemSynergy.ExplosiveTears)
                : null;
            ItemDefinition homing = inventory != null
                ? inventory.FindSynergyItem(ItemSynergy.HomingTears)
                : null;
            ItemDefinition poison = inventory != null
                ? inventory.FindSynergyItem(ItemSynergy.PoisonTears)
                : null;
            ItemDefinition crit = inventory != null
                ? inventory.FindSynergyItem(ItemSynergy.CritTears)
                : null;
            ItemDefinition split = inventory != null
                ? inventory.FindSynergyItem(ItemSynergy.SplitTears)
                : null;
            ItemDefinition aura = inventory != null
                ? inventory.FindSynergyItem(ItemSynergy.TearAura)
                : null;
            ItemDefinition coinDamage = inventory != null
                ? inventory.FindSynergyItem(ItemSynergy.CoinDamage)
                : null;
            ItemDefinition bloodRage = inventory != null
                ? inventory.FindSynergyItem(ItemSynergy.BloodRage)
                : null;
            ItemDefinition collector = inventory != null
                ? inventory.FindSynergyItem(ItemSynergy.ItemCountDamage)
                : null;
            ItemDefinition rhythm = inventory != null
                ? inventory.FindSynergyItem(ItemSynergy.EveryNthShot)
                : null;
            ItemDefinition bounce = inventory != null
                ? inventory.FindSynergyItem(ItemSynergy.BounceTears)
                : null;

            if (coinDamage != null && wallet != null)
            {
                float step = coinDamage.effectRadius >= 1f ? coinDamage.effectRadius : 10f;
                float maxBonus = coinDamage.effectPower > 0f ? coinDamage.effectPower : 2f;
                damage += Mathf.Min(maxBonus, Mathf.Floor(wallet.Coins / step) * 0.5f);
            }

            // 血怒：生命越低，伤害越高，最多 effectPower。
            if (bloodRage != null && health != null && health.MaxHealth > 0)
            {
                float missing = 1f - Mathf.Clamp01((float)health.Health / health.MaxHealth);
                damage += Mathf.Max(0f, bloodRage.effectPower) * missing;
            }

            // 收藏家之心：每持有 triggerCount 件道具，伤害 +0.5，最多 effectPower。
            if (collector != null && inventory != null)
            {
                int step = Mathf.Max(1, collector.triggerCount);
                float bonus = Mathf.Floor((float)inventory.Items.Count / step) * 0.5f;
                damage += Mathf.Min(Mathf.Max(0f, collector.effectPower), bonus);
            }

            // 节拍器：每第 triggerCount 次射击额外发射 effectPower 发。
            if (rhythm != null)
            {
                _shotCounter++;
                int every = Mathf.Max(1, rhythm.triggerCount);
                if (_shotCounter >= every)
                {
                    _shotCounter = 0;
                    shots += Mathf.Max(1, Mathf.RoundToInt(rhythm.effectPower));
                }
            }
            else
            {
                _shotCounter = 0;
            }

            var modifiers = new TearModifiers
            {
                ownerRoom = tracker != null ? tracker.CurrentRoom : null,
                explosionRadius = explosive != null ? explosive.effectRadius : 0f,
                explosionDamage = explosive != null ? explosive.effectPower : 0f,
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

            _nextFireTime = Time.time + interval;
            AudioService.Play("tear_shot", 0.55f);

            float startAngle = -(shots - 1) * spreadStep * 0.5f;

            for (int i = 0; i < shots; i++)
            {
                Vector2 shotDirection = Rotate(direction, startAngle + i * spreadStep);
                Vector2 spawnPosition = (Vector2)transform.position + shotDirection * spawnOffset;

                TearProjectile tear = ProjectilePool.Spawn(
                    tearPrefab,
                    new Vector3(spawnPosition.x, spawnPosition.y, 0f),
                    Quaternion.identity);
                tear.Initialize(
                    shotDirection,
                    speed,
                    damage,
                    lifetime,
                    pierce,
                    modifiers);
            }

            return true;
        }

        private bool TryReadAim(out Vector2 direction)
        {
            direction = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed)
            {
                return TryScreenToDirection(mouse.position.ReadValue(), out direction);
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButton(0))
            {
                return TryScreenToDirection(Input.mousePosition, out direction);
            }
#endif

            return false;
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
