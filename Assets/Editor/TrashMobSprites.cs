using UnityEditor;
using UnityEngine;

namespace Roguelike.EditorTools
{
    /// <summary>
    /// 16×16 Fantasy RPG Trash Mobs 的六只测试怪物帧。
    /// 作者 Emcee Flesher，许可 OGA-BY 3.0。
    /// </summary>
    public static class TrashMobSprites
    {
        public const string Root = "Assets/Art/ThirdParty/TrashMobs/";

        public static Sprite Load(string relativePath) =>
            AssetDatabase.LoadAssetAtPath<Sprite>(Root + relativePath);

        public static Sprite[] Frames(string prefix)
        {
            // 精灵表同一列的四行是四个朝向；这里每个怪物只取一个朝向的 2 帧。
            var frames = new Sprite[2];
            for (int i = 0; i < frames.Length; i++)
                frames[i] = Load($"{prefix}_{i}.png");
            return frames;
        }

        public static Sprite GreenSlime => Load("green_slime_0.png");
        public static Sprite[] GreenSlimeFrames => Frames("green_slime");

        public static Sprite Snake => Load("snake_0.png");
        public static Sprite[] SnakeFrames => Frames("snake");

        public static Sprite BlueBat => Load("blue_bat_0.png");
        public static Sprite[] BlueBatFrames => Frames("blue_bat");

        public static Sprite Mushroom => Load("mushroom_0.png");
        public static Sprite[] MushroomFrames => Frames("mushroom");

        public static Sprite Bee => Load("bee_0.png");
        public static Sprite[] BeeFrames => Frames("bee");

        public static Sprite Flower => Load("flower_0.png");
        public static Sprite[] FlowerFrames => Frames("flower");
    }
}
