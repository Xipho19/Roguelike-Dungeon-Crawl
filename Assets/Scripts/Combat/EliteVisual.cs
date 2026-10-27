using UnityEngine;

namespace Roguelike.Combat
{
    /// <summary>
    /// 精英怪的视觉标记：四向偏移的彩色剪影形成描边，外加一圈脉动光晕。
    /// 不使用额外 Shader，直接复制敌人的当前动画帧，兼容所有 SpriteAnimator 敌人。
    /// </summary>
    public sealed class EliteVisual : MonoBehaviour
    {
        private static readonly Color OutlineColor = new Color(1f, 0.72f, 0.18f, 1f);
        private static readonly Vector3[] OutlineOffsets =
        {
            Vector3.up,
            Vector3.down,
            Vector3.left,
            Vector3.right,
        };

        private SpriteRenderer _source;
        private SpriteRenderer _glow;
        private SpriteRenderer[] _outline;
        private bool _built;

        public static void Attach(Enemy enemy)
        {
            if (enemy == null) return;
            if (enemy.GetComponent<EliteVisual>() != null) return;

            var source = enemy.GetComponent<SpriteRenderer>();
            if (source == null) return;

            var visual = enemy.gameObject.AddComponent<EliteVisual>();
            visual._source = source;
        }

        private void LateUpdate()
        {
            if (_source == null) return;

            if (!_built) BuildLayers();

            Sprite sprite = _source.sprite;
            bool visible = sprite != null && _source.enabled;
            int order = _source.sortingOrder;

            if (_glow != null)
            {
                _glow.sprite = sprite;
                _glow.flipX = _source.flipX;
                _glow.enabled = visible;
                _glow.sortingOrder = order - 2;

                float pulse = (Mathf.Sin(Time.time * 4f) + 1f) * 0.5f;
                Color glow = OutlineColor;
                glow.a = Mathf.Lerp(0.14f, 0.32f, pulse);
                _glow.color = glow;
                _glow.transform.localScale = Vector3.one * Mathf.Lerp(1.28f, 1.42f, pulse);
            }

            for (int i = 0; i < _outline.Length; i++)
            {
                SpriteRenderer layer = _outline[i];
                if (layer == null) continue;

                layer.sprite = sprite;
                layer.flipX = _source.flipX;
                layer.enabled = visible;
                layer.sortingOrder = order - 1;
                layer.transform.localPosition = OutlineOffsets[i] * 0.055f;
            }
        }

        private void BuildLayers()
        {
            _built = true;

            _glow = CreateLayer("EliteGlow", 1.36f);
            _outline = new SpriteRenderer[OutlineOffsets.Length];

            for (int i = 0; i < _outline.Length; i++)
                _outline[i] = CreateLayer("EliteOutline_" + i, 1.04f);
        }

        private SpriteRenderer CreateLayer(string layerName, float scale)
        {
            var go = new GameObject(layerName);
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * scale;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = _source.sprite;
            renderer.color = OutlineColor;
            renderer.enabled = false;
            return renderer;
        }
    }
}