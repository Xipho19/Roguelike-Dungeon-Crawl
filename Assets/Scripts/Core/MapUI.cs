using Roguelike.Combat;
using Roguelike.Dungeon;
using Roguelike.Player;
using Roguelike.UI;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Roguelike.Core
{
    /// <summary>
    /// Tab 地图：显示已探索房间、相邻未探索房间、房间类型、邻接关系与当前位置。
    /// 按住 Tab 显示，松开隐藏；视觉使用统一的像素地牢面板。
    /// </summary>
    public sealed class MapUI : MonoBehaviour
    {
        [SerializeField] private DungeonBuilder builder;
        [SerializeField] private PlayerRoomTracker tracker;
        [SerializeField] private RunManager runManager;

        [Header("地图图标")]
        [SerializeField] private Sprite shopIcon;
        [SerializeField] private Sprite treasureIcon;
        [SerializeField] private Sprite bossIcon;
        [SerializeField] private Sprite exploredIcon;
        [SerializeField] private Sprite unexploredIcon;
        [SerializeField] private Sprite startIcon;

        [Tooltip("调试用：强制显示地图（Tab 之外）")]
        public bool ForceVisible;

        private const float CellSize = 30f;
        private const float CellGap = 6f;
        private const float Padding = 20f;
        private const float HeaderHeight = 58f;
        private const float FooterHeight = 26f;

        public static bool IsOpen { get; private set; }

        private void Awake()
        {
            if (builder == null) builder = FindFirstObjectByType<DungeonBuilder>();
            if (tracker == null) tracker = FindFirstObjectByType<PlayerRoomTracker>();
            if (runManager == null) runManager = FindFirstObjectByType<RunManager>();
        }

        private void Update()
        {
            IsOpen = ShouldShow();
        }

        private void OnDisable()
        {
            IsOpen = false;
        }

        private void OnGUI()
        {
            if (!ShouldShow()) return;
            IsOpen = true;
            DrawMap();
        }

        private bool ShouldShow()
        {
            if (CombatDebugHud.IsInventoryOpen) return false;
            if (builder == null || builder.Data == null) return false;
            if (runManager != null && runManager.State != RunManager.RunState.Playing) return false;
            return ForceVisible || IsTabHeld();
        }

        private void DrawMap()
        {
            var rooms = builder.Rooms;

            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;
            bool any = false;

            for (int i = 0; i < rooms.Count; i++)
            {
                RoomView room = rooms[i];
                if (!IsVisible(room)) continue;

                any = true;
                minX = Mathf.Min(minX, room.GridPosition.x);
                minY = Mathf.Min(minY, room.GridPosition.y);
                maxX = Mathf.Max(maxX, room.GridPosition.x);
                maxY = Mathf.Max(maxY, room.GridPosition.y);
            }

            if (!any) return;

            float contentWidth = (maxX - minX + 1) * (CellSize + CellGap) - CellGap;
            float contentHeight = (maxY - minY + 1) * (CellSize + CellGap) - CellGap;
            float width = Mathf.Max(440f, contentWidth + Padding * 2f);
            float height = Mathf.Max(280f, contentHeight + Padding * 2f + HeaderHeight + FooterHeight);

            var panel = new Rect(
                (Screen.width - width) * 0.5f,
                (Screen.height - height) * 0.5f,
                width,
                height);

            var contentRect = new Rect(
                panel.x + (panel.width - contentWidth) * 0.5f,
                panel.y + HeaderHeight + (panel.height - HeaderHeight - FooterHeight - contentHeight) * 0.5f,
                contentWidth,
                contentHeight);

            UiTheme.DrawDim();
            UiTheme.DrawPanel(panel, UiTheme.Gold, false);

            UiTheme.DrawIconSlot(
                new Rect(panel.x + 16f, panel.y + 12f, 36f, 36f),
                startIcon != null ? startIcon : exploredIcon,
                UiTheme.Gold);

            UiTheme.DrawLabelShadow(
                new Rect(panel.x + 62f, panel.y + 9f, panel.width - 80f, 26f),
                "地牢地图",
                UiTheme.Title,
                UiTheme.Text);

            string subtitle = runManager != null
                ? $"第 {runManager.CurrentFloor} 层 · {runManager.CurrentThemeName}"
                : "未知楼层";
            UiTheme.DrawLabelShadow(
                new Rect(panel.x + 64f, panel.y + 35f, panel.width - 80f, 16f),
                subtitle,
                UiTheme.Small,
                UiTheme.Muted);

            UiTheme.DrawDivider(new Rect(panel.x + 16f, panel.y + HeaderHeight - 4f, panel.width - 32f, 1f));

            DrawConnections(contentRect, minX, maxY);

            for (int i = 0; i < rooms.Count; i++)
            {
                RoomView room = rooms[i];
                if (!IsVisible(room)) continue;

                Rect cell = CellRect(room, contentRect, minX, maxY);
                bool visited = room.IsVisited;
                bool current = tracker != null && tracker.CurrentRoom == room;

                Color accent = RoomTypePalette.Accent(room.RoomType);
                Color fill = visited
                    ? Color.Lerp(accent, UiTheme.Inset, 0.72f)
                    : new Color(0.055f, 0.05f, 0.07f, 1f);

                UiTheme.DrawInset(cell, fill);
                UiTheme.DrawOutline(
                    cell,
                    current ? UiTheme.GoldBright : visited ? new Color(accent.r, accent.g, accent.b, 0.55f) : UiTheme.GoldDim,
                    current ? 2f : 1f);

                if (current)
                {
                    UiTheme.DrawOutline(
                        new Rect(cell.x - 3f, cell.y - 3f, cell.width + 6f, cell.height + 6f),
                        new Color(UiTheme.GoldBright.r, UiTheme.GoldBright.g, UiTheme.GoldBright.b, 0.28f),
                        1f);
                }

                DrawRoomIcon(room, cell, visited);
            }

            DrawLegend(panel);

            UiTheme.DrawLabelShadow(
                new Rect(panel.x, panel.yMax - FooterHeight + 4f, panel.width, 18f),
                "按住 TAB 查看 · 松开隐藏",
                UiTheme.Center,
                UiTheme.Muted);
        }

        private void DrawLegend(Rect panel)
        {
            float y = panel.yMax - FooterHeight - 24f;
            float x = panel.x + 20f;
            UiTheme.DrawDivider(new Rect(panel.x + 20f, y - 6f, panel.width - 40f, 1f));

            x = DrawLegendItem(x, y, shopIcon, "商店");
            x = DrawLegendItem(x, y, treasureIcon, "宝箱");
            x = DrawLegendItem(x, y, bossIcon, "Boss");
            x = DrawLegendItem(x, y, startIcon, "起点");
            DrawLegendItem(x, y, unexploredIcon, "未知");
        }

        private static float DrawLegendItem(float x, float y, Sprite icon, string label)
        {
            if (icon != null && icon.texture != null)
            {
                UiTheme.DrawIcon(new Rect(x, y, 16f, 16f), icon, new Color(0.85f, 0.85f, 0.90f, 0.9f));
                x += 19f;
            }

            UiTheme.DrawLabelShadow(new Rect(x, y - 2f, 46f, 18f), label, UiTheme.Small, UiTheme.Muted);
            return x + 58f;
        }

        private void DrawConnections(Rect contentRect, int minX, int maxY)
        {
            var rooms = builder.Rooms;

            for (int i = 0; i < rooms.Count; i++)
            {
                RoomView room = rooms[i];
                if (!IsVisible(room)) continue;

                RoomNode node = builder.Data.GetRoom(room.GridPosition);
                if (node == null) continue;

                Rect a = CellRect(room, contentRect, minX, maxY);

                for (int n = 0; n < node.Neighbors.Count; n++)
                {
                    Vector2Int neighborGrid = node.Neighbors[n].Grid;

                    if (neighborGrid.x < room.GridPosition.x) continue;
                    if (neighborGrid.x == room.GridPosition.x && neighborGrid.y < room.GridPosition.y) continue;

                    RoomView other = builder.GetRoom(neighborGrid);
                    if (other == null || !IsVisible(other)) continue;

                    Rect b = CellRect(other, contentRect, minX, maxY);
                    Color link = room.IsVisited && other.IsVisited
                        ? new Color(0.70f, 0.58f, 0.32f, 0.85f)
                        : new Color(0.34f, 0.30f, 0.28f, 0.65f);

                    if (neighborGrid.x > room.GridPosition.x)
                    {
                        UiTheme.Fill(new Rect(a.xMax, a.center.y - 1f, CellGap, 3f), link);
                    }
                    else
                    {
                        UiTheme.Fill(new Rect(a.center.x - 1f, b.yMax, 3f, CellGap), link);
                    }
                }
            }
        }

        private bool IsVisible(RoomView room)
        {
            if (room == null) return false;
            if (room.IsVisited) return true;
            if (builder == null || builder.Data == null) return false;

            RoomNode node = builder.Data.GetRoom(room.GridPosition);
            if (node == null) return false;

            for (int i = 0; i < node.Neighbors.Count; i++)
            {
                RoomView neighbor = builder.GetRoom(node.Neighbors[i].Grid);
                if (neighbor != null && neighbor.IsVisited) return true;
            }

            return false;
        }

        private void DrawRoomIcon(RoomView room, Rect cell, bool visited)
        {
            Sprite icon = visited ? GetRoomIcon(room) : unexploredIcon;
            if (icon == null || icon.texture == null) return;

            float size = cell.width - 10f;
            UiTheme.DrawIcon(
                new Rect(cell.center.x - size * 0.5f, cell.center.y - size * 0.5f, size, size),
                icon,
                visited ? Color.white : new Color(0.62f, 0.62f, 0.70f, 0.9f));
        }

        private Sprite GetRoomIcon(RoomView room)
        {
            switch (room.RoomType)
            {
                case RoomType.Start:
                    return startIcon != null ? startIcon : exploredIcon;
                case RoomType.Shop:
                    return shopIcon != null ? shopIcon : exploredIcon;
                case RoomType.Treasure:
                    return treasureIcon != null ? treasureIcon : exploredIcon;
                case RoomType.Boss:
                    return bossIcon != null ? bossIcon : exploredIcon;
                default:
                    return exploredIcon;
            }
        }

        private static Rect CellRect(RoomView room, Rect contentRect, int minX, int maxY)
        {
            float x = contentRect.x + (room.GridPosition.x - minX) * (CellSize + CellGap);
            float y = contentRect.y + (maxY - room.GridPosition.y) * (CellSize + CellGap);
            return new Rect(x, y, CellSize, CellSize);
        }

        private static bool IsTabHeld()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null) return keyboard.tabKey.isPressed;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(KeyCode.Tab);
#else
            return false;
#endif
        }
    }
}
