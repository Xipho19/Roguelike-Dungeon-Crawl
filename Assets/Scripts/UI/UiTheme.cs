using Roguelike.Items;
using UnityEngine;

namespace Roguelike.UI
{
    /// <summary>
    /// 像素地牢风格的 IMGUI 主题：深色石质面板、旧金色描边、内嵌槽位和统一文字样式。
    /// 不依赖额外贴图，全部用色块绘制，避免 UI 缩放时出现模糊边框。
    /// </summary>
    public static class UiTheme
    {
        public static readonly Color Void = new Color(0.012f, 0.012f, 0.020f, 0.78f);
        public static readonly Color Shadow = new Color(0f, 0f, 0f, 0.55f);
        public static readonly Color PanelOuter = new Color(0.055f, 0.048f, 0.070f, 1f);
        public static readonly Color PanelEdge = new Color(0.20f, 0.165f, 0.23f, 1f);
        public static readonly Color PanelFill = new Color(0.095f, 0.083f, 0.115f, 0.985f);
        public static readonly Color PanelRaised = new Color(0.135f, 0.118f, 0.160f, 1f);
        public static readonly Color Inset = new Color(0.045f, 0.040f, 0.060f, 0.96f);
        public static readonly Color Gold = new Color(0.76f, 0.60f, 0.28f, 1f);
        public static readonly Color GoldBright = new Color(0.98f, 0.82f, 0.42f, 1f);
        public static readonly Color GoldDim = new Color(0.40f, 0.32f, 0.18f, 1f);
        public static readonly Color Text = new Color(0.94f, 0.92f, 0.87f, 1f);
        public static readonly Color Muted = new Color(0.62f, 0.60f, 0.66f, 1f);
        public static readonly Color Danger = new Color(0.94f, 0.26f, 0.28f, 1f);
        public static readonly Color Health = new Color(0.82f, 0.16f, 0.20f, 1f);
        public static readonly Color Shield = new Color(0.28f, 0.60f, 0.92f, 1f);
        public static readonly Color Coin = new Color(1f, 0.78f, 0.24f, 1f);
        public static readonly Color Mana = new Color(0.58f, 0.42f, 0.95f, 1f);

        private static Font _font;
        private static bool _fontSearched;

        private static GUIStyle _title;
        private static GUIStyle _titleCenter;
        private static GUIStyle _floorTitle;
        private static GUIStyle _subtitle;
        private static GUIStyle _body;
        private static GUIStyle _small;
        private static GUIStyle _tiny;
        private static GUIStyle _value;
        private static GUIStyle _button;
        private static GUIStyle _buttonLarge;
        private static GUIStyle _tooltipTitle;
        private static GUIStyle _tooltipBody;
        private static GUIStyle _center;
        private static GUIStyle _cellName;
        private static GUIStyle _warning;
        private static GUIStyle _boss;
        private static GUIStyle _barValue;
        private static GUIStyle _toast;

        public static GUIStyle Title { get { EnsureStyles(); return _title; } }
        public static GUIStyle TitleCenter { get { EnsureStyles(); return _titleCenter; } }
        public static GUIStyle FloorTitle { get { EnsureStyles(); return _floorTitle; } }
        public static GUIStyle Subtitle { get { EnsureStyles(); return _subtitle; } }
        public static GUIStyle Body { get { EnsureStyles(); return _body; } }
        public static GUIStyle Small { get { EnsureStyles(); return _small; } }
        public static GUIStyle Tiny { get { EnsureStyles(); return _tiny; } }
        public static GUIStyle Value { get { EnsureStyles(); return _value; } }
        public static GUIStyle ButtonText { get { EnsureStyles(); return _button; } }
        public static GUIStyle ButtonLargeText { get { EnsureStyles(); return _buttonLarge; } }
        public static GUIStyle TooltipTitle { get { EnsureStyles(); return _tooltipTitle; } }
        public static GUIStyle TooltipBody { get { EnsureStyles(); return _tooltipBody; } }
        public static GUIStyle Center { get { EnsureStyles(); return _center; } }
        public static GUIStyle CellName { get { EnsureStyles(); return _cellName; } }
        public static GUIStyle Warning { get { EnsureStyles(); return _warning; } }
        public static GUIStyle Boss { get { EnsureStyles(); return _boss; } }
        public static GUIStyle BarValue { get { EnsureStyles(); return _barValue; } }
        public static GUIStyle Toast { get { EnsureStyles(); return _toast; } }

