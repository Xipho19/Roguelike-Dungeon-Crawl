using UnityEngine;

namespace Roguelike.Combat
{
    /// <summary>
    /// 一个 Boss 的外观数据：方向化行走帧、名称、体型缩放。
    /// 资源放在 Resources/BossVisuals，由 BossEnemy 按楼层枚举加载。
    /// </summary>
    [CreateAssetMenu(menuName = "Roguelike/Boss Visual", fileName = "BossVisual_New")]
    public sealed class BossVisualDefinition : ScriptableObject
    {
        [Header("展示")]
        public string displayName;
        public string subtitle;

        [Tooltip("相对基础体型的缩放；最终层会再乘最终 Boss 倍率")]
        [Min(0.1f)] public float visualScale = 1f;

        [Header("向下")]
        public Sprite[] idleDown;
        public Sprite[] runDown;

        [Header("侧向（默认朝右，向左由 SpriteAnimator 翻转）")]
        public Sprite[] idleSide;
        public Sprite[] runSide;

        [Header("向上")]
        public Sprite[] idleUp;
        public Sprite[] runUp;

        [Header("可选 UI")]
        public Sprite portrait;
        public Sprite hudIcon;
    }
}
