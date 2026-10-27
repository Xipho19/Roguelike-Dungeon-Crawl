using UnityEngine;

namespace Roguelike.Combat
{
    /// <summary>
    /// 死亡时留下毒雾的小怪，用于墓穴层的小僵尸。
    /// 毒雾沿用 EnemyHazard，死亡位置生成一次，不参与房间存活计数。
    /// </summary>
    public sealed class DeathCloudEnemy : Enemy
    {
        [Header("死亡毒雾")]
        [SerializeField] private EnemyHazard hazardPrefab;
        [SerializeField] private int cloudDamage = 1;
        [SerializeField] private float cloudRadius = 1.2f;
        [SerializeField] private float cloudLifetime = 2f;
        [SerializeField] private float cloudTickInterval = 0.8f;
        [SerializeField] private Color cloudTint = new Color(0.45f, 1f, 0.35f, 0.55f);

        private bool _spawned;

        protected override void Die()
        {
            if (!_spawned)
            {
                _spawned = true;
                SpawnCloud();
            }

            base.Die();
        }

        private void SpawnCloud()
        {
            if (hazardPrefab == null) return;

            EnemyHazard cloud = Instantiate(
                hazardPrefab,
                transform.position,
                Quaternion.identity,
                transform.parent);

            cloud.Initialize(
                ScaleAttackDamage(cloudDamage),
                cloudTickInterval,
                cloudLifetime,
                cloudRadius,
                1f,
                0f,
                cloudTint);
        }
    }
}
