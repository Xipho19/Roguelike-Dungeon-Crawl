using System;
using UnityEngine;

namespace Roguelike.Dungeon
{
    /// <summary>
    /// 房间图生成参数。只描述"生成什么"，不含任何状态，Editor / Runtime 共用。
    /// </summary>
    [Serializable]
    public class DungeonConfig
    {
        [Header("生成网格")]
        [Tooltip("网格宽度（以房间为单位）")]
        [Range(3, 32)] public int width = 9;

        [Tooltip("网格高度（以房间为单位）")]
        [Range(3, 32)] public int height = 9;

        [Header("主路径")]
        [Tooltip("起点到 Boss 的房间数（含起点与 Boss）")]
        [Range(2, 40)] public int mainPathLength = 7;

        [Tooltip("延续上一个方向的概率，越高走廊越直")]
        [Range(0f, 1f)] public float straightBias = 0.62f;

        [Tooltip("禁止房间堆成 2×2 的实心块，地图更像走廊而不是一坨")]
        public bool avoidDenseBlocks = true;

        [Header("分支")]
        [Tooltip("主路径之外额外生长的分支数量（尝试次数上限）")]
        [Range(0, 20)] public int branchCount = 5;

        [Range(1, 6)] public int branchLengthMin = 1;

        [Range(1, 6)] public int branchLengthMax = 3;

        [Header("特殊房间配额")]
        [Range(0, 4)] public int treasureCount = 1;

        [Range(0, 4)] public int shopCount = 1;

        [Range(0, 4)] public int secretCount = 1;

        /// <summary>把互相冲突的参数夹到合法范围，保证生成器不会因为手填数据崩溃。</summary>
        public void Clamp()
        {
            width = Mathf.Clamp(width, 3, 32);
            height = Mathf.Clamp(height, 3, 32);

            int cellCount = width * height;
            mainPathLength = Mathf.Clamp(mainPathLength, 2, Mathf.Min(cellCount, 40));

            branchLengthMin = Mathf.Clamp(branchLengthMin, 1, 6);
            branchLengthMax = Mathf.Clamp(branchLengthMax, branchLengthMin, 6);
            branchCount = Mathf.Clamp(branchCount, 0, 20);

            straightBias = Mathf.Clamp01(straightBias);

            treasureCount = Mathf.Clamp(treasureCount, 0, 4);
            shopCount = Mathf.Clamp(shopCount, 0, 4);
            secretCount = Mathf.Clamp(secretCount, 0, 4);
        }

        public DungeonConfig Clone() => (DungeonConfig)MemberwiseClone();
    }
}
