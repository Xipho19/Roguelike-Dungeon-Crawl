using System.Collections.Generic;
using Roguelike.Combat;
using Roguelike.Items;
using Roguelike.Player;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Roguelike.Dungeon
{
    /// <summary>
    /// 房间流程控制：进入战斗房间时锁门，清空后开门。
    ///
    /// 目前的"清空"来自调试键 K（后续由战斗系统调用 <see cref="ClearCurrentRoom"/>）。
    /// 非战斗房间（起点 / 宝箱 / 商店 / 秘密）不会锁门。
    /// </summary>
    public sealed class DungeonFlowController : MonoBehaviour
    {
        [SerializeField] private PlayerRoomTracker tracker;

        [Tooltip("允许用 K 键模拟清空房间")]
        [SerializeField] private bool enableDebugClearKey = true;

        [Header("遭遇战")]
        [SerializeField] private Enemy enemyPrefab;
        [SerializeField] private ShooterEnemy shooterPrefab;
        [SerializeField] private ChargerEnemy chargerPrefab;
        [SerializeField] private BomberEnemy bomberPrefab;
        [SerializeField] private NecromancerEnemy necromancerPrefab;
        [SerializeField] private SplitterEnemy splitterPrefab;
        [SerializeField] private BossEnemy bossPrefab;

        [Tooltip("普通战斗房的基础敌人数；实际数量会随房间尺寸和距离增加")]
        [SerializeField] private int combatEnemyCount = 4;

        [Tooltip("Boss 房生成的敌人数")]
        [SerializeField] private int bossEnemyCount = 1;

        [SerializeField] private float normalEnemyHealth = 2f;
        [SerializeField] private float bossEnemyHealth = 40f;

        [Header("掉落")]
        [SerializeField] private Pickup coinPrefab;
        [SerializeField] private Pickup heartPrefab;

        [Tooltip("普通敌人掉落的金币数")]
        [SerializeField] private int normalCoinDrop = 1;

        [Tooltip("Boss 掉落的金币数")]
        [SerializeField] private int bossCoinDrop = 5;

        [Tooltip("普通敌人掉落回血的概率")]
        [SerializeField, Range(0f, 1f)] private float heartDropChance = 0.2f;

        [Header("宝箱房")]
        [SerializeField] private ItemPickup itemPickupPrefab;

        [Tooltip("留空时会从 Resources/Items 自动加载所有 ItemDefinition")]
        [SerializeField] private ItemDefinition[] itemPool;

        [Header("秘密房")]
        [Tooltip("留空时从 Resources/Items 自动加载")]
        [SerializeField] private SecretAltar secretAltarPrefab;

        private readonly List<Enemy> _aliveEnemies = new List<Enemy>();
        private readonly List<ItemDefinition> _availableItems = new List<ItemDefinition>();
        private RoomView _activeRoom;
        private PlayerStats _playerStats;
        private PlayerInventory _playerInventory;

        public PlayerRoomTracker Tracker => tracker;
        public int AliveEnemyCount => _aliveEnemies.Count;

        /// <summary>Boss 被击败时触发，供 RunManager 进入通关状态。</summary>
        public event System.Action BossDefeated;

        private void Awake()
        {
            if (tracker == null) tracker = FindFirstObjectByType<PlayerRoomTracker>();
            if (enemyPrefab == null) enemyPrefab = Resources.Load<Enemy>("Combat/Enemy");
            if (shooterPrefab == null) shooterPrefab = Resources.Load<ShooterEnemy>("Combat/Shooter");
            if (chargerPrefab == null) chargerPrefab = Resources.Load<ChargerEnemy>("Combat/Charger");
            if (bomberPrefab == null) bomberPrefab = Resources.Load<BomberEnemy>("Combat/Bomber");
            if (necromancerPrefab == null) necromancerPrefab = Resources.Load<NecromancerEnemy>("Combat/Necromancer");
            if (splitterPrefab == null) splitterPrefab = Resources.Load<SplitterEnemy>("Combat/Splitter");
            if (bossPrefab == null) bossPrefab = Resources.Load<BossEnemy>("Combat/Boss");
            if (coinPrefab == null) coinPrefab = Resources.Load<Pickup>("Combat/Coin");
            if (heartPrefab == null) heartPrefab = Resources.Load<Pickup>("Combat/Heart");
            if (itemPickupPrefab == null) itemPickupPrefab = Resources.Load<ItemPickup>("Items/ItemPickup");
            if (secretAltarPrefab == null) secretAltarPrefab = Resources.Load<SecretAltar>("Items/SecretAltar");

            if (itemPool == null || itemPool.Length == 0)
                itemPool = Resources.LoadAll<ItemDefinition>("Items");

            _playerStats = FindFirstObjectByType<PlayerStats>();
            _playerInventory = FindFirstObjectByType<PlayerInventory>();
        }

        private void OnEnable()
        {
            if (tracker != null) tracker.RoomChanged += HandleRoomChanged;
        }

        private void OnDisable()
        {
            if (tracker != null) tracker.RoomChanged -= HandleRoomChanged;
        }

        private void Start()
        {
            // 若进入 Play Mode 时玩家已经站在某个房间里，补一次判定
            if (tracker != null && tracker.CurrentRoom != null)
                HandleRoomChanged(tracker.CurrentRoom);
        }

        private void Update()
        {
            if (!enableDebugClearKey) return;
            if (WasClearKeyPressed()) ClearCurrentRoom();
        }

        /// <summary>玩家进入房间：战斗房且未清空则锁门并生成敌人，否则保持开启。</summary>
        public void HandleRoomChanged(RoomView room)
        {
            if (room == null) return;

            if (_activeRoom != null && _activeRoom != room)
                CleanupEncounter();

            _activeRoom = room;

            bool isCombatRoom = room.RoomType == RoomType.Combat || room.RoomType == RoomType.Boss;

            if (isCombatRoom && !room.IsCleared)
            {
                room.SetDoorsLocked(true, false);
                SpawnEncounter(room);
                Debug.Log($"[Flow] 房间 {room.GridPosition} 上锁（{room.RoomType}）");
            }
            else
            {
                room.SetDoorsLocked(false, true);
            }

            if (room.RoomType == RoomType.Treasure)
                TrySpawnTreasure(room);

            if (room.RoomType == RoomType.Shop)
                TryStockShop(room);

            if (room.RoomType == RoomType.Secret)
                TrySpawnSecretEvent(room);
        }

        /// <summary>商店首次进入时上架 3 件随机道具，价格按效果数量计算。</summary>
        private void TryStockShop(RoomView room)
        {
            if (room.ShopStocked) return;
            if (itemPickupPrefab == null || itemPool == null || itemPool.Length == 0) return;

            room.ShopStocked = true;

            // 只上架玩家尚未持有的道具，避免重复购买同一件
            var stock = BuildAvailableItems();
            if (stock.Count == 0)
            {
                Debug.Log($"[Flow] 商店 {room.GridPosition} 无可售道具（玩家已持有全部道具）");
                return;
            }

            int count = Mathf.Min(3, stock.Count);
            Vector2 center = room.WorldCenter;

            // 小房间自动收窄商品间距，保证三件商品都在内部
            float spacing = Mathf.Clamp((room.InteriorRect.width - 2f) * 0.5f, 1.5f, 3f);
            float startOffset = -(count - 1) * 0.5f * spacing;

            for (int i = 0; i < count; i++)
            {
                int index = Random.Range(0, stock.Count);
                ItemDefinition item = stock[index];
                stock.RemoveAt(index);

                float x = center.x + startOffset + i * spacing;
                ItemPickup pickup = Instantiate(
                    itemPickupPrefab,
                    new Vector3(x, center.y, 0f),
                    Quaternion.identity,
                    room.transform);

                int price = 5 + (item.modifiers != null ? item.modifiers.Count * 2 : 0);
                pickup.Setup(item, null, price);
            }

            Debug.Log($"[Flow] 商店 {room.GridPosition} 上架 {count} 件道具");
        }

        /// <summary>宝箱房首次进入时生成一个随机道具，领取后不再刷新。</summary>
        private void TrySpawnTreasure(RoomView room)
        {
            if (room.RewardClaimed || room.RewardSpawned) return;
            if (itemPickupPrefab == null || itemPool == null || itemPool.Length == 0) return;

            // 宝箱房也只给尚未持有的道具
            List<ItemDefinition> available = BuildAvailableItems();
            if (available.Count == 0)
            {
                room.RewardSpawned = true;
                Debug.Log($"[Flow] 宝箱房 {room.GridPosition} 无可生成道具（玩家已持有全部道具）");
                return;
            }

            ItemDefinition item = available[Random.Range(0, available.Count)];
            if (item == null) return;

            room.RewardSpawned = true;

            Vector2 center = room.WorldCenter;
            ItemPickup pickup = Instantiate(
                itemPickupPrefab,
                new Vector3(center.x, center.y, 0f),
                Quaternion.identity,
                room.transform);

            pickup.Setup(item, room);

            Debug.Log($"[Flow] 宝箱房 {room.GridPosition} 生成道具：{item.itemName}");
        }

        /// <summary>秘密房首次进入时生成祭坛，祭品从玩家尚未持有的道具中抽取。</summary>
        private void TrySpawnSecretEvent(RoomView room)
        {
            if (room.SecretEventSpawned || room.SecretEventUsed) return;
            if (secretAltarPrefab == null) return;

            List<ItemDefinition> available = BuildAvailableItems();
            if (available.Count == 0)
            {
                room.SecretEventSpawned = true;
                Debug.Log($"[Flow] 秘密房 {room.GridPosition} 无可用祭品（玩家已持有全部道具）");
                return;
            }

            room.SecretEventSpawned = true;

            ItemDefinition item = available[Random.Range(0, available.Count)];
            Vector3 position = new Vector3(room.WorldCenter.x, room.WorldCenter.y, 0f);
            SecretAltar altar = Instantiate(secretAltarPrefab, position, Quaternion.identity, room.transform);
            altar.Setup(item, room);

            Debug.Log($"[Flow] 秘密房 {room.GridPosition} 生成祭坛，祭品：{item.itemName}");
        }

        /// <summary>当前可提供的道具：剔除空引用与玩家背包里已有的道具。</summary>
        private List<ItemDefinition> BuildAvailableItems()
        {
            _availableItems.Clear();
            if (itemPool == null) return _availableItems;

            for (int i = 0; i < itemPool.Length; i++)
            {
                ItemDefinition item = itemPool[i];
                if (item == null || IsOwned(item)) continue;
                _availableItems.Add(item);
            }

            return _availableItems;
        }

        private bool IsOwned(ItemDefinition item)
        {
            if (_playerInventory == null) return false;

            IReadOnlyList<ItemDefinition> owned = _playerInventory.Items;
            for (int i = 0; i < owned.Count; i++)
            {
                if (owned[i] == item) return true;
            }

            return false;
        }

        /// <summary>敌人死亡回调：先掉落奖励，再判断是否清空房间。</summary>
        public void NotifyEnemyDied(Enemy enemy)
        {
            if (enemy != null) SpawnDrops(enemy);

            _aliveEnemies.Remove(enemy);

            if (_aliveEnemies.Count == 0 && _activeRoom != null && !_activeRoom.IsCleared)
                ClearCurrentRoom();
        }

        /// <summary>按敌人类型掉落金币 / 回血。</summary>
        private void SpawnDrops(Enemy enemy)
        {
            Vector3 origin = enemy.transform.position;
            int bonus = _playerStats != null ? _playerStats.CoinBonus : 0;
            int coins = (enemy.IsBoss ? bossCoinDrop : normalCoinDrop) + bonus;

            for (int i = 0; i < coins; i++)
                SpawnPickup(coinPrefab, ScatterAround(origin, 0.7f));

            if (enemy.IsBoss || Random.value < heartDropChance)
                SpawnPickup(heartPrefab, ScatterAround(origin, 0.35f));
        }

        private Vector3 ScatterAround(Vector3 origin, float radius)
        {
            Vector2 point = (Vector2)origin + Random.insideUnitCircle * radius;

            // 避免掉进墙体
            if (_activeRoom != null)
            {
                Rect rect = _activeRoom.WorldRect;
                point.x = Mathf.Clamp(point.x, rect.xMin + 1.5f, rect.xMax - 1.5f);
                point.y = Mathf.Clamp(point.y, rect.yMin + 1.5f, rect.yMax - 1.5f);
            }

            return new Vector3(point.x, point.y, origin.z);
        }

        private void SpawnPickup(Pickup prefab, Vector3 position)
        {
            if (prefab == null) return;

            Transform parent = _activeRoom != null ? _activeRoom.transform : transform;
            Instantiate(prefab, position, Quaternion.identity, parent);
        }

        /// <summary>标记当前房间已清空并开门。</summary>
        public void ClearCurrentRoom()
        {
            RoomView room = _activeRoom != null ? _activeRoom : (tracker != null ? tracker.CurrentRoom : null);
            if (room == null) return;

            room.IsCleared = true;
            room.SetDoorsLocked(false, false);
            Debug.Log($"[Flow] 房间 {room.GridPosition} 已清空，开门");

            if (room.RoomType == RoomType.Boss)
                BossDefeated?.Invoke();
        }

        /// <summary>重开一局前清理：敌人、子弹、掉落、房间引用。</summary>
        public void ResetForNewRun()
        {
            CleanupEncounter();

            foreach (TearProjectile tear in FindObjectsByType<TearProjectile>(FindObjectsSortMode.None))
                Destroy(tear.gameObject);

            foreach (EnemyProjectile bullet in FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None))
                Destroy(bullet.gameObject);

            foreach (Pickup pickup in FindObjectsByType<Pickup>(FindObjectsSortMode.None))
                Destroy(pickup.gameObject);

            foreach (ItemPickup item in FindObjectsByType<ItemPickup>(FindObjectsSortMode.None))
                Destroy(item.gameObject);
        }

        private void SpawnEncounter(RoomView room)
        {
            bool isBoss = room.RoomType == RoomType.Boss;

            if (enemyPrefab == null)
            {
                Debug.LogError("[Combat] 找不到敌人 Prefab，先执行 Tools/Roguelike/生成战斗 Prefab。");
                return;
            }

            if (_aliveEnemies.Count > 0) return;

            int count = isBoss ? bossEnemyCount : EnemyCountFor(room);
            IReadOnlyList<Transform> points = room.EnemySpawnPoints;
            count = Mathf.Min(count, points.Count);

            for (int i = 0; i < count; i++)
            {
                Enemy prefab = isBoss ? bossPrefab : PickPrefab(room.DistanceFromStart, i);
                if (prefab == null) prefab = enemyPrefab;

                Enemy enemy = Instantiate(prefab, points[i].position, Quaternion.identity, room.transform);
                enemy.name = isBoss ? "Boss" : $"{prefab.GetType().Name}_{i}";

                enemy.Initialize(
                    isBoss ? bossEnemyHealth : HealthFor(prefab),
                    isBoss ? 1.7f : SpeedFor(prefab),
                    1f,
                    Color.white,
                    isBoss,
                    this);

                RegisterEnemy(enemy);
            }

            Debug.Log($"[Combat] 房间 {room.GridPosition} 生成 {count} 个敌人（距离 {room.DistanceFromStart}）");
        }

        /// <summary>
        /// 普通战斗房的敌人数：基础 4 只，越大、越深的房间越多。
        /// </summary>
        private int EnemyCountFor(RoomView room)
        {
            int baseCount = Mathf.Max(4, combatEnemyCount);
            int sizeBonus =
                Mathf.Max(0, (room.Width - RoomGeometry.FootprintWidth) / 2) +
                Mathf.Max(0, (room.Height - RoomGeometry.FootprintHeight) / 2);
            int depthBonus = Mathf.Clamp(room.DistanceFromStart / 2, 0, 3);

            return baseCount + sizeBonus + depthBonus;
        }

        /// <summary>
        /// 按房间距离混编敌人：
        ///   近处只有追猎者；中段加入射击者、冲锋者与自爆史莱姆；
        ///   深处固定出现亡灵法师与分裂僵尸，保证新怪物能实际登场。
        /// </summary>
        private Enemy PickPrefab(int distance, int index)
        {
            if (distance < 3) return enemyPrefab;

            if (distance >= 6)
            {
                switch (index % 3)
                {
                    case 0: return FirstAvailable(bomberPrefab, enemyPrefab);
                    case 1: return FirstAvailable(necromancerPrefab, shooterPrefab, enemyPrefab);
                    default: return FirstAvailable(splitterPrefab, chargerPrefab, enemyPrefab);
                }
            }

            if (distance >= 4)
            {
                switch (index % 3)
                {
                    case 0: return FirstAvailable(shooterPrefab, enemyPrefab);
                    case 1: return FirstAvailable(bomberPrefab, chargerPrefab, enemyPrefab);
                    default: return FirstAvailable(chargerPrefab, enemyPrefab);
                }
            }

            if (index % 3 == 1) return FirstAvailable(shooterPrefab, enemyPrefab);
            return enemyPrefab;
        }

        private Enemy FirstAvailable(params Enemy[] options)
        {
            for (int i = 0; i < options.Length; i++)
            {
                if (options[i] != null) return options[i];
            }

            return enemyPrefab;
        }

        private float HealthFor(Enemy prefab)
        {
            if (prefab is SplitterEnemy) return normalEnemyHealth * 2f;
            if (prefab is NecromancerEnemy) return normalEnemyHealth * 1.5f;
            return normalEnemyHealth;
        }

        private static float SpeedFor(Enemy prefab)
        {
            if (prefab is ShooterEnemy) return 2.0f;
            if (prefab is ChargerEnemy) return 2.6f;
            if (prefab is BomberEnemy) return 2.2f;
            if (prefab is NecromancerEnemy) return 1.9f;
            if (prefab is SplitterEnemy) return 1.6f;
            return 2.4f;
        }

        /// <summary>登记新生成的敌人（包括召唤物与分裂体），保证全部清掉才开门。</summary>
        public void RegisterEnemy(Enemy enemy)
        {
            if (enemy == null || _aliveEnemies.Contains(enemy)) return;

            _aliveEnemies.Add(enemy);
        }

        private void CleanupEncounter()
        {
            for (int i = 0; i < _aliveEnemies.Count; i++)
            {
                if (_aliveEnemies[i] != null) Destroy(_aliveEnemies[i].gameObject);
            }

            _aliveEnemies.Clear();
            _activeRoom = null;
        }

        private static bool WasClearKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null) return keyboard.kKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.K);
#else
            return false;
#endif
        }
    }
}
