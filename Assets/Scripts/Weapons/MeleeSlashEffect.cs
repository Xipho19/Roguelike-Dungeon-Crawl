using Roguelike.Visual;
using UnityEngine;

namespace Roguelike.Weapons
{
    /// <summary>
    /// 武士刀挥砍的短命刀光。
    /// 三段斩使用不同的起始/结束角度、尺寸和颜色，让每一刀都有明显区别。
    /// </summary>
    public sealed class MeleeSlashEffect : MonoBehaviour
    {
        private Sprite[] _frames;
        private SpriteRenderer _renderer;
        private float _elapsed;
        private float _lifetime = 0.2f;
        private float _framesPerSecond = 30f;
        private float _startAngle;
        private float _endAngle;
        private float _startScale = 1f;
        private float _endScale = 1f;
        private float _startAlpha = 1f;

        public static void Spawn(
            Sprite[] frames,
            Vector2 position,
            float angle,
            float scale,
            Color color,
            float startAngleOffset,
            float endAngleOffset,
            float endScale,
            float lifetime,
            float framesPerSecond = 24f)
        {
            if (frames == null || frames.Length == 0 || frames[0] == null)
            {
                Debug.LogWarning("[MeleeSlash] 武器没有可用的 slashFrames，刀光无法生成。" +
                                 "请执行 Tools/Roguelike/生成武器资产。");
                return;
            }

            var root = new GameObject("MeleeSlash");
            root.transform.position = new Vector3(position.x, position.y, 0f);
            root.transform.rotation = Quaternion.Euler(0f, 0f, angle + startAngleOffset);
            root.transform.localScale = Vector3.one * Mathf.Max(0.1f, scale);

            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = frames[0];
            renderer.color = color;
            renderer.sortingOrder = DepthSort.Effect;

            var effect = root.AddComponent<MeleeSlashEffect>();
            effect._frames = frames;
            effect._renderer = renderer;
            effect._lifetime = Mathf.Max(0.05f, lifetime);
            effect._framesPerSecond = Mathf.Max(1f, framesPerSecond);
            effect._startAngle = angle + startAngleOffset;
            effect._endAngle = angle + endAngleOffset;
            effect._startScale = Mathf.Max(0.1f, scale);
            effect._endScale = Mathf.Max(0.1f, endScale);
            effect._startAlpha = Mathf.Clamp01(color.a);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _lifetime);

            transform.rotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Lerp(_startAngle, _endAngle, t));
            transform.localScale = Vector3.one * Mathf.Lerp(_startScale, _endScale, t);

            if (_renderer != null && _frames != null && _frames.Length > 0)
            {
                int frame = Mathf.FloorToInt(_elapsed * _framesPerSecond);
                if (frame >= _frames.Length) frame = _frames.Length - 1;
                _renderer.sprite = _frames[frame];

                Color color = _renderer.color;
                color.a = _startAlpha * (1f - t);
                _renderer.color = color;
            }

            if (_elapsed >= _lifetime)
                Destroy(gameObject);
        }
    }
}
