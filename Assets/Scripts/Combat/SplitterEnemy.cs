using UnityEngine;

namespace Roguelike.Combat
{
    /// <summary>
    /// 分裂僵尸：死亡时在原地分裂成两只小僵尸。
    /// 小僵尸同样计入房间存活数，必须全部清掉才会开门。
    /// </summary>
    public sealed class SplitterEnemy : Enemy
    {
        [Header("分裂")]
        [SerializeField] private Enemy minionPrefab;
        [SerializeField] private int minionCount = 2;
        [SerializeField] private float splitOffset = 0.55f;
        [SerializeField] private float minionHealth = 1f;
        [SerializeField] private float minionSpeed = 2.8f;
        [SerializeField] private float minionScale = 0.8f;

        private bool _split;

        protected override void Die()
        {
            if (!_split)
            {
                _split = true;
                Split();
            }

            base.Die();
        }

        private void Split()
        {
            if (minionPrefab == null) return;

            int count = Mathf.Max(1, minionCount);
            for (int i = 0; i < count; i++)
            {
                float angle = 360f / count * i + 90f;
                Vector2 offset = new Vector2(
                    Mathf.Cos(angle * Mathf.Deg2Rad),
                    Mathf.Sin(angle * Mathf.Deg2Rad)) * splitOffset;

                Vector2 position = (Vector2)transform.position + offset;

                Enemy minion = Instantiate(minionPrefab, position, Quaternion.identity, transform.parent);
                minion.name = "TinyZombie_Split";
                minion.Initialize(minionHealth, minionSpeed, minionScale, Color.white, false, _flow);
                if (_flow != null) minion.SetFloorDamageBonus(_flow.EnemyDamageBonus);
                minion.ActivateCombat();

                if (_flow != null) _flow.RegisterEnemy(minion);
            }
        }
    }
}
