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

        [Tooltip("留空时从同一物体或场景中查找")]
        [SerializeField] private DungeonBuilder builder;

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

        [Header("精英怪")]
        [Tooltip("普通敌人变成精英的概率；Boss 不会精英化")]
        [SerializeField, Range(0f, 1f)] private float eliteChance = 0.15f;

        [Tooltip("精英怪血量倍率")]
        [SerializeField] private float eliteHealthMultiplier = 2f;

        [Tooltip("精英怪攻击倍率（最终伤害向上取整）")]
        [SerializeField] private float eliteDamageMultiplier = 1.2f;

        [SerializeField] private float normalEnemyHealth = 2f;
        [SerializeField] private float bossEnemyHealth = 40f;

        [Header("楼层难度成长")]
        [Tooltip("普通敌人血量成长比例（最终血量 = 基础血量 × (1 + 该值 × 当前层数)）")]
        [SerializeField] private float enemyHealthGrowthPerFloor = 1f;

        [Tooltip("普通敌人每层攻击力成长（第 1 层用基础值，之后每层增加该值）")]
        [SerializeField] private int enemyDamageGrowthPerFloor = 1;

        [Tooltip("每层普通敌人移速成长比例")]
        [SerializeField] private float enemySpeedGrowthPerFloor = 0.03f;

        [SerializeField] private float enemySpeedMultiplierCap = 1.20f;

        [Tooltip("Boss 血量成长比例（最终血量 = 40 × (1 + 该值 × 当前层数)）")]
        [SerializeField] private float bossHealthGrowthPerFloor = 0.50f;

        [Tooltip("每层 Boss 移速成长比例")]
        [SerializeField] private float bossSpeedGrowthPerFloor = 0.04f;

        [SerializeField] private float bossSpeedMultiplierCap = 1.15f;

        [Tooltip("每多少层增加 1 只普通敌人")]
        [SerializeField] private int extraEnemyEveryFloors = 2;

        [SerializeField] private int maxExtraEnemies = 2;

        [Tooltip("每高一层，敌人编成距离额外增加多少")]
        [SerializeField] private int compositionDistancePerFloor = 1;

        [Tooltip("每多少层额外掉 1 枚金币")]
        [SerializeField] private int coinGrowthEveryFloors = 2;

        [Tooltip("每层商店价格额外增加多少")]
        [SerializeField] private int shopPriceGrowthPerFloor = 2;

        [Header("掉落")]
        [SerializeField] private Pickup coinPrefab;
        [SerializeField] private Pickup heartPrefab;

        [Tooltip("普通敌人掉落的金币数")]
        [SerializeField] private int normalCoinDrop = 1;

        [Tooltip("Boss 掉落的金币数")]
        [SerializeField] private int bossCoinDrop = 5;

        [Tooltip("普通敌人每次击杀掉落金币的概率；Boss 仍然必定掉落")]
        [SerializeField, Range(0f, 1f)] private float coinDropChance = 0.5f;

        [Tooltip("普通敌人掉落回血的概率")]
        [SerializeField, Range(0f, 1f)] private float heartDropChance = 0.2f;

        [Header("宝箱房")]
        [SerializeField] private ItemPickup itemPickupPrefab;

        [Tooltip("留空时会从 Resources/Items 自动加载所有 ItemDefinition")]
        [SerializeField] private ItemDefinition[] itemPool;

        [Header("道具稀有度权重")]
        [SerializeField, Min(0f)] private float commonWeight = 60f;
        [SerializeField, Min(0f)] private float rareWeight = 30f;
        [SerializeField, Min(0f)] private float legendaryWeight = 10f;

        [Header("商店刷新")]
        [Tooltip("每次刷新商店的基础金币价格；仍会叠加楼层加价")]
        [SerializeField, Min(1)] private int shopRerollBaseCost = 3;

        [Header("秘密房")]
        [Tooltip("留空时从 Resources/Items 自动加载")]
        [SerializeField] private SecretAltar secretAltarPrefab;

        [Header("挑战房")]
        [Tooltip("挑战房的波数")]
        [SerializeField, Range(1, 5)] private int challengeWaveCount = 3;

        private readonly Dictionary<RoomView, List<Enemy>> _roomEnemies = new Dictionary<RoomView, List<Enemy>>();
        private readonly Dictionary<RoomView, int> _challengeWaves = new Dictionary<RoomView, int>();
        private readonly List<ItemDefinition> _availableItems = new List<ItemDefinition>();
        private RoomView _activeRoom;
        private PlayerStats _playerStats;
        private PlayerInventory _playerInventory;
        private PlayerWallet _playerWallet;
        private CombatDebugHud _hud;
        private bool _floorPrepared;

        private int _floor = 1;
        private FloorTheme _floorTheme;

        public PlayerRoomTracker Tracker => tracker;
        public int Floor => _floor;
        public FloorTheme CurrentFloorTheme => _floorTheme;

        /// <summary>当前房间还活着的敌人数（用于亡灵法师的召唤上限）。</summary>
        public int AliveEnemyCount
        {
            get
            {
                if (_activeRoom == null || !_roomEnemies.TryGetValue(_activeRoom, out List<Enemy> list))
                    return 0;

                int count = 0;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] != null && !list[i].IsDead) count++;
                }

                return count;
            }
        }

        /// <summary>由 RunManager 在建楼前写入当前楼层与主题。</summary>
        public void SetFloor(int floor, FloorTheme theme)
        {
            _floor = Mathf.Max(1, floor);
            _floorTheme = theme;

            // 极端情况下在 PrepareFloor 之后才切换，也立即补一次染色
            if (_floorPrepared) ApplyFloorThemeToRooms();
        }

        /// <summary>给当前楼层的所有房间应用主题色（建楼之后调用）。</summary>
        public void ApplyFloorThemeToRooms()
        {
            if (_floorTheme == null) return;

            if (builder == null) builder = GetComponent<DungeonBuilder>();
            if (builder == null) builder = FindFirstObjectByType<DungeonBuilder>();
            if (builder == null) return;

            IReadOnlyList<RoomView> rooms = builder.Rooms;
            for (int i = 0; i < rooms.Count; i++)
            {
                if (rooms[i] != null) rooms[i].ApplyFloorTheme(_floorTheme);
            }
        }

        private int FloorOffset => Mathf.Max(0, _floor - 1);

        /// <summary>当前楼层普通敌人的攻击力加成；召唤物也使用这个值。</summary>
        public int EnemyDamageBonus => Mathf.Max(0, enemyDamageGrowthPerFloor * FloorOffset);

        private float EnemyHealthMultiplier =>
            1f + Mathf.Max(0f, enemyHealthGrowthPerFloor) * Mathf.Max(1, _floor);

        private float EnemySpeedMultiplier =>
            Mathf.Min(
                Mathf.Max(0.01f, enemySpeedMultiplierCap),
                1f + Mathf.Max(0f, enemySpeedGrowthPerFloor) * FloorOffset);

        private float BossHealthMultiplier =>
            1f + Mathf.Max(0f, bossHealthGrowthPerFloor) * Mathf.Max(1, _floor);

        private float BossSpeedMultiplier =>
            Mathf.Min(
                Mathf.Max(0.01f, bossSpeedMultiplierCap),
                1f + Mathf.Max(0f, bossSpeedGrowthPerFloor) * FloorOffset);

        private int ExtraEnemiesForFloor =>
            extraEnemyEveryFloors <= 0
                ? 0
                : Mathf.Clamp(FloorOffset / extraEnemyEveryFloors, 0, Mathf.Max(0, maxExtraEnemies));

        private int CoinsFromFloor =>
            coinGrowthEveryFloors <= 0 ? 0 : FloorOffset / coinGrowthEveryFloors;

        private int ShopPriceBonus => Mathf.Max(0, shopPriceGrowthPerFloor) * FloorOffset;
        private int ShopRerollCost => Mathf.Max(1, shopRerollBaseCost + ShopPriceBonus);

        /// <summary>Boss 被击败时触发，供 RunManager 进入通关状态。</summary>
        public event System.Action BossDefeated;

        /// <summary>任意敌人死亡时触发一次（击杀型被动的唯一计数入口）。</summary>
        public event System.Action<Enemy> EnemyKilled;

        /// <summary>房间首次被清空时触发（清房奖励类道具的入口）。</summary>
        public event System.Action<RoomView> RoomCleared;

        /// <summary>挑战房开始新一波时触发（波数, 总波数）。</summary>
        public event System.Action<int, int> ChallengeWaveStarted;

        /// <summary>挑战房最后一波清空时触发。</summary>
        public event System.Action ChallengeCompleted;

        private void Awake()
        {
            if (tracker == null) tracker = FindFirstObjectByType<PlayerRoomTracker>();
            if (builder == null) builder = GetComponent<DungeonBuilder>();
            if (builder == null) builder = FindFirstObjectByType<DungeonBuilder>();
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
            _playerWallet = FindFirstObjectByType<PlayerWallet>();
            _hud = FindFirstObjectByType<CombatDebugHud>();
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
            if (builder == null) builder = GetComponent<DungeonBuilder>();
            if (builder == null) builder = FindFirstObjectByType<DungeonBuilder>();
            if (builder != null) builder.EnsureBuilt();

            // 所有战斗房在开局时就生成好怪物，玩家可以从门外观察
            PrepareFloor();

            // 若进入 Play Mode 时玩家已经站在某个房间里，补一次判定
            if (tracker != null && tracker.CurrentRoom != null)
                HandleRoomChanged(tracker.CurrentRoom);
        }

        /// <summary>
        /// 为当前楼层预生成所有房间的敌人。
        /// 在开局、换层和重开重建地牢之后调用。
        /// </summary>
        public void PrepareFloor()
        {
            if (_floorPrepared) return;

            ClearAllEncounters();

            if (builder == null) builder = GetComponent<DungeonBuilder>();
            if (builder == null) builder = FindFirstObjectByType<DungeonBuilder>();
            if (builder == null || builder.Rooms == null) return;

            _floorPrepared = true;
            ApplyFloorThemeToRooms();

            IReadOnlyList<RoomView> rooms = builder.Rooms;
            for (int i = 0; i < rooms.Count; i++)
            {
                RoomView room = rooms[i];
                if (room == null || room.IsCleared) continue;

                if (room.RoomType == RoomType.Combat ||
                    room.RoomType == RoomType.Boss ||
                    room.RoomType == RoomType.Challenge)
                {
                    SpawnRoomEncounter(room, false, false);

                    if (room.RoomType == RoomType.Challenge)
                        _challengeWaves[room] = 1;
                }
            }

            Debug.Log($"[Flow] 已为 {rooms.Count} 个房间预生成敌人");
        }

        private void Update()
        {
            if (enableDebugClearKey && WasClearKeyPressed())
                ClearCurrentRoom();

            if (WasShopRerollPressed())
                TryRerollCurrentShop();
        }

        /// <summary>玩家进入房间：战斗房且未清空则锁门并激活预生成的敌人，否则保持开启。</summary>
        public void HandleRoomChanged(RoomView room)
        {
            if (room == null) return;

            // 换房时不销毁其它房间预生成的敌人，它们会继续留在自己的房间里游荡
            _activeRoom = room;

            bool isCombatRoom = room.RoomType == RoomType.Combat ||
                room.RoomType == RoomType.Boss ||
                room.RoomType == RoomType.Challenge;

            if (isCombatRoom && !room.IsCleared)
            {
                room.SetDoorsLocked(true, false);

                if (room.RoomType == RoomType.Challenge)
                {
                    if (!_challengeWaves.ContainsKey(room))
                        _challengeWaves[room] = 1;

                    // 兜底：预生成失败时再补一次
                    if (!HasLivingEnemies(room))
                        SpawnRoomEncounter(room, true, true);
                    else
                        ActivateRoomEnemies(room);

                    ChallengeWaveStarted?.Invoke(_challengeWaves[room], challengeWaveCount);
                }
                else
                {
                    // 兜底：预生成失败时再补一次
                    if (!HasLivingEnemies(room))
                        SpawnRoomEncounter(room, true, true);
                    else
                        ActivateRoomEnemies(room);
                }

                Debug.Log($"[Flow] 房间 {room.GridPosition} 上锁（{room.RoomType}）");
            }
            else
            {
                room.SetDoorsLocked(false, true);
            }

            if (room.RoomType == RoomType.Treasure)
                TrySpawnTreasure(room);

            if (room.RoomType == RoomType.Shop)
            {
                TryStockShop(room);
                if (room.ShopStocked && _hud != null)
                    _hud.ShowToast($"商店：按 R 刷新商品（{ShopRerollCost} 金币）", 2.2f);
            }

            if (room.RoomType == RoomType.Secret)
                TrySpawnSecretEvent(room);
        }

        /// <summary>商店首次进入时上架 3 件随机道具，价格按效果数量计算。</summary>
        private void TryStockShop(RoomView room)
        {
            if (room.ShopStocked) return;
            if (itemPickupPrefab == null || itemPool == null || itemPool.Length == 0) return;

            // 只上架玩家尚未持有的道具，避免重复购买同一件
            List<ItemDefinition> stock = BuildAvailableItems();
            if (stock.Count == 0)
            {
                Debug.Log($"[Flow] 商店 {room.GridPosition} 无可售道具（玩家已持有全部道具）");
                return;
            }

            room.ShopStocked = true;

            int count = Mathf.Min(3, stock.Count);
            Vector2 center = room.WorldCenter;

            // 小房间自动收窄商品间距，保证三件商品都在内部
            float spacing = Mathf.Clamp((room.InteriorRect.width - 2f) * 0.5f, 1.5f, 3f);
            float startOffset = -(count - 1) * 0.5f * spacing;

            for (int i = 0; i < count; i++)
            {
                ItemDefinition item = PickWeightedItem(stock);
                if (item == null) break;
                stock.Remove(item);

                float x = center.x + startOffset + i * spacing;
                ItemPickup pickup = Instantiate(
                    itemPickupPrefab,
                    new Vector3(x, center.y, 0f),
                    Quaternion.identity,
                    room.transform);

                pickup.Setup(item, null, CalculateShopPrice(item));
            }

            Debug.Log($"[Flow] 商店 {room.GridPosition} 上架 {count} 件道具");
        }

        /// <summary>
        /// 按 R 花费金币刷新当前商店货架。
        /// 只替换仍在货架上的栏位；已购买的空栏位保持为空。
        /// </summary>
        public bool TryRerollCurrentShop()
        {
            // 商店购买弹窗会暂停时间；允许在弹窗中刷新，其它暂停状态不允许。
            if (Time.timeScale <= 0.001f && !ItemPickup.HasActiveDialog) return false;
            if (tracker == null) return false;

            RoomView room = tracker.CurrentRoom;
            if (room == null || room.RoomType != RoomType.Shop || !room.ShopStocked)
                return false;

            if (itemPickupPrefab == null) return false;

            ItemPickup[] allPickups = room.GetComponentsInChildren<ItemPickup>(true);
            var activeStock = new List<ItemPickup>();
            var oldItems = new List<ItemDefinition>();

            for (int i = 0; i < allPickups.Length; i++)
            {
                ItemPickup pickup = allPickups[i];
                if (pickup == null || !pickup.gameObject.activeInHierarchy ||
                    pickup.IsPurchased || pickup.Item == null)
                    continue;

                activeStock.Add(pickup);
                oldItems.Add(pickup.Item);
            }

            if (activeStock.Count == 0)
            {
                if (_hud != null) _hud.ShowToast("商店已没有可刷新商品", 1.6f);
                return false;
            }

            List<ItemDefinition> available = BuildAvailableItems();
            for (int i = 0; i < oldItems.Count; i++)
                available.Remove(oldItems[i]);

            if (available.Count == 0)
            {
                if (_hud != null) _hud.ShowToast("商店已没有新的可刷新道具", 1.6f);
                return false;
            }

            if (_playerWallet == null) _playerWallet = FindFirstObjectByType<PlayerWallet>();
            if (_playerWallet == null) return false;

            int cost = ShopRerollCost;
            if (!_playerWallet.TrySpend(cost))
            {
                if (_hud != null) _hud.ShowToast($"金币不足，刷新需要 {cost} 金币", 1.8f);
                return false;
            }

            ItemPickup.SuppressNewDialogs(0.6f);

            for (int i = 0; i < activeStock.Count; i++)
            {
                ItemPickup oldPickup = activeStock[i];
                if (oldPickup == null) continue;

                Vector3 position = oldPickup.BasePosition;
                oldPickup.gameObject.SetActive(false);
                Destroy(oldPickup.gameObject);

                ItemDefinition newItem = PickWeightedItem(available);
                if (newItem == null) continue;

                available.Remove(newItem);
                ItemPickup newPickup = Instantiate(
                    itemPickupPrefab,
                    position,
                    Quaternion.identity,
                    room.transform);

                newPickup.Setup(newItem, null, CalculateShopPrice(newItem));
            }

            if (_hud != null) _hud.ShowToast($"商店已刷新：-{cost} 金币", 1.8f);
            return true;
        }

        /// <summary>先按稀有度类别抽权重，再在该类别内等概率选具体道具。</summary>
        private ItemDefinition PickWeightedItem(List<ItemDefinition> items)
        {
            if (items == null || items.Count == 0) return null;

            int commonCount = 0;
            int rareCount = 0;
            int legendaryCount = 0;

            for (int i = 0; i < items.Count; i++)
            {
                ItemDefinition item = items[i];
                if (item == null) continue;

                switch (item.rarity)
                {
                    case ItemRarity.Common: commonCount++; break;
                    case ItemRarity.Rare: rareCount++; break;
                    case ItemRarity.Legendary: legendaryCount++; break;
                }
            }

            float commonMass = commonCount > 0 ? Mathf.Max(0f, commonWeight) : 0f;
            float rareMass = rareCount > 0 ? Mathf.Max(0f, rareWeight) : 0f;
            float legendaryMass = legendaryCount > 0 ? Mathf.Max(0f, legendaryWeight) : 0f;
            float totalWeight = commonMass + rareMass + legendaryMass;

            if (totalWeight <= 0.0001f)
                return items[Random.Range(0, items.Count)];

            float roll = Random.value * totalWeight;
            ItemRarity chosen;
            int chosenCount;

            if (roll < commonMass)
            {
                chosen = ItemRarity.Common;
                chosenCount = commonCount;
            }
            else if (roll < commonMass + rareMass)
            {
                chosen = ItemRarity.Rare;
                chosenCount = rareCount;
            }
            else
            {
                chosen = ItemRarity.Legendary;
                chosenCount = legendaryCount;
            }

            if (chosenCount <= 0)
                return items[Random.Range(0, items.Count)];

            int pick = Random.Range(0, chosenCount);
            for (int i = 0; i < items.Count; i++)
            {
                ItemDefinition item = items[i];
                if (item == null || item.rarity != chosen) continue;
                if (pick == 0) return item;
                pick--;
            }

            return items[0];
        }

        private int CalculateShopPrice(ItemDefinition item)
        {
            int basePrice = item.shopPrice > 0
                ? item.shopPrice
                : 5 + (item.modifiers != null ? item.modifiers.Count * 2 : 0);

            // 折扣券不在上架时结算：ItemPickup 会按当前背包实时计算半价，
            // 这样在商店里买到折扣券后，货架上其它商品也会立刻降价。
            return Mathf.Max(1, basePrice + ShopPriceBonus);
        }

        /// <summary>宝箱房首次进入时生成一个随机道具，领取后不再刷新。</summary>
        private void TrySpawnTreasure(RoomView room)
        {
            if (room.RewardClaimed || room.RewardSpawned) return;
            if (itemPickupPrefab == null || itemPool == null || itemPool.Length == 0) return;

            // 宝箱房只给尚未持有的道具
            List<ItemDefinition> available = BuildAvailableItems();
            if (available.Count == 0)
            {
                room.RewardSpawned = true;
                Debug.Log($"[Flow] 宝箱房 {room.GridPosition} 无可生成道具（玩家已持有全部道具）");
                return;
            }

            ItemDefinition item = PickWeightedItem(available);
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

            ItemDefinition item = PickWeightedItem(available);
            if (item == null) return;
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

        /// <summary>敌人死亡回调：先掉落奖励，再判断它所在的房间是否清空。</summary>
        public void NotifyEnemyDied(Enemy enemy)
        {
            if (enemy == null) return;

            EnemyKilled?.Invoke(enemy);

            RoomView room = FindEnemyRoom(enemy);
            SpawnDrops(enemy, room);

            if (room != null && _roomEnemies.TryGetValue(room, out List<Enemy> list))
                list.Remove(enemy);

            if (room == null || room.IsCleared || HasLivingEnemies(room)) return;

            // 挑战房：一波清空后继续下一波，最后一波才进入正常清房流程
            if (room.RoomType == RoomType.Challenge && TryAdvanceChallenge(room))
                return;

            if (room == _activeRoom)
            {
                ClearCurrentRoom();
            }
            else
            {
                // 玩家从门外把整间房的怪清掉了：标记为已清空，进入时不再锁门
                room.IsCleared = true;
                room.SetDoorsLocked(false, false);
                Debug.Log($"[Flow] 房间 {room.GridPosition} 在进入前已被清空");
                RoomCleared?.Invoke(room);

                if (room.RoomType == RoomType.Boss)
                    BossDefeated?.Invoke();
            }
        }

        /// <summary>
        /// 挑战房清空一波后推进到下一波；最后一波返回 false，让正常清房流程开门。
        /// </summary>
        private bool TryAdvanceChallenge(RoomView room)
        {
            if (!_challengeWaves.TryGetValue(room, out int wave)) return false;

            if (wave >= challengeWaveCount)
            {
                _challengeWaves.Remove(room);
                TrySpawnChallengeReward(room);
                ChallengeCompleted?.Invoke();
                return false;
            }

            int nextWave = wave + 1;
            _challengeWaves[room] = nextWave;
            SpawnRoomEncounter(room, true, true);

            if (!HasLivingEnemies(room))
            {
                _challengeWaves.Remove(room);
                Debug.LogWarning($"[Flow] 挑战房 {room.GridPosition} 第 {nextWave} 波生成失败");
                return false;
            }

            ChallengeWaveStarted?.Invoke(nextWave, challengeWaveCount);
            return true;
        }

        /// <summary>挑战房通关奖励：一件未持有的道具，免费领取。</summary>
        private void TrySpawnChallengeReward(RoomView room)
        {
            if (room == null || room.RewardClaimed || room.RewardSpawned) return;
            if (itemPickupPrefab == null || itemPool == null || itemPool.Length == 0) return;

            List<ItemDefinition> available = BuildAvailableItems();
            if (available.Count == 0)
            {
                room.RewardSpawned = true;
                Debug.Log($"[Flow] 挑战房 {room.GridPosition} 无可生成奖励（玩家已持有全部道具）");
                return;
            }

            ItemDefinition item = PickWeightedItem(available);
            if (item == null) return;

            room.RewardSpawned = true;
            Vector2 center = room.WorldCenter;
            ItemPickup pickup = Instantiate(
                itemPickupPrefab,
                new Vector3(center.x, center.y, 0f),
                Quaternion.identity,
                room.transform);

            pickup.Setup(item, room);
            Debug.Log($"[Flow] 挑战房 {room.GridPosition} 通关，奖励：{item.itemName}");
        }

        /// <summary>按敌人类型掉落金币 / 回血，掉落物挂在敌人所属的房间里。</summary>
        private void SpawnDrops(Enemy enemy, RoomView room)
        {
            Vector3 origin = enemy.transform.position;
            // 精英怪固定掉落 1 金币，不受普通金币概率/数量加成影响
            if (enemy.IsElite)
            {
                SpawnPickup(coinPrefab, ScatterAround(origin, 0.7f, room), room);
            }
            else
            {
                int bonus = _playerStats != null ? _playerStats.CoinBonus : 0;
                int coins = (enemy.IsBoss ? bossCoinDrop : normalCoinDrop) + bonus + CoinsFromFloor;

                bool dropCoins = enemy.IsBoss || Random.value < coinDropChance;
                if (dropCoins)
                {
                    for (int i = 0; i < coins; i++)
                        SpawnPickup(coinPrefab, ScatterAround(origin, 0.7f, room), room);
                }
            }

            float heartChance = heartDropChance;
            if (_playerInventory != null)
            {
                ItemDefinition luck = _playerInventory.FindSynergyItem(ItemSynergy.LuckyDrops);
                if (luck != null) heartChance += Mathf.Max(0f, luck.effectPower);
            }

            if (enemy.IsBoss || Random.value < heartChance)
                SpawnPickup(heartPrefab, ScatterAround(origin, 0.35f, room), room);
        }

        private static Vector3 ScatterAround(Vector3 origin, float radius, RoomView room)
        {
            Vector2 point = (Vector2)origin + Random.insideUnitCircle * radius;

            // 避免掉进墙体
            if (room != null)
            {
                Rect rect = room.WorldRect;
                point.x = Mathf.Clamp(point.x, rect.xMin + 1.5f, rect.xMax - 1.5f);
                point.y = Mathf.Clamp(point.y, rect.yMin + 1.5f, rect.yMax - 1.5f);
            }

            return new Vector3(point.x, point.y, origin.z);
        }

        private void SpawnPickup(Pickup prefab, Vector3 position, RoomView room)
        {
            if (prefab == null) return;

            Transform parent = room != null ? room.transform : transform;
            Instantiate(prefab, position, Quaternion.identity, parent);
        }

        /// <summary>标记当前房间已清空并开门。</summary>
        public void ClearCurrentRoom()
        {
            RoomView room = _activeRoom != null ? _activeRoom : (tracker != null ? tracker.CurrentRoom : null);
            if (room == null) return;

            bool wasCleared = room.IsCleared;
            room.IsCleared = true;
            room.SetDoorsLocked(false, false);

            if (!wasCleared)
            {
                Debug.Log($"[Flow] 房间 {room.GridPosition} 已清空，开门");
                RoomCleared?.Invoke(room);
            }

            if (room.RoomType == RoomType.Boss)
                BossDefeated?.Invoke();
        }

        /// <summary>重开一局前清理：敌人、子弹、掉落、房间引用。</summary>
        public void ResetForNewRun()
        {
            ClearAllEncounters();
            _floorPrepared = false;

            ProjectilePool.ResetAll();

            foreach (Pickup pickup in FindObjectsByType<Pickup>(FindObjectsSortMode.None))
                Destroy(pickup.gameObject);

            foreach (ItemPickup item in FindObjectsByType<ItemPickup>(FindObjectsSortMode.None))
                Destroy(item.gameObject);

            foreach (ExplosionFlash flash in FindObjectsByType<ExplosionFlash>(FindObjectsSortMode.None))
                Destroy(flash.gameObject);
        }

        /// <summary>
        /// 在房间里生成敌人。
        /// 预生成时不开出生动画、保持待机；作为进入时的兜底生成则直接激活。
        /// </summary>
        private void SpawnRoomEncounter(RoomView room, bool playSpawnAnimation, bool activate)
        {
            if (room == null) return;

            bool isBoss = room.RoomType == RoomType.Boss;
            Enemy requiredPrefab = isBoss ? bossPrefab : enemyPrefab;
            if (requiredPrefab == null)
            {
                Debug.LogError("[Combat] 找不到敌人 Prefab，先执行 Tools/Roguelike/生成战斗 Prefab。");
                return;
            }

            if (!_roomEnemies.TryGetValue(room, out List<Enemy> list))
            {
                list = new List<Enemy>();
                _roomEnemies[room] = list;
            }

            if (HasLivingEnemies(room)) return;

            int count = isBoss ? bossEnemyCount : EnemyCountFor(room);
            IReadOnlyList<Transform> points = room.EnemySpawnPoints;

            int capacity = isBoss ? points.Count : points.Count + ExtraEnemiesForFloor;
            count = Mathf.Min(count, Mathf.Max(0, capacity));

            int compositionDistance = room.DistanceFromStart +
                FloorOffset * Mathf.Max(0, compositionDistancePerFloor);

            for (int i = 0; i < count; i++)
            {
                Vector3 spawnPosition;
                if (i < points.Count)
                {
                    spawnPosition = points[i].position;
                }
                else
                {
                    // 超出出生点数量的额外敌人：在原出生点附近小幅偏移，不放到中央/门洞
                    Vector2 offset = Random.insideUnitCircle * 0.3f;
                    spawnPosition = points[i % points.Count].position + (Vector3)offset;
                }

                Enemy prefab = isBoss ? bossPrefab : PickPrefab(compositionDistance, i);
                if (prefab == null) prefab = enemyPrefab;

                Enemy enemy = Instantiate(prefab, spawnPosition, Quaternion.identity, room.transform);
                bool elite = !isBoss && Random.value < eliteChance;
                enemy.name = isBoss
                    ? "Boss"
                    : (elite ? $"Elite_{prefab.GetType().Name}_{i}" : $"{prefab.GetType().Name}_{i}");

                float health = isBoss
                    ? bossEnemyHealth * BossHealthMultiplier
                    : HealthFor(prefab) * EnemyHealthMultiplier;

                if (elite) health *= Mathf.Max(1f, eliteHealthMultiplier);

                float speed = isBoss
                    ? 1.7f * BossSpeedMultiplier
                    : SpeedFor(prefab) * EnemySpeedMultiplier;

                enemy.Initialize(
                    health,
                    speed,
                    1f,
                    Color.white,
                    isBoss,
                    this,
                    playSpawnAnimation);

                if (!isBoss) enemy.SetFloorDamageBonus(EnemyDamageBonus);
                if (elite) enemy.MakeElite(eliteDamageMultiplier);

                if (isBoss && enemy is BossEnemy boss)
                {
                    boss.ApplyFloorScaling(_floor);
                    boss.SetVisual(_floorTheme != null ? _floorTheme.bossVisual : FloorBossVisual.BigDemon);
                }

                RegisterEnemy(enemy, room);

                if (activate) enemy.ActivateCombat();
            }

            Debug.Log($"[Combat] 房间 {room.GridPosition} 生成 {count} 个敌人（第 {_floor} 层，编成距离 {compositionDistance}，预生成={!activate}）");
        }

        private bool HasLivingEnemies(RoomView room)
        {
            if (room == null || !_roomEnemies.TryGetValue(room, out List<Enemy> list)) return false;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && !list[i].IsDead) return true;
            }

            return false;
        }

        private void ActivateRoomEnemies(RoomView room)
        {
            if (room == null || !_roomEnemies.TryGetValue(room, out List<Enemy> list)) return;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && !list[i].IsDead)
                    list[i].ActivateCombat();
            }
        }

        private RoomView FindEnemyRoom(Enemy enemy)
        {
            if (enemy == null) return null;

            RoomView room = enemy.GetComponentInParent<RoomView>();
            if (room != null) return room;

            foreach (KeyValuePair<RoomView, List<Enemy>> pair in _roomEnemies)
            {
                if (pair.Value.Contains(enemy)) return pair.Key;
            }

            return null;
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

            return baseCount + sizeBonus + depthBonus + ExtraEnemiesForFloor;
        }

        /// <summary>
        /// 按房间距离混编敌人：
        ///   近处只有追猎者；中段加入射击者、冲锋者与自爆史莱姆；
        ///   深处固定出现亡灵法师与分裂僵尸，保证新怪物能实际登场。
        /// </summary>
        private Enemy PickPrefab(int distance, int index)
        {
            Enemy themed = PickThemePrefab(distance, index);
            if (themed != null) return themed;

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

        /// <summary>按当前楼层主题的小怪池轮换取怪；池为空时返回 null，走旧的六种编成。</summary>
        private Enemy PickThemePrefab(int distance, int index)
        {
            Enemy[] roster = _floorTheme != null ? _floorTheme.commonEnemies : null;
            if (roster == null || roster.Length == 0) return null;

            int start = Mathf.Clamp(distance - 2, 0, roster.Length - 1);

            for (int i = 0; i < roster.Length; i++)
            {
                int pick = (start + index + i) % roster.Length;
                if (roster[pick] != null) return roster[pick];
            }

            return null;
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
            float multiplier = prefab != null ? Mathf.Max(0.1f, prefab.HealthMultiplier) : 1f;
            float baseHealth = normalEnemyHealth * multiplier;

            if (prefab is SplitterEnemy) return baseHealth * 2f;
            if (prefab is NecromancerEnemy) return baseHealth * 1.5f;
            return baseHealth;
        }

        private static float SpeedFor(Enemy prefab)
        {
            float multiplier = prefab != null ? Mathf.Max(0.1f, prefab.SpeedMultiplier) : 1f;
            float baseSpeed;

            if (prefab is ShooterEnemy) baseSpeed = 2.0f;
            else if (prefab is ChargerEnemy) baseSpeed = 2.6f;
            else if (prefab is BomberEnemy) baseSpeed = 2.2f;
            else if (prefab is NecromancerEnemy) baseSpeed = 1.9f;
            else if (prefab is SplitterEnemy) baseSpeed = 1.6f;
            else baseSpeed = 2.4f;

            return baseSpeed * multiplier;
        }

        /// <summary>登记新生成的敌人（包括召唤物与分裂体），保证全部清掉才开门。</summary>
        public void RegisterEnemy(Enemy enemy)
        {
            if (enemy == null) return;

            RoomView room = enemy.GetComponentInParent<RoomView>();
            if (room == null) room = _activeRoom;
            RegisterEnemy(enemy, room);
        }

        private void RegisterEnemy(Enemy enemy, RoomView room)
        {
            if (enemy == null) return;
            if (room == null) room = _activeRoom;
            if (room == null) return;

            // 召唤物可能生成在房间根节点之外；登记时统一补上所属房间，
            // 让它参与清房计数，也能被 Enemy.LateUpdate 的边界钳制拉回房间。
            enemy.SetRoom(room);

            if (!_roomEnemies.TryGetValue(room, out List<Enemy> list))
            {
                list = new List<Enemy>();
                _roomEnemies[room] = list;
            }

            if (!list.Contains(enemy)) list.Add(enemy);
        }

        /// <summary>销毁所有房间里已登记的敌人（换层、重开、重新预生成前调用）。</summary>
        private void ClearAllEncounters()
        {
            foreach (KeyValuePair<RoomView, List<Enemy>> pair in _roomEnemies)
            {
                List<Enemy> list = pair.Value;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] != null) Destroy(list[i].gameObject);
                }
            }

            _roomEnemies.Clear();
            _challengeWaves.Clear();
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

        private static bool WasShopRerollPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null) return keyboard.rKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.R);
#else
            return false;
#endif
        }
    }
}
