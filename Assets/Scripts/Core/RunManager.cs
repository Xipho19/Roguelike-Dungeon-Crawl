using System.Collections.Generic;
using Roguelike.Audio;
using Roguelike.Dungeon;
using Roguelike.Items;
using Roguelike.Player;
using Roguelike.Progression;
using Roguelike.UI;
using Roguelike.Weapons;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Roguelike.Core
{
    /// <summary>
    /// 一局游戏的生命周期：进行中 / 失败 / 通关，以及"原地重开"。
    ///
    /// 重开采用原地重置而不是重载场景：
    ///   1. 清理敌人、子弹、掉落
    ///   2. 清空背包与金币，属性回到基础值
    ///   3. 用新种子重建地牢
    ///   4. 把玩家放回新的起点房，相机吸附
    /// 这样即使场景尚未保存也不会丢失玩家与地牢配置。
    /// </summary>
    public sealed class RunManager : MonoBehaviour
    {
        public enum RunState
        {
            MainMenu,
            Playing,
            GameOver,
            Victory,
        }

        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private DungeonFlowController flow;
        [SerializeField] private DungeonBuilder builder;
        [SerializeField] private PlayerRoomTracker tracker;
        [SerializeField] private RoomCamera roomCamera;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private WeaponInventory weaponInventory;

        [Header("楼层推进")]
        [SerializeField] private NextFloorPortal portalPrefab;

        [Tooltip("当前楼层，从 1 开始")]
        [SerializeField] private int currentFloor = 1;

        [Header("楼层上限")]
        [Tooltip("击败该层 Boss 即通关；小于等于 0 表示无尽模式")]
        [SerializeField] private int maxFloor = 6;

        [Header("楼层主题")]
        [Tooltip("场景中显式指定的主题；留空或缺少时自动补入 Resources/FloorThemes 中的主题")]
        [SerializeField] private FloorTheme[] floorThemes = new FloorTheme[0];

        [Header("跨局继承")]
        [Tooltip("旧版通关继承开关；现在跨楼层会保留道具，死亡时始终清空")]
        [SerializeField] private bool keepItemsAfterVictory = true;

        private RunState _state = RunState.MainMenu;
        private NextFloorPortal _portal;
        private FloorTheme _currentTheme;
        private bool _runCoinsBanked;
        private int _lastBankedCoins;
        private int _lastBankedTotal;
        private bool _suspendedRun;
        private bool _carryVictoryRewards;

        public RunState State => _state;
        public int CurrentFloor => currentFloor;
        public int MaxFloor => maxFloor;
        public bool IsEndlessRun => maxFloor <= 0;
        public FloorTheme CurrentFloorTheme => _currentTheme;

        /// <summary>存在可继续的进度：本局仍挂起，或磁盘上有单局存档。</summary>
        public bool HasResumableRun =>
            _state == RunState.MainMenu && (_suspendedRun || RunSaveService.HasSave);

        /// <summary>原地重开完成后触发（换层不触发），用于重置主动冷却与击杀计数。</summary>
        public event System.Action RunRestarted;

        public string CurrentThemeName =>
            _currentTheme != null && !string.IsNullOrEmpty(_currentTheme.displayName)
                ? _currentTheme.displayName
                : "地牢";

        private void Awake()
        {
            Time.timeScale = _state == RunState.MainMenu ? 0f : 1f;

            if (playerHealth == null) playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (flow == null) flow = FindFirstObjectByType<DungeonFlowController>();
            if (builder == null) builder = FindFirstObjectByType<DungeonBuilder>();
            if (tracker == null) tracker = FindFirstObjectByType<PlayerRoomTracker>();
            if (roomCamera == null) roomCamera = FindFirstObjectByType<RoomCamera>();
            if (inventory == null) inventory = FindFirstObjectByType<PlayerInventory>();
            if (wallet == null) wallet = FindFirstObjectByType<PlayerWallet>();
            if (playerStats == null) playerStats = FindFirstObjectByType<PlayerStats>();
            if (weaponInventory == null) weaponInventory = FindFirstObjectByType<WeaponInventory>();
            if (portalPrefab == null) portalPrefab = Resources.Load<NextFloorPortal>("Combat/NextFloorPortal");

            if (playerHealth != null) playerHealth.Died += HandlePlayerDied;
            if (flow != null) flow.BossDefeated += HandleBossDefeated;

            if (currentFloor < 1) currentFloor = 1;
            LoadFloorThemes();
            ApplyFloorPresentation();
        }

        private void LoadFloorThemes()
        {
            FloorTheme[] resources = Resources.LoadAll<FloorTheme>("FloorThemes");
            FloorTheme[] themes;

            if (floorThemes == null || floorThemes.Length == 0)
            {
                themes = resources;
            }
            else
            {
                // 场景里可能只绑定了旧的三套主题；把 Resources 中新增的主题补进来，
                // 这样新增第 4-6 层主题时不需要手动改场景引用。
                var merged = new List<FloorTheme>(floorThemes);
                for (int i = 0; i < resources.Length; i++)
                {
                    if (resources[i] != null && !merged.Contains(resources[i]))
                        merged.Add(resources[i]);
                }
                themes = merged.ToArray();
            }

            themes = System.Array.FindAll(themes, theme => theme != null);
            System.Array.Sort(themes, (a, b) =>
            {
                int byOrder = a.order.CompareTo(b.order);
                return byOrder != 0 ? byOrder : string.CompareOrdinal(a.name, b.name);
            });

            floorThemes = themes;

            if (floorThemes.Length == 0)
            {
                Debug.LogWarning(
                    "[Run] 未找到楼层主题资产，使用默认地牢外观。" +
                    "请执行 Tools/Roguelike/生成楼层主题。");
            }
        }

        private FloorTheme ThemeForFloor(int floor)
        {
            if (floorThemes == null || floorThemes.Length == 0) return null;

            int index = ((Mathf.Max(1, floor) - 1) % floorThemes.Length + floorThemes.Length) % floorThemes.Length;
            return floorThemes[index];
        }

        private void ApplyFloorPresentation()
        {
            _currentTheme = ThemeForFloor(currentFloor);

            if (flow != null) flow.SetFloor(currentFloor, _currentTheme);
            if (roomCamera != null && _currentTheme != null)
                roomCamera.SetBackgroundColor(_currentTheme.cameraBackground);

            if (_currentTheme != null)
                AudioService.PlayMusic(_currentTheme.bgm, _currentTheme.bgmVolume);
            else
                AudioService.StopMusic();
        }

        private void EnterVictory(string reason)
        {
            if (_state == RunState.Victory || _state == RunState.GameOver) return;

            if (_portal != null)
            {
                Destroy(_portal.gameObject);
                _portal = null;
            }

            _state = RunState.Victory;
            Time.timeScale = 0f;
            _suspendedRun = false;
            _carryVictoryRewards = true;
            RunSaveService.Delete();
            BankRunCoins(true);
            AudioService.StopMusic();
            AudioService.Play("bookOpen", 0.9f);
            Debug.Log($"[Run] 通关：{reason}");
        }

        private void OnDestroy()
        {
            if (playerHealth != null) playerHealth.Died -= HandlePlayerDied;
            if (flow != null) flow.BossDefeated -= HandleBossDefeated;
        }

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            HandleDebugFloorKeys();
#endif

            if (_state == RunState.Playing || _state == RunState.MainMenu) return;
        }

        private void HandlePlayerDied()
        {
            if (_state != RunState.Playing) return;

            _state = RunState.GameOver;
            Time.timeScale = 0f;
            _suspendedRun = false;
            RunSaveService.Delete();
            BankRunCoins(false);
            AudioService.StopMusic();
            AudioService.Play("Get_Out_Loser", 1f, 0f);
            Debug.Log("[Run] 玩家死亡，本局结束");
        }

        /// <summary>每局结束时把剩余金币存入局外金库；重复调用只结算一次。</summary>
        private void BankRunCoins(bool victory)
        {
            if (_runCoinsBanked) return;

            _runCoinsBanked = true;
            int remaining = wallet != null ? wallet.Coins : 0;
            RunBankResult result = MetaProgression.BankRunCoins(remaining, currentFloor, victory);
            _lastBankedCoins = result.BankedCoins;
            _lastBankedTotal = result.TotalCoins;
        }

        /// <summary>把已购买的技能树节点转成永久属性修正，并在每局开始时应用。</summary>
        private void ApplyMetaUpgrades()
        {
            var modifiers = new List<StatModifier>();
            int shieldBonus = 0;

            MetaUpgradeDefinition[] nodes = MetaUpgrades.Nodes;
            for (int i = 0; i < nodes.Length; i++)
            {
                MetaUpgradeDefinition definition = nodes[i];
                int level = MetaProgression.GetUpgradeLevel(definition.id);
                if (level <= 0) continue;

                switch (definition.effect)
                {
                    case MetaUpgradeEffect.MaxHealth:
                        modifiers.Add(new StatModifier(
                            StatType.MaxHealth,
                            StatOp.Add,
                            definition.value * level));
                        break;

                    case MetaUpgradeEffect.MaxShield:
                        shieldBonus += Mathf.RoundToInt(definition.value * level);
                        break;

                    case MetaUpgradeEffect.MoveSpeed:
                        modifiers.Add(new StatModifier(
                            StatType.MoveSpeed,
                            StatOp.Multiply,
                            Mathf.Pow(definition.value, level)));
                        break;

                    case MetaUpgradeEffect.Damage:
                        modifiers.Add(new StatModifier(
                            StatType.Damage,
                            StatOp.Multiply,
                            Mathf.Pow(definition.value, level)));
                        break;

                    case MetaUpgradeEffect.CoinBonus:
                        modifiers.Add(new StatModifier(
                            StatType.CoinBonus,
                            StatOp.Add,
                            definition.value * level));
                        break;

                    case MetaUpgradeEffect.FireInterval:
                        modifiers.Add(new StatModifier(
                            StatType.FireInterval,
                            StatOp.Multiply,
                            Mathf.Pow(definition.value, level)));
                        break;
                }
            }

            if (playerStats != null) playerStats.SetMetaModifiers(modifiers);
            if (playerHealth != null) playerHealth.SetMetaShieldBonus(shieldBonus);
        }

        private void HandleBossDefeated()
        {
            if (_state != RunState.Playing) return;

            if (maxFloor > 0 && currentFloor >= maxFloor)
            {
                EnterVictory($"击败第 {currentFloor} 层 Boss");
                return;
            }

            if (portalPrefab == null)
            {
                // 没有传送门 Prefab 时回退到旧的通关结算，避免 Boss 死后无事发生
                Debug.LogWarning("[Run] 缺少 NextFloorPortal Prefab，回退为通关结算");
                EnterVictory("Boss 已被击败");
                return;
            }

            SpawnNextFloorPortal();
            AudioService.Play("bookOpen", 0.9f);
            Debug.Log("[Run] Boss 被击败，传送门已开启");
        }

        private void SpawnNextFloorPortal()
        {
            if (_portal != null) return;

            RoomView bossRoom = FindBossRoom();
            if (bossRoom == null)
            {
                Debug.LogWarning("[Run] 找不到 Boss 房，无法生成传送门");
                return;
            }

            // 放在 Boss 房中央偏下，避开中央柱和下方墙角；按房间底部定位以适配尺寸
            Vector3 position = new Vector3(
                bossRoom.WorldCenter.x,
                bossRoom.WorldRect.yMin + 2.2f,
                0f);

            _portal = Instantiate(portalPrefab, position, Quaternion.identity, bossRoom.transform);
            _portal.Initialize(this);
        }

        private RoomView FindBossRoom()
        {
            if (builder == null || builder.Data == null) return null;

            foreach (RoomNode node in builder.Data.Rooms)
            {
                if (node.Type != RoomType.Boss) continue;

                RoomView room = builder.GetRoom(node.Grid);
                if (room != null) return room;
            }

            return null;
        }

        /// <summary>保留旧入口：从主菜单开始一局全新的游戏。</summary>
        public bool StartRun() => StartNewRun();

        /// <summary>从主菜单开始新的一局：清空存档、道具、金币与武器，并重建地牢。</summary>
        public bool StartNewRun()
        {
            if (_state != RunState.MainMenu) return false;

            RunSaveService.Delete();
            _suspendedRun = false;

            bool carryVictory = _carryVictoryRewards;
            _carryVictoryRewards = false;

            if (!carryVictory && weaponInventory != null)
                weaponInventory.ResetToStarting();

            RestartRun(carryVictory);
            Debug.Log(carryVictory ? "[Run] 开始新的一局（继承通关道具与武器）" : "[Run] 开始新的一局");
            return true;
        }

        /// <summary>继续上次挂起或保存的进度。</summary>
        public bool ResumeRun()
        {
            if (_state != RunState.MainMenu) return false;

            if (_suspendedRun)
            {
                _suspendedRun = false;
                ApplyMetaUpgrades();
                _state = RunState.Playing;
                Time.timeScale = 1f;

                if (_currentTheme != null)
                    AudioService.PlayMusic(_currentTheme.bgm, _currentTheme.bgmVolume);

                Debug.Log("[Run] 继续当前局");
                return true;
            }

            if (!RunSaveService.TryLoad(out RunSaveData save))
            {
                Debug.LogWarning("[Run] 没有可继续的单局存档");
                return false;
            }

            return RestoreRun(save);
        }

        /// <summary>保存本局进度、冻结游戏并返回主菜单。</summary>
        public bool SuspendRunToMainMenu()
        {
            if (_state != RunState.Playing) return false;

            RunSaveData snapshot = CreateRunSnapshot();
            if (snapshot == null) return false;

            if (!RunSaveService.TrySave(snapshot))
                return false;

            _suspendedRun = true;
            _state = RunState.MainMenu;
            Time.timeScale = 0f;
            AudioService.SaveAudioSettings();
            AudioService.StopMusic();

            MainMenuUI menu = FindFirstObjectByType<MainMenuUI>(FindObjectsInactive.Include);
            if (menu != null) menu.ShowMenu();

            Debug.Log("[Run] 已保存本局进度并返回主菜单");
            return true;
        }

        private RunSaveData CreateRunSnapshot()
        {
            if (builder == null || builder.Data == null)
            {
                Debug.LogError("[Save] 地牢尚未构建，无法保存单局进度");
                return null;
            }

            var data = new RunSaveData
            {
                floor = currentFloor,
                seed = builder.Seed,
                coins = wallet != null ? wallet.Coins : 0,
                health = playerHealth != null ? playerHealth.Health : 1,
                shield = playerHealth != null ? playerHealth.Shield : 0,
                bossCleared = false,
                startWeaponTaken = true,
                currentWeaponSlot = weaponInventory != null ? weaponInventory.CurrentSlot : 0,
            };

            RoomView currentRoom = tracker != null ? tracker.CurrentRoom : null;
            if (currentRoom == null) currentRoom = builder.GetRoom(builder.Data.Start.Grid);
            if (currentRoom != null)
            {
                data.currentRoomX = currentRoom.GridPosition.x;
                data.currentRoomY = currentRoom.GridPosition.y;
            }

            RoomView bossRoom = FindBossRoom();
            data.bossCleared = bossRoom != null && bossRoom.IsCleared;

            if (flow != null && flow.WeaponOffering != null &&
                flow.WeaponOffering.Weapon != null)
            {
                data.startWeaponTaken = false;
                data.startWeaponId = flow.WeaponOffering.Weapon.name;
            }

            if (inventory != null)
            {
                IReadOnlyList<ItemDefinition> items = inventory.Items;
                data.itemIds = new string[items.Count];
                for (int i = 0; i < items.Count; i++)
                    data.itemIds[i] = items[i] != null ? items[i].name : string.Empty;
            }

            data.weaponIds = new string[WeaponInventory.SlotCount];
            data.weaponAmmo = new int[WeaponInventory.SlotCount];
            if (weaponInventory != null)
            {
                for (int i = 0; i < WeaponInventory.SlotCount; i++)
                {
                    WeaponDefinition weapon = weaponInventory.SlotAt(i);
                    data.weaponIds[i] = weapon != null ? weapon.name : string.Empty;
                    data.weaponAmmo[i] = weaponInventory.AmmoAt(i);
                }
            }

            var rooms = new List<RunRoomSave>();
            IReadOnlyList<RoomView> allRooms = builder.Rooms;
            for (int i = 0; i < allRooms.Count; i++)
            {
                RoomView room = allRooms[i];
                if (room == null) continue;
                if (!room.IsVisited && !room.IsCleared &&
                    !room.RewardClaimed && !room.SecretEventUsed)
                    continue;

                rooms.Add(new RunRoomSave
                {
                    x = room.GridPosition.x,
                    y = room.GridPosition.y,
                    visited = room.IsVisited,
                    cleared = room.IsCleared,
                    rewardClaimed = room.RewardClaimed,
                    secretUsed = room.SecretEventUsed,
                });
            }

            data.rooms = rooms.ToArray();
            return data;
        }

        private bool RestoreRun(RunSaveData save)
        {
            if (save == null || builder == null) return false;

            Time.timeScale = 1f;
            _runCoinsBanked = false;
            _lastBankedCoins = 0;
            _lastBankedTotal = MetaProgression.Coins;
            _suspendedRun = false;

            if (_portal != null)
            {
                Destroy(_portal.gameObject);
                _portal = null;
            }

            if (flow != null) flow.ResetForNewRun();

            currentFloor = maxFloor > 0
                ? Mathf.Clamp(save.floor, 1, maxFloor)
                : Mathf.Max(1, save.floor);
            ApplyFloorPresentation();

            builder.SetSeed(save.seed);
            builder.BuildFromSeed();

            RestoreRoomStates(save);

            if (flow != null)
                flow.SetWeaponOfferingRestore(save.startWeaponTaken, save.startWeaponId);

            ApplyMetaUpgrades();
            if (playerHealth != null) playerHealth.ResetForNewRun();

            RestoreInventory(save);
            if (wallet != null) wallet.SetCoins(save.coins);

            if (flow != null) flow.PrepareFloor();

            if (playerHealth != null)
                playerHealth.RestoreVitals(save.health, save.shield);

            if (save.bossCleared && (maxFloor <= 0 || currentFloor < maxFloor))
                SpawnNextFloorPortal();

            if (tracker != null && builder.Data != null)
            {
                var savedGrid = new Vector2Int(save.currentRoomX, save.currentRoomY);
                RoomView room = builder.GetRoom(savedGrid);
                if (room == null) room = builder.GetRoom(builder.Data.Start.Grid);

                if (room != null)
                {
                    tracker.TeleportTo(room);
                    if (roomCamera != null) roomCamera.SnapTo(room);
                }
            }

            _state = RunState.Playing;
            RunRestarted?.Invoke();
            RestoreWeapons(save);

            Debug.Log($"[Run] 已继续第 {currentFloor} 层进度（种子 {save.seed}）");
            return true;
        }

        private void RestoreRoomStates(RunSaveData save)
        {
            if (save.rooms == null) return;

            for (int i = 0; i < save.rooms.Length; i++)
            {
                RunRoomSave entry = save.rooms[i];
                if (entry == null) continue;

                RoomView room = builder.GetRoom(new Vector2Int(entry.x, entry.y));
                if (room == null) continue;

                room.IsVisited = entry.visited;
                room.IsCleared = entry.cleared;
                room.RewardClaimed = entry.rewardClaimed;
                if (entry.rewardClaimed) room.RewardSpawned = true;
                room.SecretEventUsed = entry.secretUsed;
                if (entry.secretUsed) room.SecretEventSpawned = true;
            }
        }

        private void RestoreInventory(RunSaveData save)
        {
            if (inventory == null) return;

            var restored = new List<ItemDefinition>();
            if (save.itemIds != null && save.itemIds.Length > 0)
            {
                Dictionary<string, ItemDefinition> pool = BuildItemLookup();
                for (int i = 0; i < save.itemIds.Length; i++)
                {
                    string id = save.itemIds[i];
                    if (!string.IsNullOrEmpty(id) && pool.TryGetValue(id, out ItemDefinition item))
                        restored.Add(item);
                }
            }

            inventory.RestoreItems(restored);
        }

        private void RestoreWeapons(RunSaveData save)
        {
            if (weaponInventory == null) return;

            Dictionary<string, WeaponDefinition> pool = BuildWeaponLookup();
            WeaponDefinition slot0 = null;
            WeaponDefinition slot1 = null;
            int ammo0 = 0;
            int ammo1 = 0;

            if (save.weaponIds != null && save.weaponIds.Length > 0)
            {
                pool.TryGetValue(save.weaponIds[0] ?? string.Empty, out slot0);
                if (save.weaponAmmo != null && save.weaponAmmo.Length > 0)
                    ammo0 = save.weaponAmmo[0];
            }

            if (save.weaponIds != null && save.weaponIds.Length > 1)
            {
                pool.TryGetValue(save.weaponIds[1] ?? string.Empty, out slot1);
                if (save.weaponAmmo != null && save.weaponAmmo.Length > 1)
                    ammo1 = save.weaponAmmo[1];
            }

            weaponInventory.RestoreFromSave(slot0, ammo0, slot1, ammo1, save.currentWeaponSlot);
        }

        private static Dictionary<string, ItemDefinition> BuildItemLookup()
        {
            var lookup = new Dictionary<string, ItemDefinition>();
            ItemDefinition[] items = Resources.LoadAll<ItemDefinition>("Items");
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null) lookup[items[i].name] = items[i];
            }

            return lookup;
        }

        private static Dictionary<string, WeaponDefinition> BuildWeaponLookup()
        {
            var lookup = new Dictionary<string, WeaponDefinition>();
            WeaponDefinition[] weapons = Resources.LoadAll<WeaponDefinition>("Weapons");
            for (int i = 0; i < weapons.Length; i++)
            {
                if (weapons[i] != null) lookup[weapons[i].name] = weapons[i];
            }

            return lookup;
        }

        /// <summary>进入下一层：保留生命、金币与道具，重建地牢并回到新起点房。</summary>
        public void DescendToNextFloor()
        {
            if (_state != RunState.Playing) return;

            if (maxFloor > 0 && currentFloor >= maxFloor)
            {
                EnterVictory("已达楼层上限");
                return;
            }

            AudioService.Play("teleport", 0.9f, 0f);
            Time.timeScale = 1f;
            RunSaveService.Delete();

            if (_portal != null)
            {
                Destroy(_portal.gameObject);
                _portal = null;
            }

            if (flow != null) flow.ResetForNewRun();

            currentFloor++;
            ApplyFloorPresentation();

            if (builder != null)
            {
                builder.SetSeed(Random.Range(1, int.MaxValue));
                builder.BuildFromSeed();
            }

            if (flow != null) flow.PrepareFloor();

            if (tracker != null && builder != null && builder.Data != null)
            {
                RoomView start = builder.GetRoom(builder.Data.Start.Grid);
                tracker.TeleportTo(start);

                if (roomCamera != null) roomCamera.SnapTo(start);
            }

            _state = RunState.Playing;
            Debug.Log($"[Run] 进入第 {currentFloor} 层（{CurrentThemeName}），新种子 {(builder != null ? builder.Seed : 0)}");
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>调试：直接跳到指定楼层，默认补满全部道具。</summary>
        public void DebugJumpToFloor(int floor, bool grantAllItems = true)
        {
            if (_state != RunState.Playing) return;

            int target = Mathf.Max(1, floor);
            if (maxFloor > 0) target = Mathf.Min(target, maxFloor);

            Time.timeScale = 1f;

            if (_portal != null)
            {
                Destroy(_portal.gameObject);
                _portal = null;
            }

            if (flow != null) flow.ResetForNewRun();

            currentFloor = target;
            ApplyFloorPresentation();

            if (builder != null)
            {
                builder.SetSeed(Random.Range(1, int.MaxValue));
                builder.BuildFromSeed();
            }

            if (flow != null) flow.PrepareFloor();

            if (tracker != null && builder != null && builder.Data != null)
            {
                RoomView start = builder.GetRoom(builder.Data.Start.Grid);
                tracker.TeleportTo(start);

                if (roomCamera != null) roomCamera.SnapTo(start);
            }

            if (grantAllItems) GrantAllItems();

            Debug.Log($"[Run] 调试跳转到第 {currentFloor} 层（{CurrentThemeName}）");
        }

        private void GrantAllItems()
        {
            if (inventory == null) inventory = FindFirstObjectByType<PlayerInventory>();
            if (inventory == null) return;

            ItemDefinition[] items = Resources.LoadAll<ItemDefinition>("Items");
            for (int i = 0; i < items.Length; i++)
            {
                ItemDefinition item = items[i];
                if (item == null || inventory.Owns(item)) continue;

                // 批量发放时不叠 37 次开书音效。
                ItemGrant.Apply(item, inventory, playerHealth, wallet, false);
            }
        }
#endif

        /// <summary>用新种子原地重开一局。</summary>
        public void RestartRun() => RestartRun(false);

        private void RestartRun(bool keepItemsOverride)
        {
            Time.timeScale = 1f;
            RunSaveService.Delete();
            _suspendedRun = false;

            _runCoinsBanked = false;
            _lastBankedCoins = 0;
            _lastBankedTotal = MetaProgression.Coins;
            bool keepItems = keepItemsOverride ||
                (keepItemsAfterVictory && _state == RunState.Victory);

            if (_portal != null)
            {
                Destroy(_portal.gameObject);
                _portal = null;
            }

            if (flow != null) flow.ResetForNewRun();
            if (inventory != null && !keepItems) inventory.Clear();
            if (wallet != null) wallet.ResetCoins();
            currentFloor = 1;
            ApplyFloorPresentation();

            if (builder != null)
            {
                builder.SetSeed(Random.Range(1, int.MaxValue));
                builder.BuildFromSeed();
            }

            if (flow != null) flow.PrepareFloor();

            ApplyMetaUpgrades();
            if (playerHealth != null) playerHealth.ResetForNewRun();

            if (tracker != null && builder != null && builder.Data != null)
            {
                RoomView start = builder.GetRoom(builder.Data.Start.Grid);
                tracker.TeleportTo(start);

                if (roomCamera != null) roomCamera.SnapTo(start);
            }

            _state = RunState.Playing;
            RunRestarted?.Invoke();
            Debug.Log($"[Run] 已重开，新种子 {(builder != null ? builder.Seed : 0)}，道具{(keepItems ? "已继承" : "已清空")}");
        }

        private void OnGUI()
        {
            if (_state == RunState.Playing || _state == RunState.MainMenu) return;

            UiTheme.DrawDim();

            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;
            bool victory = _state == RunState.Victory;
            var panel = new Rect(cx - 260f, Mathf.Max(66f, cy - 160f), 520f, 320f);
            UiTheme.DrawPanel(panel, victory ? UiTheme.Gold : UiTheme.Danger, false);

            string title = victory ? "通 关" : "你 死 了";
            Color titleColor = victory ? UiTheme.GoldBright : UiTheme.Danger;
            UiTheme.DrawLabelShadow(
                new Rect(panel.x + 24f, panel.y + 24f, panel.width - 48f, 42f),
                title,
                UiTheme.TitleCenter,
                titleColor);

            UiTheme.DrawLabelShadow(
                new Rect(panel.x + 24f, panel.y + 68f, panel.width - 48f, 16f),
                victory ? "RUN COMPLETE" : "YOU DIED",
                UiTheme.Tiny,
                UiTheme.Muted);

            UiTheme.DrawDivider(new Rect(panel.x + 30f, panel.y + 94f, panel.width - 60f, 1f));

            string body = !victory
                ? "本局结束"
                : maxFloor > 0
                    ? $"你已征服 {maxFloor} 层地牢"
                    : "Boss 已被击败";
            UiTheme.DrawLabelShadow(
                new Rect(panel.x + 28f, panel.y + 116f, panel.width - 56f, 30f),
                body,
                UiTheme.Center,
                UiTheme.Text);

            string detail = victory
                ? "已获得的道具与武器将保留到下一局；剩余金币已存入局外金库"
                : "剩余金币已存入局外金库；返回主页后可开始新的一局";
            UiTheme.DrawLabelShadow(
                new Rect(panel.x + 28f, panel.y + 152f, panel.width - 56f, 22f),
                detail,
                UiTheme.Center,
                UiTheme.Muted);

            var coinIconRect = new Rect(panel.x + 36f, panel.y + 187f, 16f, 16f);
            UiTheme.DrawCoinIcon(coinIconRect);
            UiTheme.DrawLabelShadow(
                new Rect(coinIconRect.x + 24f, panel.y + 183f, panel.width - 100f, 24f),
                $"带出金币 +{_lastBankedCoins}  ·  局外金库 {_lastBankedTotal}",
                UiTheme.Small,
                UiTheme.GoldBright);

            if (UiTheme.DrawButton(
                    new Rect(panel.center.x - 110f, panel.yMax - 64f, 220f, 42f),
                    "返回主页面",
                    true,
                    false,
                    16))
            {
                ReturnToMainMenuFromResults();
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void HandleDebugFloorKeys()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.f1Key.wasPressedThisFrame) DebugJumpToFloor(1);
            else if (keyboard.f2Key.wasPressedThisFrame) DebugJumpToFloor(2);
            else if (keyboard.f3Key.wasPressedThisFrame) DebugJumpToFloor(3);
            else if (keyboard.f4Key.wasPressedThisFrame) DebugJumpToFloor(4);
            else if (keyboard.f5Key.wasPressedThisFrame) DebugJumpToFloor(5);
            else if (keyboard.f6Key.wasPressedThisFrame) DebugJumpToFloor(6);
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.F1)) DebugJumpToFloor(1);
            else if (Input.GetKeyDown(KeyCode.F2)) DebugJumpToFloor(2);
            else if (Input.GetKeyDown(KeyCode.F3)) DebugJumpToFloor(3);
            else if (Input.GetKeyDown(KeyCode.F4)) DebugJumpToFloor(4);
            else if (Input.GetKeyDown(KeyCode.F5)) DebugJumpToFloor(5);
            else if (Input.GetKeyDown(KeyCode.F6)) DebugJumpToFloor(6);
#endif
        }
#endif

        /// <summary>结算页返回主页：保留局外结算结果，清掉本局残留状态。</summary>
        public bool ReturnToMainMenuFromResults()
        {
            if (_state != RunState.GameOver && _state != RunState.Victory) return false;

            _state = RunState.MainMenu;
            _suspendedRun = false;
            Time.timeScale = 0f;
            AudioService.StopMusic();

            MainMenuUI menu = FindFirstObjectByType<MainMenuUI>(FindObjectsInactive.Include);
            if (menu != null) menu.ShowMenu();

            Debug.Log("[Run] 已从结算页返回主页面");
            return true;
        }
    }
}
