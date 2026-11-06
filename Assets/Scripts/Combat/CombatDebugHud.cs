using System.Collections.Generic;
using Roguelike.Core;
using Roguelike.Dungeon;
using Roguelike.Items;
using Roguelike.Player;
using Roguelike.UI;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Roguelike.Combat
{
    /// <summary>
    /// 战斗 HUD：顶部状态栏、楼层横幅、拾取提示、Boss 血条和背包面板。
    /// 视觉统一使用 UiTheme 的像素地牢面板。
    /// </summary>
    public sealed class CombatDebugHud : MonoBehaviour
    {
        [SerializeField] private PlayerHealth health;
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private RunManager runManager;
        [SerializeField] private DungeonFlowController flow;

        [Header("状态栏图标")]
        [SerializeField] private Sprite floorIcon;
        [SerializeField] private Sprite heartIcon;
        [SerializeField] private Sprite shieldIcon;
        [SerializeField] private Sprite coinIcon;
        [SerializeField] private Sprite backpackIcon;

        private const float BarHeight = 58f;
        private const float BackpackButtonSize = 40f;
        private const int InventoryColumns = 6;
        private const float InventoryCellHeight = 78f;
        private const float InventoryCellGap = 10f;

        private string _toastText;
        private float _toastUntil;

        private string _floorToastText;
        private float _floorToastUntil;
        private int _lastShownFloor = -1;
        private string _lastShownTheme;

        private bool _inventoryOpen;
        private bool _pausedByInventory;
        private Vector2 _inventoryScroll;
        private ItemDefinition _hoveredInventoryItem;
        private static bool _anyInventoryOpen;

        public static bool IsInventoryOpen => _anyInventoryOpen;
        public bool IsOpen => _inventoryOpen;

        private void Awake()
        {
            if (health == null) health = FindFirstObjectByType<PlayerHealth>();
            if (wallet == null) wallet = FindFirstObjectByType<PlayerWallet>();
            if (inventory == null) inventory = FindFirstObjectByType<PlayerInventory>();
            if (runManager == null) runManager = FindFirstObjectByType<RunManager>();
            if (flow == null) flow = FindFirstObjectByType<DungeonFlowController>();

            if (inventory != null) inventory.ItemAdded += HandleItemAdded;
            if (flow != null)
            {
                flow.ChallengeWaveStarted += HandleChallengeWaveStarted;
                flow.ChallengeCompleted += HandleChallengeCompleted;
            }
        }

        private void OnDestroy()
        {
            if (inventory != null) inventory.ItemAdded -= HandleItemAdded;
            if (flow != null)
            {
                flow.ChallengeWaveStarted -= HandleChallengeWaveStarted;
                flow.ChallengeCompleted -= HandleChallengeCompleted;
            }
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

        private void HandleChallengeWaveStarted(int wave, int total)
        {
            ShowToast($"挑战房：第 {wave}/{total} 波", 2f);
        }

        private void HandleChallengeCompleted()
        {
            ShowToast("挑战完成！奖励已出现", 2.5f);
        }

        public void ShowToast(string text, float duration = 3f)
        {
            _toastText = text;
            _toastUntil = Time.time + Mathf.Max(0.1f, duration);
        }

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

        public void ToggleInventory()
        {
            if (_inventoryOpen)
            {
                CloseInventory();
                return;
            }

            if (runManager != null && runManager.State != RunManager.RunState.Playing) return;
            if (Time.timeScale <= 0.001f) return;

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

        public void CloseInventory()
        {
            if (!_inventoryOpen) return;

            _inventoryOpen = false;
            _anyInventoryOpen = false;

            if (_pausedByInventory) Time.timeScale = 1f;
            _pausedByInventory = false;
        }

        private void OnGUI()
        {
            if (health == null) return;

            DrawStatusBar();
            DrawFloorIntro();
            DrawToast();
            DrawBossBar();
            DrawBackpackButton();

            if (_inventoryOpen) DrawInventoryPanel();
        }

        private void DrawStatusBar()
        {
            var bar = new Rect(0f, 0f, Screen.width, BarHeight);
            UiTheme.DrawTopBar(bar);

            const float slot = 34f;
            float y = (BarHeight - slot) * 0.5f;
            float x = 12f;

            int floor = runManager != null ? runManager.CurrentFloor : 1;
            string floorText = runManager != null && runManager.MaxFloor > 0
                ? $"{floor}/{runManager.MaxFloor}"
                : floor.ToString();
            string themeText = runManager != null ? runManager.CurrentThemeName : "地牢";

            UiTheme.DrawIconSlot(new Rect(x, y, slot, slot), floorIcon, UiTheme.Gold);
            x += slot + 8f;
            UiTheme.DrawLabelShadow(new Rect(x, 6f, 110f, 24f), floorText, UiTheme.Value, UiTheme.GoldBright);
            UiTheme.DrawLabelShadow(new Rect(x, 31f, 150f, 18f), themeText, UiTheme.Small, UiTheme.Muted);
            x += Mathf.Max(58f, UiTheme.Small.CalcSize(new GUIContent(themeText)).x + 6f);
            x += 12f;

            UiTheme.DrawDivider(new Rect(x, y + 3f, 1f, slot - 6f), UiTheme.GoldDim);
            x += 14f;

            float healthRatio = health.MaxHealth > 0 ? health.Health / (float)health.MaxHealth : 0f;
            float shieldRatio = health.MaxShield > 0 ? health.Shield / (float)health.MaxShield : 0f;

            x = DrawVital(x, y, slot, heartIcon, "生命", $"{health.Health}/{health.MaxHealth}", healthRatio, UiTheme.Health, 100f);
            x = DrawVital(x, y, slot, shieldIcon, "护盾", $"{health.Shield}/{health.MaxShield}", shieldRatio, UiTheme.Shield, 92f);

            UiTheme.DrawIconSlot(new Rect(x, y, slot, slot), coinIcon, UiTheme.Coin);
            x += slot + 8f;
            int coins = wallet != null ? wallet.Coins : 0;
            UiTheme.DrawLabelShadow(new Rect(x, 7f, 92f, 24f), coins.ToString(), UiTheme.Value, UiTheme.Coin);
            UiTheme.DrawLabelShadow(new Rect(x, 32f, 92f, 16f), "金币", UiTheme.Small, UiTheme.Muted);
        }

        private static float DrawVital(
            float x,
            float y,
            float slot,
            Sprite icon,
            string label,
            string value,
            float ratio,
            Color color,
            float width)
        {
            UiTheme.DrawIconSlot(new Rect(x, y, slot, slot), icon, color);
            float textX = x + slot + 8f;

            UiTheme.DrawLabelShadow(new Rect(textX, 3f, width, 15f), label, UiTheme.Small, UiTheme.Muted);
            UiTheme.DrawLabelShadow(new Rect(textX, 17f, width, 20f), value, UiTheme.Value, UiTheme.Text);
            UiTheme.DrawBar(new Rect(textX, 39f, width, 7f), ratio, color);

            return textX + width + 18f;
        }

        private bool FloorIntroVisible =>
            !_inventoryOpen &&
            !MapUI.IsOpen &&
            Time.timeScale > 0.001f &&
            !string.IsNullOrEmpty(_floorToastText) &&
            Time.time < _floorToastUntil;

        private void DrawFloorIntro()
        {
            if (!FloorIntroVisible) return;

            var panel = new Rect(Screen.width * 0.5f - 210f, BarHeight + 12f, 420f, 56f);
            UiTheme.DrawPanel(panel, UiTheme.Gold, false);
            UiTheme.DrawLabelShadow(
                new Rect(panel.x + 10f, panel.y + 8f, panel.width - 20f, 24f),
                _floorToastText,
                UiTheme.FloorTitle,
                UiTheme.GoldBright);
            UiTheme.DrawLabelShadow(
                new Rect(panel.x + 10f, panel.y + 33f, panel.width - 20f, 14f),
                "DUNGEON",
                UiTheme.Tiny,
                UiTheme.Muted);
        }

        private void DrawToast()
        {
            if (_inventoryOpen || MapUI.IsOpen || Time.timeScale <= 0.001f) return;
            if (string.IsNullOrEmpty(_toastText) || Time.time >= _toastUntil) return;

            float top = BarHeight + 14f + (FloorIntroVisible ? 66f : 0f);
            var panel = new Rect(Screen.width * 0.5f - 225f, top, 450f, 66f);
            UiTheme.DrawPanel(panel, UiTheme.GoldDim, false);
            UiTheme.DrawLabelShadow(
                new Rect(panel.x + 14f, panel.y + 8f, panel.width - 28f, panel.height - 16f),
                _toastText,
                UiTheme.Toast,
                UiTheme.Text);
        }

        private void DrawBossBar()
        {
            if (MapUI.IsOpen) return;
            if (Time.timeScale <= 0.001f) return;
            if (runManager != null && runManager.State != RunManager.RunState.Playing) return;

            BossEnemy boss = BossEnemy.Active;
            if (boss == null || boss.IsDead || !boss.CombatActive) return;

            float ratio = boss.MaxHealth > 0f ? Mathf.Clamp01(boss.Health / boss.MaxHealth) : 0f;
            var panel = new Rect(Screen.width * 0.5f - 310f, Screen.height - 68f, 620f, 50f);
            UiTheme.DrawPanel(panel, UiTheme.Danger, false);

            var bar = new Rect(panel.x + 14f, panel.y + 16f, panel.width - 28f, 20f);
            UiTheme.DrawBar(bar, ratio, UiTheme.Health);

            UiTheme.DrawLabelShadow(
                new Rect(panel.x, panel.y - 25f, panel.width, 20f),
                "◆  BOSS  ◆",
                UiTheme.Boss,
                UiTheme.GoldBright);

            string healthText = $"{Mathf.CeilToInt(boss.Health)} / {Mathf.CeilToInt(boss.MaxHealth)}";
            var healthRect = new Rect(panel.x + panel.width - 154f, panel.y - 27f, 140f, 22f);
            UiTheme.DrawInset(healthRect, new Color(0.045f, 0.035f, 0.05f, 0.96f));
            UiTheme.DrawLabelShadow(healthRect, healthText, UiTheme.Boss, UiTheme.GoldBright);
        }

        private Rect GetBackpackButtonRect()
        {
            return new Rect(
                Screen.width - BackpackButtonSize - 14f,
                (BarHeight - BackpackButtonSize) * 0.5f,
                BackpackButtonSize,
                BackpackButtonSize);
        }

        private void DrawBackpackButton()
        {
            if (runManager != null && runManager.State != RunManager.RunState.Playing) return;

            Rect buttonRect = GetBackpackButtonRect();
            if (UiTheme.DrawIconButton(buttonRect, backpackIcon, _inventoryOpen))
                ToggleInventory();

            UiTheme.DrawKeyBadge(new Rect(buttonRect.x - 29f, buttonRect.y + 11f, 22f, 18f), "B");

            if (Screen.width > 760)
            {
                UiTheme.DrawLabelShadow(
                    new Rect(buttonRect.x - 122f, buttonRect.y + 9f, 88f, 20f),
                    "TAB 地图",
                    UiTheme.Small,
                    UiTheme.Muted);
            }
        }

        private void DrawInventoryPanel()
        {
            UiTheme.DrawDim();

            float width = Mathf.Min(780f, Screen.width - 48f);
            float height = Mathf.Min(560f, Screen.height - 28f);
            var panel = new Rect(
                (Screen.width - width) * 0.5f,
                (Screen.height - height) * 0.5f,
                width,
                height);

            UiTheme.DrawPanel(panel, UiTheme.Gold, false);

            UiTheme.DrawIconSlot(new Rect(panel.x + 18f, panel.y + 14f, 44f, 44f), backpackIcon, UiTheme.Gold);
            UiTheme.DrawLabelShadow(
                new Rect(panel.x + 74f, panel.y + 10f, panel.width - 100f, 30f),
                "背包",
                UiTheme.Title,
                UiTheme.Text);

            int count = inventory != null ? inventory.Items.Count : 0;
            UiTheme.DrawLabelShadow(
                new Rect(panel.x + 76f, panel.y + 39f, panel.width - 100f, 18f),
                $"已收藏 {count} 件道具",
                UiTheme.Small,
                UiTheme.Muted);

            float legendX = panel.xMax - 214f;
            UiTheme.DrawRarityChip(new Rect(legendX, panel.y + 22f, 60f, 18f), ItemRarity.Common);
            UiTheme.DrawRarityChip(new Rect(legendX + 66f, panel.y + 22f, 60f, 18f), ItemRarity.Rare);
            UiTheme.DrawRarityChip(new Rect(legendX + 132f, panel.y + 22f, 64f, 18f), ItemRarity.Legendary);

            UiTheme.DrawDivider(new Rect(panel.x + 18f, panel.y + 66f, panel.width - 36f, 1f));

            var listRect = new Rect(panel.x + 18f, panel.y + 76f, panel.width - 36f, panel.height - 76f - 56f);

            if (count == 0)
            {
                UiTheme.DrawInset(listRect);
                UiTheme.DrawLabelShadow(
                    listRect,
                    "背包是空的。\n去宝箱房开箱，或在商店购买道具吧。",
                    UiTheme.Center,
                    UiTheme.Muted);
            }
            else
            {
                IReadOnlyList<ItemDefinition> items = inventory.Items;
                int rows = Mathf.CeilToInt(items.Count / (float)InventoryColumns);
                float contentHeight = Mathf.Max(listRect.height, rows * InventoryCellHeight + 4f);
                var content = new Rect(0f, 0f, listRect.width - 18f, contentHeight);

                _hoveredInventoryItem = null;
                _inventoryScroll = GUI.BeginScrollView(listRect, _inventoryScroll, content);

                float cellWidth = (content.width - InventoryCellGap * (InventoryColumns - 1)) / InventoryColumns;

                for (int i = 0; i < items.Count; i++)
                {
                    int column = i % InventoryColumns;
                    int row = i / InventoryColumns;
                    var cell = new Rect(
                        column * (cellWidth + InventoryCellGap),
                        row * InventoryCellHeight,
                        cellWidth,
                        InventoryCellHeight - InventoryCellGap);

                    DrawInventoryCell(items[i], cell);
                }

                GUI.EndScrollView();

                if (_hoveredInventoryItem != null)
                    DrawInventoryTooltip(_hoveredInventoryItem);
            }

            if (UiTheme.DrawButton(
                    new Rect(panel.center.x - 80f, panel.yMax - 46f, 160f, 34f),
                    "关闭  B",
                    false,
                    false,
                    14))
            {
                CloseInventory();
            }

            UiTheme.DrawLabelShadow(
                new Rect(panel.x + 20f, panel.yMax - 42f, 220f, 24f),
                "滚轮浏览 · 悬停查看效果",
                UiTheme.Small,
                UiTheme.Muted);
        }

        private void DrawInventoryCell(ItemDefinition item, Rect cell)
        {
            bool hovered = Event.current.type == EventType.Repaint && cell.Contains(Event.current.mousePosition);
            if (hovered) _hoveredInventoryItem = item;

            UiTheme.DrawInset(cell, hovered ? UiTheme.PanelRaised : UiTheme.Inset);
            if (hovered) UiTheme.DrawOutline(cell, UiTheme.GoldBright, 1f);

            UiTheme.Fill(new Rect(cell.x + 2f, cell.y + 2f, 3f, cell.height - 4f), UiTheme.RarityColor(item.rarity));
            UiTheme.Fill(new Rect(cell.xMax - 7f, cell.y + 5f, 3f, 3f), UiTheme.RarityColor(item.rarity));

            const float iconSize = 42f;
            var iconRect = new Rect(
                cell.x + (cell.width - iconSize) * 0.5f,
                cell.y + 8f,
                iconSize,
                iconSize);

            if (item.icon != null && item.icon.texture != null)
            {
                UiTheme.DrawIcon(iconRect, item.icon);
            }
            else
            {
                UiTheme.Fill(iconRect, item.color);
                UiTheme.DrawOutline(iconRect, new Color(0f, 0f, 0f, 0.45f), 1f);
            }

            UiTheme.DrawLabelShadow(
                new Rect(cell.x + 4f, cell.y + 53f, cell.width - 8f, 16f),
                item.itemName,
                UiTheme.CellName,
                UiTheme.Text);
        }

        private void DrawInventoryTooltip(ItemDefinition item)
        {
            if (Event.current.type != EventType.Repaint) return;

            const float width = 350f;
            const float height = 156f;

            Vector2 mouse = Event.current.mousePosition;
            float x = mouse.x + 18f;
            float y = mouse.y + 18f;

            if (x + width > Screen.width - 8f) x = Screen.width - width - 8f;
            if (y + height > Screen.height - 8f) y = mouse.y - height - 18f;
            x = Mathf.Max(8f, x);
            y = Mathf.Max(8f, y);

            var rect = new Rect(x, y, width, height);
            UiTheme.DrawPanel(rect, UiTheme.RarityColor(item.rarity), false);

            if (item.icon != null && item.icon.texture != null)
            {
                UiTheme.DrawIconSlot(new Rect(rect.x + 14f, rect.y + 16f, 44f, 44f), item.icon, UiTheme.RarityColor(item.rarity));
            }

            UiTheme.DrawLabelShadow(
                new Rect(rect.x + 68f, rect.y + 12f, rect.width - 82f, 26f),
                item.itemName,
                UiTheme.TooltipTitle,
                UiTheme.Text);

            UiTheme.DrawRarityChip(
                new Rect(rect.x + 68f, rect.y + 40f, 62f, 17f),
                item.rarity);

            string description = string.IsNullOrWhiteSpace(item.description)
                ? "没有详细说明。"
                : item.description;
            UiTheme.DrawLabelShadow(
                new Rect(rect.x + 14f, rect.y + 70f, rect.width - 28f, rect.height - 82f),
                description,
                UiTheme.TooltipBody,
                UiTheme.Text);
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
