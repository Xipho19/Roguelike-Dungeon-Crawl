using Roguelike.Audio;
using Roguelike.Combat;
using Roguelike.Core;
using Roguelike.Dungeon;
using Roguelike.Items;
using Roguelike.Player;
using Roguelike.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Roguelike.EditorTools
{
    /// <summary>
    /// 一键创建/重建玩家，并把玩家、房间跟踪器、相机全部接好。
    /// 菜单：Tools / Roguelike / 创建玩家
    ///
    /// 生成的 Player 是一个临时方块（白色 Sprite 染色），
    /// 只用于验证移动、门触发与房间切换，不涉及美术表现。
    /// </summary>
    public static class PlayerSetupMenu
    {
        private const string PlayerName = "Player";
        private const string SpritePath = "Assets/Art/White.png";

        [MenuItem("Tools/Roguelike/创建玩家")]
        public static void CreatePlayer()
        {
            var builder = Object.FindFirstObjectByType<DungeonBuilder>();
            if (builder == null)
            {
                Debug.LogError("[Player] 场景中没有 DungeonBuilder，请先执行 Tools/Roguelike/构建地牢到当前场景。");
                return;
            }

            if (builder.Data == null) builder.BuildFromSeed();

            if (!RoomPrefabFactory.AllVariantsExist())
                RoomPrefabFactory.Rebuild();

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            if (sprite == null)
            {
                RoomPrefabFactory.Rebuild();
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            }

            if (AssetDatabase.LoadAssetAtPath<Enemy>(CombatPrefabFactory.EnemyPath) == null ||
                AssetDatabase.LoadAssetAtPath<BossEnemy>(CombatPrefabFactory.BossPath) == null ||
                AssetDatabase.LoadAssetAtPath<ShooterEnemy>(CombatPrefabFactory.ShooterPath) == null ||
                AssetDatabase.LoadAssetAtPath<ChargerEnemy>(CombatPrefabFactory.ChargerPath) == null ||
                AssetDatabase.LoadAssetAtPath<BomberEnemy>(CombatPrefabFactory.BomberPath) == null ||
                AssetDatabase.LoadAssetAtPath<NecromancerEnemy>(CombatPrefabFactory.NecromancerPath) == null ||
                AssetDatabase.LoadAssetAtPath<SplitterEnemy>(CombatPrefabFactory.SplitterPath) == null ||
                AssetDatabase.LoadAssetAtPath<TearProjectile>(CombatPrefabFactory.TearPath) == null ||
                !CombatPrefabFactory.RosterPrefabsExist())
                CombatPrefabFactory.Rebuild();

            ItemAssetFactory.EnsureItems();
            FloorThemeAssetFactory.EnsureThemes();
            CombatPrefabFactory.EnsureTearExplosionSprite();

            RoomView startRoom = builder.GetRoom(builder.Data.Start.Grid);

            GameObject player = GameObject.Find(PlayerName);
            if (player == null) player = new GameObject(PlayerName);

            // 视觉
            var renderer = Ensure<SpriteRenderer>(player);
            Sprite playerSprite = ThirdPartySprites.Player;
            renderer.sprite = playerSprite != null ? playerSprite : sprite;
            renderer.color = Color.white;
            renderer.sortingOrder = 10;
            player.transform.localScale = Vector3.one * 0.8f;

            // 待机 / 行走帧动画
            var animator = Ensure<SpriteAnimator>(player);
            animator.Configure(ThirdPartySprites.PlayerIdle, ThirdPartySprites.PlayerRun);

            // 物理
            var body = Ensure<Rigidbody2D>(player);
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            var collider = Ensure<CircleCollider2D>(player);
            collider.radius = 0.45f;
            collider.isTrigger = false;

            // 属性与背包（必须先于其它玩家系统存在）
            Ensure<PlayerStats>(player);
            Ensure<PlayerInventory>(player);

            // 控制与房间跟踪
            Ensure<PlayerController>(player);
            Ensure<PlayerHealth>(player);
            Ensure<PlayerWallet>(player);
            Ensure<PlayerMoveLoopAudio>(player);
            var tracker = Ensure<PlayerRoomTracker>(player);

            // 战斗
            var combat = Ensure<PlayerCombat>(player);
            var inventory = player.GetComponent<PlayerInventory>();
            var health = player.GetComponent<PlayerHealth>();

            var tearPrefab = AssetDatabase.LoadAssetAtPath<TearProjectile>(CombatPrefabFactory.TearPath);
            var combatObject = new SerializedObject(combat);
            if (tearPrefab != null)
                combatObject.FindProperty("tearPrefab").objectReferenceValue = tearPrefab;
            combatObject.FindProperty("inventory").objectReferenceValue = inventory;
            combatObject.FindProperty("health").objectReferenceValue = health;
            combatObject.ApplyModifiedPropertiesWithoutUndo();

            // 触发型被动
            var itemEffects = Ensure<PlayerItemEffects>(player);

            if (startRoom != null)
                player.transform.position = new Vector3(startRoom.WorldCenter.x, startRoom.WorldCenter.y, 0f);

            var trackerObject = new SerializedObject(tracker);
            trackerObject.FindProperty("builder").objectReferenceValue = builder;
            trackerObject.ApplyModifiedPropertiesWithoutUndo();

            // 相机
            Camera camera = Camera.main;
            if (camera != null)
            {
                var roomCamera = Ensure<RoomCamera>(camera.gameObject);
                var cameraObject = new SerializedObject(roomCamera);
                cameraObject.FindProperty("tracker").objectReferenceValue = tracker;
                cameraObject.ApplyModifiedPropertiesWithoutUndo();
                roomCamera.SnapTo(startRoom);
            }
            else
            {
                Debug.LogWarning("[Player] 场景中没有 Main Camera，房间相机未绑定。");
            }

            // 房间流程：进入战斗房锁门 + 生成敌人，清空后开门
            var flow = Ensure<DungeonFlowController>(builder.gameObject);
            var flowObject = new SerializedObject(flow);
            flowObject.FindProperty("tracker").objectReferenceValue = tracker;

            SerializedProperty enemyCountProperty = flowObject.FindProperty("combatEnemyCount");
            if (enemyCountProperty != null && enemyCountProperty.intValue < 4)
                enemyCountProperty.intValue = 4;

            var enemyPrefab = AssetDatabase.LoadAssetAtPath<Enemy>(CombatPrefabFactory.EnemyPath);
            if (enemyPrefab != null)
                flowObject.FindProperty("enemyPrefab").objectReferenceValue = enemyPrefab;

            var shooterPrefab = AssetDatabase.LoadAssetAtPath<ShooterEnemy>(CombatPrefabFactory.ShooterPath);
            if (shooterPrefab != null)
                flowObject.FindProperty("shooterPrefab").objectReferenceValue = shooterPrefab;

            var chargerPrefab = AssetDatabase.LoadAssetAtPath<ChargerEnemy>(CombatPrefabFactory.ChargerPath);
            if (chargerPrefab != null)
                flowObject.FindProperty("chargerPrefab").objectReferenceValue = chargerPrefab;

            var bomberPrefab = AssetDatabase.LoadAssetAtPath<BomberEnemy>(CombatPrefabFactory.BomberPath);
            if (bomberPrefab != null)
                flowObject.FindProperty("bomberPrefab").objectReferenceValue = bomberPrefab;

            var necromancerPrefab = AssetDatabase.LoadAssetAtPath<NecromancerEnemy>(CombatPrefabFactory.NecromancerPath);
            if (necromancerPrefab != null)
                flowObject.FindProperty("necromancerPrefab").objectReferenceValue = necromancerPrefab;

            var splitterPrefab = AssetDatabase.LoadAssetAtPath<SplitterEnemy>(CombatPrefabFactory.SplitterPath);
            if (splitterPrefab != null)
                flowObject.FindProperty("splitterPrefab").objectReferenceValue = splitterPrefab;

            var bossPrefab = AssetDatabase.LoadAssetAtPath<BossEnemy>(CombatPrefabFactory.BossPath);
            if (bossPrefab != null)
                flowObject.FindProperty("bossPrefab").objectReferenceValue = bossPrefab;

            var coinPrefab = AssetDatabase.LoadAssetAtPath<Pickup>(CombatPrefabFactory.CoinPath);
            if (coinPrefab != null)
                flowObject.FindProperty("coinPrefab").objectReferenceValue = coinPrefab;

            var heartPrefab = AssetDatabase.LoadAssetAtPath<Pickup>(CombatPrefabFactory.HeartPath);
            if (heartPrefab != null)
                flowObject.FindProperty("heartPrefab").objectReferenceValue = heartPrefab;

            var itemPickup = AssetDatabase.LoadAssetAtPath<ItemPickup>(ItemAssetFactory.PickupPath);
            if (itemPickup != null)
                flowObject.FindProperty("itemPickupPrefab").objectReferenceValue = itemPickup;

            var secretAltar = AssetDatabase.LoadAssetAtPath<SecretAltar>(ItemAssetFactory.AltarPath);
            if (secretAltar != null)
                flowObject.FindProperty("secretAltarPrefab").objectReferenceValue = secretAltar;

            flowObject.ApplyModifiedPropertiesWithoutUndo();

            var hud = Ensure<CombatDebugHud>(builder.gameObject);
            var hudObject = new SerializedObject(hud);
            hudObject.FindProperty("health").objectReferenceValue = health;
            hudObject.FindProperty("wallet").objectReferenceValue = player.GetComponent<PlayerWallet>();
            hudObject.FindProperty("inventory").objectReferenceValue = inventory;
            hudObject.FindProperty("flow").objectReferenceValue = flow;
            hudObject.FindProperty("floorIcon").objectReferenceValue = ThirdPartySprites.FloorLadder;
            hudObject.FindProperty("heartIcon").objectReferenceValue = ThirdPartySprites.Heart;
            hudObject.FindProperty("shieldIcon").objectReferenceValue = ThirdPartySprites.ShieldIcon;
            hudObject.FindProperty("coinIcon").objectReferenceValue = ThirdPartySprites.HudCoin;
            hudObject.FindProperty("backpackIcon").objectReferenceValue = ThirdPartySprites.BackpackIcon;
            hudObject.ApplyModifiedPropertiesWithoutUndo();

            // 一局生命周期：死亡 / 通关 / 重开
            var runManager = Ensure<RunManager>(builder.gameObject);
            var runObject = new SerializedObject(runManager);
            runObject.FindProperty("playerHealth").objectReferenceValue = player.GetComponent<PlayerHealth>();
            runObject.FindProperty("flow").objectReferenceValue = flow;
            runObject.FindProperty("builder").objectReferenceValue = builder;
            runObject.FindProperty("tracker").objectReferenceValue = tracker;
            runObject.FindProperty("inventory").objectReferenceValue = player.GetComponent<PlayerInventory>();
            runObject.FindProperty("wallet").objectReferenceValue = player.GetComponent<PlayerWallet>();

            RoomCamera boundCamera = camera != null ? camera.GetComponent<RoomCamera>() : null;
            runObject.FindProperty("roomCamera").objectReferenceValue = boundCamera;

            runObject.ApplyModifiedPropertiesWithoutUndo();

            hudObject.FindProperty("runManager").objectReferenceValue = runManager;
            hudObject.ApplyModifiedPropertiesWithoutUndo();

            // Esc 暂停
            var pauseMenu = Ensure<PauseMenu>(builder.gameObject);
            var pauseObject = new SerializedObject(pauseMenu);
            pauseObject.FindProperty("runManager").objectReferenceValue = runManager;
            pauseObject.ApplyModifiedPropertiesWithoutUndo();

            // 触发型被动
            var effectsObject = new SerializedObject(itemEffects);
            effectsObject.FindProperty("inventory").objectReferenceValue = inventory;
            effectsObject.FindProperty("health").objectReferenceValue = health;
            effectsObject.FindProperty("controller").objectReferenceValue = player.GetComponent<PlayerController>();
            effectsObject.FindProperty("tracker").objectReferenceValue = tracker;
            effectsObject.FindProperty("flow").objectReferenceValue = flow;
            effectsObject.FindProperty("runManager").objectReferenceValue = runManager;
            effectsObject.FindProperty("hud").objectReferenceValue = hud;
            effectsObject.FindProperty("explosionSprite").objectReferenceValue = ThirdPartySprites.Explosion;
            effectsObject.ApplyModifiedPropertiesWithoutUndo();

            // Tab 地图
            var mapUi = Ensure<MapUI>(builder.gameObject);
            var mapObject = new SerializedObject(mapUi);
            mapObject.FindProperty("builder").objectReferenceValue = builder;
            mapObject.FindProperty("tracker").objectReferenceValue = tracker;
            mapObject.FindProperty("runManager").objectReferenceValue = runManager;
            mapObject.FindProperty("shopIcon").objectReferenceValue = ThirdPartySprites.MapShopIcon;
            mapObject.FindProperty("treasureIcon").objectReferenceValue = ThirdPartySprites.Chest;
            mapObject.FindProperty("bossIcon").objectReferenceValue = ThirdPartySprites.MapBossIcon;
            mapObject.FindProperty("exploredIcon").objectReferenceValue = ThirdPartySprites.MapExploredIcon;
            mapObject.FindProperty("unexploredIcon").objectReferenceValue = ThirdPartySprites.MapUnexploredIcon;
            mapObject.FindProperty("startIcon").objectReferenceValue = ThirdPartySprites.FloorLadder;
            mapObject.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(player.scene);
            Selection.activeGameObject = player;

            Debug.Log($"[Player] 已创建玩家并绑定房间跟踪器与相机，起始房间 = {builder.Data.Start.Grid}。");
        }

        private static T Ensure<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }
    }
}
