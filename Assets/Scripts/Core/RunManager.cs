using Roguelike.Audio;
using Roguelike.Dungeon;
using Roguelike.Items;
using Roguelike.Player;
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

        [Header("楼层推进")]
        [SerializeField] private NextFloorPortal portalPrefab;

        [Tooltip("当前楼层，从 1 开始")]
        [SerializeField] private int currentFloor = 1;

        [Header("跨局继承")]
        [Tooltip("旧版通关继承开关；现在跨楼层会保留道具，死亡时始终清空")]
        [SerializeField] private bool keepItemsAfterVictory = true;

        private RunState _state = RunState.Playing;
        private NextFloorPortal _portal;
        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;

        public RunState State => _state;
        public int CurrentFloor => currentFloor;

        private void Awake()
        {
            Time.timeScale = 1f;

            if (playerHealth == null) playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (flow == null) flow = FindFirstObjectByType<DungeonFlowController>();
            if (builder == null) builder = FindFirstObjectByType<DungeonBuilder>();
            if (tracker == null) tracker = FindFirstObjectByType<PlayerRoomTracker>();
            if (roomCamera == null) roomCamera = FindFirstObjectByType<RoomCamera>();
            if (inventory == null) inventory = FindFirstObjectByType<PlayerInventory>();
            if (wallet == null) wallet = FindFirstObjectByType<PlayerWallet>();
            if (portalPrefab == null) portalPrefab = Resources.Load<NextFloorPortal>("Combat/NextFloorPortal");

            if (playerHealth != null) playerHealth.Died += HandlePlayerDied;
            if (flow != null) flow.BossDefeated += HandleBossDefeated;
        }

        private void OnDestroy()
        {
            if (playerHealth != null) playerHealth.Died -= HandlePlayerDied;
            if (flow != null) flow.BossDefeated -= HandleBossDefeated;
        }

        private void Update()
        {
            if (_state == RunState.Playing) return;
            if (WasRestartPressed()) RestartRun();
        }

        private void HandlePlayerDied()
        {
            if (_state != RunState.Playing) return;

            _state = RunState.GameOver;
            Time.timeScale = 0f;
            AudioService.Play("Get_Out_Loser", 1f, 0f);
            Debug.Log("[Run] 玩家死亡，本局结束");
        }

        private void HandleBossDefeated()
        {
            if (_state != RunState.Playing) return;

            if (portalPrefab == null)
            {
                // 没有传送门 Prefab 时回退到旧的通关结算，避免 Boss 死后无事发生
                _state = RunState.Victory;
                Time.timeScale = 0f;
                AudioService.Play("bookOpen", 0.9f);
                Debug.LogWarning("[Run] 缺少 NextFloorPortal Prefab，回退为通关结算");
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

            // 放在 Boss 房中央偏下，避开中央柱和下方墙角
            Vector3 position = new Vector3(
                bossRoom.WorldCenter.x,
                bossRoom.WorldCenter.y - 2.4f,
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

        /// <summary>进入下一层：保留生命、金币与道具，重建地牢并回到新起点房。</summary>
        public void DescendToNextFloor()
        {
            if (_state != RunState.Playing) return;

            Time.timeScale = 1f;

            if (_portal != null)
            {
                Destroy(_portal.gameObject);
                _portal = null;
            }

            if (flow != null) flow.ResetForNewRun();

            currentFloor++;

            if (builder != null)
            {
                builder.SetSeed(Random.Range(1, int.MaxValue));
                builder.BuildFromSeed();
            }

            if (tracker != null && builder != null && builder.Data != null)
            {
                RoomView start = builder.GetRoom(builder.Data.Start.Grid);
                tracker.TeleportTo(start);

                if (roomCamera != null) roomCamera.SnapTo(start);
            }

            _state = RunState.Playing;
            AudioService.Play("doorOpen_1", 0.6f);
            Debug.Log($"[Run] 进入第 {currentFloor} 层，新种子 {(builder != null ? builder.Seed : 0)}");
        }

        /// <summary>用新种子原地重开一局。</summary>
        public void RestartRun()
        {
            Time.timeScale = 1f;

            bool keepItems = keepItemsAfterVictory && _state == RunState.Victory;

            if (_portal != null)
            {
                Destroy(_portal.gameObject);
                _portal = null;
            }

            if (flow != null) flow.ResetForNewRun();
            if (inventory != null && !keepItems) inventory.Clear();
            if (wallet != null) wallet.ResetCoins();
            currentFloor = 1;

            if (builder != null)
            {
                builder.SetSeed(Random.Range(1, int.MaxValue));
                builder.BuildFromSeed();
            }

            if (playerHealth != null) playerHealth.ResetForNewRun();

            if (tracker != null && builder != null && builder.Data != null)
            {
                RoomView start = builder.GetRoom(builder.Data.Start.Grid);
                tracker.TeleportTo(start);

                if (roomCamera != null) roomCamera.SnapTo(start);
            }

            _state = RunState.Playing;
            Debug.Log($"[Run] 已重开，新种子 {(builder != null ? builder.Seed : 0)}，道具{(keepItems ? "已继承" : "已清空")}");
        }

        private void OnGUI()
        {
            if (_state == RunState.Playing) return;

            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 42, alignment = TextAnchor.MiddleCenter };
                _bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter };
            }

            _titleStyle.normal.textColor = _state == RunState.Victory
                ? new Color(0.96f, 0.86f, 0.32f)
                : new Color(0.92f, 0.32f, 0.32f);
            _bodyStyle.normal.textColor = Color.white;

            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.72f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previous;

            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;

            GUI.Label(new Rect(cx - 300f, cy - 120f, 600f, 70f),
                _state == RunState.Victory ? "通关！" : "你死了", _titleStyle);

            GUI.Label(new Rect(cx - 300f, cy - 38f, 600f, 34f),
                _state == RunState.Victory ? "Boss 已被击败" : "本局结束",
                _bodyStyle);

            if (GUI.Button(new Rect(cx - 110f, cy + 32f, 220f, 48f), "重新开始 (R)"))
                RestartRun();
        }

        private static bool WasRestartPressed()
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
