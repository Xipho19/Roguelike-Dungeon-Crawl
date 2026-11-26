using UnityEngine;

namespace Roguelike.Visual
{
    /// <summary>
    /// 把本体的 sortingOrder 绑定到世界 Y：Y 越小越靠前。
    /// 挂在角色（玩家 / 敌人 / Boss）根节点上，SpriteAnimator 会自动补挂。
    ///
    /// 排序锚点默认取精灵包围盒的底边（脚底），而不是 Transform 原点：
    /// 大体型敌人（Boss）的原点在身体中心，若按中心排序，站在石像前方时
    /// 中心仍高于石像底座，反而会被画到石像后面。用脚底作为锚点，
    /// 「站在建筑前面就挡住建筑、站在后面就被建筑挡住」才对得上。
    /// </summary>
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class YSort : MonoBehaviour
    {
        [Tooltip("留空时优先取同物体上的 SpriteRenderer，其次取子物体（Boss 的 Visual）")]
        [SerializeField] private SpriteRenderer target;

        [Tooltip("同一角色存在多层时的排序偏移")]
        [SerializeField] private int orderOffset;

        [Tooltip("按精灵包围盒底边排序；关闭则按 Transform 原点排序")]
        [SerializeField] private bool anchorAtSpriteBottom = true;

        public int CurrentOrder => target != null ? target.sortingOrder : 0;

        /// <summary>由 SpriteAnimator 指定它实际驱动的渲染器，避免多渲染器时排错对象。</summary>
        public void SetTarget(SpriteRenderer renderer)
        {
            if (renderer != null) target = renderer;
        }

        private void Awake()
        {
            if (target == null) target = GetComponent<SpriteRenderer>();
            if (target == null) target = GetComponentInChildren<SpriteRenderer>(true);
        }

        private void LateUpdate()
        {
            if (target == null) return;
            target.sortingOrder = DepthSort.OrderFor(SortAnchorY(), orderOffset);
        }

        private float SortAnchorY()
        {
            if (!anchorAtSpriteBottom || target.sprite == null || !target.enabled)
                return transform.position.y;
            return Mathf.Min(transform.position.y, target.bounds.min.y);
        }
    }
}
