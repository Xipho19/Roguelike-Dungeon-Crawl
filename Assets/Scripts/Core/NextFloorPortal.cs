using Roguelike.Player;
using Roguelike.UI;
using UnityEngine;

namespace Roguelike.Core
{
    /// <summary>
    /// Boss 被击败后出现的传送门。
    ///
    /// 玩家进入触发范围时暂停游戏并弹出「是 / 否」提示：
    ///   是 → 由 RunManager 进入下一层
    ///   否 → 关闭提示，玩家可以继续在本层探索；离开后再靠近会重新提示
    /// </summary>
    public sealed class NextFloorPortal : MonoBehaviour
    {
        [SerializeField] private float spinSpeed = 35f;
        [SerializeField] private float pulseSpeed = 3f;
        [SerializeField] private float pulseAmount = 0.06f;

        private RunManager _runManager;
        private bool _promptVisible;
        private bool _dismissed;
        private bool _pausedByPrompt;

        public bool IsPromptVisible => _promptVisible;

        public void Initialize(RunManager runManager)
        {
            _runManager = runManager;
        }

        private void Update()
        {
            float time = Time.unscaledTime;
            transform.rotation = Quaternion.Euler(0f, 0f, time * spinSpeed);
            transform.localScale = Vector3.one * (1f + Mathf.Sin(time * pulseSpeed) * pulseAmount);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerHealth>() == null) return;
            if (_dismissed) return;
            ShowPrompt();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerHealth>() == null) return;
            _dismissed = false;
            HidePrompt();
        }

        private void ShowPrompt()
        {
            if (_promptVisible) return;

            _promptVisible = true;
            _pausedByPrompt = Time.timeScale > 0f;
            if (_pausedByPrompt) Time.timeScale = 0f;
        }

        private void HidePrompt()
        {
            if (!_promptVisible) return;

            _promptVisible = false;
            if (_pausedByPrompt) Time.timeScale = 1f;
            _pausedByPrompt = false;
        }

        /// <summary>点击「是」：进入下一层。</summary>
        public void Confirm()
        {
            if (!_promptVisible) return;

            _promptVisible = false;
            if (_pausedByPrompt) Time.timeScale = 1f;
            _pausedByPrompt = false;

            if (_runManager != null) _runManager.DescendToNextFloor();
        }

        /// <summary>点击「否」：关闭提示，留本层继续探索。</summary>
        public void Decline()
        {
            if (!_promptVisible) return;

            _promptVisible = false;
            _dismissed = true;
            if (_pausedByPrompt) Time.timeScale = 1f;
            _pausedByPrompt = false;
        }

        private void OnDisable()
        {
            if (_pausedByPrompt) Time.timeScale = 1f;
            _pausedByPrompt = false;
        }

        private void OnDestroy()
        {
            if (_pausedByPrompt) Time.timeScale = 1f;
            _pausedByPrompt = false;
        }

        private void OnGUI()
        {
            if (!_promptVisible) return;

            UiTheme.DrawDim();

            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;
            var panel = new Rect(cx - 240f, cy - 138f, 480f, 276f);
            UiTheme.DrawPanel(panel, UiTheme.Mana, false);

            UiTheme.DrawLabelShadow(
                new Rect(panel.x + 24f, panel.y + 18f, panel.width - 48f, 34f),
                "传 送 门",
                UiTheme.TitleCenter,
                new Color(0.80f, 0.74f, 1f));

            UiTheme.DrawLabelShadow(
                new Rect(panel.x + 24f, panel.y + 54f, panel.width - 48f, 16f),
                "ANCIENT GATE",
                UiTheme.Tiny,
                UiTheme.Muted);

            UiTheme.DrawDivider(new Rect(panel.x + 30f, panel.y + 78f, panel.width - 60f, 1f));

            string question = _runManager != null
                ? $"进入第 {_runManager.CurrentFloor + 1} 层？"
                : "是否进入下一层？";
            UiTheme.DrawLabelShadow(
                new Rect(panel.x + 24f, panel.y + 98f, panel.width - 48f, 32f),
                question,
                UiTheme.TitleCenter,
                UiTheme.Text);

            UiTheme.DrawLabelShadow(
                new Rect(panel.x + 24f, panel.y + 138f, panel.width - 48f, 20f),
                "选择「否」可以留在本层继续探索",
                UiTheme.Center,
                UiTheme.Muted);

            float buttonY = panel.yMax - 62f;
            if (UiTheme.DrawButton(
                    new Rect(cx - 156f, buttonY, 136f, 42f),
                    "否",
                    false,
                    false,
                    16))
            {
                Decline();
            }

            if (UiTheme.DrawButton(
                    new Rect(cx + 20f, buttonY, 136f, 42f),
                    "是",
                    true,
                    false,
                    16))
            {
                Confirm();
            }
        }
    }
}
