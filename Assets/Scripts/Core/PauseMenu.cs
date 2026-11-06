using Roguelike.Audio;
using Roguelike.Combat;
using Roguelike.UI;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Roguelike.Core
{
    /// <summary>
    /// Esc 暂停：冻结时间并显示像素地牢风格的暂停面板。
    /// 结算界面（失败 / 通关）打开时自动退出暂停。
    /// </summary>
    public sealed class PauseMenu : MonoBehaviour
    {
        [SerializeField] private RunManager runManager;

        private bool _paused;
        private bool _musicDragging;
        private bool _sfxDragging;

        public bool IsPaused => _paused;

        private void Awake()
        {
            if (runManager == null) runManager = FindFirstObjectByType<RunManager>();
        }

        private void Update()
        {
            if (runManager != null && runManager.State != RunManager.RunState.Playing)
            {
                if (_paused) SetPaused(false);
                return;
            }

            if (CombatDebugHud.IsInventoryOpen) return;
            if (WasPausePressed()) SetPaused(!_paused);
        }

        public void SetPaused(bool paused)
        {
            if (_paused && !paused)
                AudioService.SaveAudioSettings();

            _paused = paused;
            Time.timeScale = paused ? 0f : 1f;
        }

        private void OnDisable()
        {
            AudioService.SaveAudioSettings();
            if (!_paused) return;

            _paused = false;
            Time.timeScale = 1f;
        }

        private void OnGUI()
        {
            if (!_paused) return;

            UiTheme.DrawDim();

            float width = Mathf.Min(470f, Screen.width - 60f);
            float height = 318f;
            var panel = new Rect(
                (Screen.width - width) * 0.5f,
                Mathf.Max(64f, (Screen.height - height) * 0.5f),
                width,
                height);

            UiTheme.DrawPanel(panel, UiTheme.Gold, false);

            UiTheme.DrawLabelShadow(
                new Rect(panel.x + 24f, panel.y + 14f, panel.width - 48f, 34f),
                "暂 停",
                UiTheme.TitleCenter,
                UiTheme.GoldBright);

            UiTheme.DrawLabelShadow(
                new Rect(panel.x + 24f, panel.y + 46f, panel.width - 48f, 18f),
                "地牢的回声暂时沉寂",
                UiTheme.Center,
                UiTheme.Muted);

            UiTheme.DrawDivider(new Rect(panel.x + 26f, panel.y + 72f, panel.width - 52f, 1f));

            float row1 = panel.y + 94f;
            float row2 = panel.y + 158f;
            float sliderX = panel.x + 176f;
            float sliderWidth = panel.width - 260f;

            DrawVolumeRow(
                panel,
                row1,
                "音乐音量",
                AudioService.MusicVolume,
                sliderX,
                sliderWidth,
                ref _musicDragging,
                UiTheme.Gold,
                value => AudioService.SetMusicVolume(value));

            DrawVolumeRow(
                panel,
                row2,
                "音效音量",
                AudioService.SfxVolume,
                sliderX,
                sliderWidth,
                ref _sfxDragging,
                UiTheme.Shield,
                value => AudioService.SetSfxVolume(value));

            if (UiTheme.DrawButton(
                    new Rect(panel.center.x - 88f, panel.yMax - 54f, 176f, 36f),
                    "继续游戏  Esc",
                    true,
                    false,
                    15))
            {
                SetPaused(false);
            }
        }

        private static void DrawVolumeRow(
            Rect panel,
            float y,
            string label,
            float value,
            float sliderX,
            float sliderWidth,
            ref bool dragging,
            Color fill,
            System.Action<float> apply)
        {
            UiTheme.DrawLabelShadow(
                new Rect(panel.x + 28f, y, 140f, 22f),
                label,
                UiTheme.Body,
                UiTheme.Text);

            var slider = new Rect(sliderX, y + 2f, sliderWidth, 20f);
            float next = DrawVolumeSlider(slider, value, ref dragging, fill);
            if (!Mathf.Approximately(next, value)) apply(next);

            UiTheme.DrawLabelShadow(
                new Rect(panel.x + panel.width - 58f, y, 42f, 22f),
                $"{Mathf.RoundToInt(next * 100f)}%",
                UiTheme.Small,
                UiTheme.GoldBright);
        }

        private static float DrawVolumeSlider(Rect rect, float value, ref bool dragging, Color fill)
        {
            UiTheme.DrawInset(rect, new Color(0.035f, 0.03f, 0.045f, 1f));

            float fillWidth = Mathf.Max(0f, (rect.width - 4f) * Mathf.Clamp01(value));
            if (fillWidth > 0f)
            {
                UiTheme.Fill(new Rect(rect.x + 2f, rect.y + 2f, fillWidth, rect.height - 4f), fill);
                UiTheme.Fill(new Rect(rect.x + 2f, rect.y + 2f, fillWidth, 1f), new Color(1f, 1f, 1f, 0.18f));
            }

            float handleX = rect.x + 2f + (rect.width - 4f) * Mathf.Clamp01(value);
            var handle = new Rect(handleX - 5f, rect.y - 3f, 10f, rect.height + 6f);
            UiTheme.Fill(handle, UiTheme.GoldBright);
            UiTheme.DrawOutline(handle, new Color(0.18f, 0.13f, 0.05f, 1f), 1f);

            Event e = Event.current;
            if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
            {
                dragging = true;
                e.Use();
            }

            if (dragging)
            {
                if (e.type == EventType.MouseDrag || e.type == EventType.MouseDown)
                {
                    value = Mathf.Clamp01((e.mousePosition.x - rect.x) / Mathf.Max(1f, rect.width));
                    e.Use();
                }
                else if (e.type == EventType.MouseUp)
                {
                    value = Mathf.Clamp01((e.mousePosition.x - rect.x) / Mathf.Max(1f, rect.width));
                    dragging = false;
                    e.Use();
                }
            }

            return value;
        }

        private static bool WasPausePressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null) return keyboard.escapeKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Escape);
#else
            return false;
#endif
        }
    }
}
