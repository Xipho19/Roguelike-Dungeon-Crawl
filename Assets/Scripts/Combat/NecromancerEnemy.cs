using System.Collections.Generic;
using Roguelike.Audio;
using UnityEngine;

namespace Roguelike.Combat
{
    /// <summary>
    /// 亡灵法师：与玩家保持中远距离，蓄力后召唤骷髅。
    /// 召唤物会登记到房间的存活敌人列表，必须一并清掉才能开门。
    /// </summary>
    public sealed class NecromancerEnemy : Enemy
    {
        [Header("召唤")]
        [SerializeField] private Enemy minionPrefab;
        [SerializeField] private float preferredDistance = 5f;
        [SerializeField] private float summonInterval = 5f;
        [SerializeField] private float summonWindup = 0.6f;
        [SerializeField] private int maxMinions = 3;
        [SerializeField] private float minionHealth = 1f;
        [SerializeField] private float minionSpeed = 2.4f;
        [SerializeField] private float minionScale = 0.85f;

        [Tooltip("整场战斗的敌人上限，避免召唤物无限堆积")]
        [SerializeField] private int totalEnemyCap = 10;

        private readonly List<Enemy> _minions = new List<Enemy>();
        private SpriteRenderer _renderer;
        private float _nextSummonTime;
        private bool _summoning;
        private float _summonUntil;

        protected override void Start()
        {
            base.Start();

            _renderer = GetComponentInChildren<SpriteRenderer>();
            if (_renderer != null) _baseColor = _renderer.color;

            _nextSummonTime = Time.time + 1.2f;
        }

        protected override void FixedUpdate()
        {
            if (IsDead || _target == null || _rb == null) return;

            if (_summoning)
            {
                _rb.linearVelocity = Vector2.zero;

                if (Time.time >= _summonUntil)
                {
                    _summoning = false;
                    RestoreColor();
                    SummonMinion();
                    _nextSummonTime = Time.time + summonInterval;
                }

                return;
            }

            Vector2 toTarget = (Vector2)_target.position - _rb.position;
            float distance = toTarget.magnitude;
            Vector2 direction = distance > 0.0001f ? toTarget / distance : Vector2.zero;

            // 太远靠近、太近后退、距离合适则绕行
            Vector2 movement;
            if (distance > preferredDistance + 0.8f) movement = direction;
            else if (distance < preferredDistance - 0.8f) movement = -direction;
            else movement = new Vector2(-direction.y, direction.x);

            Vector2 steered = GetSteeredDirection(movement.normalized);
            _rb.linearVelocity = steered * moveSpeed;

            if (Time.time >= _nextSummonTime && CountLivingMinions() < maxMinions)
                BeginSummon();
        }

        private void BeginSummon()
        {
            if (minionPrefab == null || _flow == null) return;
            if (_flow.AliveEnemyCount >= totalEnemyCap) return;

            _summoning = true;
            _summonUntil = Time.time + summonWindup;
            _rb.linearVelocity = Vector2.zero;

            if (_renderer != null) _renderer.color = new Color(0.75f, 0.45f, 1f, 1f);
            AudioService.Play("bookOpen", 0.45f);
        }

        private void SummonMinion()
        {
            if (minionPrefab == null || _rb == null) return;

            Vector2 offset = Random.insideUnitCircle.normalized * 0.9f;
            Vector2 position = _rb.position + offset;

            Enemy minion = Instantiate(minionPrefab, position, Quaternion.identity, transform.parent);
            minion.name = "Skeleton_Minion";
            minion.Initialize(minionHealth, minionSpeed, minionScale, Color.white, false, _flow);

            if (_flow != null) _flow.RegisterEnemy(minion);

            _minions.Add(minion);
        }

        private int CountLivingMinions()
        {
            int count = 0;

            for (int i = _minions.Count - 1; i >= 0; i--)
            {
                if (_minions[i] == null) _minions.RemoveAt(i);
                else count++;
            }

            return count;
        }

        private void RestoreColor()
        {
            if (_renderer != null) _renderer.color = _baseColor;
        }
    }
}
