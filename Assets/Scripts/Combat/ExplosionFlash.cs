using UnityEngine;

namespace Roguelike.Combat
{
    /// <summary>一次性爆炸特效：播放拆帧后的爆炸序列，同时快速放大并淡出。</summary>
    public sealed class ExplosionFlash : MonoBehaviour
    {
        private static Sprite[] _cachedFrames;

        private SpriteRenderer _renderer;
        private Sprite[] _frames;
        private float _duration;
        private float _startScale;
        private float _endScale;
        private float _elapsed;

        public static void Spawn(Vector2 position, Sprite sprite, float radius, float duration = 0.28f)
        {
            // 序列帧存在时，旧 Sprite 只作为缺失回退，不再要求调用方绑定它。
            if (sprite == null && LoadFrames().Length == 0) return;

            var go = new GameObject("ExplosionFlash");
            go.transform.position = position;

            var flash = go.AddComponent<ExplosionFlash>();
            flash.Initialize(sprite, radius, duration);
        }

        private void Initialize(Sprite sprite, float radius, float duration)
        {
            _renderer = gameObject.AddComponent<SpriteRenderer>();
            _frames = LoadFrames();
            _renderer.sprite = _frames.Length > 0 ? _frames[0] : sprite;
            _renderer.sortingOrder = 9;
            _renderer.color = Color.white;

            _duration = Mathf.Max(0.05f, duration);

            // 贴图约 1.5 世界单位宽，放大到直径约等于 2×爆炸半径
            _endScale = Mathf.Max(0.1f, radius * 1.35f);
            _startScale = _endScale * 0.3f;

            transform.localScale = Vector3.one * _startScale;
            _elapsed = 0f;
        }

        private static Sprite[] LoadFrames()
        {
            if (_cachedFrames != null) return _cachedFrames;

            var frames = new Sprite[8];
            int count = 0;

            for (int i = 0; i < frames.Length; i++)
            {
                Sprite frame = Resources.Load<Sprite>($"Explosion/Explosion_{i}");
                if (frame != null) frames[count++] = frame;
            }

            if (count != frames.Length)
                System.Array.Resize(ref frames, count);

            _cachedFrames = frames;
            return _cachedFrames;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;

            float k = Mathf.Clamp01(_elapsed / _duration);
            transform.localScale = Vector3.one * Mathf.Lerp(_startScale, _endScale, k);

            if (_frames != null && _frames.Length > 0)
            {
                int frameIndex = Mathf.Min(_frames.Length - 1, Mathf.FloorToInt(k * _frames.Length));
                _renderer.sprite = _frames[frameIndex];
            }

            Color color = _renderer.color;
            color.a = 1f - k;
            _renderer.color = color;

            if (k >= 1f) Destroy(gameObject);
        }
    }
}
