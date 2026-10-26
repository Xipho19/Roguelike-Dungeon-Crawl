using UnityEngine;

namespace Roguelike.Combat
{
    /// <summary>自爆敌人的一次性爆炸闪光：快速放大并淡出。</summary>
    public sealed class ExplosionFlash : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private float _duration;
        private float _startScale;
        private float _endScale;
        private float _elapsed;

        public static void Spawn(Vector2 position, Sprite sprite, float radius, float duration = 0.28f)
        {
            if (sprite == null) return;

            var go = new GameObject("ExplosionFlash");
            go.transform.position = position;

            var flash = go.AddComponent<ExplosionFlash>();
            flash.Initialize(sprite, radius, duration);
        }

        private void Initialize(Sprite sprite, float radius, float duration)
        {
            _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sprite = sprite;
            _renderer.sortingOrder = 9;
            _renderer.color = Color.white;

            _duration = Mathf.Max(0.05f, duration);

            // 贴图约 1.5 世界单位宽，放大到直径约等于 2×爆炸半径
            _endScale = Mathf.Max(0.1f, radius * 1.35f);
            _startScale = _endScale * 0.3f;

            transform.localScale = Vector3.one * _startScale;
            _elapsed = 0f;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;

            float k = Mathf.Clamp01(_elapsed / _duration);
            transform.localScale = Vector3.one * Mathf.Lerp(_startScale, _endScale, k);

            Color color = _renderer.color;
            color.a = 1f - k;
            _renderer.color = color;

            if (k >= 1f) Destroy(gameObject);
        }
    }
}
