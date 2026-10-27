using System;
using Roguelike.Audio;
using Roguelike.Items;
using UnityEngine;

namespace Roguelike.Player
{
    /// <summary>
    /// 玩家生命值。
    ///
    /// 生命上限来自 PlayerStats（会被道具修改）；受击后有无敌时间并闪烁。
    /// 血量归零时触发 Died 事件，由 RunManager 负责结算与重开。
    /// </summary>
    public sealed class PlayerHealth : MonoBehaviour
    {
        [Tooltip("没有 PlayerStats 时的回退上限")]
        [SerializeField] private int maxHealth = 6;

        [SerializeField] private float invulnerableDuration = 0.9f;

        [Header("护盾")]
        [Tooltip("护盾上限；开局和重开时充满")]
        [SerializeField, Min(0)] private int maxShield = 5;

        [Tooltip("最后一次受伤后，等待多少秒开始恢复护盾")]
        [SerializeField, Min(0f)] private float shieldRegenDelay = 5f;

        [Tooltip("恢复护盾的间隔（秒）")]
        [SerializeField, Min(0.1f)] private float shieldRegenInterval = 1f;

        private int _health;
        private int _maxHealth;
        private int _shield;
        private float _lastDamageTime = -999f;
        private float _nextShieldRegenTime;
        private float _invulnerableUntil;
        private Rigidbody2D _rb;
        private PlayerStats _stats;
        private PlayerController _controller;
        private PlayerCombat _combat;
        private SpriteRenderer _renderer;
        private Color _baseColor;

        public int Health => _health;
        public int MaxHealth => _maxHealth;
        public int Shield => _shield;
        public int MaxShield => maxShield;
        public bool IsInvulnerable => Time.time < _invulnerableUntil;
        public bool IsDead => _health <= 0;

        public event Action Died;

        /// <summary>受到有效伤害时触发（无敌帧内不触发），供相机震动等反馈使用。</summary>
        public event Action Damaged;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _stats = GetComponent<PlayerStats>();
            _controller = GetComponent<PlayerController>();
            _combat = GetComponent<PlayerCombat>();
            _renderer = GetComponentInChildren<SpriteRenderer>();

            _maxHealth = _stats != null ? _stats.MaxHealth : maxHealth;
            _health = _maxHealth;
            _shield = Mathf.Max(0, maxShield);

            if (_renderer != null) _baseColor = _renderer.color;
            if (_stats != null) _stats.StatsChanged += HandleStatsChanged;
        }

        private void OnDestroy()
        {
            if (_stats != null) _stats.StatsChanged -= HandleStatsChanged;
        }

        private void Update()
        {
            UpdateShieldRegen();

            if (_renderer == null) return;

            if (IsInvulnerable && !IsDead)
            {
                Color color = _baseColor;
                color.a = Mathf.PingPong(Time.time * 12f, 0.55f) + 0.35f;
                _renderer.color = color;
            }
            else if (_renderer.color != _baseColor)
            {
                _renderer.color = _baseColor;
            }
        }

        /// <summary>脱战 5 秒后，每秒恢复 1 点护盾，直到上限。</summary>
        private void UpdateShieldRegen()
        {
            if (IsDead || _shield >= maxShield) return;
            if (Time.time - _lastDamageTime < shieldRegenDelay) return;
            if (Time.time < _nextShieldRegenTime) return;

            _shield++;
            _nextShieldRegenTime = Time.time + shieldRegenInterval;
        }

        /// <summary>生命上限变化时：新增的容器直接补满，减少时夹紧当前血量。</summary>
        private void HandleStatsChanged()
        {
            if (_stats == null) return;

            int newMax = _stats.MaxHealth;

            if (newMax > _maxHealth)
                _health += newMax - _maxHealth;

            _maxHealth = newMax;
            _health = Mathf.Clamp(_health, IsDead ? 0 : 1, _maxHealth);
        }

        /// <summary>尝试对玩家造成伤害。无敌中或已死亡返回 false。</summary>
        public bool TryDamage(int amount)
        {
            if (amount <= 0 || IsDead || IsInvulnerable) return false;

            // 先扣护盾，护盾耗尽后再扣血
            int healthDamage = amount;
            if (_shield > 0)
            {
                int absorbed = Mathf.Min(_shield, healthDamage);
                _shield -= absorbed;
                healthDamage -= absorbed;
            }

            _lastDamageTime = Time.time;
            _nextShieldRegenTime = Time.time + shieldRegenDelay;
            _invulnerableUntil = Time.time + invulnerableDuration;

            Damaged?.Invoke();

            if (healthDamage > 0)
            {
                _health -= healthDamage;
                AudioService.Play("chop", 0.75f);

                if (_health <= 0)
                {
                    _health = 0;
                    Die();
                }
            }
            else
            {
                AudioService.Play("metalClick", 0.65f);
            }

            return true;
        }

        public void Heal(int amount)
        {
            if (amount <= 0 || IsDead) return;

            _health = Mathf.Clamp(_health + amount, 0, _maxHealth);
        }

        /// <summary>获得一段额外无敌时间（进房护盾等自动效果）。</summary>
        public void GrantInvulnerability(float seconds)
        {
            if (seconds <= 0f || IsDead) return;

            _invulnerableUntil = Mathf.Max(_invulnerableUntil, Time.time + seconds);
        }

        /// <summary>
        /// 主动献祭生命（秘密祭坛等）：只在不会致死时扣除，且不触发无敌帧。
        /// </summary>
        public bool TrySacrifice(int amount)
        {
            if (amount <= 0 || IsDead || _health <= amount) return false;

            _health -= amount;
            AudioService.Play("chop", 0.75f);
            return true;
        }

        private void Die()
        {
            if (_rb != null) _rb.linearVelocity = Vector2.zero;
            if (_controller != null) _controller.enabled = false;
            if (_combat != null) _combat.enabled = false;
            if (_renderer != null) _renderer.color = _baseColor;

            Died?.Invoke();
        }

        /// <summary>重开一局：恢复满血并重新启用操作。</summary>
        public void ResetForNewRun()
        {
            _maxHealth = _stats != null ? _stats.MaxHealth : maxHealth;
            _health = _maxHealth;
            _shield = Mathf.Max(0, maxShield);
            _lastDamageTime = -999f;
            _nextShieldRegenTime = 0f;
            _invulnerableUntil = Time.time + 1f;

            if (_controller != null) _controller.enabled = true;
            if (_combat != null) _combat.enabled = true;

            if (_renderer != null) _renderer.color = _baseColor;
        }
    }
}
