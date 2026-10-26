using Roguelike.Audio;
using Roguelike.Player;
using UnityEngine;

namespace Roguelike.Combat
{
    /// <summary>
    /// 自爆史莱姆：贴近玩家后停下并闪烁引信，随后范围爆炸。
    /// 被眼泪提前击杀时同样会爆炸，所以远程击杀也要注意距离。
    /// </summary>
    public sealed class BomberEnemy : Enemy
    {
        [Header("自爆")]
        [SerializeField] private float triggerRange = 2.4f;
        [SerializeField] private float fuseTime = 0.85f;
        [SerializeField] private float explosionRadius = 2.3f;
        [SerializeField] private int explosionDamage = 1;
        [SerializeField] private Sprite explosionSprite;

        private SpriteRenderer _renderer;
        private bool _fusing;
        private bool _exploded;
        private float _fuseTimer;

        protected override void Start()
        {
            base.Start();

            _renderer = GetComponentInChildren<SpriteRenderer>();
            if (_renderer != null) _baseColor = _renderer.color;
        }

        protected override void FixedUpdate()
        {
            if (IsDead || _target == null || _rb == null) return;

            if (_fusing)
            {
                _rb.linearVelocity = Vector2.zero;
                _fuseTimer -= Time.fixedDeltaTime;

                if (_renderer != null)
                {
                    bool bright = Mathf.FloorToInt(_fuseTimer * 12f) % 2 == 0;
                    _renderer.color = bright
                        ? new Color(1f, 0.92f, 0.45f, 1f)
                        : new Color(1f, 0.45f, 0.18f, 1f);
                }

                if (_fuseTimer <= 0f)
                {
                    if (_renderer != null) _renderer.color = _baseColor;
                    Die();
                }

                return;
            }

            Vector2 toTarget = (Vector2)_target.position - _rb.position;
            float distance = toTarget.magnitude;
            Vector2 direction = distance > 0.0001f ? toTarget / distance : Vector2.zero;

            if (distance <= triggerRange)
            {
                BeginFuse();
                return;
            }

            Vector2 steered = GetSteeredDirection(direction);
            _rb.linearVelocity = steered * moveSpeed;
        }

        private void BeginFuse()
        {
            if (_fusing || IsDead) return;

            _fusing = true;
            _fuseTimer = fuseTime;
            _rb.linearVelocity = Vector2.zero;
        }

        protected override void Die()
        {
            if (!_exploded)
            {
                _exploded = true;
                Explode();
            }

            base.Die();
        }

        private void Explode()
        {
            Vector2 center = _rb != null ? _rb.position : (Vector2)transform.position;

            ExplosionFlash.Spawn(center, explosionSprite, explosionRadius);
            AudioService.Play("explosion", 0.9f);

            Collider2D[] hits = Physics2D.OverlapCircleAll(center, explosionRadius);
            for (int i = 0; i < hits.Length; i++)
            {
                var health = hits[i].GetComponentInParent<PlayerHealth>();
                if (health == null) continue;

                health.TryDamage(explosionDamage);
                break;
            }
        }
    }
}