        public static void EnsureStyles()
        {
            if (_title != null) return;

            // 可选：把像素字体放在 Resources/UI/ArkPixel12.ttf 后自动启用；
            // 没有字体资源时回退到 Unity 默认字体，不影响布局。
            if (!_fontSearched)
            {
                _fontSearched = true;
                _font = Resources.Load<Font>("UI/ArkPixel12");
            }

            _title = MakeStyle(22, TextAnchor.MiddleLeft, Text);
            _titleCenter = MakeStyle(22, TextAnchor.MiddleCenter, Text);
            _floorTitle = MakeStyle(20, TextAnchor.MiddleCenter, GoldBright);
            _subtitle = MakeStyle(12, TextAnchor.MiddleLeft, Muted);
            _body = MakeStyle(14, TextAnchor.UpperLeft, Text, true);
            _small = MakeStyle(12, TextAnchor.MiddleLeft, Muted);
            _tiny = MakeStyle(10, TextAnchor.MiddleCenter, Muted);
            _value = MakeStyle(18, TextAnchor.MiddleLeft, Text);
            _button = MakeStyle(14, TextAnchor.MiddleCenter, Text);
            _buttonLarge = MakeStyle(16, TextAnchor.MiddleCenter, Text);
            _tooltipTitle = MakeStyle(17, TextAnchor.MiddleLeft, Text);
            _tooltipBody = MakeStyle(13, TextAnchor.UpperLeft, Muted, true);
            _center = MakeStyle(14, TextAnchor.MiddleCenter, Text, true);
            _cellName = MakeStyle(12, TextAnchor.MiddleCenter, Text);
            _warning = MakeStyle(13, TextAnchor.MiddleCenter, Danger, true);
            _boss = MakeStyle(15, TextAnchor.MiddleCenter, GoldBright);
            _barValue = MakeStyle(11, TextAnchor.MiddleCenter, Color.white);
            _toast = MakeStyle(14, TextAnchor.MiddleCenter, Text, true);
        }

