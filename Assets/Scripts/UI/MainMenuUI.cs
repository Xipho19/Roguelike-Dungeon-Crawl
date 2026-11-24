using System.Collections.Generic;
using Roguelike.Audio;
using Roguelike.Core;
using Roguelike.Player;
using Roguelike.Progression;
using Roguelike.UI;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Roguelike.UI
{
    /// <summary>
    /// 启动时的标题页。RunManager 初始状态为 MainMenu，时间冻结；
    /// 点击“开始游戏”或按 Enter 后才进入 Playing。
    /// 地牢会保留在背景中作为暗化场景，玩家角色在菜单期间隐藏。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private RunManager runManager;
        [SerializeField] private string title = "地牢回响";
        [SerializeField] private string subtitle = "ROGUELIKE DUNGEON CRAWL";
        [SerializeField] private string tagline = "向下，直到没有退路。";

        private bool _visible = true;
        private bool _showHelp;
        private bool _showSkillTree;
        private bool _started;
        private string _selectedUpgradeId = "life1";
        private GUIStyle _titleStyle;
        private GUIStyle _treeTitleStyle;
        private GUIStyle _nodeGlyphStyle;
        private GUIStyle _coreGlyphStyle;
        private SpriteRenderer[] _playerRenderers;

        private Texture2D _backgroundTexture;
        private Texture2D _frameTexture;
        private Texture2D _titleTexture;
        private Texture2D _crestTexture;
        private Texture2D _dividerTexture;
        private Texture2D _buttonFrameTexture;
        private bool _artLoaded;

        private void Awake()
        {
            LoadArt();
            if (runManager == null) runManager = FindFirstObjectByType<RunManager>();
            if (runManager == null)
            {
                Debug.LogError("[MainMenu] 找不到 RunManager，主菜单无法初始化。", this);
                enabled = false;
                return;
            }

            if (runManager.State != RunManager.RunState.MainMenu)
            {
                // 场景被直接以 Playing 状态启动时，不再显示主菜单。
                _visible = false;
                enabled = false;
            }
        }

        private void Start()
        {
            if (!_visible) return;

            Time.timeScale = 0f;
            CachePlayerRenderers();
            SetPlayerVisible(false);
        }

        private void Update()
        {
            if (!_visible) return;

            if (runManager == null || runManager.State != RunManager.RunState.MainMenu)
            {
                HideMenu();
                return;
            }

            if (_showSkillTree)
            {
                if (EscapePressed()) CloseSkillTree();
                return;
            }

            HandleKeyboard();
        }

        private void LateUpdate()
        {
            if (!_visible || _playerRenderers == null) return;

            // WeaponVisual 会在 LateUpdate 中重新启用手持 Sprite；
            // forceRenderingOff 不受 enabled 影响，可稳定隐藏玩家。
            for (int i = 0; i < _playerRenderers.Length; i++)
            {
                if (_playerRenderers[i] != null)
                    _playerRenderers[i].forceRenderingOff = true;
            }
        }

        private void OnGUI()
        {
            if (!_visible) return;

            EnsureStyles();
            DrawBackdrop(!_showSkillTree && !_showHelp);

            if (_showSkillTree)
            {
                DrawSkillTreeScreen();
                return;
            }

            if (_showHelp)
            {
                DrawHelpScreen();
                return;
            }

            DrawStartScreen();
        }

        private void LoadArt()
        {
            if (_artLoaded) return;
            _artLoaded = true;

            _backgroundTexture = Resources.Load<Texture2D>("UI/MainMenu/main_menu_background");
            _frameTexture = Resources.Load<Texture2D>("UI/MainMenu/main_menu_frame");
            _titleTexture = Resources.Load<Texture2D>("UI/MainMenu/main_menu_title");
            _crestTexture = Resources.Load<Texture2D>("UI/MainMenu/main_menu_crest");
            _dividerTexture = Resources.Load<Texture2D>("UI/MainMenu/main_menu_divider");
            _buttonFrameTexture = Resources.Load<Texture2D>("UI/MainMenu/main_menu_button_frame");
        }

        private void DrawBackdrop(bool showFrame)
        {
            float width = Screen.width;
            float height = Screen.height;
            var full = new Rect(0f, 0f, width, height);

            if (_backgroundTexture != null)
            {
                GUI.DrawTexture(full, _backgroundTexture, ScaleMode.ScaleAndCrop, false);
            }
            else
            {
                UiTheme.DrawDim();
                UiTheme.Fill(full, new Color(0.01f, 0.01f, 0.02f, 0.48f));
            }

            UiTheme.Fill(full, new Color(0.008f, 0.007f, 0.012f, showFrame ? 0.16f : 0.58f));

            if (showFrame && _frameTexture != null)
            {
                Color previous = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, 0.92f);
                GUI.DrawTexture(full, _frameTexture, ScaleMode.StretchToFill, true);
                GUI.color = previous;
            }

            // 缓慢上升的金色余烬，让标题页保持生气；使用 unscaledTime 以在暂停时继续移动。
            float time = Time.unscaledTime;
            for (int i = 0; i < 26; i++)
            {
                float seed = i * 13.37f;
                float x = Mathf.Repeat(seed * 97f, width);
                float y = height - Mathf.Repeat(time * 12f + seed * 41f, height + 70f);
                float alpha = 0.035f + 0.045f * (0.5f + 0.5f * Mathf.Sin(time * 0.9f + seed));
                UiTheme.Fill(new Rect(x, y, 2f, 2f), new Color(0.98f, 0.82f, 0.42f, alpha));
            }
        }

        private void DrawStartScreen()
        {
            float width = Screen.width;
            float height = Screen.height;
            float centerX = width * 0.5f;

            DrawStartBackdrop(width, height);
            DrawStartTitle(width, height, centerX);
            DrawStartMenu(width, height, centerX);
            DrawStartBottomBar(width, height);
        }

        private static void DrawStartBackdrop(float width, float height)
        {
            float scale = Mathf.Min(width, height);
            var center = new Vector2(width * 0.5f, height * 0.30f);
            Color gold = UiTheme.Gold;

            UiTheme.DrawDiamond(center, scale * 0.78f, new Color(gold.r, gold.g, gold.b, 0.010f));
            UiTheme.DrawCircle(center, scale * 0.29f, new Color(gold.r, gold.g, gold.b, 0.040f), 1f, 72);
            UiTheme.DrawDashedCircle(center, scale * 0.35f, new Color(gold.r, gold.g, gold.b, 0.028f), 1f, 72, 3);

            float time = Time.unscaledTime;
            for (int i = 0; i < 2; i++)
            {
                float flicker = 0.72f + 0.28f * Mathf.Sin(time * 4.2f + i * 2.7f);
                var torch = new Vector2(width * (i == 0 ? 0.25f : 0.75f), height * 0.43f);
                float radius = scale * 0.14f;
                UiTheme.DrawCircle(
                    torch,
                    radius,
                    new Color(1f, 0.42f, 0.12f, 0.022f * flicker),
                    1f,
                    36);
                UiTheme.DrawCircle(
                    torch,
                    radius * 0.55f,
                    new Color(1f, 0.62f, 0.22f, 0.035f * flicker),
                    1f,
                    36);
            }
        }

        private void DrawStartTitle(float width, float height, float centerX)
        {
            float titleY = height * 0.065f;
            float titleWidth = Mathf.Clamp(width * 0.54f, 400f, 560f);
            _titleStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(height * 0.085f), 34, 56);
            float titleHeight = _titleTexture != null
                ? titleWidth * _titleTexture.height / _titleTexture.width
                : _titleStyle.fontSize + 24f;

            UiTheme.DrawDiamond(
                new Vector2(centerX, titleY + titleHeight * 0.5f),
                titleWidth * 1.18f,
                new Color(0.98f, 0.82f, 0.42f, 0.018f));
            UiTheme.DrawCircle(
                new Vector2(centerX, titleY + titleHeight * 0.5f),
                titleWidth * 0.52f,
                new Color(0.98f, 0.82f, 0.42f, 0.055f),
                1f,
                72);

            if (_crestTexture != null)
            {
                float crestSize = Mathf.Min(titleHeight * 1.35f, height * 0.28f);
                var crestRect = new Rect(
                    centerX - crestSize * 0.5f,
                    titleY + titleHeight * 0.5f - crestSize * 0.5f,
                    crestSize,
                    crestSize);
                Color previous = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, 0.16f);
                GUI.DrawTexture(crestRect, _crestTexture, ScaleMode.ScaleToFit, true);
                GUI.color = previous;
            }

            var titleRect = new Rect(centerX - titleWidth * 0.5f, titleY, titleWidth, titleHeight);
            if (_titleTexture != null)
            {
                GUI.DrawTexture(titleRect, _titleTexture, ScaleMode.ScaleToFit, true);
            }
            else
            {
                float glow = 0.82f + 0.18f * Mathf.Sin(Time.unscaledTime * 1.6f);
                Color titleColor = new Color(
                    UiTheme.GoldBright.r,
                    UiTheme.GoldBright.g,
                    UiTheme.GoldBright.b,
                    glow);
                UiTheme.DrawLabelShadow(titleRect, title, _titleStyle, titleColor);
            }

            float subtitleY = titleY + titleHeight - 2f;
            UiTheme.DrawLabelShadow(
                new Rect(0f, subtitleY, width, 16f),
                subtitle,
                UiTheme.Tiny,
                UiTheme.Muted);

            float taglineY = subtitleY + 26f;
            UiTheme.DrawLabelShadow(
                new Rect(0f, taglineY, width, 24f),
                tagline,
                UiTheme.Center,
                UiTheme.Muted);

            float dividerY = titleY + titleHeight + 52f;
            if (_dividerTexture != null)
            {
                float dividerWidth = Mathf.Min(width * 0.68f, 560f);
                float dividerHeight = dividerWidth * _dividerTexture.height / _dividerTexture.width;
                GUI.DrawTexture(
                    new Rect(centerX - dividerWidth * 0.5f, dividerY, dividerWidth, dividerHeight),
                    _dividerTexture,
                    ScaleMode.ScaleToFit,
                    true);
            }
            else
            {
                float margin = Mathf.Clamp(width * 0.16f, 60f, 280f);
                UiTheme.DrawLine(
                    new Vector2(margin, dividerY),
                    new Vector2(width - margin, dividerY),
                    new Color(UiTheme.Gold.r, UiTheme.Gold.g, UiTheme.Gold.b, 0.32f),
                    1f);
                UiTheme.DrawDiamond(new Vector2(centerX, dividerY), 7f, UiTheme.GoldBright);
            }
        }

        private void DrawStartMenu(float width, float height, float centerX)
        {
            float startWidth = Mathf.Min(420f, width - 100f);
            float startHeight = Mathf.Clamp(height * 0.085f, 42f, 58f);
            float startY = height * 0.50f;
            float gap = 14f;
            bool canResume = runManager != null && runManager.HasResumableRun;

            if (DrawMenuButton(
                    new Rect(centerX - startWidth * 0.5f, startY, startWidth, startHeight),
                    canResume ? "继续游戏   ENTER" : "开始游戏   ENTER",
                    true,
                    20))
            {
                StartGame();
            }

            float rowWidth = Mathf.Min(720f, width - 100f);
            float rowX = centerX - rowWidth * 0.5f;
            float rowY = startY + startHeight + 16f;
            float rowHeight = Mathf.Clamp(height * 0.06f, 36f, 44f);
            int secondaryCount = canResume ? 4 : 3;
            float secondaryWidth = (rowWidth - gap * (secondaryCount - 1)) / secondaryCount;
            int column = 0;

            if (canResume)
            {
                if (DrawMenuButton(
                        new Rect(rowX, rowY, secondaryWidth, rowHeight),
                        "新游戏",
                        false,
                        15))
                {
                    StartNewGame();
                }

                column++;
            }

            if (DrawMenuButton(
                    new Rect(rowX + (secondaryWidth + gap) * column, rowY, secondaryWidth, rowHeight),
                    "局外成长",
                    false,
                    15))
            {
                OpenSkillTree();
            }

            column++;

            if (DrawMenuButton(
                    new Rect(rowX + (secondaryWidth + gap) * column, rowY, secondaryWidth, rowHeight),
                    "操作说明",
                    false,
                    15))
            {
                _showHelp = true;
                AudioService.Play("bookFlip1", 0.55f);
            }

            column++;

            if (DrawMenuButton(
                    new Rect(rowX + (secondaryWidth + gap) * column, rowY, secondaryWidth, rowHeight),
                    "退出游戏",
                    false,
                    15))
            {
                QuitGame();
            }

            float hintY = rowY + rowHeight + 14f;
            UiTheme.DrawLabelShadow(
                new Rect(0f, hintY, width, 18f),
                canResume ? "鼠标点击，或按 Enter 继续上次进度" : "鼠标点击，或按 Enter 开始",
                UiTheme.Center,
                new Color(0.82f, 0.74f, 0.58f, 1f));

            float loreY = height * 0.84f;
            float loreMargin = Mathf.Clamp(width * 0.30f, 80f, 420f);
            UiTheme.DrawLine(
                new Vector2(loreMargin, loreY - 14f),
                new Vector2(width - loreMargin, loreY - 14f),
                new Color(UiTheme.Gold.r, UiTheme.Gold.g, UiTheme.Gold.b, 0.14f),
                1f);
            UiTheme.DrawDiamond(new Vector2(centerX, loreY - 14f), 5f, UiTheme.GoldDim);
            UiTheme.DrawLabelShadow(
                new Rect(0f, loreY, width, 20f),
                "每一次深入，都是一次新的构筑。",
                UiTheme.Center,
                new Color(UiTheme.Muted.r, UiTheme.Muted.g, UiTheme.Muted.b, 0.65f));
        }

        private bool DrawMenuButton(Rect rect, string text, bool selected, int fontSize)
        {
            bool hovered = rect.Contains(Event.current.mousePosition);
            Color fill = selected
                ? new Color(0.78f, 0.62f, 0.30f, 1f)
                : hovered
                    ? new Color(0.145f, 0.115f, 0.095f, 0.98f)
                    : new Color(0.06f, 0.052f, 0.068f, 0.97f);

            UiTheme.Fill(new Rect(rect.x + 2f, rect.y + 3f, rect.width, rect.height), UiTheme.Shadow);
            UiTheme.Fill(rect, fill);
            UiTheme.Fill(
                new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height * 0.46f),
                new Color(1f, 1f, 1f, selected ? 0.10f : 0.035f));
            UiTheme.Fill(
                new Rect(rect.x + 2f, rect.yMax - rect.height * 0.30f, rect.width - 4f, rect.height * 0.28f),
                new Color(0f, 0f, 0f, selected ? 0.10f : 0.16f));
            UiTheme.Fill(
                new Rect(rect.x + 8f, rect.y + 4f, rect.width - 16f, 1f),
                new Color(1f, 1f, 1f, selected ? 0.22f : 0.08f));

            if (_buttonFrameTexture != null)
            {
                UiTheme.DrawNineSlice(
                    rect,
                    _buttonFrameTexture,
                    selected
                        ? new Color(1f, 0.98f, 0.88f, 1f)
                        : hovered
                            ? new Color(1f, 0.92f, 0.72f, 1f)
                            : new Color(0.92f, 0.84f, 0.68f, 1f),
                    14f);
            }
            else
            {
                UiTheme.DrawOutline(rect, selected ? UiTheme.GoldBright : UiTheme.GoldDim, 1f);
            }

            Color textColor = selected
                ? new Color(0.09f, 0.07f, 0.04f, 1f)
                : UiTheme.Text;
            GUIStyle style = fontSize >= 18 ? UiTheme.ButtonLargeText : UiTheme.ButtonText;
            UiTheme.DrawLabelShadow(
                new Rect(rect.x + 4f, rect.y, rect.width - 8f, rect.height),
                text,
                style,
                textColor);

            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        private static void DrawStartBottomBar(float width, float height)
        {
            float barHeight = Mathf.Clamp(height * 0.10f, 42f, 58f);
            var bar = new Rect(0f, height - barHeight, width, barHeight);
            UiTheme.Fill(bar, new Color(0.012f, 0.011f, 0.018f, 0.94f));
            UiTheme.DrawLine(
                new Vector2(0f, height - barHeight),
                new Vector2(width, height - barHeight),
                new Color(UiTheme.Gold.r, UiTheme.Gold.g, UiTheme.Gold.b, 0.38f),
                1f);

            float inset = Mathf.Clamp(width * 0.04f, 18f, 54f);
            float centerY = height - barHeight * 0.5f;
            UiTheme.DrawCoinIcon(new Rect(inset, centerY - 8f, 16f, 16f));
            UiTheme.DrawLabelShadow(
                new Rect(inset + 24f, centerY - 10f, 300f, 20f),
                $"局外金币 {MetaProgression.Coins}",
                UiTheme.Small,
                UiTheme.GoldBright);

            UiTheme.DrawLabelShadow(
                new Rect(width - 360f, centerY - 9f, 336f, 18f),
                "原型版本 · 6 层地牢 · 8 件武器",
                UiTheme.Tiny,
                UiTheme.GoldDim);
        }

        private void DrawHelpScreen()
        {
            float panelWidth = Mathf.Min(760f, Screen.width - 60f);
            float panelHeight = Mathf.Min(430f, Screen.height - 40f);
            var panel = new Rect(
                (Screen.width - panelWidth) * 0.5f,
                (Screen.height - panelHeight) * 0.5f,
                panelWidth,
                panelHeight);

            UiTheme.DrawPanel(panel, UiTheme.Gold, false);

            UiTheme.DrawLabelShadow(
                new Rect(panel.x + 30f, panel.y + 16f, panel.width - 60f, 36f),
                "操作说明",
                _treeTitleStyle,
                UiTheme.GoldBright);
            UiTheme.DrawLabelShadow(
                new Rect(panel.x + 30f, panel.y + 50f, panel.width - 60f, 16f),
                "移动、战斗与交互",
                UiTheme.Tiny,
                UiTheme.Muted);
            UiTheme.DrawDivider(new Rect(panel.x + 34f, panel.y + 76f, panel.width - 68f, 1f));

            DrawHelpContent(panel);

            if (UiTheme.DrawButton(
                    new Rect(panel.center.x - 130f, panel.yMax - 56f, 260f, 36f),
                    "返回   ESC",
                    false,
                    false,
                    16))
            {
                _showHelp = false;
                AudioService.Play("bookFlip1", 0.5f);
            }
        }

        private void OpenSkillTree()
        {
            _showSkillTree = true;
            _showHelp = false;

            if (string.IsNullOrEmpty(_selectedUpgradeId) || MetaUpgrades.Find(_selectedUpgradeId) == null)
                _selectedUpgradeId = MetaUpgrades.Nodes.Length > 0 ? MetaUpgrades.Nodes[0].id : MetaUpgrades.CoreId;

            AudioService.Play("bookFlip1", 0.55f);
        }

        private void CloseSkillTree()
        {
            _showSkillTree = false;
            AudioService.Play("bookFlip1", 0.5f);
        }

        private void DrawSkillTreeScreen()
        {
            float panelWidth = Mathf.Min(940f, Screen.width - 24f);
            float panelHeight = Mathf.Min(420f, Screen.height - 20f);
            var panel = new Rect(
                (Screen.width - panelWidth) * 0.5f,
                (Screen.height - panelHeight) * 0.5f,
                panelWidth,
                panelHeight);

            UiTheme.Fill(
                new Rect(panel.x - 18f, panel.y - 18f, panel.width + 36f, panel.height + 36f),
                new Color(0.98f, 0.82f, 0.42f, 0.035f));
            UiTheme.DrawPanel(panel, UiTheme.Gold, false);

            UiTheme.DrawLabelShadow(
                new Rect(panel.x + 34f, panel.y + 12f, panel.width - 370f, 34f),
                "局外成长 · 技能树",
                _treeTitleStyle,
                UiTheme.GoldBright);

            UiTheme.DrawLabelShadow(
                new Rect(panel.x + 36f, panel.y + 45f, panel.width - 370f, 16f),
                "六条分支从冒险者核心向外延伸 · 永久生效",
                UiTheme.Tiny,
                UiTheme.Muted);

            UiTheme.DrawDivider(new Rect(panel.x + 36f, panel.y + 66f, panel.width - 382f, 1f));
            DrawSkillCoinPill(new Rect(panel.xMax - 316f, panel.y + 16f, 286f, 30f));
            DrawSkillLegend(panel);

            var details = new Rect(
                panel.xMax - 310f,
                panel.y + 92f,
                280f,
                panel.height - 115f);

            DrawSkillTree(panel, details);
            DrawSkillDetails(details);

            if (UiTheme.DrawButton(
                    new Rect(panel.x + 34f, panel.yMax - 52f, 142f, 34f),
                    "返回   ESC",
                    false,
                    false,
                    15))
            {
                CloseSkillTree();
            }
        }

        private static void DrawSkillCoinPill(Rect rect)
        {
            UiTheme.Fill(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), UiTheme.Shadow);
            UiTheme.Fill(rect, UiTheme.GoldDim);
            UiTheme.Fill(
                new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, rect.height - 2f),
                new Color(0.045f, 0.040f, 0.055f, 0.98f));
            UiTheme.DrawCoinIcon(new Rect(rect.x + 9f, rect.y + 7f, 16f, 16f));
            UiTheme.DrawLabelShadow(
                new Rect(rect.x + 32f, rect.y, rect.width - 42f, rect.height),
                $"{MetaProgression.Coins}  局外金币",
                UiTheme.Small,
                UiTheme.GoldBright);
        }

        private static void DrawSkillLegend(Rect panel)
        {
            float x = panel.x + 38f;
            float y = panel.y + 74f;

            for (int i = 0; i < MetaUpgrades.Nodes.Length; i++)
            {
                MetaUpgradeDefinition inner = MetaUpgrades.Nodes[i];
                if (inner.ring != 1) continue;

                Color color = BranchColor(inner.effect);
                MetaUpgradeDefinition outer = MetaUpgrades.FindByPrerequisite(inner.id);
                bool innerPurchased = MetaProgression.GetUpgradeLevel(inner.id) >= inner.maxLevel;
                bool outerPurchased = outer != null &&
                                      MetaProgression.GetUpgradeLevel(outer.id) >= outer.maxLevel;

                UiTheme.DrawDiamond(new Vector2(x + 6f, y + 8f), 8f, color);
                UiTheme.DrawLabelShadow(
                    new Rect(x + 15f, y, 40f, 16f),
                    BranchName(inner),
                    UiTheme.Tiny,
                    UiTheme.Muted);

                UiTheme.DrawDiamond(
                    new Vector2(x + 57f, y + 8f),
                    6f,
                    innerPurchased ? color : new Color(color.r, color.g, color.b, 0.16f));
                UiTheme.DrawDiamond(
                    new Vector2(x + 68f, y + 8f),
                    6f,
                    outerPurchased ? color : new Color(color.r, color.g, color.b, 0.16f));

                x += 78f;
            }
        }

        private void DrawSkillTree(Rect panel, Rect details)
        {
            Vector2 center = new Vector2(
                panel.x + (panel.width - 330f) * 0.5f,
                panel.y + panel.height * 0.57f);

            UiTheme.DrawDiamond(center, 340f, new Color(0.98f, 0.82f, 0.42f, 0.014f));
            UiTheme.DrawDashedCircle(center, 66f, new Color(0.76f, 0.60f, 0.28f, 0.16f), 1f, 64, 2);
            UiTheme.DrawDashedCircle(center, 118f, new Color(0.76f, 0.60f, 0.28f, 0.11f), 1f, 64, 2);
            DrawTreeDust(center);

            var positions = new Dictionary<string, Vector2>
            {
                { MetaUpgrades.CoreId, center }
            };

            for (int i = 0; i < MetaUpgrades.Nodes.Length; i++)
            {
                MetaUpgradeDefinition definition = MetaUpgrades.Nodes[i];
                positions[definition.id] = NodePosition(center, definition);
            }

            for (int i = 0; i < MetaUpgrades.Nodes.Length; i++)
            {
                MetaUpgradeDefinition definition = MetaUpgrades.Nodes[i];
                Vector2 from = center;
                if (!string.IsNullOrEmpty(definition.prerequisiteId) &&
                    positions.TryGetValue(definition.prerequisiteId, out Vector2 parentPosition))
                {
                    from = parentPosition;
                }

                bool purchased = MetaProgression.GetUpgradeLevel(definition.id) >= definition.maxLevel;
                bool parentPurchased = string.IsNullOrEmpty(definition.prerequisiteId) ||
                                       MetaProgression.GetUpgradeLevel(definition.prerequisiteId) > 0;
                Color color = BranchColor(definition.effect);
                DrawBranchConnection(from, positions[definition.id], color, purchased, parentPurchased);
            }

            DrawCoreNode(center);

            for (int i = 0; i < MetaUpgrades.Nodes.Length; i++)
            {
                MetaUpgradeDefinition definition = MetaUpgrades.Nodes[i];
                Vector2 position = positions[definition.id];

                bool purchased = MetaProgression.GetUpgradeLevel(definition.id) >= definition.maxLevel;
                bool available = string.IsNullOrEmpty(definition.prerequisiteId) ||
                                 MetaProgression.GetUpgradeLevel(definition.prerequisiteId) > 0;

                if (DrawSkillNode(position, definition, purchased, available))
                    _selectedUpgradeId = definition.id;
            }
        }

        private static void DrawBranchConnection(
            Vector2 from,
            Vector2 to,
            Color color,
            bool purchased,
            bool available)
        {
            float pulse = 0.78f + 0.22f * Mathf.Sin(Time.unscaledTime * 2.2f + from.x * 0.013f);
            float baseAlpha = purchased ? 0.9f : available ? 0.48f : 0.15f;

            UiTheme.DrawLine(from, to, new Color(color.r, color.g, color.b, purchased ? 0.10f : 0.035f), 10f);
            UiTheme.DrawLine(from, to, new Color(color.r, color.g, color.b, purchased ? 0.24f : 0.10f), 5f);
            UiTheme.DrawLine(
                from,
                to,
                new Color(color.r, color.g, color.b, baseAlpha * pulse),
                purchased ? 2.2f : 1.4f);

            if (!purchased) return;

            float travel = Mathf.Repeat(Time.unscaledTime * 0.26f + from.x * 0.011f, 1f);
            Vector2 mote = Vector2.Lerp(from, to, travel);
            UiTheme.DrawDiamond(mote, 5f, new Color(color.r, color.g, color.b, 0.72f));
        }

        private static void DrawTreeDust(Vector2 center)
        {
            for (int i = 0; i < 26; i++)
            {
                float angle = i * 2.39996f;
                float radius = 28f + (i * 37 % 94);
                var point = new Vector2(
                    center.x + Mathf.Cos(angle) * radius,
                    center.y + Mathf.Sin(angle) * radius);
                float alpha = 0.035f + 0.035f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 0.8f + i));
                UiTheme.Fill(new Rect(point.x, point.y, 1.5f, 1.5f), new Color(0.98f, 0.82f, 0.42f, alpha));
            }
        }

        private void DrawCoreNode(Vector2 center)
        {
            float pulse = 0.72f + 0.28f * Mathf.Sin(Time.unscaledTime * 2.4f);
            Color gold = UiTheme.GoldBright;

            UiTheme.DrawDiamond(center, 104f, new Color(gold.r, gold.g, gold.b, 0.035f));
            UiTheme.DrawDiamond(center, 82f, new Color(gold.r, gold.g, gold.b, 0.065f));
            UiTheme.DrawDashedCircle(center, 48f, new Color(gold.r, gold.g, gold.b, 0.28f), 1f, 48, 2);

            for (int i = 0; i < 6; i++)
            {
                float angle = Time.unscaledTime * 18f + i * 60f;
                float radians = angle * Mathf.Deg2Rad;
                var mote = center + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * 38f;
                UiTheme.DrawDiamond(mote, 5f, new Color(gold.r, gold.g, gold.b, 0.62f));
            }

            UiTheme.DrawDiamond(center, 68f, UiTheme.GoldDim);
            UiTheme.DrawDiamond(center, 60f, new Color(0.06f, 0.052f, 0.038f, 1f));
            UiTheme.DrawDiamondOutline(center, 73f, new Color(gold.r, gold.g, gold.b, pulse), 2f);

            UiTheme.DrawLabelShadow(
                new Rect(center.x - 30f, center.y - 30f, 60f, 60f),
                "核",
                _coreGlyphStyle,
                gold);

            if (GUI.Button(new Rect(center.x - 36f, center.y - 36f, 72f, 72f), GUIContent.none, GUIStyle.none))
                _selectedUpgradeId = MetaUpgrades.CoreId;
        }

        private bool DrawSkillNode(
            Vector2 center,
            MetaUpgradeDefinition definition,
            bool purchased,
            bool available)
        {
            float size = definition.ring == 1 ? 40f : 44f;
            Color branch = BranchColor(definition.effect);
            bool selected = _selectedUpgradeId == definition.id;
            var hitRect = new Rect(center.x - 30f, center.y - 30f, 60f, 60f);
            bool hovered = hitRect.Contains(Event.current.mousePosition);

            if (hovered && Event.current.type == EventType.Repaint)
                _selectedUpgradeId = definition.id;

            Color border = purchased
                ? UiTheme.GoldBright
                : available
                    ? branch
                    : UiTheme.PanelEdge;
            Color fill = purchased
                ? new Color(branch.r * 0.30f + 0.03f, branch.g * 0.30f + 0.025f, branch.b * 0.30f + 0.02f, 1f)
                : available
                    ? new Color(branch.r * 0.10f + 0.03f, branch.g * 0.10f + 0.03f, branch.b * 0.10f + 0.035f, 1f)
                    : new Color(0.045f, 0.042f, 0.052f, 1f);

            float glow = purchased ? 0.14f : available ? 0.075f : 0.015f;
            if (hovered || selected) glow += 0.09f;

            UiTheme.DrawDiamond(center, size + 28f, new Color(branch.r, branch.g, branch.b, glow * 0.45f));
            UiTheme.DrawDiamond(center, size + 14f, new Color(branch.r, branch.g, branch.b, glow));
            UiTheme.DrawDiamond(center, size, border);
            UiTheme.DrawDiamond(center, size - 5f, fill);
            UiTheme.DrawDiamondOutline(
                center,
                size - 9f,
                new Color(branch.r, branch.g, branch.b, purchased ? 0.65f : available ? 0.38f : 0.12f),
                1f);

            Color glyphColor = purchased
                ? UiTheme.GoldBright
                : available
                    ? branch
                    : UiTheme.Muted;
            UiTheme.DrawLabelShadow(
                new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size),
                definition.glyph,
                _nodeGlyphStyle,
                glyphColor);

            if (purchased)
            {
                UiTheme.DrawDiamond(center + new Vector2(0f, size * 0.5f + 8f), 6f, UiTheme.GoldBright);
            }
            else if (!available)
            {
                UiTheme.Fill(
                    new Rect(center.x - 3f, center.y + size * 0.5f + 5f, 6f, 5f),
                    new Color(0.36f, 0.34f, 0.40f, 0.9f));
            }

            if (selected)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f);
                UiTheme.DrawDiamondOutline(
                    center,
                    size + 13f,
                    new Color(UiTheme.GoldBright.r, UiTheme.GoldBright.g, UiTheme.GoldBright.b, 0.45f + 0.35f * pulse),
                    2f);
            }

            return GUI.Button(hitRect, GUIContent.none, GUIStyle.none);
        }

        private void DrawSkillDetails(Rect details)
        {
            MetaUpgradeDefinition definition = MetaUpgrades.Find(_selectedUpgradeId);
            bool core = definition == null;
            int level = core ? 1 : MetaProgression.GetUpgradeLevel(definition.id);
            int maxLevel = core ? 1 : definition.maxLevel;

            string name = core ? MetaUpgrades.CoreName : definition.displayName;
            string branch = core ? "核心" : BranchName(definition);
            string description = core ? MetaUpgrades.CoreDescription : definition.description;
            Color accent = core ? UiTheme.Gold : BranchColor(definition.effect);

            UiTheme.DrawPanel(details, accent, false);

            Vector2 previewCenter = new Vector2(details.x + 50f, details.y + 46f);
            UiTheme.DrawDiamond(previewCenter, 64f, new Color(accent.r, accent.g, accent.b, 0.10f));
            UiTheme.DrawDiamond(previewCenter, 44f, accent);
            UiTheme.DrawDiamond(previewCenter, 37f, new Color(0.05f, 0.045f, 0.055f, 1f));
            UiTheme.DrawLabelShadow(
                new Rect(previewCenter.x - 22f, previewCenter.y - 22f, 44f, 44f),
                core ? "核" : definition.glyph,
                _nodeGlyphStyle,
                core ? UiTheme.GoldBright : accent);

            UiTheme.DrawLabelShadow(
                new Rect(details.x + 84f, details.y + 18f, details.width - 100f, 28f),
                name,
                UiTheme.TooltipTitle,
                UiTheme.GoldBright);
            UiTheme.DrawLabelShadow(
                new Rect(details.x + 84f, details.y + 46f, details.width - 100f, 16f),
                core ? "所有分支的起点" : $"{branch}分支 · 永久生效",
                UiTheme.Tiny,
                accent);
            UiTheme.DrawLabelShadow(
                new Rect(details.x + details.width - 68f, details.y + 20f, 52f, 18f),
                core ? "" : $"{level}/{maxLevel}",
                UiTheme.Small,
                level > 0 ? UiTheme.GoldBright : UiTheme.Muted);

            UiTheme.DrawDivider(new Rect(details.x + 16f, details.y + 76f, details.width - 32f, 1f));

            var descriptionRect = new Rect(details.x + 16f, details.y + 84f, details.width - 32f, 56f);
            UiTheme.DrawInset(descriptionRect, new Color(0.028f, 0.025f, 0.036f, 0.98f));
            UiTheme.DrawLabelShadow(
                new Rect(descriptionRect.x + 10f, descriptionRect.y + 8f, descriptionRect.width - 20f, descriptionRect.height - 16f),
                description,
                UiTheme.TooltipBody,
                UiTheme.Text);

            if (core)
            {
                UiTheme.DrawLabelShadow(
                    new Rect(details.x + 18f, details.y + 160f, details.width - 36f, 20f),
                    "核心节点已激活",
                    UiTheme.Small,
                    UiTheme.GoldBright);
                UiTheme.DrawButton(
                    new Rect(details.x + 16f, details.yMax - 44f, details.width - 32f, 34f),
                    "所有分支的起点",
                    false,
                    true,
                    14);
                return;
            }

            UiTheme.DrawLabelShadow(
                new Rect(details.x + 18f, details.y + 148f, 72f, 18f),
                "等级",
                UiTheme.Small,
                UiTheme.Muted);
            UiTheme.DrawLabelShadow(
                new Rect(details.x + 86f, details.y + 148f, details.width - 104f, 18f),
                level > 0 ? "已激活" : "未激活",
                UiTheme.Small,
                level > 0 ? accent : UiTheme.Muted);

            UiTheme.DrawLabelShadow(
                new Rect(details.x + 18f, details.y + 172f, 72f, 18f),
                "花费",
                UiTheme.Small,
                UiTheme.Muted);
            UiTheme.DrawCoinIcon(new Rect(details.x + 88f, details.y + 174f, 12f, 12f));
            UiTheme.DrawLabelShadow(
                new Rect(details.x + 105f, details.y + 172f, details.width - 123f, 18f),
                $"{MetaProgression.GetUpgradeCost(definition)} 局外金币",
                UiTheme.Small,
                UiTheme.GoldBright);

            string prerequisite = string.IsNullOrEmpty(definition.prerequisiteId)
                ? "无"
                : PrerequisiteName(definition.prerequisiteId);
            bool prerequisiteMet = string.IsNullOrEmpty(definition.prerequisiteId) ||
                                   MetaProgression.GetUpgradeLevel(definition.prerequisiteId) > 0;
            UiTheme.DrawLabelShadow(
                new Rect(details.x + 18f, details.y + 196f, 72f, 18f),
                "前置",
                UiTheme.Small,
                UiTheme.Muted);
            UiTheme.DrawLabelShadow(
                new Rect(details.x + 86f, details.y + 196f, details.width - 104f, 18f),
                prerequisite,
                UiTheme.Small,
                prerequisiteMet ? UiTheme.Muted : UiTheme.Danger);

            var purchaseRect = new Rect(details.x + 16f, details.yMax - 44f, details.width - 32f, 34f);
            if (level >= maxLevel)
            {
                UiTheme.DrawButton(purchaseRect, "已激活", false, true, 14);
                return;
            }

            bool canPurchase = MetaProgression.CanPurchaseUpgrade(definition, out string reason);
            string label = canPurchase ? $"激活 · {MetaProgression.GetUpgradeCost(definition)} 金币" : reason;
            if (UiTheme.DrawButton(purchaseRect, label, canPurchase, !canPurchase, 14))
            {
                if (MetaProgression.TryPurchaseUpgrade(definition, out _))
                    AudioService.Play("bookOpen", 0.85f);
            }
        }

        private static Vector2 NodePosition(Vector2 center, MetaUpgradeDefinition definition)
        {
            float radius = definition.ring <= 1 ? 64f : 116f;
            float radians = definition.angleDegrees * Mathf.Deg2Rad;
            return center + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius;
        }

        private static Color BranchColor(MetaUpgradeEffect effect)
        {
            switch (effect)
            {
                case MetaUpgradeEffect.MaxHealth:
                    return new Color(0.96f, 0.36f, 0.40f, 1f);
                case MetaUpgradeEffect.MaxShield:
                    return new Color(0.36f, 0.66f, 1f, 1f);
                case MetaUpgradeEffect.MoveSpeed:
                    return new Color(0.42f, 0.92f, 0.62f, 1f);
                case MetaUpgradeEffect.Damage:
                    return new Color(1f, 0.56f, 0.25f, 1f);
                case MetaUpgradeEffect.CoinBonus:
                    return new Color(1f, 0.82f, 0.34f, 1f);
                case MetaUpgradeEffect.FireInterval:
                    return new Color(0.70f, 0.55f, 1f, 1f);
                default:
                    return UiTheme.Gold;
            }
        }

        private static string BranchName(MetaUpgradeDefinition definition)
        {
            switch (Mathf.RoundToInt(definition.angleDegrees))
            {
                case -90: return "生命";
                case -30: return "护盾";
                case 30: return "移动";
                case 90: return "伤害";
                case 150: return "金币";
                case 210: return "射速";
                default: return "强化";
            }
        }

        private static string PrerequisiteName(string id)
        {
            MetaUpgradeDefinition prerequisite = MetaUpgrades.Find(id);
            return prerequisite != null ? prerequisite.displayName : id;
        }

        private static void DrawHelpContent(Rect panel)
        {
            float leftX = panel.x + 38f;
            float rightX = panel.x + panel.width * 0.5f + 6f;
            float rowY = panel.y + 104f;
            const float rowGap = 26f;
            float colWidth = panel.width * 0.5f - 48f;

            DrawControl(leftX, rowY, colWidth, "移动", "WASD / 方向键");
            DrawControl(leftX, rowY + rowGap, colWidth, "瞄准 / 射击", "鼠标");
            DrawControl(leftX, rowY + rowGap * 2f, colWidth, "切换武器", "Q · 1 · 2");
            DrawControl(leftX, rowY + rowGap * 3f, colWidth, "装填", "R");

            DrawControl(rightX, rowY, colWidth, "拾取 / 购买", "E");
            DrawControl(rightX, rowY + rowGap, colWidth, "地图", "Tab");
            DrawControl(rightX, rowY + rowGap * 2f, colWidth, "背包", "B");
            DrawControl(rightX, rowY + rowGap * 3f, colWidth, "暂停", "Esc");

            UiTheme.DrawLabelShadow(
                new Rect(panel.x + 24f, panel.y + 226f, panel.width - 48f, 18f),
                "战斗房会锁门；清空敌人后房门重新开启。",
                UiTheme.Tiny,
                UiTheme.Muted);
        }

        private static void DrawControl(float x, float y, float width, string label, string value)
        {
            UiTheme.DrawLabelShadow(
                new Rect(x, y, 104f, 22f),
                label,
                UiTheme.Small,
                UiTheme.GoldDim);
            UiTheme.DrawLabelShadow(
                new Rect(x + 108f, y, width - 108f, 22f),
                value,
                UiTheme.Small,
                UiTheme.Text);
        }

        private void HandleKeyboard()
        {
            if (_showHelp)
            {
                if (EscapePressed())
                {
                    _showHelp = false;
                    AudioService.Play("bookFlip1", 0.5f);
                }
                else if (ConfirmPressed())
                {
                    _showHelp = false;
                    AudioService.Play("bookFlip1", 0.5f);
                }

                return;
            }

            if (ConfirmPressed())
                StartGame();
        }

        private void StartGame()
        {
            if (runManager != null && runManager.HasResumableRun)
                ContinueGame();
            else
                StartNewGame();
        }

        private void StartNewGame()
        {
            if (_started || runManager == null) return;
            if (runManager.State != RunManager.RunState.MainMenu) return;

            _started = true;
            AudioService.Play("bookOpen", 0.85f);
            SetPlayerVisible(true);

            if (!runManager.StartNewRun())
            {
                _started = false;
                SetPlayerVisible(false);
                return;
            }

            HideMenu();
        }

        private void ContinueGame()
        {
            if (_started || runManager == null) return;
            if (runManager.State != RunManager.RunState.MainMenu) return;

            if (!runManager.HasResumableRun)
            {
                StartNewGame();
                return;
            }

            _started = true;
            AudioService.Play("bookOpen", 0.85f);
            SetPlayerVisible(true);

            if (!runManager.ResumeRun())
            {
                _started = false;
                SetPlayerVisible(false);
                return;
            }

            HideMenu();
        }

        /// <summary>从暂停菜单保存并返回主页时重新启用标题界面。</summary>
        public void ShowMenu()
        {
            _visible = true;
            _started = false;
            _showHelp = false;
            _showSkillTree = false;
            enabled = true;
            Time.timeScale = 0f;

            _playerRenderers = null;
            CachePlayerRenderers();
            SetPlayerVisible(false);
        }

        private void HideMenu()
        {
            _visible = false;
            SetPlayerVisible(true);
            enabled = false;
        }

        private void CachePlayerRenderers()
        {
            if (_playerRenderers != null && _playerRenderers.Length > 0) return;

            PlayerRoomTracker tracker = FindFirstObjectByType<PlayerRoomTracker>();
            if (tracker == null) return;

            _playerRenderers = tracker.GetComponentsInChildren<SpriteRenderer>(true);
        }

        private void SetPlayerVisible(bool visible)
        {
            if (_playerRenderers == null) return;

            for (int i = 0; i < _playerRenderers.Length; i++)
            {
                if (_playerRenderers[i] != null)
                    _playerRenderers[i].forceRenderingOff = !visible;
            }
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null) return;

            _titleStyle = new GUIStyle(UiTheme.TitleCenter)
            {
                fontSize = 36,
            };
            _treeTitleStyle = new GUIStyle(UiTheme.TitleCenter)
            {
                fontSize = 24,
            };
            _nodeGlyphStyle = new GUIStyle(UiTheme.TitleCenter)
            {
                fontSize = 17,
            };
            _coreGlyphStyle = new GUIStyle(UiTheme.TitleCenter)
            {
                fontSize = 22,
            };
        }

        private void QuitGame()
        {
            AudioService.Play("bookClose", 0.8f);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static bool ConfirmPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            return keyboard != null &&
                   (keyboard.enterKey.wasPressedThisFrame ||
                    keyboard.numpadEnterKey.wasPressedThisFrame ||
                    keyboard.spaceKey.wasPressedThisFrame);
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Return) ||
                   Input.GetKeyDown(KeyCode.KeypadEnter) ||
                   Input.GetKeyDown(KeyCode.Space);
#else
            return false;
#endif
        }

        private static bool EscapePressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Escape);
#else
            return false;
#endif
        }

        /// <summary>
        /// 即使场景里忘记挂主菜单组件，也保证启动后能进入 MainMenu，而不是无 UI 冻结。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInstance()
        {
            if (FindFirstObjectByType<MainMenuUI>() != null) return;
            if (FindFirstObjectByType<RunManager>() == null) return;

            var menu = new GameObject("MainMenu");
            menu.AddComponent<MainMenuUI>();
        }
    }
}
