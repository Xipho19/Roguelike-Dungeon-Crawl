using UnityEngine;

namespace Roguelike.Visual
{
    /// <summary>
    /// 统一的深度排序换算：按世界 Y 计算 sortingOrder，越靠下（Y 越小）越靠前。
    ///
    /// 角色、石像、可破坏道具共用同一套公式，才能实现「走到后面被遮住、走到前面挡住它」。
    /// 注意：YSort 对角色取精灵包围盒的底边（脚底）作为 Y 锚点，而不是 Transform 原点。
    /// Boss 这类大体型精灵的原点在身体中心，按中心排序会导致站在石像前方却被石像遮挡。
    /// 排序带：
    ///   - 地板 / 墙体 / 门板 / 警示：-10 ~ 5
    ///   - 场景装饰（旗帜、贴墙杂物）：1 ~ 2
    ///   - 地面实体（角色、石像、木箱、宝箱）：10000 ~ 20000（按 Y 换算）
    ///   - 弹幕：30000
    ///   - 打击特效：31000
    /// </summary>
    public static class DepthSort
    {
        /// <summary>地面实体的排序基准；Y 越小排序值越大。</summary>
        public const int GroundBase = 20000;

        /// <summary>每世界单位对应的排序值，100 足够区分 0.01 单位的站位差。</summary>
        public const float GroundScale = 100f;

        /// <summary>飞行中的弹幕，始终在角色与石像之上。</summary>
        public const int Projectile = 30000;

        /// <summary>刀光、爆炸闪光等打击特效，压在弹幕之上。</summary>
        public const int Effect = 31000;

        /// <summary>按世界 Y 取排序值；offset 用于同一物体拆成多层时的微调。</summary>
        public static int OrderFor(float worldY, int offset = 0)
        {
            return GroundBase + Mathf.RoundToInt(-worldY * GroundScale) + offset;
        }
    }
}