        private static GUIStyle MakeStyle(int size, TextAnchor anchor, Color color, bool wrap = false)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                alignment = anchor,
                wordWrap = wrap,
                clipping = TextClipping.Clip,
            };
            style.normal.textColor = color;
            if (_font != null) style.font = _font;
            return style;
        }

        public static void DrawDim()
        {
            DrawSolid(new Rect(0f, 0f, Screen.width, Screen.height), Void);
        }

        public static void DrawTopBar(Rect rect)
        {
            DrawSolid(rect, new Color(0.035f, 0.032f, 0.045f, 0.96f));
            DrawSolid(new Rect(rect.x, rect.yMax - 3f, rect.width, 1f), GoldDim);
            DrawSolid(new Rect(rect.x, rect.yMax - 2f, rect.width, 1f), new Color(0.02f, 0.02f, 0.03f, 0.9f));
            DrawSolid(new Rect(rect.x, rect.y, rect.width, 1f), new Color(1f, 1f, 1f, 0.035f));
        }

        public static void DrawPanel(Rect rect, Color? accent = null, bool raised = true)
        {
            Color trim = accent ?? GoldDim;

            DrawSolid(new Rect(rect.x + 4f, rect.y + 5f, rect.width, rect.height), Shadow);
            DrawSolid(rect, PanelOuter);
            DrawSolid(new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f), PanelEdge);
            DrawSolid(
                new Rect(rect.x + 4f, rect.y + 4f, rect.width - 8f, rect.height - 8f),
                raised ? PanelRaised : PanelFill);

            // 顶部/底部金色压边，保持像素硬边
            DrawSolid(new Rect(rect.x + 10f, rect.y + 4f, rect.width - 20f, 1f), trim);
            DrawSolid(new Rect(rect.x + 10f, rect.yMax - 5f, rect.width - 20f, 1f), new Color(trim.r, trim.g, trim.b, 0.5f));

            DrawRivets(rect, trim);
        }

        public static void DrawInset(Rect rect, Color? fill = null)
        {
            DrawSolid(rect, new Color(0.02f, 0.018f, 0.03f, 0.94f));
            DrawSolid(new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, rect.height - 2f), fill ?? Inset);
            DrawSolid(new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, 1f), new Color(0f, 0f, 0f, 0.40f));
            DrawSolid(new Rect(rect.x + 1f, rect.yMax - 2f, rect.width - 2f, 1f), new Color(1f, 1f, 1f, 0.045f));
        }

        public static void DrawOutline(Rect rect, Color color, float thickness = 1f)
        {
            DrawSolid(new Rect(rect.x, rect.y, rect.width, thickness), color);
            DrawSolid(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            DrawSolid(new Rect(rect.x, rect.y, thickness, rect.height), color);
            DrawSolid(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        public static void DrawDivider(Rect rect, Color? color = null)
        {
            Color c = color ?? GoldDim;
            DrawSolid(rect, new Color(c.r, c.g, c.b, 0.55f));
            DrawSolid(new Rect(rect.x, rect.y + 1f, rect.width, 1f), new Color(0f, 0f, 0f, 0.35f));
        }

        public static void DrawRivets(Rect rect, Color color)
        {
            DrawRivet(new Vector2(rect.x + 7f, rect.y + 7f), color);
            DrawRivet(new Vector2(rect.xMax - 10f, rect.y + 7f), color);
            DrawRivet(new Vector2(rect.x + 7f, rect.yMax - 10f), color);
            DrawRivet(new Vector2(rect.xMax - 10f, rect.yMax - 10f), color);
        }

        private static void DrawRivet(Vector2 point, Color color)
        {
            DrawSolid(new Rect(point.x, point.y, 3f, 3f), color);
            DrawSolid(new Rect(point.x + 1f, point.y + 1f, 1f, 1f), new Color(1f, 0.92f, 0.65f, 0.7f));
        }

        public static void DrawBar(Rect rect, float ratio, Color fill, bool showTicks = true)
        {
            ratio = Mathf.Clamp01(ratio);
            DrawInset(rect, new Color(0.035f, 0.03f, 0.045f, 0.98f));

            if (ratio > 0f)
            {
                var fillRect = new Rect(rect.x + 2f, rect.y + 2f, (rect.width - 4f) * ratio, rect.height - 4f);
                DrawSolid(fillRect, fill);
                DrawSolid(new Rect(fillRect.x, fillRect.y, fillRect.width, 1f), new Color(1f, 1f, 1f, 0.22f));
                DrawSolid(new Rect(fillRect.x, fillRect.yMax - 1f, fillRect.width, 1f), new Color(0f, 0f, 0f, 0.28f));
            }

            if (showTicks)
            {
                for (int i = 1; i < 10; i++)
                {
                    float x = rect.x + 2f + (rect.width - 4f) * (i / 10f);
                    DrawSolid(new Rect(x, rect.y + 2f, 1f, rect.height - 4f), new Color(0f, 0f, 0f, 0.22f));
                }
            }
        }

        public static bool DrawButton(Rect rect, string text, bool selected = false, bool disabled = false, int size = 14)
        {
            bool hovered = !disabled && rect.Contains(Event.current.mousePosition);
            Color fill = disabled
                ? new Color(0.055f, 0.050f, 0.065f, 1f)
                : selected
                    ? Gold
                    : hovered
                        ? PanelRaised
                        : new Color(0.085f, 0.072f, 0.105f, 1f);
            Color border = disabled ? GoldDim : selected ? GoldBright : hovered ? Gold : GoldDim;

            DrawSolid(new Rect(rect.x + 2f, rect.y + 3f, rect.width, rect.height), Shadow);
            DrawSolid(rect, border);
            DrawSolid(new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, rect.height - 2f), fill);
            DrawSolid(new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, 1f), new Color(1f, 1f, 1f, selected ? 0.25f : 0.07f));

            Color textColor = disabled
                ? new Color(0.42f, 0.40f, 0.46f, 1f)
                : selected
                    ? new Color(0.10f, 0.075f, 0.04f, 1f)
                    : Text;

            GUIStyle style = size >= 16 ? ButtonLargeText : ButtonText;
            DrawLabelShadow(new Rect(rect.x + 4f, rect.y, rect.width - 8f, rect.height), text, style, textColor);

            if (disabled) return false;

            bool clicked = GUI.Button(rect, GUIContent.none, GUIStyle.none);
            return clicked;
        }

        public static bool DrawIconButton(Rect rect, Sprite icon, bool selected = false)
        {
            bool hovered = rect.Contains(Event.current.mousePosition);
            Color border = selected ? GoldBright : hovered ? Gold : GoldDim;
            DrawSolid(new Rect(rect.x + 2f, rect.y + 3f, rect.width, rect.height), Shadow);
            DrawSolid(rect, border);
            DrawSolid(new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f), selected ? GoldDim : PanelFill);
            DrawIcon(new Rect(rect.x + 5f, rect.y + 5f, rect.width - 10f, rect.height - 10f), icon);
            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        public static void DrawIconSlot(Rect rect, Sprite icon, Color? accent = null)
        {
            DrawInset(rect);
            if (accent.HasValue)
                DrawSolid(new Rect(rect.x + 2f, rect.y + 2f, 2f, rect.height - 4f), accent.Value);
            DrawIcon(new Rect(rect.x + 4f, rect.y + 4f, rect.width - 8f, rect.height - 8f), icon);
        }

        public static void DrawIcon(Rect rect, Sprite icon, Color? tint = null)
        {
            if (icon == null || icon.texture == null) return;
            Color previous = GUI.color;
            GUI.color = tint ?? Color.white;
            GUI.DrawTexture(rect, icon.texture, ScaleMode.ScaleToFit, true);
            GUI.color = previous;
        }

        public static void DrawKeyBadge(Rect rect, string key)
        {
            DrawInset(rect);
            DrawSolid(new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, 1f), GoldDim);
            DrawLabelShadow(rect, key, Tiny, GoldBright);
        }

        public static void DrawPill(Rect rect, string text, Color accent, GUIStyle style = null)
        {
            DrawSolid(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), Shadow);
            DrawSolid(rect, new Color(0.035f, 0.03f, 0.045f, 0.94f));
            DrawSolid(new Rect(rect.x, rect.y, 3f, rect.height), accent);
            DrawSolid(new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, 1f), new Color(accent.r, accent.g, accent.b, 0.35f));
            DrawLabelShadow(new Rect(rect.x + 8f, rect.y, rect.width - 12f, rect.height), text, style ?? Center, Text);
        }

        public static void DrawRarityChip(Rect rect, ItemRarity rarity)
        {
            Color color = RarityColor(rarity);
            DrawSolid(rect, new Color(0.04f, 0.035f, 0.05f, 0.95f));
            DrawSolid(new Rect(rect.x, rect.y, 3f, rect.height), color);
            DrawLabelShadow(
                new Rect(rect.x + 7f, rect.y, rect.width - 9f, rect.height),
                RarityName(rarity),
                Tiny,
                color);
        }

        public static void DrawLabelShadow(Rect rect, string text, GUIStyle style, Color? color = null)
        {
            Color old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, style);
            GUI.color = color ?? Text;
            GUI.Label(rect, text, style);
            GUI.color = old;
        }

        public static void DrawCoinIcon(Rect rect)
        {
            DrawSolid(rect, new Color(0.30f, 0.20f, 0.05f, 1f));
            DrawSolid(new Rect(rect.x + 2f, rect.y + 1f, rect.width - 4f, rect.height - 2f), Gold);
            DrawSolid(new Rect(rect.x + 1f, rect.y + 2f, rect.width - 2f, rect.height - 4f), Gold);
            DrawSolid(new Rect(rect.x + 4f, rect.y + 3f, rect.width - 8f, rect.height - 6f), Coin);
            DrawSolid(new Rect(rect.x + 6f, rect.y + 5f, Mathf.Max(1f, rect.width - 12f), Mathf.Max(1f, rect.height - 10f)), new Color(1f, 0.92f, 0.55f, 1f));
        }

        public static Color RarityColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Rare: return new Color(0.38f, 0.64f, 0.98f, 1f);
                case ItemRarity.Legendary: return new Color(0.98f, 0.78f, 0.28f, 1f);
                default: return new Color(0.66f, 0.68f, 0.72f, 1f);
            }
        }

        public static string RarityName(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Rare: return "稀有";
                case ItemRarity.Legendary: return "传说";
                default: return "普通";
            }
        }

        public static void Fill(Rect rect, Color color) => DrawSolid(rect, color);

        private static void DrawSolid(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
