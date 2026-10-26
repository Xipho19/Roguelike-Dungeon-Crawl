using System.Collections.Generic;
using Roguelike.Audio;
using Roguelike.Combat;
using Roguelike.Core;
using Roguelike.Dungeon;
using Roguelike.Items;
using UnityEngine;

namespace Roguelike.Player
{
    /// <summary>
    /// 自动触发道具的运行时结算：
    ///   - 吸血鬼獠牙：累计击杀回血
    ///   - 尸爆菌：击杀敌人时产生范围爆炸
    ///   - 荆棘之心：受到伤害时反击周围敌人
    ///
    /// 击杀事件统一来自 DungeonFlowController.EnemyKilled，由 NotifyEnemyDied 触发一次。
    /// </summary>
    public sealed class PlayerItemEffects : MonoBehaviour
    {
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private PlayerHealth health;
        [SerializeField] private PlayerRoomTracker tracker;
        [SerializeField] private DungeonFlowController flow;
        [SerializeField] private RunManager runManager;
        [SerializeField] private CombatDebugHud hud;

        [Header("特效")]
        [SerializeField] private Sprite explosionSprite;

        private int _killCount;
        private bool _deathExplosionActive;

        private void Awake()
        {
            if (inventory == null) inventory = GetComponent<PlayerInventory>();
            if (health == null) health = GetComponent<PlayerHealth>();
            if (tracker == null) tracker = GetComponent<PlayerRoomTracker>();
            if (flow == null) flow = FindFirstObjectByType<DungeonFlowController>();
            if (runManager == null) runManager = FindFirstObjectByType<RunManager>();
            if (hud == null) hud = FindFirstObjectByType<CombatDebugHud>();
        }

        private void OnEnable()
        {
            if (flow != null)
            {
                flow.EnemyKilled += HandleEnemyKilled;
                flow.RoomCleared += HandleRoomCleared;
            }

            if (health != null) health.Damaged += HandlePlayerDamaged;
            if (tracker != null) tracker.RoomChanged += HandleRoomChanged;
            if (inventory != null) inventory.Cleared += ResetKillCount;
            if (runManager != null) runManager.RunRestarted += ResetKillCount;
        }

        private void OnDisable()
        {
            if (flow != null)
            {
                flow.EnemyKilled -= HandleEnemyKilled;
                flow.RoomCleared -= HandleRoomCleared;
            }

            if (health != null) health.Damaged -= HandlePlayerDamaged;
            if (tracker != null) tracker.RoomChanged -= HandleRoomChanged;
            if (inventory != null) inventory.Cleared -= ResetKillCount;
            if (runManager != null) runManager.RunRestarted -= ResetKillCount;
        }

        private void HandleEnemyKilled(Enemy enemy)
        {
            if (inventory == null || health == null || health.IsDead) return;

            HandleKillHeal();
            HandleDeathExplosion(enemy);
        }

        /// <summary>余烬圣餐：每清空一个房间回复生命。</summary>
        private void HandleRoomCleared(RoomView room)
        {
            if (inventory == null || health == null || health.IsDead) return;
            if (health.Health >= health.MaxHealth) return;

            ItemDefinition meal = inventory.FindSynergyItem(ItemSynergy.RoomClearHeal);
            if (meal == null) return;

            int amount = Mathf.Max(1, Mathf.RoundToInt(meal.effectPower));
            health.Heal(amount);
            if (hud != null) hud.ShowToast($"{meal.itemName}：回复 {amount} 点生命", 1.8f);
        }

        /// <summary>圣光帷幕：进入新房间时获得短暂无敌。</summary>
        private void HandleRoomChanged(RoomView room)
        {
            if (inventory == null || health == null || health.IsDead) return;

            ItemDefinition veil = inventory.FindSynergyItem(ItemSynergy.RoomEntryShield);
            if (veil == null) return;

            health.GrantInvulnerability(Mathf.Max(0.1f, veil.effectPower));
        }

        private void HandleKillHeal()
        {
            ItemDefinition fang = inventory.FindSynergyItem(ItemSynergy.KillHeal);
            if (fang == null)
            {
                _killCount = 0;
                return;
            }

            _killCount++;
            if (_killCount < Mathf.Max(1, fang.triggerCount)) return;

            _killCount = 0;

            if (health.Health >= health.MaxHealth) return;

            int amount = Mathf.Max(1, Mathf.RoundToInt(fang.effectPower));
            health.Heal(amount);

            if (hud != null) hud.ShowToast($"{fang.itemName}：回复 {amount} 点生命", 1.8f);
        }

        private void HandleDeathExplosion(Enemy killed)
        {
            if (_deathExplosionActive || killed == null || inventory == null) return;

            ItemDefinition bomb = inventory.FindSynergyItem(ItemSynergy.DeathExplosion);
            if (bomb == null) return;

            RoomView room = killed.Room;
            if (room == null) return;

            _deathExplosionActive = true;
            try
            {
                Vector2 center = killed.transform.position;
                DamageEnemiesAround(center, room, bomb.effectRadius, bomb.effectPower, killed);
                ExplosionFlash.Spawn(center, explosionSprite, bomb.effectRadius);
                AudioService.Play("explosion", 0.45f);
            }
            finally
            {
                _deathExplosionActive = false;
            }
        }

        private void HandlePlayerDamaged()
        {
            if (inventory == null || health == null || health.IsDead) return;

            ItemDefinition thorns = inventory.FindSynergyItem(ItemSynergy.Thorns);
            if (thorns == null) return;

            RoomView room = tracker != null ? tracker.CurrentRoom : null;
            if (room == null) return;

            Vector2 center = transform.position;
            DamageEnemiesAround(center, room, thorns.effectRadius, thorns.effectPower, null);
            ExplosionFlash.Spawn(center, explosionSprite, thorns.effectRadius);
            AudioService.Play("explosion", 0.4f);
        }

        private static void DamageEnemiesAround(
            Vector2 center,
            RoomView room,
            float radius,
            float damage,
            Enemy ignore)
        {
            if (room == null || radius <= 0f || damage <= 0f) return;

            Collider2D[] overlaps = Physics2D.OverlapCircleAll(center, radius);
            var damaged = new HashSet<Enemy>();

            for (int i = 0; i < overlaps.Length; i++)
            {
                Collider2D collider = overlaps[i];
                if (collider == null) continue;

                Enemy enemy = collider.GetComponentInParent<Enemy>();
                if (enemy == null || enemy.IsDead || enemy == ignore) continue;
                if (enemy.Room != room) continue;
                if (!damaged.Add(enemy)) continue;

                enemy.TakeDamage(damage);
            }
        }

        public void ResetKillCount()
        {
            _killCount = 0;
        }
    }
}
