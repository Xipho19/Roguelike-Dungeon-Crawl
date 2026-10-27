using System.Collections.Generic;
using Roguelike.Dungeon;
using UnityEditor;
using UnityEngine;

namespace Roguelike.EditorTools
{
    /// <summary>
    /// 房间图生成器的可视化调试窗口。
    /// 菜单：Tools / Roguelike / 房间图生成器
    ///
    /// 作用：在没有美术、没有场景的情况下，直接看到一局地牢的拓扑长什么样，
    /// 快速试参数、试种子，确认结构没问题后再去接房间 Prefab。
    /// </summary>
    public sealed class DungeonGeneratorWindow : EditorWindow
    {
        private const float CellSize = 44f;
        private const float CellGap = 4f;

        private readonly DungeonConfig _config = new DungeonConfig();
        private int _seed = 20261022;
        private DungeonData _data;
        private Vector2 _scroll;
        private Vector2Int? _selected;

        private bool _configFoldout = true;
        private bool _autoRegenerate = true;

        private GUIStyle _cellLabelStyle;

        [MenuItem("Tools/Roguelike/房间图生成器")]
        public static void Open()
        {
            var window = GetWindow<DungeonGeneratorWindow>("房间图生成器");
            window.minSize = new Vector2(620f, 640f);
            if (window.position.width < 700f)
                window.position = new Rect(window.position.x, window.position.y, 900f, 760f);
            window.Show();
        }

        private void OnEnable()
        {
            if (_data == null) Generate();
        }

        private void Generate()
        {
            _data = DungeonGenerator.Generate(_config, _seed);
            _selected = null;
            Repaint();
        }

        private void OnGUI()
        {
            DrawToolbar();

            _scroll = EditorGUILayout.BeginScrollView(_scroll, false, true);

            DrawConfig();
            DrawBody();

            EditorGUILayout.EndScrollView();
        }

        // ------------------------------------------------------------------
        // 顶部工具条
        // ------------------------------------------------------------------

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Seed", EditorStyles.miniLabel, GUILayout.Width(32));
                _seed = EditorGUILayout.IntField(_seed, EditorStyles.toolbarTextField, GUILayout.Width(100));
                if (GUILayout.Button("生成", EditorStyles.toolbarButton, GUILayout.Width(50))) Generate();
                if (GUILayout.Button("随机", EditorStyles.toolbarButton, GUILayout.Width(50)))
                {
                    // 仅用于点按钮，不参与生成逻辑，因此用系统时间做种子无妨
                    _seed = (int)(System.DateTime.Now.Ticks % 100000000L);
                    Generate();
                }

                GUILayout.Space(8);
                bool auto = GUILayout.Toggle(_autoRegenerate, "改参数即时刷新", EditorStyles.toolbarButton, GUILayout.Width(120));
                if (auto != _autoRegenerate)
                {
                    _autoRegenerate = auto;
                    if (auto) Generate();
                }

