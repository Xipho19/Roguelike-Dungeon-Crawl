using System;
using System.Collections.Generic;
using Roguelike.Audio;
using Roguelike.Combat;
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

        [Header("连续撞击保护")]
        [Tooltip("短时间内受到多少次有效撞击后进入穿敌无敌状态")]
        [SerializeField, Min(1)] private int contactHitsToEscape = 3;

        [Tooltip("从第一次撞击开始，必须在多少秒内累计到阈值")]
        [SerializeField, Min(0.5f)] private float contactHitWindow = 3f;

        [Tooltip("穿敌无敌状态的持续时间")]
        [SerializeField, Min(0.5f)] private float escapeDuration = 3f;

        private int _health;
        private int _maxHealth;
        private int _shield;
        private float _lastDamageTime = -999f;
        private float _nextShieldRegenTime;
        private float _invulnerableUntil;
        private int _contactHits;
        private float _contactWindowStart = -999f;
        private bool _escapeActive;
        private float _escapeUntil;
        private float _nextEscapeRefresh;
        private readonly List<Collider2D> _playerColliders = new List<Collider2D>();
        private readonly HashSet<Collider2D> _ignoredEnemyColliders = new HashSet<Collider2D>();
        private Rigidbody2D _rb;
        private PlayerStats _stats;
        private PlayerController _controller;
        private PlayerCombat _combat;
        private PlayerInventory _inventory;
        private SpriteRenderer _renderer;
        private Color _baseColor;

        public int Health => _health;
        public int MaxHealth => _maxHealth;
        public int Shield => _shield;
        public int MaxShield => maxShield;
        public bool IsInvulnerable => Time.time < _invulnerableUntil;
        public bool IsDead => _health <= 0;
        public bool IsEscapeActive => _escapeActive;
        public int ContactHits => _contactHits;

        public event Action Died;

        /// <summary>连续撞击达到阈值、进入穿敌无敌状态时触发，供 HUD 提示。</summary>
        public event Action EscapeModeStarted;

        /// <summary>受到有效伤害时触发（无敌帧内不触发），供相机震动等反馈使用。</summary>
        public event Action Damaged;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _stats = GetComponent<PlayerStats>();
            _controller = GetComponent<PlayerController>();
            _combat = GetComponent<PlayerCombat>();
            _inventory = GetComponent<PlayerInventory>();
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

        private void OnDisable()
        {
            EndEscape();
        }

        private void Update()
        {
            UpdateEscapeMode();
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

            // 守护之壳：只要还有护盾，伤害就全部由护盾承担，不会溢出扣血。
            bool shieldGuard = _shield > 0
                && _inventory != null
                && _inventory.HasSynergy(ItemSynergy.ShieldGuard);

            // 记录受伤瞬间是否有护盾，用于播放盾牌撞击声。
            bool shieldHit = _shield > 0;

            // 先扣护盾，护盾耗尽后再扣血
            int healthDamage = amount;
            if (_shield > 0)
            {
                if (shieldGuard)
                {
                    _shield = Mathf.Max(0, _shield - amount);
                    healthDamage = 0;
                }
                else
                {
                    int absorbed = Mathf.Min(_shield, healthDamage);
                    _shield -= absorbed;
                    healthDamage -= absorbed;
                }
            }

            _lastDamageTime = Time.time;
            _nextShieldRegenTime = Time.time + shieldRegenDelay;
            _invulnerableUntil = Time.time + invulnerableDuration;

            Damaged?.Invoke();

            // 有护盾参与承伤时播放盾牌撞击声；如果护盾被击穿并扣血，再叠加玩家受伤声。
            if (shieldHit)
                AudioService.Play("shield_hit", 1f, 0.03f);

            if (healthDamage > 0)
            {
                _health -= healthDamage;
                AudioService.Play("player_hurt", 1f, 0.03f);

                if (_health <= 0)
                {
                    _health = 0;
                    Die();
                }
            }

            return true;
        }

        /// <summary>
        /// 敌人接触伤害入口。连续有效撞击达到阈值后进入穿敌无敌状态，
        /// 避免玩家被堵在墙角持续吃接触伤害。
        /// </summary>
        public bool TryContactDamage(int amount)
        {
            if (amount <= 0 || IsDead || IsInvulnerable) return false;

            bool accepted = TryDamage(amount);
            if (!accepted || IsDead) return accepted;

            // 以第一次撞击为起点计算 3 秒窗口；超时后本次撞击重新作为第一次。
            if (_contactHits <= 0 || Time.time - _contactWindowStart > contactHitWindow)
            {
                _contactHits = 0;
                _contactWindowStart = Time.time;
            }

            _contactHits++;

            if (_contactHits >= Mathf.Max(1, contactHitsToEscape))
                BeginEscape();

            return true;
        }

        private void BeginEscape()
        {
            _contactHits = 0;
            _contactWindowStart = -999f;
            _escapeActive = true;

            float duration = Mathf.Max(0.5f, escapeDuration);
            _escapeUntil = Time.time + duration;
            _nextEscapeRefresh = 0f;

            GrantInvulnerability(duration);
            RefreshPlayerColliders();
            RefreshEnemyCollisionIgnore();

            EscapeModeStarted?.Invoke();
            Debug.Log("[Player] 连续撞击达到阈值，进入穿敌无敌状态");
        }

        private void UpdateEscapeMode()
        {
            if (!_escapeActive) return;

            if (Time.time >= _escapeUntil)
            {
                EndEscape();
                return;
            }

            if (Time.time < _nextEscapeRefresh) return;

            _nextEscapeRefresh = Time.time + 0.1f;
            RefreshEnemyCollisionIgnore();
        }

        private void RefreshPlayerColliders()
        {
            _playerColliders.Clear();
            Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null) _playerColliders.Add(colliders[i]);
            }
        }

        private void RefreshEnemyCollisionIgnore()
        {
            if (_playerColliders.Count == 0) return;

            IReadOnlyList<Enemy> enemies = Enemy.AllActiveEnemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy enemy = enemies[i];
                if (enemy == null || enemy.IsDead) continue;

                Collider2D[] enemyColliders = enemy.GetComponentsInChildren<Collider2D>(true);
                for (int j = 0; j < enemyColliders.Length; j++)
                {
                    Collider2D enemyCollider = enemyColliders[j];
                    if (enemyCollider == null || !_ignoredEnemyColliders.Add(enemyCollider))
                        continue;

                    for (int k = 0; k < _playerColliders.Count; k++)
                    {
                        Collider2D playerCollider = _playerColliders[k];
                        if (playerCollider != null)
                            Physics2D.IgnoreCollision(playerCollider, enemyCollider, true);
                    }
                }
            }
        }

        private void EndEscape()
        {
            if (!_escapeActive && _ignoredEnemyColliders.Count == 0) return;

            _escapeActive = false;
            _escapeUntil = 0f;
            _nextEscapeRefresh = 0f;

            foreach (Collider2D enemyCollider in _ignoredEnemyColliders)
            {
                if (enemyCollider == null) continue;

                for (int i = 0; i < _playerColliders.Count; i++)
                {
                    Collider2D playerCollider = _playerColliders[i];
                    if (playerCollider != null)
                        Physics2D.IgnoreCollision(playerCollider, enemyCollider, false);
                }
            }

            _ignoredEnemyColliders.Clear();
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

        /// <summary>恢复护盾；满护盾或死亡时返回 false。</summary>
        public bool AddShield(int amount)
        {
            if (amount <= 0 || IsDead) return false;

            int before = _shield;
            _shield = Mathf.Clamp(_shield + amount, 0, maxShield);
            return _shield > before;
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
            EndEscape();

            if (_rb != null) _rb.linearVelocity = Vector2.zero;
            if (_controller != null) _controller.enabled = false;
            if (_combat != null) _combat.enabled = false;
            if (_renderer != null) _renderer.color = _baseColor;

            Died?.Invoke();
        }

        /// <summary>重开一局：恢复满血并重新启用操作。</summary>
        public void ResetForNewRun()
        {
            EndEscape();
            _contactHits = 0;
            _contactWindowStart = -999f;

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
