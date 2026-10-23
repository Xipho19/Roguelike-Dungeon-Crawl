using System.Collections;
using Roguelike.Audio;
using UnityEngine;

namespace Roguelike.Dungeon
{
    /// <summary>
    /// 单个门洞的封门板。
    ///
    /// 结构：
    ///   DoorBlocker_&lt;Side&gt;（BoxCollider2D，锁门时启用）
    ///     └── Panel（SpriteRenderer，做淡入淡出 + 轻微缩放动画）
    ///
    /// 碰撞体在锁门瞬间启用，保证动画期间也不会漏人；
    /// 开锁时先播动画，动画结束前碰撞体已经关闭。
    /// </summary>
    public sealed class RoomDoorBlocker : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer panel;
        [SerializeField] private BoxCollider2D blockerCollider;

        [Tooltip("封门 / 开门动画时长（秒）")]
        [SerializeField] private float duration = 0.16f;

        [Tooltip("面板完整显示时的缩放，由工厂写入")]
        [SerializeField] private Vector3 fullScale = Vector3.one;

        private Coroutine _routine;
        private bool _stateInitialized;

        public bool IsLocked { get; private set; }

        public void Setup(SpriteRenderer panelRenderer, BoxCollider2D collider)
        {
            panel = panelRenderer;
            blockerCollider = collider;

            if (panel != null)
                fullScale = panel.transform.localScale;

            if (blockerCollider != null)
                blockerCollider.enabled = false;

            SetPanelAlpha(0f);
        }

        /// <summary>锁门 / 开门。instant = true 时跳过动画（初始化用）。</summary>
        public void SetLocked(bool locked, bool instant)
        {
            bool changed = _stateInitialized && IsLocked != locked;
            IsLocked = locked;

            if (blockerCollider != null)
                blockerCollider.enabled = locked;

            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            if (!instant && changed)
                AudioService.Play(locked ? "doorClose_1" : "doorOpen_1", 0.6f);

            _stateInitialized = true;

            if (instant || !isActiveAndEnabled)
            {
                Apply(locked ? 1f : 0f);
                return;
            }

            _routine = StartCoroutine(Animate(locked ? 1f : 0f));
        }

        private IEnumerator Animate(float target)
        {
            float start = panel != null ? panel.color.a : 0f;
            float elapsed = 0f;

            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime / Mathf.Max(0.01f, duration);
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed));
                Apply(Mathf.Lerp(start, target, k));
                yield return null;
            }

            Apply(target);
            _routine = null;
        }

        private void Apply(float alpha01)
        {
            if (panel == null) return;

            SetPanelAlpha(alpha01);
            panel.transform.localScale = fullScale * Mathf.Lerp(0.72f, 1f, alpha01);
        }

        private void SetPanelAlpha(float alpha)
        {
            if (panel == null) return;

            Color color = panel.color;
            color.a = alpha;
            panel.color = color;
        }
    }
}