                GUILayout.FlexibleSpace();
            }
        }

        // ------------------------------------------------------------------
        // 参数面板（两列紧凑布局，避免挤占网格）
        // ------------------------------------------------------------------

        private void DrawConfig()
        {
            EditorGUILayout.Space(4);
            _configFoldout = EditorGUILayout.Foldout(_configFoldout, "生成参数", true, EditorStyles.foldoutHeader);
            if (!_configFoldout) return;

            float prevLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 76f;

            EditorGUI.BeginChangeCheck();
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.LabelField("网格", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _config.width = EditorGUILayout.IntSlider("宽度", _config.width, 3, 24);
                    _config.height = EditorGUILayout.IntSlider("高度", _config.height, 3, 24);
                }

                EditorGUILayout.LabelField("主路径", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _config.mainPathLength = EditorGUILayout.IntSlider("房间数", _config.mainPathLength, 2, 30);
                    _config.straightBias = EditorGUILayout.Slider("直行倾向", _config.straightBias, 0f, 1f);
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("避免 2×2 密堆", GUILayout.Width(112f));
                    _config.avoidDenseBlocks = EditorGUILayout.Toggle(_config.avoidDenseBlocks, GUILayout.Width(18f));
                }

                EditorGUILayout.LabelField("分支", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _config.branchCount = EditorGUILayout.IntSlider("分支数", _config.branchCount, 0, 15);
                    _config.branchLengthMin = EditorGUILayout.IntField("最短", _config.branchLengthMin, GUILayout.Width(92f));
                    _config.branchLengthMax = EditorGUILayout.IntField("最长", _config.branchLengthMax, GUILayout.Width(92f));
                }

                EditorGUILayout.LabelField("特殊房间", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _config.treasureCount = EditorGUILayout.IntField("宝箱", _config.treasureCount, GUILayout.Width(96f));
                    _config.shopCount = EditorGUILayout.IntField("商店", _config.shopCount, GUILayout.Width(96f));
                    _config.secretCount = EditorGUILayout.IntField("秘密", _config.secretCount, GUILayout.Width(96f));
                    GUILayout.FlexibleSpace();
                }
            }
            EditorGUIUtility.labelWidth = prevLabelWidth;

            if (EditorGUI.EndChangeCheck() && _autoRegenerate) Generate();
        }

        // ------------------------------------------------------------------
        // 主体：统计 / 图例 / 网格 / 选中信息 / 校验
        // ------------------------------------------------------------------

        private void DrawBody()
        {
            if (_data == null) return;

            EditorGUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label($"房间数 {_data.RoomCount}", EditorStyles.boldLabel, GUILayout.Width(80));
                GUILayout.Label($"最远 {_data.MaxDistance} 步", EditorStyles.miniLabel, GUILayout.Width(90));
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("复制种子", EditorStyles.miniButton, GUILayout.Width(68)))
                    EditorGUIUtility.systemCopyBuffer = _data.Seed.ToString();
                if (GUILayout.Button("打印 ASCII", EditorStyles.miniButton, GUILayout.Width(78)))
                    Debug.Log($"[Dungeon] seed={_data.Seed}\n{_data.ToDebugString()}");
            }

            DrawLegend();
            DrawGrid();
            DrawRoomInfo();
            DrawValidation();
            EditorGUILayout.Space(8);
        }

        private void DrawLegend()
        {
            EditorGUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope())
            {
                LegendItem(RoomType.Start, "起点");
                LegendItem(RoomType.Combat, "战斗");
                LegendItem(RoomType.Boss, "Boss");
                LegendItem(RoomType.Treasure, "宝箱");
                LegendItem(RoomType.Shop, "商店");
                LegendItem(RoomType.Challenge, "挑战");
                LegendItem(RoomType.Secret, "秘密");
                GUILayout.FlexibleSpace();
            }
        }

        private void LegendItem(RoomType type, string text)
        {
            Rect chip = GUILayoutUtility.GetRect(12f, 12f, GUILayout.Width(12f), GUILayout.Height(12f));
            chip.y += 3f;
            EditorGUI.DrawRect(chip, ColorFor(type));
            GUILayout.Label(text, EditorStyles.miniLabel);
            GUILayout.Space(6);
        }

        private void DrawGrid()
        {
            if (_data == null) return;

            float width = _config.width * CellSize;
            float height = _config.height * CellSize;
            Rect area = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height));

            HandleGridClick(area);

            EditorGUI.DrawRect(area, new Color(0.11f, 0.11f, 0.13f));

            for (int y = 0; y < _config.height; y++)
            {
                for (int x = 0; x < _config.width; x++)
                {
                    var grid = new Vector2Int(x, y);
                    Rect cell = new Rect(
                        area.x + x * CellSize,
                        area.y + (_config.height - 1 - y) * CellSize,
                        CellSize - CellGap,
                        CellSize - CellGap);

                    if (!_data.TryGetRoom(grid, out RoomNode room))
                    {
                        EditorGUI.DrawRect(cell, new Color(0.15f, 0.15f, 0.18f, 0.7f));
                        continue;
                    }

                    EditorGUI.DrawRect(cell, ColorFor(room.Type));
                    DrawDoors(cell, room.Doors);

                    if (_selected.HasValue && _selected.Value == grid)
                        DrawOutline(cell, Color.white, 2f);

                    GUI.Label(cell, LabelFor(room.Type), CellLabelStyle);
                }
            }
        }

        private void HandleGridClick(Rect area)
        {
            Event e = Event.current;
            if (e == null || e.type != EventType.MouseDown || e.button != 0) return;
            if (!area.Contains(e.mousePosition)) return;

            int gx = Mathf.FloorToInt((e.mousePosition.x - area.x) / CellSize);
            int gy = _config.height - 1 - Mathf.FloorToInt((e.mousePosition.y - area.y) / CellSize);

            _selected = new Vector2Int(gx, gy);
            e.Use();
            Repaint();
        }

        private static void DrawDoors(Rect cell, DoorDirections doors)
        {
            const float t = 3f;
            const float len = 12f;
            Color c = new Color(0.96f, 0.96f, 0.96f, 0.92f);

            if ((doors & DoorDirections.Up) != 0)
                EditorGUI.DrawRect(new Rect(cell.center.x - len * 0.5f, cell.y - t * 0.5f, len, t), c);
            if ((doors & DoorDirections.Down) != 0)
                EditorGUI.DrawRect(new Rect(cell.center.x - len * 0.5f, cell.yMax - t * 0.5f, len, t), c);
            if ((doors & DoorDirections.Left) != 0)
                EditorGUI.DrawRect(new Rect(cell.x - t * 0.5f, cell.center.y - len * 0.5f, t, len), c);
            if ((doors & DoorDirections.Right) != 0)
                EditorGUI.DrawRect(new Rect(cell.xMax - t * 0.5f, cell.center.y - len * 0.5f, t, len), c);
        }

        private static void DrawOutline(Rect r, Color c, float t)
        {
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, t), c);
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - t, r.width, t), c);
            EditorGUI.DrawRect(new Rect(r.x, r.y, t, r.height), c);
            EditorGUI.DrawRect(new Rect(r.xMax - t, r.y, t, r.height), c);
        }

        private void DrawRoomInfo()
        {
            if (!_selected.HasValue)
            {
                EditorGUILayout.HelpBox("点击任意格子查看房间详情", MessageType.None);
                return;
            }

            if (!_data.TryGetRoom(_selected.Value, out RoomNode room))
            {
                EditorGUILayout.HelpBox($"坐标 {_selected.Value} 是空地", MessageType.None);
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"{TypeName(room.Type)}  {room.Grid}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("距离起点", room.DistanceFromStart + " 步");
                EditorGUILayout.LabelField("门位", DirUtil.Describe(room.Doors));

                var names = new List<string>(room.Neighbors.Count);
                for (int i = 0; i < room.Neighbors.Count; i++)
                    names.Add(TypeName(room.Neighbors[i].Type));
                EditorGUILayout.LabelField("相邻房间", names.Count > 0 ? string.Join(" / ", names) : "无");
            }
        }

        private void DrawValidation()
        {
            List<string> issues = _data.Validate();
            if (issues.Count == 0)
            {
                EditorGUILayout.HelpBox("结构校验通过：连通、Boss 为死胡同、特殊房间配额正常", MessageType.Info);
                return;
            }

            for (int i = 0; i < issues.Count; i++)
            {
                MessageType type = issues[i].StartsWith("[错误]") ? MessageType.Error : MessageType.Warning;
                EditorGUILayout.HelpBox(issues[i], type);
            }
        }

        // ------------------------------------------------------------------
        // 样式 / 颜色
        // ------------------------------------------------------------------

        private GUIStyle CellLabelStyle
        {
            get
            {
                if (_cellLabelStyle == null)
                {
                    _cellLabelStyle = new GUIStyle(EditorStyles.boldLabel);
                    _cellLabelStyle.alignment = TextAnchor.MiddleCenter;
                    _cellLabelStyle.fontSize = 15;
                    _cellLabelStyle.normal.textColor = new Color(1f, 1f, 1f, 0.95f);
                }
                return _cellLabelStyle;
            }
        }

        private static Color ColorFor(RoomType type)
        {
            switch (type)
            {
                case RoomType.Start: return new Color(0.20f, 0.72f, 0.36f);
                case RoomType.Boss: return new Color(0.82f, 0.22f, 0.24f);
                case RoomType.Treasure: return new Color(0.93f, 0.72f, 0.16f);
                case RoomType.Shop: return new Color(0.20f, 0.60f, 0.85f);
                case RoomType.Challenge: return new Color(0.92f, 0.45f, 0.18f);
                case RoomType.Secret: return new Color(0.60f, 0.36f, 0.80f);
                default: return new Color(0.33f, 0.35f, 0.41f);
            }
        }

        private static string LabelFor(RoomType type)
        {
            switch (type)
            {
                case RoomType.Start: return "起";
                case RoomType.Boss: return "王";
                case RoomType.Treasure: return "宝";
                case RoomType.Shop: return "商";
                case RoomType.Challenge: return "挑";
                case RoomType.Secret: return "秘";
                default: return string.Empty;
            }
        }

        private static string TypeName(RoomType type)
        {
            switch (type)
            {
                case RoomType.Start: return "起点";
                case RoomType.Boss: return "Boss";
                case RoomType.Treasure: return "宝箱";
                case RoomType.Shop: return "商店";
                case RoomType.Challenge: return "挑战";
                case RoomType.Secret: return "秘密";
                default: return "战斗";
            }
        }
    }
}
