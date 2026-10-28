using UnityEditor;
using UnityEngine;

namespace Roguelike.EditorTools
{
    /// <summary>
    /// Danaida 16×16 怪物预览帧入口。
    /// 当前保留为未使用测试资源；目录中的帧来自公开预览页，
    /// 完整包下载后可整组替换。
    /// </summary>
    public static class DanaidaSprites
    {
        public const string Root = "Assets/Art/ThirdParty/DanaidaMonsters/";

        public static Sprite Load(string relativePath) =>
            AssetDatabase.LoadAssetAtPath<Sprite>(Root + relativePath);

        public static Sprite[] Frames(string prefix, int count)
        {
            var frames = new Sprite[count];
            for (int i = 0; i < count; i++)
                frames[i] = Load($"{prefix}_{i}.png");
            return frames;
        }

        public static Sprite RedImp => Load("red_imp_0.png");
        public static Sprite[] RedImpFrames => Frames("red_imp", 4);

        public static Sprite GreenSlime => Load("green_slime_0.png");
        public static Sprite[] GreenSlimeFrames => Frames("green_slime", 3);

        public static Sprite PurpleGhost => Load("purple_ghost_0.png");
        public static Sprite[] PurpleGhostFrames => Frames("purple_ghost", 4);

        public static Sprite TealSpider => Load("teal_spider_0.png");
        public static Sprite[] TealSpiderFrames => Frames("teal_spider", 4);

        public static Sprite BrownCyclops => Load("brown_cyclops_0.png");
        public static Sprite[] BrownCyclopsFrames => Frames("brown_cyclops", 4);

        public static Sprite PurpleDemon => Load("purple_demon_0.png");
        public static Sprite[] PurpleDemonFrames => Frames("purple_demon", 6);
    }
}
