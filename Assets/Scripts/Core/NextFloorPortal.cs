using Roguelike.Player;
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
        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _hintStyle;

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

            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 30,
                    alignment = TextAnchor.MiddleCenter,
                };
                _titleStyle.normal.textColor = new Color(0.75f, 0.90f, 1f);

                _bodyStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 20,
                    alignment = TextAnchor.MiddleCenter,
                };
                _bodyStyle.normal.textColor = Color.white;

                _hintStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    alignment = TextAnchor.MiddleCenter,
                };
                _hintStyle.normal.textColor = new Color(0.75f, 0.75f, 0.8f);
            }

            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.62f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previous;

            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;
            var panel = new Rect(cx - 230f, cy - 120f, 460f, 250f);

            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(panel.x, panel.y + 20f, panel.width, 40f), "传送门", _titleStyle);

            string question = _runManager != null
                ? $"进入第 {_runManager.CurrentFloor + 1} 层？"
                : "是否进入下一层？";
            GUI.Label(new Rect(panel.x, panel.y + 76f, panel.width, 32f), question, _bodyStyle);
            GUI.Label(new Rect(panel.x, panel.y + 116f, panel.width, 24f), "选择「否」可以留在本层继续探索", _hintStyle);

            if (GUI.Button(new Rect(cx - 150f, panel.y + 164f, 130f, 46f), "否"))
                Decline();

            if (GUI.Button(new Rect(cx + 20f, panel.y + 164f, 130f, 46f), "是"))
                Confirm();
        }
    }
}
