using System.Collections.Generic;
using Roguelike.Core;
using Roguelike.Items;
using Roguelike.Player;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Roguelike.Combat
{
    /// <summary>
    /// 顶部状态栏：楼层、生命、金币、背包按钮，以及拾取提示与 Boss 血条。
    /// 点击背包按钮或按 B 打开背包面板；面板打开时暂停游戏。
    /// </summary>
    public sealed class CombatDebugHud : MonoBehaviour
    {
        [SerializeField] private PlayerHealth health;
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private RunManager runManager;

        [Header("状态栏图标")]
        [SerializeField] private Sprite floorIcon;
        [SerializeField] private Sprite heartIcon;
        [SerializeField] private Sprite coinIcon;
        [SerializeField] private Sprite backpackIcon;

        private const float BarHeight = 48f;
        private const float BackpackButtonSize = 34f;
        private const float InventoryRowHeight = 100f;

        private GUIStyle _style;
        private GUIStyle _toastStyle;
        private GUIStyle _bossStyle;
        private GUIStyle _inventoryTitleStyle;
        private GUIStyle _inventoryCountStyle;
        private GUIStyle _inventoryNameStyle;
        private GUIStyle _inventoryDescStyle;
        private GUIStyle _inventoryHintStyle;
        private GUIStyle _keyHintStyle;
        private GUIStyle _themeStyle;
        private GUIStyle _floorToastStyle;

        private string _toastText;
        private float _toastUntil;

        private string _floorToastText;
        private float _floorToastUntil;
        private int _lastShownFloor = -1;
        private string _lastShownTheme;

        private bool _inventoryOpen;
        private bool _pausedByInventory;
        private Vector2 _inventoryScroll;
        private static bool _anyInventoryOpen;

        /// <summary>供地图等其它界面判断是否需要让位。</summary>
        public static bool IsInventoryOpen => _anyInventoryOpen;

        public bool IsOpen => _inventoryOpen;

        private void Awake()
        {
            if (health == null) health = FindFirstObjectByType<PlayerHealth>();
            if (wallet == null) wallet = FindFirstObjectByType<PlayerWallet>();
            if (inventory == null) inventory = FindFirstObjectByType<PlayerInventory>();
            if (runManager == null) runManager = FindFirstObjectByType<RunManager>();

            if (inventory != null) inventory.ItemAdded += HandleItemAdded;
        }

        private void OnDestroy()
        {
            if (inventory != null) inventory.ItemAdded -= HandleItemAdded;
            if (_anyInventoryOpen) _anyInventoryOpen = false;
        }

        private void OnDisable()
        {
            if (_inventoryOpen) CloseInventory();
        }

        private void HandleItemAdded(ItemDefinition item)
        {
            if (item == null) return;

            ShowToast($"获得道具：{item.itemName}\n{item.description}", 3f);
        }

        /// <summary>显示一条顶部提示（道具、事件等）。</summary>
        public void ShowToast(string text, float duration = 3f)
        {
            _toastText = text;
            _toastUntil = Time.time + Mathf.Max(0.1f, duration);
        }

        /// <summary>进入新楼层时显示“第 N 层 · 主题”横幅。</summary>
        public void ShowFloorIntro(int floor, string themeName, float duration = 2.5f)
        {
            _floorToastText = string.IsNullOrEmpty(themeName)
                ? $"第 {floor} 层"
                : $"第 {floor} 层 · {themeName}";
            _floorToastUntil = Time.time + Mathf.Max(0.5f, duration);
        }

        private void Update()
        {
            TrackFloorAndTheme();

            if (_inventoryOpen)
            {
                // 结算 / 暂停等其它界面接管时间后自动收起，避免两层界面叠在一起
                if (runManager != null && runManager.State != RunManager.RunState.Playing)
                {
                    CloseInventory();
                    return;
                }

                if (Time.timeScale > 0.001f)
                {
                    CloseInventory();
                    return;
                }
            }

            if (WasBackpackPressed()) ToggleInventory();
        }

        private void TrackFloorAndTheme()
        {
            if (runManager == null || runManager.State != RunManager.RunState.Playing) return;

            int floor = runManager.CurrentFloor;
            string theme = runManager.CurrentThemeName;

            if (floor == _lastShownFloor && theme == _lastShownTheme) return;

            _lastShownFloor = floor;
            _lastShownTheme = theme;
            ShowFloorIntro(floor, theme);
        }

        /// <summary>打开 / 关闭背包。商店、传送门、暂停与结算界面打开时不响应。</summary>
        public void ToggleInventory()
        {
            if (_inventoryOpen)
            {
                CloseInventory();
                return;
            }

            if (runManager != null && runManager.State != RunManager.RunState.Playing) return;
            if (Time.timeScale <= 0.001f) return; // 其它界面已经把游戏暂停

            OpenInventory();
        }

        private void OpenInventory()
        {
            _inventoryOpen = true;
            _anyInventoryOpen = true;
            _inventoryScroll = Vector2.zero;

            _pausedByInventory = Time.timeScale > 0f;
            if (_pausedByInventory) Time.timeScale = 0f;
        }

        /// <summary>关闭背包并恢复时间。</summary>
        public void CloseInventory()
        {
            if (!_inventoryOpen) return;

            _inventoryOpen = false;
            _anyInventoryOpen = false;

            if (_pausedByInventory) Time.timeScale = 1f;
            _pausedByInventory = false;
        }

        /// <summary>绘制一组「图标 + 数值」，返回下一组的起始 x。</summary>
        private float DrawStat(float x, float y, float iconSize, float gap, Sprite icon, string value, float groupGap)
        {
            if (icon != null && icon.texture != null)
            {
                GUI.DrawTexture(
                    new Rect(x, y, iconSize, iconSize),
                    icon.texture,
                    ScaleMode.ScaleToFit,
                    true);
                x += iconSize + gap;
            }

            GUI.Label(new Rect(x, y - 2f, 140f, iconSize + 4f), value, _style);
            x += _style.CalcSize(new GUIContent(value)).x + groupGap;
            return x;
        }

        private void OnGUI()
        {
            if (health == null) return;

            EnsureStyles();
            DrawStatusBar();
            DrawFloorIntro();
            DrawToast();
            DrawBossBar();
            DrawBackpackButton();

            if (_inventoryOpen) DrawInventoryPanel();
        }

        private void EnsureStyles()
        {
            if (_style != null) return;

            _style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleLeft,
            };
            _style.normal.textColor = Color.white;

            _toastStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
            };
            _toastStyle.normal.textColor = Color.white;

            _bossStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
            _bossStyle.normal.textColor = Color.white;

            _inventoryTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 26,
                alignment = TextAnchor.MiddleLeft,
            };
            _inventoryTitleStyle.normal.textColor = new Color(0.85f, 0.93f, 1f);

            _inventoryCountStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft,
            };
            _inventoryCountStyle.normal.textColor = new Color(0.68f, 0.72f, 0.82f);

            _inventoryNameStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleLeft,
            };
            _inventoryNameStyle.normal.textColor = Color.white;

            _inventoryDescStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
            };
            _inventoryDescStyle.normal.textColor = new Color(0.78f, 0.80f, 0.88f);

            _inventoryHintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
            };
            _inventoryHintStyle.normal.textColor = new Color(0.62f, 0.66f, 0.76f);

            _keyHintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
            };
            _keyHintStyle.normal.textColor = new Color(0.62f, 0.66f, 0.76f);

            _themeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft,
            };
            _themeStyle.normal.textColor = new Color(0.68f, 0.72f, 0.82f);

            _floorToastStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                alignment = TextAnchor.MiddleCenter,
            };
            _floorToastStyle.normal.textColor = new Color(0.95f, 0.88f, 0.70f);
        }

        private void DrawStatusBar()
        {
            var statusBar = new Rect(0f, 0f, Screen.width, BarHeight);

            Color previousColor = GUI.color;
            GUI.color = new Color(0.045f, 0.04f, 0.06f, 0.82f);
            GUI.DrawTexture(statusBar, Texture2D.whiteTexture);
            GUI.color = new Color(0.26f, 0.55f, 0.72f, 0.55f);
            GUI.DrawTexture(new Rect(0f, BarHeight - 1f, Screen.width, 1f), Texture2D.whiteTexture);
            GUI.color = previousColor;

            float x = 16f;
            float y = 10f;
            const float iconSize = 28f;
            const float iconGap = 7f;
            const float groupGap = 22f;

            int floor = runManager != null ? runManager.CurrentFloor : 1;
            string floorText = runManager != null && runManager.MaxFloor > 0
                ? $"{floor}/{runManager.MaxFloor}"
                : floor.ToString();

            x = DrawStat(x, y, iconSize, iconGap, floorIcon, floorText, 8f);

            string themeLabel = "· " + (runManager != null ? runManager.CurrentThemeName : "地牢");
            GUI.Label(new Rect(x, y + 8f, 200f, 20f), themeLabel, _themeStyle);
            x += _themeStyle.CalcSize(new GUIContent(themeLabel)).x + groupGap;

            x = DrawStat(x, y, iconSize, iconGap, heartIcon,
                $"{health.Health}/{health.MaxHealth}", groupGap);

            DrawStat(x, y, iconSize, iconGap, coinIcon,
                (wallet != null ? wallet.Coins : 0).ToString(), groupGap);
        }

        private bool FloorIntroVisible =>
            !_inventoryOpen &&
            Time.timeScale > 0.001f &&
            !string.IsNullOrEmpty(_floorToastText) &&
            Time.time < _floorToastUntil;

        private void DrawFloorIntro()
        {
            if (!FloorIntroVisible) return;

            var rect = new Rect(Screen.width * 0.5f - 240f, BarHeight + 12f, 480f, 36f);

            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;

            GUI.Label(
                new Rect(rect.x, rect.y + 3f, rect.width, 28f),
                _floorToastText,
                _floorToastStyle);
        }

        private void DrawToast()
        {
            if (_inventoryOpen) return;
            if (string.IsNullOrEmpty(_toastText) || Time.time >= _toastUntil) return;

            float top = BarHeight + 14f + (FloorIntroVisible ? 46f : 0f);
            var rect = new Rect(Screen.width * 0.5f - 230f, top, 460f, 76f);
            GUI.Box(rect, GUIContent.none);
            GUI.Label(new Rect(rect.x + 12f, rect.y + 8f, rect.width - 24f, rect.height - 16f), _toastText, _toastStyle);
        }

        private void DrawBossBar()
        {
            BossEnemy boss = BossEnemy.Active;
            if (boss == null || boss.IsDead || !boss.CombatActive) return;

            float ratio = boss.MaxHealth > 0f ? Mathf.Clamp01(boss.Health / boss.MaxHealth) : 0f;
            var bar = new Rect(Screen.width * 0.5f - 260f, Screen.height - 48f, 520f, 22f);

            GUI.Box(bar, GUIContent.none);

            Color previous = GUI.color;
            GUI.color = new Color(0.88f, 0.22f, 0.26f, 1f);
            GUI.DrawTexture(new Rect(bar.x + 2f, bar.y + 2f, (bar.width - 4f) * ratio, bar.height - 4f), Texture2D.whiteTexture);
            GUI.color = previous;

            GUI.Label(new Rect(bar.x, bar.y - 26f, bar.width, 22f), "BOSS", _bossStyle);
        }

        private Rect GetBackpackButtonRect()
        {
            float size = BackpackButtonSize;
            return new Rect(
                Screen.width - size - 14f,
                (BarHeight - size) * 0.5f,
                size,
                size);
        }

        private void DrawBackpackButton()
        {
            if (runManager != null && runManager.State != RunManager.RunState.Playing) return;

            float size = BackpackButtonSize;
            Rect buttonRect = GetBackpackButtonRect();

            Color previousBackground = GUI.backgroundColor;
            GUI.backgroundColor = _inventoryOpen
                ? new Color(0.42f, 0.62f, 0.82f, 1f)
                : new Color(0.20f, 0.24f, 0.32f, 1f);

            if (GUI.Button(buttonRect, GUIContent.none)) ToggleInventory();

            GUI.backgroundColor = previousBackground;

            if (backpackIcon != null && backpackIcon.texture != null)
            {
                GUI.DrawTexture(
                    new Rect(buttonRect.x + 4f, buttonRect.y + 4f, buttonRect.width - 8f, buttonRect.height - 8f),
                    backpackIcon.texture,
                    ScaleMode.ScaleToFit,
                    true);
            }

            GUI.Label(
                new Rect(buttonRect.x - 24f, buttonRect.y + (size - 16f) * 0.5f, 18f, 16f),
                "B",
                _keyHintStyle);
        }

        private void DrawInventoryPanel()
        {
            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.62f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previous;

            float width = Mathf.Min(720f, Screen.width - 100f);
            float height = Mathf.Min(560f, Screen.height - 100f);
            var panel = new Rect(
                (Screen.width - width) * 0.5f,
                (Screen.height - height) * 0.5f,
                width,
                height);

            // 先铺一层不透明底色，避免大面板透出地板显得杂乱
            Color panelColor = GUI.color;
            GUI.color = new Color(0.07f, 0.075f, 0.105f, 0.98f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = panelColor;
            GUI.Box(panel, GUIContent.none);

            if (backpackIcon != null && backpackIcon.texture != null)
            {
                GUI.DrawTexture(
                    new Rect(panel.x + 20f, panel.y + 14f, 40f, 40f),
                    backpackIcon.texture,
                    ScaleMode.ScaleToFit,
                    true);
            }

            GUI.Label(new Rect(panel.x + 70f, panel.y + 10f, panel.width - 90f, 34f), "背包", _inventoryTitleStyle);

            int count = inventory != null ? inventory.Items.Count : 0;
            GUI.Label(
                new Rect(panel.x + 72f, panel.y + 40f, panel.width - 90f, 20f),
                $"已持有 {count} 件道具",
                _inventoryCountStyle);

            var listRect = new Rect(panel.x + 18f, panel.y + 72f, panel.width - 36f, panel.height - 72f - 64f);

            if (count == 0)
            {
                GUI.Label(
                    listRect,
                    "背包是空的。\n去宝箱房开箱，或在商店购买道具吧。",
                    _inventoryHintStyle);
            }
            else
            {
                IReadOnlyList<ItemDefinition> items = inventory.Items;
                float contentHeight = Mathf.Max(listRect.height, items.Count * InventoryRowHeight + 4f);
                var content = new Rect(0f, 0f, listRect.width - 18f, contentHeight);

                _inventoryScroll = GUI.BeginScrollView(listRect, _inventoryScroll, content);

                for (int i = 0; i < items.Count; i++)
                    DrawInventoryRow(items[i], content.width, i * InventoryRowHeight);

                GUI.EndScrollView();
            }

            if (GUI.Button(new Rect(panel.center.x - 80f, panel.yMax - 52f, 160f, 38f), "关闭 (B)"))
                CloseInventory();
        }

        private void DrawInventoryRow(ItemDefinition item, float width, float y)
        {
            var row = new Rect(0f, y, width, InventoryRowHeight - 10f);

            Color previous = GUI.color;
            GUI.color = new Color(0.12f, 0.13f, 0.18f, 0.92f);
            GUI.DrawTexture(row, Texture2D.whiteTexture);

            // 左侧稀有度色条
            GUI.color = RarityColor(item.rarity);
            GUI.DrawTexture(new Rect(row.x, row.y, 4f, row.height), Texture2D.whiteTexture);
            GUI.color = previous;

            var iconRect = new Rect(row.x + 14f, row.y + 14f, 52f, 52f);
            if (item.icon != null && item.icon.texture != null)
            {
                GUI.DrawTexture(iconRect, item.icon.texture, ScaleMode.ScaleToFit, true);
            }
            else
            {
                Color previousColor = GUI.color;
                GUI.color = item.color;
                GUI.DrawTexture(iconRect, Texture2D.whiteTexture);
                GUI.color = previousColor;
            }

            GUI.Label(
                new Rect(row.x + 80f, row.y + 8f, row.width - 96f, 26f),
                item.itemName,
                _inventoryNameStyle);

            string description = string.IsNullOrWhiteSpace(item.description)
                ? "没有详细说明。"
                : item.description;
            GUI.Label(
                new Rect(row.x + 80f, row.y + 34f, row.width - 96f, row.height - 40f),
                description,
                _inventoryDescStyle);
        }

        private static Color RarityColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Rare: return new Color(0.36f, 0.62f, 0.98f);
                case ItemRarity.Legendary: return new Color(0.96f, 0.78f, 0.28f);
                default: return new Color(0.55f, 0.58f, 0.66f);
            }
        }

        private static bool WasBackpackPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null) return keyboard.bKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.B);
#else
            return false;
#endif
        }
    }
}
