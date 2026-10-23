using Roguelike.Combat;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Roguelike.Core
{
    /// <summary>
    /// Esc 暂停：冻结时间并显示暂停覆盖层；再次按 Esc 继续。
    /// 结算界面（失败 / 通关）打开时自动退出暂停。
    /// </summary>
    public sealed class PauseMenu : MonoBehaviour
    {
        [SerializeField] private RunManager runManager;

        private bool _paused;
        private GUIStyle _titleStyle;
        private GUIStyle _hintStyle;

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

            // 背包打开时 Esc 不叠加暂停菜单，避免两个覆盖层互相恢复时间
            if (CombatDebugHud.IsInventoryOpen) return;

            if (WasPausePressed()) SetPaused(!_paused);
        }

        public void SetPaused(bool paused)
        {
            _paused = paused;
            Time.timeScale = paused ? 0f : 1f;
        }

        private void OnDisable()
        {
            if (!_paused) return;

            _paused = false;
            Time.timeScale = 1f;
        }

        private void OnGUI()
        {
            if (!_paused) return;

            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 40, alignment = TextAnchor.MiddleCenter };
                _hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            }

            _titleStyle.normal.textColor = Color.white;
            _hintStyle.normal.textColor = new Color(0.85f, 0.85f, 0.9f);

            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previous;

            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;

            GUI.Label(new Rect(cx - 300f, cy - 70f, 600f, 60f), "已暂停", _titleStyle);
            GUI.Label(new Rect(cx - 300f, cy, 600f, 30f), "按 Esc 继续", _hintStyle);
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
