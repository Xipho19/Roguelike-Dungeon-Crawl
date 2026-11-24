using UnityEngine;

namespace Roguelike.Combat
{
    /// <summary>
    /// 子弹柔光子节点：复制子弹本体颜色，并做轻微呼吸式缩放。
    /// 玩家眼泪、敌人弹丸和 Boss 弹幕共用这一套光效。
    /// </summary>
    public sealed class ProjectileGlow : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer source;
        [SerializeField] private SpriteRenderer glow;
        [SerializeField, Range(0f, 1f)] private float alpha = 0.62f;

        [Header("呼吸")]
        [SerializeField] private float pulseScale = 0.08f;
        [SerializeField] private float pulseSpeed = 9f;

        private Vector3 _baseScale;
        private float _phase;

        private void Awake()
        {
            if (glow == null) glow = GetComponent<SpriteRenderer>();

            if (source == null)
            {
                SpriteRenderer[] parents = GetComponentsInParent<SpriteRenderer>();
                for (int i = 0; i < parents.Length; i++)
                {
                    if (parents[i] != null && parents[i] != glow)
                    {
                        source = parents[i];
                        break;
                    }
                }
            }

            _baseScale = transform.localScale;
            _phase = Random.value * Mathf.PI * 2f;
        }

        private void OnEnable()
        {
            ApplyGlow();
        }

        private void LateUpdate()
        {
            ApplyGlow();

            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed + _phase) * pulseScale;
            transform.localScale = _baseScale * pulse;
        }

        /// <summary>立即同步颜色；子弹染色/缩放后调用，避免出现一帧白色光晕。</summary>
        public void SyncNow()
        {
            ApplyGlow();
        }

        private void ApplyGlow()
        {
            if (source == null || glow == null) return;

            Color color = source.color;
            color.a = alpha;
            glow.color = color;
        }
    }
}
