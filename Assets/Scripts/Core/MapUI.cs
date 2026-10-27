using Roguelike.Combat;
using Roguelike.Dungeon;
using Roguelike.Player;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Roguelike.Core
{
    /// <summary>
    /// Tab 地图：显示已探索房间、相邻未探索房间、房间类型、邻接关系与当前位置。
    /// 按住 Tab 显示，松开隐藏。完全未接触的远处房间不会出现在地图上。
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

        private const float CellSize = 26f;
        private const float CellGap = 4f;
        private const float Padding = 18f;
        private const float HeaderHeight = 30f;

        private GUIStyle _titleStyle;
        private GUIStyle _hintStyle;

        private void Awake()
        {
            if (builder == null) builder = FindFirstObjectByType<DungeonBuilder>();
            if (tracker == null) tracker = FindFirstObjectByType<PlayerRoomTracker>();
            if (runManager == null) runManager = FindFirstObjectByType<RunManager>();
        }

        private void OnGUI()
        {
            if (!ForceVisible && !IsTabHeld()) return;
            if (CombatDebugHud.IsInventoryOpen) return;
            if (builder == null || builder.Data == null) return;
            if (runManager != null && runManager.State != RunManager.RunState.Playing) return;

            DrawMap();
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

            float width = (maxX - minX + 1) * (CellSize + CellGap) - CellGap + Padding * 2f;
            float height = (maxY - minY + 1) * (CellSize + CellGap) - CellGap + Padding * 2f + HeaderHeight;

            var panel = new Rect(
                (Screen.width - width) * 0.5f,
                (Screen.height - height) * 0.5f,
                width,
                height);

            Color previous = GUI.color;
            GUI.color = new Color(0.04f, 0.04f, 0.06f, 0.92f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = previous;

            EnsureStyles();

            GUI.Label(new Rect(panel.x, panel.y + 6f, panel.width, 22f), "地图", _titleStyle);

            // 先画连线，再画房间，避免线盖住方块
            DrawConnections(panel, minX, maxY);

            for (int i = 0; i < rooms.Count; i++)
            {
                RoomView room = rooms[i];
                if (!IsVisible(room)) continue;

                Rect cell = CellRect(room, panel, minX, maxY);

                GUI.color = room.IsVisited
                    ? RoomTypePalette.Accent(room.RoomType)
                    : new Color(0.08f, 0.09f, 0.12f, 0.95f);
                GUI.DrawTexture(cell, Texture2D.whiteTexture);
                GUI.color = previous;

                if (tracker != null && tracker.CurrentRoom == room)
                    DrawOutline(cell, Color.white, 2f);

                DrawRoomIcon(room, cell);
            }

            GUI.Label(new Rect(panel.x, panel.yMax - 24f, panel.width, 20f), "松开 Tab 关闭", _hintStyle);
        }

        private void DrawConnections(Rect panel, int minX, int maxY)
        {
            var rooms = builder.Rooms;

            for (int i = 0; i < rooms.Count; i++)
            {
                RoomView room = rooms[i];
                if (!IsVisible(room)) continue;

                RoomNode node = builder.Data.GetRoom(room.GridPosition);
                if (node == null) continue;

                Rect a = CellRect(room, panel, minX, maxY);

                for (int n = 0; n < node.Neighbors.Count; n++)
                {
                    Vector2Int neighborGrid = node.Neighbors[n].Grid;

                    // 每条边只画一次
                    if (neighborGrid.x < room.GridPosition.x) continue;
                    if (neighborGrid.x == room.GridPosition.x && neighborGrid.y < room.GridPosition.y) continue;

                    RoomView other = builder.GetRoom(neighborGrid);
                    if (other == null || !IsVisible(other)) continue;

                    Rect b = CellRect(other, panel, minX, maxY);

                    GUI.color = room.IsVisited && other.IsVisited
                        ? new Color(0.55f, 0.57f, 0.65f, 0.9f)
                        : new Color(0.34f, 0.36f, 0.44f, 0.5f);

                    if (neighborGrid.x > room.GridPosition.x)
                    {
                        GUI.DrawTexture(new Rect(a.xMax, a.center.y - 2f, CellGap, 4f), Texture2D.whiteTexture);
                    }
                    else
                    {
                        GUI.DrawTexture(new Rect(a.center.x - 2f, b.yMax, 4f, CellGap), Texture2D.whiteTexture);
                    }

                    GUI.color = Color.white;
                }
            }
        }

        /// <summary>已探索房间和与已探索房间相邻的未探索房间会显示在地图上。</summary>
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

        private void DrawRoomIcon(RoomView room, Rect cell)
        {
            Sprite icon = room.IsVisited ? GetRoomIcon(room) : unexploredIcon;
            if (icon == null || icon.texture == null) return;

            float size = CellSize - 8f;
            var iconRect = new Rect(
                cell.center.x - size * 0.5f,
                cell.center.y - size * 0.5f,
                size,
                size);

            Color previous = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(iconRect, icon.texture, ScaleMode.ScaleToFit, true);
            GUI.color = previous;
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

        private static Rect CellRect(RoomView room, Rect panel, int minX, int maxY)
        {
            float x = panel.x + Padding + (room.GridPosition.x - minX) * (CellSize + CellGap);
            float y = panel.y + HeaderHeight + Padding + (maxY - room.GridPosition.y) * (CellSize + CellGap);
            return new Rect(x, y, CellSize, CellSize);
        }

        private static void DrawOutline(Rect rect, Color color, float thickness)
        {
            Color previous = GUI.color;
            GUI.color = color;

            GUI.DrawTexture(new Rect(rect.x - thickness, rect.y - thickness, rect.width + thickness * 2f, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x - thickness, rect.yMax, rect.width + thickness * 2f, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x - thickness, rect.y - thickness, thickness, rect.height + thickness * 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax, rect.y - thickness, thickness, rect.height + thickness * 2f), Texture2D.whiteTexture);

            GUI.color = previous;
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null) return;

            _titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
            _titleStyle.normal.textColor = Color.white;

            _hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleCenter };
            _hintStyle.normal.textColor = new Color(0.75f, 0.77f, 0.85f);
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
